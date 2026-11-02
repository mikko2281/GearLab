import * as THREE from 'three';
import {TransformControls} from 'three/addons/TransformControls.js';

// The gizmo edits a joint coordinate, never an individual casing mesh.
export class JointController {
  constructor(scene,camera,canvas,orbit,send,onBegin){
    this.send=send;this.orbit=orbit;this.onBegin=onBegin;this.canvas=canvas;
    this.proxy=new THREE.Object3D();scene.add(this.proxy);
    this.control=new TransformControls(camera,canvas);this.control.setSpace('local');
    this.control.showX=false;this.control.showY=false;this.control.showZ=true;this.control.setSize(.9);
    scene.add(this.control.getHelper());this.control.enabled=false;
    this.badge=document.createElement('div');this.badge.id='joint-badge';document.body.append(this.badge);
    this.control.addEventListener('dragging-changed',event=>{orbit.enabled=!event.value;});
    this.control.addEventListener('mouseDown',()=>this.begin());
    this.control.addEventListener('objectChange',()=>this.change());
    this.control.addEventListener('mouseUp',()=>this.finish());
    addEventListener('keydown',event=>{if(event.key==='Escape'&&this.drag){event.preventDefault();this.cancel();}});
    addEventListener('blur',()=>{if(this.drag)this.finish();});
  }
  receive(data){
    if(this.drag&&data.id!==this.drag.id)this.cancel();
    this.data=data;
    if(!data.enabled){if(this.drag)this.cancel();this.control.detach();this.control.enabled=false;this.badge.hidden=true;return;}
    this.badge.hidden=false;this.badge.textContent=`${data.label}: ${Number(data.value).toFixed(2)} ${data.linear?'мм':'°'} · ${data.minimum} … ${data.maximum}`;
    if(this.drag)return;
    const axis=new THREE.Vector3(...data.axis).normalize();this.proxy.position.fromArray(data.origin);
    this.proxy.quaternion.setFromUnitVectors(new THREE.Vector3(0,0,1),axis);
    this.control.setMode(data.linear?'translate':'rotate');this.control.setSpace('local');
    this.control.showX=false;this.control.showY=false;this.control.showZ=true;
    this.control.enabled=true;this.control.attach(this.proxy);
  }
  begin(){
    if(!this.data?.enabled||this.drag)return;
    this.onBegin();this.drag={id:this.data.id,value:this.data.value,position:this.proxy.position.clone(),inverse:this.proxy.quaternion.clone().invert(),axis:new THREE.Vector3(...this.data.axis).normalize(),lastAngle:0,totalAngle:0,lastSent:0,pending:this.data.value};
    this.send({type:'jointBegin',id:this.drag.id});
  }
  change(){
    const d=this.drag;if(!d)return;
    let delta;
    if(this.data.linear)delta=this.proxy.position.clone().sub(d.position).dot(d.axis);
    else{
      const q=d.inverse.clone().multiply(this.proxy.quaternion);let angle=2*Math.atan2(q.z,q.w);
      let increment=angle-d.lastAngle;while(increment>Math.PI)increment-=2*Math.PI;while(increment< -Math.PI)increment+=2*Math.PI;
      d.totalAngle+=increment;d.lastAngle=angle;delta=THREE.MathUtils.radToDeg(d.totalAngle);
    }
    const step=Math.max(.01,this.data.step||1);
    d.pending=THREE.MathUtils.clamp(d.value+Math.round(delta/step)*step,this.data.minimum,this.data.maximum);
    const now=performance.now();if(now-d.lastSent>=40){d.lastSent=now;this.send({type:'jointPose',id:d.id,value:d.pending});}
  }
  finish(){const d=this.drag;if(!d)return;this.drag=null;this.send({type:'jointPose',id:d.id,value:d.pending});this.send({type:'jointEnd',id:d.id});this.orbit.enabled=true;}
  cancel(){const d=this.drag;if(!d)return;this.drag=null;this.control?.reset();this.control?.pointerUp(null);this.send({type:'jointCancel',id:d.id});this.orbit.enabled=true;}
  info(){return{enabled:this.control.enabled,attached:!!this.control.object,mode:this.control.mode,id:this.data?.id||null,dragging:!!this.drag,value:this.data?.value??null,origin:this.proxy.position.toArray(),axis:this.data?.axis||null};}
}
