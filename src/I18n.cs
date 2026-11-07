using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace GearLab {
 public static partial class I18n {
  sealed class TextState {public string Raw;public bool Busy;}
  static readonly ConditionalWeakTable<object,TextState> texts=new ConditionalWeakTable<object,TextState>();
  public static bool English{get{return AppSettings.Current!=null&&AppSettings.Current.Language==GearLanguage.English;}}
  static readonly System.Collections.Concurrent.ConcurrentDictionary<string,string> Translated=new System.Collections.Concurrent.ConcurrentDictionary<string,string>(StringComparer.Ordinal);
  public static string T(string value){if(string.IsNullOrEmpty(value)||!English)return value;string translated;if(Catalog.TryGetValue(value,out translated))return translated;bool cyrillic=false;foreach(char c in value)if(c>='\u0400'&&c<='\u052f'){cyrillic=true;break;}if(!cyrillic)return value;if(Translated.TryGetValue(value,out translated))return translated;translated=TranslateParts(value);if(value.Length<=2048){if(Translated.Count>=1024)Translated.Clear();Translated.TryAdd(value,translated);}return translated;}
  public static string Raw(Control c){TextState s;return texts.TryGetValue(c,out s)?s.Raw:c.Text;}
  public static void Apply(Control c){var combo=c as ComboBox;if(combo!=null){TextState s;if(!texts.TryGetValue(c,out s)){texts.Add(c,new TextState());combo.FormattingEnabled=true;combo.Format+=(sender,e)=>e.Value=T(Convert.ToString(e.ListItem));}combo.Refresh();return;}
   var list=c as ListBox;if(list!=null){TextState s;if(!texts.TryGetValue(c,out s)){texts.Add(c,new TextState());list.FormattingEnabled=true;list.Format+=(sender,e)=>e.Value=T(Convert.ToString(e.ListItem));}list.Refresh();return;}
   var grid=c as DataGridView;if(grid!=null){TextState s;if(!texts.TryGetValue(c,out s)){texts.Add(c,new TextState());grid.CellFormatting+=(sender,e)=>{if(e.Value is string)e.Value=T((string)e.Value);};}foreach(DataGridViewColumn col in grid.Columns){var headerState=texts.GetValue(col,k=>new TextState{Raw=col.HeaderText});col.HeaderText=T(headerState.Raw);}return;}
   var pg=c as PropertyGrid;if(pg!=null){TextState s;if(!texts.TryGetValue(c,out s)){texts.Add(c,new TextState());pg.SelectedObjectsChanged+=(sender,e)=>LocalizeProperties(pg);}LocalizeProperties(pg);return;}
   var textBox=c as TextBoxBase;if((textBox!=null&&(!textBox.ReadOnly||c.Name=="UserPath"))||c is NumericUpDown||c is TreeView||c is ToolStrip)return;
   TextState state;if(!texts.TryGetValue(c,out state)){state=new TextState{Raw=c.Text};texts.Add(c,state);c.TextChanged+=(sender,e)=>{if(!state.Busy){string next=c.Text,displayed=T(state.Raw);state.Raw=English&&!string.IsNullOrEmpty(displayed)&&next.Length>displayed.Length&&next.StartsWith(displayed,StringComparison.Ordinal)?state.Raw+next.Substring(displayed.Length):next;SetText(c,state);}};}SetText(c,state);
  }
  static void SetText(Control c,TextState s){if(s.Busy)return;s.Busy=true;try{string t=T(s.Raw);if(c.Text!=t)c.Text=t;}finally{s.Busy=false;}}
  public static void ApplyItem(ToolStripItem item){TextState s;if(!texts.TryGetValue(item,out s)){s=new TextState{Raw=item.Text};texts.Add(item,s);item.TextChanged+=(sender,e)=>{if(!s.Busy){s.Raw=item.Text;SetItem(item,s);}};}SetItem(item,s);}
  static void SetItem(ToolStripItem i,TextState s){s.Busy=true;try{i.Text=T(s.Raw);}finally{s.Busy=false;}}
  public static void ApplyWpf(System.Windows.Controls.TextBlock text){TextState s;if(!texts.TryGetValue(text,out s)){s=new TextState{Raw=text.Text};texts.Add(text,s);DependencyPropertyDescriptor.FromProperty(System.Windows.Controls.TextBlock.TextProperty,typeof(System.Windows.Controls.TextBlock)).AddValueChanged(text,(sender,e)=>{if(!s.Busy){s.Raw=text.Text;s.Busy=true;try{text.Text=T(s.Raw);}finally{s.Busy=false;}}});}s.Busy=true;try{text.Text=T(s.Raw);}finally{s.Busy=false;}}
  static readonly ConditionalWeakTable<object,object> propertyObjects=new ConditionalWeakTable<object,object>();
  static void LocalizeProperties(PropertyGrid grid){foreach(var obj in grid.SelectedObjects){if(propertyObjects.TryGetValue(obj,out dummy))continue;propertyObjects.Add(obj,new object());TypeDescriptor.AddProvider(new LocalizedProvider(TypeDescriptor.GetProvider(obj)),obj);}grid.Refresh();}static object dummy;
  sealed class LocalizedProvider:TypeDescriptionProvider {public LocalizedProvider(TypeDescriptionProvider p):base(p){}public override ICustomTypeDescriptor GetTypeDescriptor(Type type,object obj){return new LocalizedType(base.GetTypeDescriptor(type,obj));}}
  sealed class LocalizedType:CustomTypeDescriptor {public LocalizedType(ICustomTypeDescriptor p):base(p){}public override PropertyDescriptorCollection GetProperties(){return Wrap(base.GetProperties());}public override PropertyDescriptorCollection GetProperties(Attribute[] a){return Wrap(base.GetProperties(a));}static PropertyDescriptorCollection Wrap(PropertyDescriptorCollection p){return new PropertyDescriptorCollection(p.Cast<PropertyDescriptor>().Select(x=>(PropertyDescriptor)new LocalizedProperty(x)).ToArray());}}
  sealed class LocalizedProperty:PropertyDescriptor {readonly PropertyDescriptor p;public LocalizedProperty(PropertyDescriptor p):base(p){this.p=p;}public override string DisplayName{get{return T(p.DisplayName);}}public override string Category{get{return T(p.Category);}}public override string Description{get{return T(p.Description);}}public override Type ComponentType{get{return p.ComponentType;}}public override Type PropertyType{get{return p.PropertyType;}}public override bool IsReadOnly{get{return p.IsReadOnly;}}public override bool CanResetValue(object c){return p.CanResetValue(c);}public override object GetValue(object c){return p.GetValue(c);}public override void SetValue(object c,object v){p.SetValue(c,v);}public override void ResetValue(object c){p.ResetValue(c);}public override bool ShouldSerializeValue(object c){return p.ShouldSerializeValue(c);}public override TypeConverter Converter{get{return p.PropertyType.IsEnum?new LocalizedEnum(p.PropertyType):p.Converter;}}}
  sealed class LocalizedEnum:EnumConverter {readonly Type type;public LocalizedEnum(Type t):base(t){type=t;}public override object ConvertTo(ITypeDescriptorContext c,System.Globalization.CultureInfo culture,object value,Type target){if(target==typeof(string))return T(Convert.ToString(value));return base.ConvertTo(c,culture,value,target);}public override object ConvertFrom(ITypeDescriptorContext c,System.Globalization.CultureInfo culture,object value){if(value is string)foreach(var v in Enum.GetValues(type))if(T(v.ToString())==(string)value)return v;return base.ConvertFrom(c,culture,value);}}
  // No editable text or project identifiers are rewritten by the localization layer.

 }
 public static class UiMessage {
  public static DialogResult Show(string text){return MessageBox.Show(I18n.T(text));}
  public static DialogResult Show(IWin32Window owner,string text){return MessageBox.Show(owner,I18n.T(text));}
  public static DialogResult Show(string text,string title){return MessageBox.Show(I18n.T(text),I18n.T(title));}
  public static DialogResult Show(IWin32Window owner,string text,string title){return MessageBox.Show(owner,I18n.T(text),I18n.T(title));}
  public static DialogResult Show(IWin32Window owner,string text,string title,MessageBoxButtons buttons){return MessageBox.Show(owner,I18n.T(text),I18n.T(title),buttons);}
  public static DialogResult Show(IWin32Window owner,string text,string title,MessageBoxButtons buttons,MessageBoxIcon icon){return MessageBox.Show(owner,I18n.T(text),I18n.T(title),buttons,icon);}
  public static DialogResult Show(string text,string title,MessageBoxButtons buttons,MessageBoxIcon icon){return MessageBox.Show(I18n.T(text),I18n.T(title),buttons,icon);}
 }
}
