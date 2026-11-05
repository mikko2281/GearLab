"""Independent OCCT reimport and STL topology audits for generated examples."""
import json, math, sys, time, traceback, zipfile, struct
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'kernel'))
import cad_kernel as k
from transmission_assembly import generate

def run(folder):
    rows=[]
    for kind in range(5,13):
        started=time.perf_counter(); row={'Kind':kind}
        try:
            spec=json.loads((folder/f'type-{kind}.assembly.json').read_text(encoding='utf-8-sig'))
            step=folder/f'type-{kind}.step'
            generate({'Spec':spec,'Folder':str(folder/f'native-{kind}'),'Path':str(step)},k)
            reader=k.STEPControl_Reader()
            assert reader.ReadFile(str(step))==k.IFSelect_RetDone
            reader.TransferRoots();shape=reader.OneShape();solids=k.shapes(shape,k.TopAbs_SOLID)
            assert len(solids)==len(spec),(len(solids),len(spec))
            assert all(k.BRepCheck_Analyzer(s).IsValid() and k.props(s)[0]>1e-6 for s in solids)
            row.update(Ok=True,Solids=len(solids),Volume_mm3=sum(k.props(s)[0] for s in solids))
            count=0
            with zipfile.ZipFile(folder/f'type-{kind}-stl.zip') as archive:
                assert archive.testzip() is None
                for name in archive.namelist():
                    if not name.endswith('.stl'):continue
                    data=archive.read(name);n=struct.unpack_from('<I',data,80)[0];assert len(data)==84+50*n
                    edges={};volume=0
                    for i in range(n):
                        tri=struct.unpack_from('<12fH',data,84+50*i)
                        a,b,c=[tuple(round(x,4) for x in tri[j:j+3]) for j in (3,6,9)]
                        volume+=(a[0]*(b[1]*c[2]-b[2]*c[1])+a[1]*(b[2]*c[0]-b[0]*c[2])+a[2]*(b[0]*c[1]-b[1]*c[0]))/6
                        for x,y in ((a,b),(b,c),(c,a)):
                            if x==y:continue
                            key=tuple(sorted((x,y)));edges[key]=edges.get(key,0)+1
                    assert volume>0,(name,'volume',volume)
                    assert all(v==2 for v in edges.values()),(name,'non-manifold edges',sum(v!=2 for v in edges.values()))
                    count+=1
            row['STL_parts']=count
        except Exception as ex:row.update(Ok=False,Error=str(ex),Trace=traceback.format_exc())
        row['Seconds']=time.perf_counter()-started;rows.append(row)
        (folder/'native-results.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
        print(kind,row['Ok'],round(row['Seconds'],2),row.get('Error',''),flush=True)
    return 0 if all(r['Ok'] for r in rows) else 1
if __name__=='__main__':sys.exit(run(Path(sys.argv[1]) if len(sys.argv)>1 else root/'docs/verification/6.0'))
