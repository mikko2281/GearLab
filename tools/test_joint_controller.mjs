// Exercise the exact shipped gizmo maths with the shipped Three.js classes.
import {SourceTextModule} from 'node:vm';
import {readFile,mkdir,writeFile} from 'node:fs/promises';
import {pathToFileURL,fileURLToPath} from 'node:url';
import path from 'node:path';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..'),cache=new Map();
async function load(file){file=path.resolve(file);if(cache.has(file))return cache.get(file);const module=new SourceTextModule(await readFile(file,'utf8'),{identifier:pathToFileURL(file).href});cache.set(file,module);await module.link((name,parent)=>load(name==='three'?path.join(root,'web/vendor/three.module.js'):name.startsWith('three/addons/')?path.join(root,'web/vendor/addons',name.slice('three/addons/'.length)):fileURLToPath(new URL(name,parent.identifier))));return module;}
const module=await load(path.join(root,'web/joint-controller.js'));await module.evaluate();
const THREE=cache.get(path.join(root,'web/vendor/three.module.js')).namespace,Controller=module.namespace.JointController,passed=[];
const check=(condition,label)=>{if(!condition)throw Error(label);passed.push(label);};
function fixture(linear=false,axis=[0,0,1]){const c=Object.create(Controller.prototype);c.proxy=new THREE.Object3D();c.proxy.quaternion.setFromUnitVectors(new THREE.Vector3(0,0,1),new THREE.Vector3(...axis));c.data={enabled:true,id:'j',value:0,minimum:-720,maximum:720,step:1,axis,linear};c.orbit={enabled:false};c.sent=[];c.send=e=>c.sent.push(e);c.onBegin=()=>{};return c;}
for(const axis of [[0,0,1],[0,1,0],[1,0,0]]){
 const c=fixture(false,axis);const start=c.proxy.quaternion.clone();c.begin();
 for(const angle of [45,179,181,270]){c.proxy.quaternion.copy(start).multiply(new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0,0,1),angle*Math.PI/180));c.change();check(c.drag.pending===angle,'local rotation/wrap '+axis+' / '+angle);}
 c.finish();check(c.sent.at(-1).type==='jointEnd'&&c.sent.at(-2).value===270,'release flushes last pose '+axis);
}
const negative=fixture();negative.begin();negative.proxy.quaternion.setFromAxisAngle(new THREE.Vector3(0,0,1),-30*Math.PI/180);negative.change();check(negative.drag.pending===-30,'negative rotation');negative.cancel();check(negative.sent.at(-1).type==='jointCancel'&&!negative.drag,'escape cancels transaction');
const limit=fixture();limit.data.minimum=-10;limit.data.maximum=20;limit.begin();limit.proxy.quaternion.setFromAxisAngle(new THREE.Vector3(0,0,1),30*Math.PI/180);limit.change();check(limit.drag.pending===20,'gizmo enforces upper joint limit');
for(const axis of [[0,0,1],[0,1,0],[1,0,0]]){const c=fixture(true,axis);c.begin();c.proxy.position.addScaledVector(new THREE.Vector3(...axis),12.4);c.change();check(c.drag.pending===12,'linear local-axis projection '+axis);c.finish();}
const offAxis=fixture(true);offAxis.begin();offAxis.proxy.position.x=100;offAxis.change();check(offAxis.drag.pending===0,'orthogonal displacement does not move prismatic coordinate');
const folder=path.resolve(process.argv[2]||path.join(root,'test-results/controls-js'));await mkdir(folder,{recursive:true});await writeFile(path.join(folder,'joint-controller-results.json'),JSON.stringify({passed},null,2));console.log('PASS',passed.length,'shipped gizmo math checks');
