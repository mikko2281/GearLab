import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';

// Execute the shipped scene and scheduler. Only the browser/WebGL device is replaced.
let queue=[],webMessage,geometryBuilds=0;
const events=new Map();
class V {constructor(x=0,y=0,z=0){this.set(x,y,z);}set(x,y,z){this.x=x;this.y=y;this.z=z;return this;}normalize(){return this;}toArray(){return [this.x,this.y,this.z];}}
class Group {constructor(){this.children=[];}add(...items){this.children.push(...items);} }
class Camera {constructor(){this.up=new V();this.position=new V();this.zoom=1;}lookAt(){}updateProjectionMatrix(){} }
class Colors {constructor(){this.count=6;}setXYZ(){} }
class Geometry {constructor(){geometryBuilds++;this.attributes={color:new Colors()};}setAttribute(k,v){this.attributes[k]=v;}setIndex(v){this.index=v;}computeBoundingSphere(){} }
class Mesh {constructor(geometry,material){this.geometry=geometry;this.material=material;this.matrix={fromArray(){}};}}
class Material {constructor(){this.emissive={set(){}};} }
class Renderer {constructor(){this.domElement={addEventListener(){}};this.info={render:{triangles:1}};this.ratio=1;}setPixelRatio(v){this.ratio=v;}getPixelRatio(){return this.ratio;}setSize(w,h){this.domElement.width=Math.floor(w*this.ratio);this.domElement.height=Math.floor(h*this.ratio);}render(){} }
class Orbit {constructor(){this.target=new V();}addEventListener(){}update(){return false;} }
class Transform {constructor(){this.translationSnap=null;}setMode(){}setSpace(){}getHelper(){return {};}addEventListener(){}setTranslationSnap(v){this.translationSnap=v;} }
const attribute={color:new Colors()};
const three={Scene:class extends Group{},Color:class {constructor(){this.r=this.g=this.b=.5;}},WebGLRenderer:Renderer,OrthographicCamera:Camera,HemisphereLight:class{},DirectionalLight:class{constructor(){this.position=new V();}},Group,GridHelper:class{constructor(){this.rotation={};this.position={};this.geometry={attributes:attribute};}},AxesHelper:class{constructor(){this.geometry={attributes:attribute};}},Object3D:class{constructor(){this.position=new V();}},Vector2:V,Raycaster:class{},BufferGeometry:Geometry,Float32BufferAttribute:class{},MeshStandardMaterial:Material,Mesh,DoubleSide:2};
const elements=new Map();
function element(key){if(!elements.has(key))elements.set(key,{style:{},value:'all',classList:{toggle(){},remove(){}},replaceChildren(){}});return elements.get(key);}
const document={hidden:false,body:{prepend(){}},documentElement:{dataset:{},style:{setProperty(){}}},getElementById:element,querySelector:element,querySelectorAll(){return [];},addEventListener(name,fn){events.set('document:'+name,fn);}};
const context=vm.createContext({document,window:{chrome:{webview:{postMessage(){},addEventListener(name,fn){webMessage=fn;}}}},innerWidth:1200,innerHeight:800,devicePixelRatio:2,requestAnimationFrame:fn=>queue.push(fn),addEventListener:(name,fn)=>events.set(name,fn),console});
function synthetic(exports){return new vm.SyntheticModule(Object.keys(exports),function(){for(const [key,value]of Object.entries(exports))this.setExport(key,value);},{context});}
const scheduler=new vm.SourceTextModule(fs.readFileSync(new URL('../web/render-scheduler.js',import.meta.url),'utf8'),{context});
const modules={'./render-scheduler.js':scheduler,'three':synthetic(three),'three/addons/OrbitControls.js':synthetic({OrbitControls:Orbit}),'three/addons/TransformControls.js':synthetic({TransformControls:Transform}),'./joint-controller.js':synthetic({JointController:class{constructor(){this.control={addEventListener(){}};}info(){return {};}receive(){}}})};
const scene=new vm.SourceTextModule(fs.readFileSync(new URL('../web/scene.js',import.meta.url),'utf8'),{context});await scene.link(name=>modules[name]);await scene.evaluate();
const info=()=>context.window.gearlabSceneInfo();
function flush(){let count=0;while(queue.length){assert.ok(count++<20,'render scheduler settles');queue.shift()(0);}return count;}
function message(data){webMessage({data});}
flush();
message({type:'part',id:'test',positions:[0,0,0,10,0,0,0,10,0],normals:[0,0,1,0,0,1,0,0,1],indices:[0,1,2],matrix:Array(16).fill(0)});flush();
const builds=geometryBuilds,worldWidth=info().width;
message({type:'appearance',quality:0,snapEnabled:true,snapStep:2.5});assert.equal(flush(),1,'one redraw for quality change');
assert.equal(info().quality,0);assert.equal(info().pixelRatio,1);assert.equal(info().canvasWidth,1200);assert.equal(info().translationSnap,2.5);
message({type:'appearance',quality:1,snapEnabled:false,snapStep:2.5});flush();assert.equal(info().pixelRatio,1.5);assert.equal(info().canvasWidth,1800);assert.equal(info().translationSnap,null);
message({type:'appearance',quality:2,snapEnabled:true,snapStep:.25});flush();assert.equal(info().pixelRatio,2);assert.equal(info().canvasWidth,2400);assert.equal(info().translationSnap,.25);
assert.equal(geometryBuilds,builds,'quality/snap preserve geometry buffers');assert.equal(info().parts,1);assert.equal(info().width,worldWidth,'quality preserves world framing');
context.devicePixelRatio=1;context.innerWidth=640;context.innerHeight=360;events.get('resize')();flush();assert.equal(info().pixelRatio,1.5);assert.equal(info().canvasWidth,960);assert.equal(info().canvasHeight,540);assert.equal(info().width,worldWidth);
message({type:'appearance',quality:0,snapEnabled:false,snapStep:1});flush();assert.equal(info().pixelRatio,.75);assert.equal(info().canvasWidth,480);assert.equal(info().translationSnap,null);
const frames=info().renderFrames;assert.equal(queue.length,0,'idle view has no pending frames');assert.equal(flush(),0);assert.equal(info().renderFrames,frames,'idle view does not redraw');
console.log('PASS WebView preferences: real shipped message handler, quality/DPR/resize, snap, mesh reuse and idle rendering');
