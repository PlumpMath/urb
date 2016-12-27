using System;
namespace Urb
{
	public class Token:IComparable<Token>
	{
		public readonly string type;
		public readonly string value;
		public Token(string type, string value)
		{
			this.type = type;
			this.value = value;
		}

		public override string ToString()
		{
			return string.Format("token-{1} ", type, value);
		}

        public string InferenceType
        {
            get { return value; }
        }

        public Token InferencedToken {
        get
            {
                return new Token(value, type);
            }
        }

        public string Info
        {
            get { return string.Format("{0}-{1}", type, value); }
        }
                                 
        public int CompareTo(Token other)
        {
            return this.Info == other.Info ? 1 : -1;
        }
    }
}

