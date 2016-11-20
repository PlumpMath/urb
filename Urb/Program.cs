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
            //var uLisp = new ULisp();
            var ufo = new UForth();

            // Test Source:
            //var source = File.ReadAllText("../../examples/Lisp.ul");
            var source = File.ReadAllText("../../examples/Forth.ufo");

            // Compiling..
            //uLisp.Compile(source, "demo.dll");
            //uLisp.Compile(source, "demo.dll", false, true, true);

            //ufo.ReplTest(source);
            ufo.ReplTest(@"
                (def (x:int -> square:int) :public :static
                    (
                        ((x x *) return)
                    )
                )
                (4 square)
            ");

            // it's not ready yet.
            // uLisp.ReplSession();


        }
    }
}
