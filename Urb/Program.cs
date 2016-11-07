using System;
using System.IO;

namespace Urb
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var urb = new UrbCore();

			// Test Source:
			var source = File.ReadAllText("../../examples/Ruby.rb");

			// Compiling..
			urb.Compile(source, "demo.dll", isExe: false);
		}
	}
}
