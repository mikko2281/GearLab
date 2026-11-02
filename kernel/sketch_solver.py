"""Small numerical 2D constraint solver. Explicit residual and DOF reporting."""
import copy, math, re

def linear(a,b):
    a=[list(r)+[v] for r,v in zip(a,b)];n=len(b)
    for i in range(n):
        p=max(range(i,n),key=lambda k:abs(a[k][i]))
        if abs(a[p][i])<1e-15:continue
        a[i],a[p]=a[p],a[i];scale=a[i][i];a[i]=[v/scale for v in a[i]]
        for k in range(n):
            if k!=i:
                factor=a[k][i]
                if factor:a[k]=[v-factor*w for v,w in zip(a[k],a[i])]
    return [a[i][-1] for i in range(n)]
def rank(rows):
    a=[list(r) for r in rows];row=0
    for col in range(len(a[0]) if a else 0):
        p=max(range(row,len(a)),key=lambda k:abs(a[k][col]),default=None)
        if p is None or abs(a[p][col])<1e-6:continue
        a[row],a[p]=a[p],a[row];f=a[row][col];a[row]=[v/f for v in a[row]]
        for k in range(row+1,len(a)):
            f=a[k][col];a[k]=[v-f*w for v,w in zip(a[k],a[row])]
        row+=1
        if row==len(a):break
    return row
def solve(sketch,ev):
    result=copy.deepcopy(sketch)
    for index,e in enumerate(result.get('Entities',[])):e.setdefault('Id','entity_'+str(index))
    entities={e['Id']:e for e in result.get('Entities',[])}
    constraints=result.get('Constraints') or [];variables=[];keys={}
    driven={}
    for id,e in entities.items():
        for i,p in enumerate(e.get('Points') or []):
            for j,v in enumerate(p):keys[(id,i,j)]=len(variables);variables.append(float(v))
        if e['Kind']=='circle':
            keys[(id,'radius')]=len(variables);variables.append(ev(e.get('Radius',10)))
            expression=str(e.get('Radius','10'))
            if not re.fullmatch(r'[+\-]?(?:\d+(?:\.\d*)?|\.\d+)',expression.strip()):
                driven[id]=expression;constraints=constraints+[dict(Kind='radius',A=id,Value=expression)]
    if len(variables)>180:raise ValueError('В одном эскизе допускается до 90 точек. Разделите его на несколько эскизов.')
    def point(x,id,index=0):return [x[keys[(id,index,0)]],x[keys[(id,index,1)]]]
    def radius(x,id):return x[keys[(id,'radius')]]
    def vec(x,id):
        p,q=point(x,id),point(x,id,1);return [q[0]-p[0],q[1]-p[1]]
    def length(v):return max(1e-10,math.hypot(*v))
    def residual(x):
        out=[]
        for c in constraints:
            kind=c['Kind'];a=c['A'];b=c.get('B');pa=point(x,a,int(c.get('PointA',0)));pb=point(x,b,int(c.get('PointB',0))) if b else [0,0]
            va=vec(x,a) if entities[a]['Kind']!='circle' and len(entities[a].get('Points',[]))>1 else None
            vb=vec(x,b) if b and entities[b]['Kind']!='circle' and len(entities[b].get('Points',[]))>1 else None
            directional={'horizontal','vertical','parallel','perpendicular','angle','tangent'}
            if kind in directional:
                for vector in (va,vb):
                    if vector is not None and math.hypot(*vector)<1e-7:
                        raise ValueError('Направление отрезка не определено: длина меньше 0.0000001 мм. Удалите вырожденный отрезок или задайте разные точки.')
            if kind=='horizontal':out.append(va[1])
            elif kind=='vertical':out.append(va[0])
            elif kind=='coincident':out.extend([pa[0]-pb[0],pa[1]-pb[1]])
            elif kind=='concentric':out.extend([pa[0]-pb[0],pa[1]-pb[1]])
            elif kind=='distance':out.append(math.dist(pa,pb)-ev(c['Value']))
            elif kind=='length':out.append(length(va)-ev(c['Value']))
            elif kind=='radius':out.append(radius(x,a)-ev(c['Value']))
            elif kind=='parallel':out.append((va[0]*vb[1]-va[1]*vb[0])/max(length(va),length(vb)))
            elif kind=='perpendicular':out.append((va[0]*vb[0]+va[1]*vb[1])/max(length(va),length(vb)))
            elif kind=='equal':out.append((radius(x,a) if va is None else length(va))-(radius(x,b) if vb is None else length(vb)))
            elif kind=='angle':
                angle=math.atan2(va[0]*vb[1]-va[1]*vb[0],va[0]*vb[0]+va[1]*vb[1])-math.radians(ev(c['Value']));out.append(math.atan2(math.sin(angle),math.cos(angle))*10)
            elif kind=='tangent':
                if va is None and vb is None:out.append(math.dist(pa,pb)-radius(x,a)-radius(x,b))
                else:
                    line,center,rr=(va,[pb[0]-pa[0],pb[1]-pa[1]],radius(x,b)) if va is not None else (vb,[pa[0]-pb[0],pa[1]-pb[1]],radius(x,a))
                    out.append(abs(line[0]*center[1]-line[1]*center[0])/length(line)-rr)
            elif kind=='fixed':
                for i,p in enumerate(c.get('Points') or entities[a].get('Points',[])):
                    q=point(x,a,i);out.extend([q[0]-float(p[0]),q[1]-float(p[1])])
                if entities[a]['Kind']=='circle':out.append(radius(x,a)-ev(c.get('Value',entities[a].get('Radius',10))))
            else:raise ValueError('Неизвестная связь эскиза: '+kind)
        return out
    x=variables[:];lam=1e-5;iterations=0
    def jacobian(x,r):
        j=[[0.0]*len(x) for _ in r]
        for col in range(len(x)):
            trial=x[:];h=1e-5*max(1,abs(x[col]));trial[col]+=h;rr=residual(trial)
            for row in range(len(r)):j[row][col]=(rr[row]-r[row])/h
        return j
    for iterations in range(50):
        r=residual(x)
        if max(map(abs,r),default=0)<1e-6:break
        j=jacobian(x,r);n=len(x)
        a=[[sum(row[i]*row[k] for row in j)+(lam if i==k else 0) for k in range(n)] for i in range(n)]
        rhs=[-sum(j[row][i]*r[row] for row in range(len(r))) for i in range(n)];step=linear(a,rhs);candidate=[v+d for v,d in zip(x,step)]
        if sum(v*v for v in residual(candidate))<sum(v*v for v in r):x=candidate;lam=max(1e-10,lam*.25)
        else:lam=min(1e8,lam*10)
    r=residual(x);error=max(map(abs,r),default=0)
    if error>1e-4:raise ValueError('Связи эскиза противоречат друг другу или не сходятся. Остаток: '+format(error,'.4g')+' мм. Удалите конфликтующую связь.')
    for key,index in keys.items():
        if key[1]=='radius':
            if x[index]<=0:raise ValueError('Радиус должен оставаться положительным.')
            entities[key[0]]['Radius']=driven.get(key[0],format(x[index],'.12g'))
        else:entities[key[0]]['Points'][key[1]][key[2]]=x[index]
    return result,dict(DegreesOfFreedom=len(x)-rank(jacobian(x,r)),Error=error,Iterations=iterations)
