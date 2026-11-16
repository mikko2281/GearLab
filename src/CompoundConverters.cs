using System;using System.ComponentModel;using System.Globalization;
namespace GearLab {
 public sealed class SharedShapeConverter: StringConverter {
  public override bool GetStandardValuesSupported(ITypeDescriptorContext c){return true;}public override bool GetStandardValuesExclusive(ITypeDescriptorContext c){return true;}public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext c){return new StandardValuesCollection(new[]{"Round","Square"});}
  public override object ConvertTo(ITypeDescriptorContext c,CultureInfo culture,object value,Type type){if(type==typeof(string))return I18n.T((string)value=="Square"?"Квадрат":"Круг");return base.ConvertTo(c,culture,value,type);}public override object ConvertFrom(ITypeDescriptorContext c,CultureInfo culture,object value){string v=value as string;return v=="Square"||v=="Квадрат"||v=="Square housing"?"Square":"Round";}
 }
 public sealed class SharedJointConverter:StringConverter {
  public override bool GetStandardValuesSupported(ITypeDescriptorContext c){return true;}public override bool GetStandardValuesExclusive(ITypeDescriptorContext c){return true;}public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext c){return new StandardValuesCollection(new[]{"CommonShaft","D","Spline"});}
  public override object ConvertTo(ITypeDescriptorContext c,CultureInfo culture,object value,Type type){if(type==typeof(string))return I18n.T((string)value=="D"?"D-вал":(string)value=="Spline"?"Шлицы":"Общий вал");return base.ConvertTo(c,culture,value,type);}public override object ConvertFrom(ITypeDescriptorContext c,CultureInfo culture,object value){string v=value as string;return v=="D"||v=="D-вал"||v=="D shaft"?"D":v=="Spline"||v=="Шлицы"||v=="Splines"?"Spline":"CommonShaft";}
 }
}
