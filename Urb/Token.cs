using System;
namespace Urb
{
	public class Token
	{
		public readonly string Name;
		public readonly string Value;
		public Token(string name, string value)
		{
			Name = name;
			Value = value;
		}

		public override string ToString()
		{
			return string.Format("token-{1} ", Name, Value);
		}
	}
}

