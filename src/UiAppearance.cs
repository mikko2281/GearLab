using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace GearLab {
 public static class UiPalette {
  public static readonly string[] Keys={"Window","Panel","Header","Field","Text","Muted","Border","Accent","Selection","Viewport","Grid","AxisX","AxisY","AxisZ","Success","Warning","Error"};
  static readonly string[] Dark={"#252525","#303030","#202020","#454545","#E5E5E5","#B0B0B0","#191919","#E8923C","#66503C","#393939","#555555","#D65B60","#78B653","#639BDD","#78BF8B","#EAC16B","#EF8585"};
  static readonly string[] Light={"#E2E2E2","#F0F0F0","#D0D0D0","#FFFFFF","#242424","#595959","#B0B0B0","#A95300","#E8CBA9","#E6E6E6","#C5C5C5","#B93B43","#438624","#316CAD","#23653A","#855E00","#AF2936"};
  public static Color Get(string key){return Get(AppSettings.Current,key);}
  static readonly Dictionary<string,int> Index=Keys.Select((key,i)=>new{key,i}).ToDictionary(x=>x.key,x=>x.i);
  static readonly Color[] DarkColors=Dark.Select(ColorTranslator.FromHtml).ToArray(),LightColors=Light.Select(ColorTranslator.FromHtml).ToArray();
  static readonly System.Collections.Concurrent.ConcurrentDictionary<string,Color> Parsed=new System.Collections.Concurrent.ConcurrentDictionary<string,Color>(StringComparer.OrdinalIgnoreCase);
  public static Color Get(AppSettings settings,string key){int i;if(!Index.TryGetValue(key,out i))throw new ArgumentException(key);string hex;if(settings!=null&&settings.Colors!=null&&settings.Colors.TryGetValue(key,out hex)){Color color;if(Parsed.TryGetValue(hex,out color))return color;color=ColorTranslator.FromHtml(hex);if(Parsed.Count>=512)Parsed.Clear();Parsed.TryAdd(hex,color);return color;}return (settings!=null&&settings.Theme==GearTheme.Светлая?LightColors:DarkColors)[i];}
  public static System.Windows.Media.Color Wpf(string key){var c=Get(key);return System.Windows.Media.Color.FromRgb(c.R,c.G,c.B);}
 }
 public static class UiAppearance {
  sealed class State {public bool Busy;public Color Fore,Back;}
  static readonly ConditionalWeakTable<Control,State> states=new ConditionalWeakTable<Control,State>();
  public static void Apply(Control root){if(root==null||root.IsDisposed)return;ApplyCore(root);root.Invalidate(true);}
  static void ApplyCore(Control root){if(root==null||root.IsDisposed)return;State st;bool fresh=!states.TryGetValue(root,out st);if(fresh){st=new State{Fore=root.ForeColor,Back=root.BackColor};states.Add(root,st);root.ControlAdded+=(s,e)=>Apply(e.Control);root.HandleCreated+=(s,e)=>{Style(root,st);I18n.Apply(root);root.Invalidate();};root.ForeColorChanged+=(s,e)=>{if(!st.Busy){st.Fore=root.ForeColor;Style(root,st);}};root.BackColorChanged+=(s,e)=>{if(!st.Busy){st.Back=root.BackColor;Style(root,st);}};}
   Style(root,st);I18n.Apply(root);foreach(Control c in root.Controls)ApplyCore(c);
  }
  static void Style(Control c,State st){if(st.Busy||c.IsDisposed)return;st.Busy=true;try{
   NativeAppearance.Apply(c);bool field=c is TextBoxBase||c is NumericUpDown||c is ComboBox||c is ListBox||c is TreeView;
   c.BackColor=UiPalette.Get(field?"Field":c is ToolStrip?"Header":c is Form||c is TabControl?"Window":"Panel");
   string semantic=st.Fore==Color.Firebrick||st.Fore==Color.Red||st.Fore==Color.Maroon||st.Back==Color.MistyRose?"Error":st.Back==Color.FromArgb(255,247,221)||st.Back==Color.FromArgb(255,244,216)?"Warning":st.Back==Color.FromArgb(234,244,242)||st.Fore==Color.FromArgb(33,109,92)?"Success":"Text";
   c.ForeColor=UiPalette.Get(semantic);
   var button=c as Button;if(button!=null){button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderColor=UiPalette.Get("Border");button.FlatAppearance.MouseOverBackColor=UiPalette.Get("Selection");button.FlatAppearance.MouseDownBackColor=UiPalette.Get("Accent");button.UseVisualStyleBackColor=false;button.BackColor=UiPalette.Get("Field");}
   var text=c as TextBoxBase;if(text!=null)text.BorderStyle=text.Parent is NumericUpDown?BorderStyle.None:BorderStyle.FixedSingle;
   var box=c as ComboBox;if(box!=null){box.FlatStyle=FlatStyle.Flat;if(box.DrawMode!=DrawMode.OwnerDrawFixed){box.DrawMode=DrawMode.OwnerDrawFixed;box.DrawItem+=(sender,e)=>{if(e.Index<0||e.Index>=box.Items.Count)return;var rect=e.Bounds;bool selected=(e.State&DrawItemState.Selected)!=0;using(var b=new SolidBrush(UiPalette.Get(selected?"Selection":"Field")))e.Graphics.FillRectangle(b,rect);TextRenderer.DrawText(e.Graphics,I18n.T(box.GetItemText(box.Items[e.Index])),box.Font,rect,UiPalette.Get(box.Enabled?"Text":"Muted"),TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);};}}
   var tree=c as TreeView;if(tree!=null){tree.BorderStyle=BorderStyle.None;tree.LineColor=UiPalette.Get("Border");if(tree.DrawMode!=TreeViewDrawMode.OwnerDrawText){tree.DrawMode=TreeViewDrawMode.OwnerDrawText;tree.DrawNode+=(sender,e)=>{bool selected=(e.State&TreeNodeStates.Selected)!=0;var rect=new Rectangle(e.Bounds.X,e.Bounds.Y,Math.Max(e.Bounds.Width,tree.ClientSize.Width-e.Bounds.X),e.Bounds.Height);using(var b=new SolidBrush(UiPalette.Get(selected?"Selection":"Field")))e.Graphics.FillRectangle(b,rect);TextRenderer.DrawText(e.Graphics,I18n.T(e.Node.Text),tree.Font,rect,UiPalette.Get("Text"),TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis);};}}
   var panel=c as SplitContainer;if(panel!=null){panel.BackColor=UiPalette.Get("Border");panel.SplitterWidth=4;}
   var tab=c as TabControl;if(tab!=null){tab.DrawMode=TabDrawMode.OwnerDrawFixed;object handler;if(!tabHandlers.TryGetValue(tab,out handler)){tabHandlers.Add(tab,new object());tab.DrawItem+=(s,e)=>{var t=(TabControl)s;var r=t.GetTabRect(e.Index);using(var b=new SolidBrush(UiPalette.Get(e.Index==t.SelectedIndex?"Panel":"Header")))e.Graphics.FillRectangle(b,r);TextRenderer.DrawText(e.Graphics,t.TabPages[e.Index].Text,t.Font,r,UiPalette.Get("Text"),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(e.Index==t.SelectedIndex)using(var p=new Pen(UiPalette.Get("Accent"),2))e.Graphics.DrawLine(p,r.Left+2,r.Bottom-2,r.Right-2,r.Bottom-2);};}}
   var strip=c as ToolStrip;if(strip!=null){strip.Renderer=new ToolStripProfessionalRenderer(new ThemeTable()){RoundedEdges=false};foreach(ToolStripItem item in strip.Items)StyleItem(item);}
   var g=c as DataGridView;if(g!=null){g.BackgroundColor=UiPalette.Get("Panel");g.BorderStyle=BorderStyle.None;g.GridColor=UiPalette.Get("Border");g.EnableHeadersVisualStyles=false;g.DefaultCellStyle.BackColor=UiPalette.Get("Panel");g.DefaultCellStyle.ForeColor=UiPalette.Get("Text");g.DefaultCellStyle.SelectionBackColor=UiPalette.Get("Selection");g.DefaultCellStyle.SelectionForeColor=UiPalette.Get("Text");g.AlternatingRowsDefaultCellStyle.BackColor=UiPalette.Get("Field");g.ColumnHeadersDefaultCellStyle.BackColor=UiPalette.Get("Header");g.ColumnHeadersDefaultCellStyle.ForeColor=UiPalette.Get("Text");g.RowHeadersDefaultCellStyle.BackColor=UiPalette.Get("Header");g.RowHeadersDefaultCellStyle.ForeColor=UiPalette.Get("Text");}
   var pg=c as PropertyGrid;if(pg!=null){pg.ViewBackColor=UiPalette.Get("Field");pg.ViewForeColor=UiPalette.Get("Text");pg.HelpBackColor=UiPalette.Get("Panel");pg.HelpForeColor=UiPalette.Get("Text");pg.CategoryForeColor=UiPalette.Get("Text");pg.LineColor=UiPalette.Get("Border");pg.CommandsBackColor=UiPalette.Get("Panel");pg.CommandsForeColor=UiPalette.Get("Text");}
   var host=c as ElementHost;if(host!=null&&host.Child!=null)ApplyWpf(host.Child);
   var web=c as RobotWebViewport;if(web!=null)web.ApplyAppearance();
  }finally{st.Busy=false;}}
  static readonly ConditionalWeakTable<TabControl,object> tabHandlers=new ConditionalWeakTable<TabControl,object>();
  static void StyleItem(ToolStripItem i){i.BackColor=UiPalette.Get("Header");i.ForeColor=UiPalette.Get("Text");I18n.ApplyItem(i);var d=i as ToolStripDropDownItem;if(d!=null){d.DropDown.BackColor=UiPalette.Get("Header");d.DropDown.Renderer=new ToolStripProfessionalRenderer(new ThemeTable());foreach(ToolStripItem child in d.DropDownItems)StyleItem(child);}}
  public static void ApplyWpf(System.Windows.DependencyObject node){var grid=node as System.Windows.Controls.Grid;if(grid!=null&&grid.Parent==null)grid.Background=new System.Windows.Media.SolidColorBrush(UiPalette.Wpf("Viewport"));var text=node as System.Windows.Controls.TextBlock;if(text!=null){text.Foreground=new System.Windows.Media.SolidColorBrush(UiPalette.Wpf("Muted"));I18n.ApplyWpf(text);}var button=node as System.Windows.Controls.Button;if(button!=null){button.Background=new System.Windows.Media.SolidColorBrush(UiPalette.Wpf("Field"));button.Foreground=new System.Windows.Media.SolidColorBrush(UiPalette.Wpf("Text"));button.BorderBrush=new System.Windows.Media.SolidColorBrush(UiPalette.Wpf("Border"));}for(int i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(node);i++)ApplyWpf(System.Windows.Media.VisualTreeHelper.GetChild(node,i));}
  sealed class ThemeTable:ProfessionalColorTable {
   public override Color ToolStripGradientBegin{get{return UiPalette.Get("Header");}}public override Color ToolStripGradientMiddle{get{return ToolStripGradientBegin;}}public override Color ToolStripGradientEnd{get{return ToolStripGradientBegin;}}
   public override Color MenuStripGradientBegin{get{return ToolStripGradientBegin;}}public override Color MenuStripGradientEnd{get{return ToolStripGradientBegin;}}public override Color ToolStripDropDownBackground{get{return ToolStripGradientBegin;}}
   public override Color ImageMarginGradientBegin{get{return ToolStripGradientBegin;}}public override Color ImageMarginGradientMiddle{get{return ToolStripGradientBegin;}}public override Color ImageMarginGradientEnd{get{return ToolStripGradientBegin;}}
   public override Color MenuItemSelected{get{return UiPalette.Get("Selection");}}public override Color MenuItemBorder{get{return UiPalette.Get("Accent");}}public override Color MenuBorder{get{return UiPalette.Get("Border");}}
   public override Color ButtonSelectedGradientBegin{get{return MenuItemSelected;}}public override Color ButtonSelectedGradientMiddle{get{return MenuItemSelected;}}public override Color ButtonSelectedGradientEnd{get{return MenuItemSelected;}}
   public override Color MenuItemSelectedGradientBegin{get{return MenuItemSelected;}}public override Color MenuItemSelectedGradientEnd{get{return MenuItemSelected;}}public override Color SeparatorDark{get{return UiPalette.Get("Border");}}
  }
 }
 public static class UiDrawing {
  public static SolidBrush Muted=new SolidBrush(UiPalette.Get("Muted")),Error=new SolidBrush(UiPalette.Get("Error"));
  static UiDrawing(){AppSettings.Changed+=()=>{Muted.Color=UiPalette.Get("Muted");Error.Color=UiPalette.Get("Error");};}
 }
}
