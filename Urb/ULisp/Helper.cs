using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Urb
{
    public partial class ULisp
    {


        #region Line Helpers 
        private static List<string> _csharp_blocks = new List<string>();

        private static void InspectLine(List<Token> line)
        {
            foreach (var word in line) Console.Write("{0} ", word);
            Console.WriteLine();
        }

        private static string ViewLine(Token[] line)
        {
            var s = String.Empty;
            foreach (var word in line) s += String.Format("{0} ", word.value);
            return s;
        }

        private static void AddSource(string line)
        {
            _csharp_blocks.Add(line);
        }

        private static string SourceEnforce(object[] args, int index)
        {
            var arg = args[index];

            if (arg.GetType().IsSubclassOf(typeof(Expression)))
            {
                return ((Expression)arg).CompileToCSharp();
            }
            else if (arg is Atom)
            {
                return (arg as Atom).valueString;
            }
            throw new NotImplementedException();

        }

        private static void _warning(string line)
        {
            var _backupColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            _print(line);
            Console.ForegroundColor = _backupColor;
        }

        private static void _warning(string line, params object[] args)
        {
            var _backupColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            _print(line, args);
            Console.ForegroundColor = _backupColor;
        }

        private static void _print(string line)
        {
            Console.WriteLine(line);
        }


        private static void _print(string line, params object[] args)
        {
            Console.Write(line, args);
        }

        #endregion

    }
}
