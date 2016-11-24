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
            var source = "";
            var uLisp = new ULisp();
            if (args.Length > 0)
            {
                var name = "compiled_ulisp.dll";
                if (args.Length == 2) name = args[0];
                source = File.ReadAllText(args[args.Length - 1]);
                uLisp.Compile(source, name, isDebugTransform: true);
            }

            // Test Source:
            else
            {
                source = File.ReadAllText("../../examples/Lisp.ul");

                // Compiling..
                uLisp.Compile(source, "demo.dll", isDebugTransform: true);
                //uLisp.Compile(source, "demo.dll", false, true, true);

                // minimal test.
                uLisp.ReplTest(@"
                (def (square -> x) 
                     (int    -> int)
                     (:public :static)
                     (begin
                        (return (* x x))))

                (square 4)
            ");

                // it's not ready yet.
                // uLisp.ReplSession();
            }
            // wait for prompt.
            Console.ReadLine();
        }
    }
}
