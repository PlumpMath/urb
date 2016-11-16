using System;
using System.IO;

namespace Urb
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			//var urb = new UrbCore();
			var uLisp = new ULisp();

			// Test Source:
			var source = File.ReadAllText("../../examples/Lisp.urb");

			// Compiling..
			//urb.Compile(source, "demo.dll", false);
			uLisp.Compile(source, "demo.dll", false, true, true);
		}
	}
}
