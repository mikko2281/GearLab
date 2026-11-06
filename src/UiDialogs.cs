using System;using System.Collections.Generic;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace GearLab {
 public static class UiDialogs {
  public static DialogResult ShowLocalizedDialog(this Form form,IWin32Window owner){AppSettings.Apply(form);return form.ShowDialog(owner);}
  public static DialogResult ShowLocalizedDialog(this Form form){AppSettings.Apply(form);return form.ShowDialog();}
  public static DialogResult ShowLocalizedDialog(this CommonDialog dialog,IWin32Window owner){Prepare(dialog);return dialog.ShowDialog(owner);}
  public static DialogResult ShowLocalizedDialog(this CommonDialog dialog){Prepare(dialog);return dialog.ShowDialog();}
  static void Prepare(CommonDialog dialog){var file=dialog as FileDialog;if(file!=null){file.Title=I18n.T(file.Title);file.Filter=I18n.T(file.Filter);}var folder=dialog as FolderBrowserDialog;if(folder!=null)folder.Description=I18n.T(folder.Description);}
  sealed class TipState {public Dictionary<Control,string> Texts=new Dictionary<Control,string>();public Action Update;}
  static readonly ConditionalWeakTable<ToolTip,TipState> tips=new ConditionalWeakTable<ToolTip,TipState>();
  public static void SetLocalizedToolTip(this ToolTip tip,Control c,string text){TipState state;if(!tips.TryGetValue(tip,out state)){state=new TipState();tips.Add(tip,state);state.Update=()=>{foreach(var entry in state.Texts)if(!entry.Key.IsDisposed)tip.SetToolTip(entry.Key,I18n.T(entry.Value));};AppSettings.Changed+=state.Update;tip.Disposed+=(s,e)=>{AppSettings.Changed-=state.Update;state.Texts.Clear();};}state.Texts[c]=text;tip.SetToolTip(c,I18n.T(text));}
 }
}
