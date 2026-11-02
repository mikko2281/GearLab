using System;using System.Collections.Generic;using System.Globalization;
namespace GearLab {
 public sealed class CADExpression {
  readonly string s;readonly IDictionary<string,double> parameters;int pos;
  CADExpression(string text,IDictionary<string,double> values){s=(text??"0").Replace(',','.');parameters=values;}
  public static double Evaluate(string text,IDictionary<string,double> values){var p=new CADExpression(text,values);double v=p.Sum();p.Space();if(p.pos!=p.s.Length||!MechanismCad.Finite(v))throw new Exception("Неверная формула: "+text);return v;}
  void Space(){while(pos<s.Length&&char.IsWhiteSpace(s[pos]))pos++;}bool Eat(char c){Space();if(pos<s.Length&&s[pos]==c){pos++;return true;}return false;}
  double Sum(){double v=Product();while(true){if(Eat('+'))v+=Product();else if(Eat('-'))v-=Product();else return v;}}
  double Product(){double v=Power();while(true){Space();if(pos+1<s.Length&&s[pos]=='*'&&s[pos+1]=='*')return v;if(Eat('*'))v*=Power();else if(Eat('/'))v/=Power();else return v;}}
  double Power(){double v=Atom();Space();if(pos+1<s.Length&&s[pos]=='*'&&s[pos+1]=='*'){pos+=2;v=Math.Pow(v,Power());}return v;}
  double Atom(){if(Eat('+'))return Atom();if(Eat('-'))return -Atom();if(Eat('(')){double v=Sum();if(!Eat(')'))throw new Exception("Не закрыта скобка.");return v;}Space();int start=pos;if(pos<s.Length&&(char.IsLetter(s[pos])||s[pos]=='_')){while(pos<s.Length&&(char.IsLetterOrDigit(s[pos])||s[pos]=='_'))pos++;string key=s.Substring(start,pos-start);if(Eat('(')){double x=Sum();if(!Eat(')'))throw new Exception("Не закрыта скобка.");switch(key){case "sqrt":return Math.Sqrt(x);case "sin":return Math.Sin(x*Math.PI/180);case "cos":return Math.Cos(x*Math.PI/180);case "tan":return Math.Tan(x*Math.PI/180);case "abs":return Math.Abs(x);default:throw new Exception("Неизвестная функция.");}}if(key=="pi")return Math.PI;double result;if(parameters!=null&&parameters.TryGetValue(key,out result))return result;throw new Exception("Неизвестный параметр: "+key);}while(pos<s.Length&&(char.IsDigit(s[pos])||s[pos]=='.'))pos++;double number;if(start==pos||!double.TryParse(s.Substring(start,pos-start),NumberStyles.Float,CultureInfo.InvariantCulture,out number))throw new Exception("Ожидалось число.");return number;}
 }
}
