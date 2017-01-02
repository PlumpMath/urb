using System;
using System.IO;
using Forth;

namespace Urb
{
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
                BaileyTest();
                // wait for prompt.
                Console.ReadLine();
            }
        }

        private static void BaileyTest()
        {
            var gd = new Bailey();
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.Write("> ");
                var input = Console.ReadLine();
                gd.Repl(input);

                Console.WriteLine();
                Console.WriteLine(Bailey.nTimes("_", 80));
            }
        }

        private static void ULispTest()

        {

            // minimal test.
            //ULisp.ReplTest(@"
            //    ;; as quote test
            //    ;; (compile @((require System)))
            //");
            //var uLisp = new ULisp();
            //var source = File.ReadAllText("../../examples/Lisp.ul");

            // Compiling..
            //uLisp.Compile(source, "Lisp", isDebugTransform: true, isDebugGrammar:true);

            //uLisp.Compile(source, "demo.dll", false, true, true);

            // it's not ready yet.
            //ULisp.ReplSession();

            ULisp.Test();

        }
        
        private static void UforthRepl()
        {
            var ufo = new UForth();
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.Write("> ");
                var input = Console.ReadLine();
                ufo.Repl(input);

                Console.WriteLine();
                Console.WriteLine(UForth.nTimes("_", 80));
            }
        }
    }
}
