"""Independent STEP reread for all families and exact bearing interference audit."""
import json,sys,time,traceback
from pathlib import Path
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'kernel'))
import cad_kernel as k
from transmission_assembly import generate
folder=Path(sys.argv[1]).resolve()
rows=[]
for kind in range(13):
    started=time.perf_counter()
    row={'Kind':kind}
    try:
        spec=json.loads((folder/f'family-{kind}.assembly.json').read_text(encoding='utf-8-sig'))
        step=folder/f'family-{kind}.step'
        generate({'Spec':spec,'Folder':str(folder/f'native-{kind}'),'Path':str(step)},k)
        reader=k.STEPControl_Reader()
        assert reader.ReadFile(str(step))==k.IFSelect_RetDone
        reader.TransferRoots()
        solids=k.shapes(reader.OneShape(),k.TopAbs_SOLID)
        assert len(solids)==len(spec),(len(solids),len(spec))
        assert all(k.BRepCheck_Analyzer(s).IsValid() and k.props(s)[0]>1e-6 for s in solids)
        row.update(Passed=True,Solids=len(solids),Volume=sum(k.props(s)[0] for s in solids))
        if kind==4:
            bearing=[q for q in spec if q['Id'].startswith('wave_output_bearing')]
            audit=generate({'Spec':bearing,'Folder':str(folder/'bearing-contact'),'Mode':'assembly_audit'},k)['Audit']
            assert audit['Complete'] and not audit['Pairs'],audit
            row['BearingContactAudit']=audit
    except Exception as ex:
        row.update(Passed=False,Error=str(ex),Trace=traceback.format_exc())
    row['Seconds']=time.perf_counter()-started
    rows.append(row)
    (folder/'native62-results.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
    print('FAMILY',kind,row['Passed'],round(row['Seconds'],2),row.get('Error',''),flush=True)
sys.exit(0 if all(q['Passed'] for q in rows) else 1)
