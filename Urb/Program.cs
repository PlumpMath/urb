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
            //var ufo = new UForth();

            // Test Source:
            var source = File.ReadAllText("../../examples/Lisp.ul");
            //var source = File.ReadAllText("../../examples/Forth.ufo");

            //uLisp.ReplTest(source);
            //ufo.ReplTest(source);
            uLisp.ReplTest(@"
                (def (square -> x) 
                     (int    -> int)
                     (:public :static)
                     (progn
                        (return (* x x))))

                (square 4)
            ");

            // it's not ready yet.
            // uLisp.ReplSession();

            // Compiling..
            //uLisp = new ULisp();
            uLisp.Compile(source, "demo.dll", isDebugTransform: true);
            //uLisp.Compile(source, "demo.dll", false, true, true);

        }
    }
}
