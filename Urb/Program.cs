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
            if (args.Length > 0)
            {
                var source = "";
                var uLisp = new ULisp();
                var name = "compiled_ulisp.dll";
                if (args.Length == 2) name = args[0];
                source = File.ReadAllText(args[args.Length - 1]);
                uLisp.Compile(source, name, isDebugTransform: true);
            }

            // Test Source:
            else
            {
                //var ufo = new UForth();
                //while (true)
                //{
                //    Console.Write("> ");
                //    var input = Console.ReadLine();
                //    ufo.Repl(input);

                //    Console.WriteLine();
                //    Console.WriteLine(UForth.nTimes("_", 80));
                //}


                // minimal test.
                //ULisp.ReplTest(@"
                //    ;; as quote test
                //    ;; (compile @((require System)))
                //");
                var uLisp = new ULisp();
                var source = File.ReadAllText("../../examples/Lisp.ul");

                // Compiling..
                uLisp.Compile(source, "demo.dll", isDebugTransform: true);
                //uLisp.Compile(source, "demo.dll", false, true, true);

                // it's not ready yet.
                //ULisp.ReplSession();
            }
            // wait for prompt.
            Console.ReadLine();
        }
    }
}
