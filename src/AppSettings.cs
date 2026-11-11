using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Drawing;using System.ComponentModel;using System.Windows.Forms;using System.Windows.Forms.Integration;using System.Web.Script.Serialization;

namespace GearLab {

 public enum GearTheme {Светлая,Тёмная,Системная}

 public enum GearAccent {Синий,Фиолетовый,Бирюзовый,Зелёный,Оранжевый,Розовый}

 public enum GearStart {Редукторы,Робототехника,Ступени}

 public enum GearLanguage {Русский,English}
 public enum GearQuality {Быстрый,Обычный,Подробный}

 public sealed class AppSettings {

  public static AppSettings Current=Load();
  public static event Action Changed;
  public static int AppearanceRevision{get;private set;}public static int LanguageRevision{get;private set;}
  static bool SameColors(Dictionary<string,string> a,Dictionary<string,string> b){if(ReferenceEquals(a,b))return true;if(a==null||b==null||a.Count!=b.Count)return false;foreach(var pair in a){string value;if(!b.TryGetValue(pair.Key,out value)||!string.Equals(pair.Value,value,StringComparison.OrdinalIgnoreCase))return false;}return true;}

  public static void SetLanguage(GearLanguage language){if(Current.Language==language)return;var settings=Current.Clone();settings.Language=language;Commit(settings);}
  public static void SetAnimationSpeed(double speed){if(Math.Abs(Current.AnimationScale-speed)<1e-10)return;var settings=Current.Clone();settings.AnimationScale=speed;Commit(settings);}

  public int FormatVersion{get;set;}
  public GearLanguage Language{get;set;}
  public Dictionary<string,string> Colors{get;set;}
  public static string LoadWarning;
  public static void Commit(AppSettings draft){var saved=draft.Clone();saved.Validate();bool appearance=Current.Theme!=saved.Theme||Current.Accent!=saved.Accent||!SameColors(Current.Colors,saved.Colors),language=Current.Language!=saved.Language;saved.Save();Current=saved;if(appearance)AppearanceRevision++;if(language)LanguageRevision++;if(appearance||language)foreach(Form f in Application.OpenForms.Cast<Form>().Where(f=>f.TopLevel||f.Parent==null).ToArray())Apply(f);if(Changed!=null)Changed();}


  [Category("Интерфейс"),DisplayName("Тема")]public GearTheme Theme{get;set;}

  [Category("Интерфейс"),DisplayName("Акцентный цвет")]public GearAccent Accent{get;set;}

  [Category("Интерфейс"),DisplayName("Показывать подсказки")]public bool ShowHints{get;set;}

  [Category("Интерфейс"),DisplayName("Раздел при запуске")]public GearStart StartSection{get;set;}

  [Category("3D"),DisplayName("Качество предпросмотра")]public GearQuality Quality{get;set;}

  [Category("3D"),DisplayName("Чувствительность вращения камеры")]public double OrbitSensitivity{get;set;}

  [Category("Моделирование"),DisplayName("Привязка перемещения к сетке")]public bool SnapEnabled{get;set;}

  [Category("Моделирование"),DisplayName("Шаг сетки / перемещения, мм")]public double SnapStep{get;set;}

  [Category("Анимация"),DisplayName("Скорость показа по умолчанию ×")]public double AnimationScale{get;set;}

  [Category("Сохранение"),DisplayName("Автосохранение рабочих проектов")]public bool AutoSave{get;set;}

  [Category("Сохранение"),DisplayName("Интервал автосохранения, с")]public int AutoSaveSeconds{get;set;}

  public AppSettings(){FormatVersion=3;Theme=GearTheme.Тёмная;Accent=GearAccent.Синий;ShowHints=true;StartSection=GearStart.Редукторы;Language=GearLanguage.Русский;Colors=new Dictionary<string,string>();Quality=GearQuality.Обычный;OrbitSensitivity=9;SnapEnabled=true;SnapStep=1;AnimationScale=.1;AutoSave=true;AutoSaveSeconds=60;}

