using System;
namespace Urb
{
	public class Atom:IComparable<Atom>
	{
		public string type;
		public object value;
        public string valueString
        {
            get
            {
                return value is bool ? 
                    (bool)value ? "true" : "false" : 
                    value.ToString();
            }
        }
		public Atom(string type, object value)
		{
			this.type = type;
			this.value = value;
		}
		public override string ToString()
		{
			return string.Format("{0}", valueString);
		}

        public string Info
        {
            get { return string.Format("{0}-{1}", type, value); }
        }

        public int CompareTo(Atom other)
        {
            return this.Info == other.Info ? 1 : -1;
        }
    }
}
