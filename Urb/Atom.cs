using System;
namespace Urb
{
	public class Atom
	{
		public string type;
		public object value;
		public Atom(string type, object value)
		{
			this.type = type;
			this.value = value;
		}
		public override string ToString()
		{
			return string.Format("{0}", value);
		}
	}
}