  public static string Folder{get{string isolated=Environment.GetEnvironmentVariable("GEARLAB_PROFILE_DIR");return string.IsNullOrWhiteSpace(isolated)?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GearLab"):Path.GetFullPath(isolated);}}

  public static string SettingsPath{get{return Path.Combine(Folder,"settings.json");}}

  public void Validate(){if(!Enum.IsDefined(typeof(GearLanguage),Language)||!Enum.IsDefined(typeof(GearTheme),Theme)||!Enum.IsDefined(typeof(GearAccent),Accent)||!Enum.IsDefined(typeof(GearStart),StartSection)||!Enum.IsDefined(typeof(GearQuality),Quality)||!MechanismCad.Finite(SnapStep)||SnapStep<.01||SnapStep>100||!MechanismCad.Finite(OrbitSensitivity)||OrbitSensitivity<1||OrbitSensitivity>30||!MechanismCad.Finite(AnimationScale)||AnimationScale<.01||AnimationScale>10||AutoSaveSeconds<15||AutoSaveSeconds>600)throw new Exception(I18n.T("Шаг 0,01–100 мм; чувствительность 1–30; анимация 0,01–10; автосохранение 15–600 с."));if(Colors==null)Colors=new Dictionary<string,string>();foreach(var pair in Colors)if(!UiPalette.Keys.Contains(pair.Key)||!System.Text.RegularExpressions.Regex.IsMatch(pair.Value??"","^#[0-9a-fA-F]{6}$"))throw new Exception(I18n.T("Недопустимый цвет: ")+pair.Key);}

  static AppSettings Load(){return LoadFrom(SettingsPath);}

  public static AppSettings LoadFrom(string path){try{if(File.Exists(path)){if(new FileInfo(path).Length>=65536)throw new IOException("Settings file is too large");string json=File.ReadAllText(path);var s=new JavaScriptSerializer().Deserialize<AppSettings>(json);if(s!=null&&!json.Contains("\"FormatVersion\"")){s.Theme=GearTheme.Тёмная;s.FormatVersion=2;}if(s==null)throw new IOException("Empty settings");if(s.FormatVersion<3){UiPalette.DropLegacyDefaults(s);s.FormatVersion=3;}s.Validate();return s;}}catch(Exception ex){LoadWarning="Не удалось прочитать настройки. Загружены стандартные значения.\n"+ex.Message;}return new AppSettings();}

  public AppSettings Clone(){return new AppSettings{FormatVersion=FormatVersion,Theme=Theme,Accent=Accent,ShowHints=ShowHints,StartSection=StartSection,Language=Language,Colors=Colors==null?null:new Dictionary<string,string>(Colors),Quality=Quality,OrbitSensitivity=OrbitSensitivity,SnapEnabled=SnapEnabled,SnapStep=SnapStep,AnimationScale=AnimationScale,AutoSave=AutoSave,AutoSaveSeconds=AutoSaveSeconds};}

  public void Save(){SaveTo(SettingsPath);}public void SaveTo(string path){Validate();Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));if(File.Exists(path))File.Copy(path,path+".bak",true);ReleaseSupport.WriteText(path,new JavaScriptSerializer().Serialize(this),System.Text.Encoding.UTF8);}

  public static double Snap(double v){return Current.SnapEnabled?Math.Round(v/Current.SnapStep)*Current.SnapStep:v;}

  static readonly string RecoverySession=Guid.NewGuid().ToString("N");

  internal static void SaveRecoveryTo(string folder,string kind,string key,string text){if(!new[]{"robot","mechanism","cad"}.Contains(kind)||string.IsNullOrEmpty(key)||key.Any(c=>!char.IsLetterOrDigit(c)))throw new Exception("Неверный идентификатор автосохранения.");Directory.CreateDirectory(folder);ReleaseSupport.WriteText(Path.Combine(folder,"recovery-"+kind+"-"+key+".json"),text,System.Text.Encoding.UTF8);}

  public static void Recovery(string kind,string text,string key=null){if(Current.AutoSave)SaveRecoveryTo(Folder,kind,key??RecoverySession,text);}

  public static string ReadRecovery(string kind){var files=Directory.Exists(Folder)?Directory.GetFiles(Folder,"recovery-"+kind+"*.json").OrderByDescending(File.GetLastWriteTimeUtc).ToArray():new string[0];if(files.Length==0)throw new Exception("Автосохранение пока не создано.");string path=files[0];if(files.Length>1)using(var dialog=new OpenFileDialog{Title="Выберите восстановительную копию (дата изменения — время сохранения)",InitialDirectory=Folder,Filter="Копии проектов|recovery-"+kind+"*.json",FileName=Path.GetFileName(path)}){if(dialog.ShowLocalizedDialog()!=DialogResult.OK)throw new Exception("Восстановление отменено.");path=dialog.FileName;}return File.ReadAllText(path);}

  public static void Apply(Control root){UiAppearance.Apply(root);}

  // «Как в Windows»: читаем системную тему приложений и следим за её сменой.
  static bool? systemDark;static bool watchingSystem;
  public static bool SystemDark{get{if(!systemDark.HasValue)systemDark=ReadSystemDark();return systemDark.Value;}}
  static bool ReadSystemDark(){try{object value=Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","AppsUseLightTheme",null);return value is int&&(int)value==0;}catch{return true;}}
  public static bool IsDark(AppSettings settings){return settings==null||settings.Theme==GearTheme.Тёмная||settings.Theme==GearTheme.Системная&&SystemDark;}
  public static bool Dark{get{return IsDark(Current);}}
  internal static void WatchSystemTheme(){if(watchingSystem)return;watchingSystem=true;try{Microsoft.Win32.SystemEvents.UserPreferenceChanged+=(s,e)=>{if(e.Category!=Microsoft.Win32.UserPreferenceCategory.General)return;try{bool now=ReadSystemDark();if(systemDark.HasValue&&systemDark.Value==now)return;systemDark=now;if(Current.Theme!=GearTheme.Системная)return;var forms=Application.OpenForms.Cast<Form>().Where(f=>f.TopLevel||f.Parent==null).ToArray();if(forms.Length==0)return;forms[0].BeginInvoke(new Action(()=>{AppearanceRevision++;foreach(var f in forms)if(!f.IsDisposed)Apply(f);if(Changed!=null)Changed();}));}catch(InvalidOperationException){}};}catch{}}
 }
}
