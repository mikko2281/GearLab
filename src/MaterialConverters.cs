using System;using System.ComponentModel;using System.Globalization;
namespace GearLab {
 public abstract class EngineeringChoiceConverter:StringConverter {
  protected abstract string[] Keys{get;}protected abstract string[] Labels{get;}
  public override bool GetStandardValuesSupported(ITypeDescriptorContext c){return true;}public override bool GetStandardValuesExclusive(ITypeDescriptorContext c){return true;}public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext c){return new StandardValuesCollection(Keys);}
  public override object ConvertTo(ITypeDescriptorContext c,CultureInfo culture,object value,Type type){if(type==typeof(string)){int i=Array.IndexOf(Keys,value as string);return i<0?Convert.ToString(value):I18n.T(Labels[i]);}return base.ConvertTo(c,culture,value,type);}
  public override object ConvertFrom(ITypeDescriptorContext c,CultureInfo culture,object value){string text=value as string;for(int i=0;i<Keys.Length;i++)if(text==Keys[i]||text==Labels[i]||text==I18n.T(Labels[i]))return Keys[i];throw new FormatException(I18n.T("Выберите значение из списка."));}
 }
 public sealed class MaterialKindConverter:EngineeringChoiceConverter {protected override string[] Keys{get{return new[]{"FDM","Metal","Polymer"};}}protected override string[] Labels{get{return new[]{"Печать FDM","Металл","Покупной полимер"};}}}
 public sealed class AxialSupportConverter:EngineeringChoiceConverter {protected override string[] Keys{get{return new[]{"Unknown","First","Second"};}}protected override string[] Labels{get{return new[]{"Не задана","Первая опора","Вторая опора"};}}}
}
