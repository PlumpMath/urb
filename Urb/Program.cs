using System;
using System.IO;

namespace Urb
{
    static partial class demo1 { }
    static partial class demo1 { }
    class MainClass
    {
        public static void Main(string[] args)
        {
            //var urb = new UrbCore();
            var uLisp = new ULisp();

            // Test Source:
            //var source = File.ReadAllText("../../examples/Lisp.ul");

            // Compiling..
            //uLisp.Compile(source, "demo.dll");
            //uLisp.Compile(source, "demo.dll", false, true, true);

            // it's not ready yet.
            // uLisp.ReplSession();

            // testing:
            uLisp.ReplTest(@"
            (defun square:int x:int
                (progn
                    (return (* x x))))

            (Console.WriteLine
                (square 12))
");
        }
    }
}
