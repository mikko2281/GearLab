using System;using System.Windows.Forms;
namespace GearLab {internal static class UiTestLoop672 {
 internal static int Run(Func<int> action){int result=1;using(var driver=new Form{ShowInTaskbar=false,Opacity=0,Width=1,Height=1}){driver.Shown+=(s,e)=>driver.BeginInvoke(new Action(()=>{try{result=action();}finally{driver.Close();}}));Application.Run(driver);}return result;}
}}
