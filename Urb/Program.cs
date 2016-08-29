using System;
using System.IO;

namespace Urb
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var urb = new UrbCore();
			Console.WriteLine("* Urb :: A Rubylike post-fix language compiler *");

			// Test Source:
			string test = File.ReadAllText("../../examples/demo 3.urb");
			urb.Parse(test, isDebug: false);

			//Console.ReadLine ();
		}
	}
}
