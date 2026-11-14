using System;using System.Linq;using System.Collections;using System.Collections.Generic;
namespace GearLab {
 // JSON member enumeration order can change after WinForms installs a type-description provider.
 // History compares values and ordered arrays, not reflection/dictionary member order.
 public static class TrainJson {
  public static string Normalize(string json){return Canonical(CADDocument.Serializer().DeserializeObject(json));}
  static string Canonical(object value){if(value==null||value is string)return CADDocument.Serializer().Serialize(value);var map=value as IDictionary<string,object>;if(map!=null)return "{"+string.Join(",",map.Keys.OrderBy(k=>k,StringComparer.Ordinal).Select(k=>CADDocument.Serializer().Serialize(k)+":"+Canonical(map[k])))+"}";var list=value as IEnumerable;if(list!=null)return "["+string.Join(",",list.Cast<object>().Select(Canonical))+"]";return CADDocument.Serializer().Serialize(value);}
 }
}
