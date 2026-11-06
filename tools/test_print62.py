"""Check actual binary STL triangles: closed paired edges, positive volume and ZIP CRC."""
import json,struct,sys,zipfile
from pathlib import Path
rows=[]
for argument in sys.argv[1:]:
    archive=Path(argument)
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None
        count=0
        for name in z.namelist():
            if not name.endswith('.stl'):continue
            data=z.read(name);n=struct.unpack_from('<I',data,80)[0]
            assert len(data)==84+50*n,(archive,name,'length')
            edges={};volume=0
            for i in range(n):
                values=struct.unpack_from('<12fH',data,84+50*i)
                a,b,c=[tuple(round(v,4) for v in values[j:j+3]) for j in (3,6,9)]
                volume+=(a[0]*(b[1]*c[2]-b[2]*c[1])+a[1]*(b[2]*c[0]-b[0]*c[2])+a[2]*(b[0]*c[1]-b[1]*c[0]))/6
                for x,y in ((a,b),(b,c),(c,a)):
                    if x==y:continue
                    key=tuple(sorted((x,y)));edges[key]=edges.get(key,0)+1
            assert volume>0,(archive,name,'nonpositive volume')
            assert all(v==2 for v in edges.values()),(archive,name,'unpaired edges',sum(v!=2 for v in edges.values()))
            count+=1
        rows.append({'Archive':archive.name,'Passed':True,'Parts':count})
print(json.dumps(rows,indent=2))
