using System;
using System.IO;

namespace Urb
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var urb = new UrbCore();
			Console.WriteLine("* Urb :: A Rubylike language compiler *");

			// Test Source:
			string test = File.ReadAllText("../../examples/Ruby.rb");
			urb.Parse(test, isDebug: false);

			//Console.ReadLine ();
		}
	}
}
