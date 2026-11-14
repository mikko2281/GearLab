import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import * as THREE from '../web/vendor/three.module.js';

// Execute the production intro/animate functions with actual Three.js matrices.
// Only the GPU draw call is replaced; no duplicated easing implementation.
const source=fs.readFileSync(new URL('../web/scene.js',import.meta.url),'utf8');
const block=source.slice(source.indexOf('let introEnabled='),source.indexOf('const renderScheduler='));
const parts=new Map();
for(let i=0;i<3;i++){
 const geometry=new THREE.BoxGeometry(3,5,7);geometry.computeBoundingSphere();
 const mesh=new THREE.Mesh(geometry,new THREE.MeshBasicMaterial());
 mesh.matrixAutoUpdate=false;mesh.matrix.makeRotationY(.4);mesh.matrix.setPosition(10+i*20,30,40);mesh.userData.introOrder=i;
 parts.set('p'+i,{mesh,label:'Part '+i});
}
const originals=[...parts.values()].map(p=>p.mesh.matrix.clone());
const context=vm.createContext({THREE,parts,width:300,performance:{now:()=>1000},orbit:{update:()=>false},bones:new Map(),camera:{},scene:{},renderer:{render(){}},performanceFrames:0});
vm.runInContext(block,context);
vm.runInContext('introEnabled=true;introArmed=true;introCount=3;introStart=1000;for(const p of parts.values())introParts.add(p.mesh);introApply(1000);',context);
assert.ok([...parts.values()].some((p,i)=>!p.mesh.matrix.equals(originals[i])),'intro must run');
vm.runInContext('introRestore()',context);
for(const [i,p]of [...parts.values()].entries())assert.ok(p.mesh.matrix.equals(originals[i]),'canonical pose restored after a frame');
const start=source.indexOf('function animate(time)');
const end=source.indexOf('requestRender();send',start);
vm.runInContext(source.slice(start,end),context);
context.renderer.render=()=>{throw new Error('GPU draw failed');};
assert.throws(()=>vm.runInContext('animate(1000)',context),/GPU draw failed/);
for(const [i,p]of [...parts.values()].entries())assert.ok(p.mesh.matrix.equals(originals[i]),'failed draw must restore canonical pose');
const messageStart=source.indexOf('function message(data)');
const messageEnd=source.indexOf('window.chrome',messageStart);
vm.runInContext(source.slice(messageStart,messageEnd),context);
vm.runInContext("message({type:'clip',axis:1,coordinate:0})",context);
assert.equal(vm.runInContext('introParts.size',context),0,'section mode stops intro');
for(const [i,p]of [...parts.values()].entries())assert.ok(p.mesh.matrix.equals(originals[i]),'section mode uses canonical geometry');
console.log('PASS active WebGL intro: real matrices, frame restoration, draw-failure restoration and section mode');
