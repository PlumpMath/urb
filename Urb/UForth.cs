using System;
using System.Text;
using Microsoft.CSharp;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace Urb
{
    public class UForth
    {
        #region Collections
        // function
        private Dictionary<string, Action<object>> functionMap =
            new Dictionary<string, Action<object>>();
        // codeblock
        private Dictionary<string, Action> codeblockMap =
            new Dictionary<string, Action>();
        #endregion

        #region Init 

        public UForth()
        {
            _print(" uForth :: a minimal lisp family language compiler ");
        }
        public class GenericClass<T>
        {
            public GenericClass(T type) { }
        }
        // Create new codeblock.
        public void NewCodeBlock(string name, Action codeBlock)
        {
            codeblockMap.Add(name, codeBlock);
        }

        // Create new function.
        public void NewFunction(string name, Action<object> codeBlock)
        {
            functionMap.Add(name, codeBlock);
        }

        #endregion

        #region Syntax Table
        /********************
         *  Syntax Pattern. *
         ********************/
        private const string pattern =
            // \n and \r
            @"(?<newline>\n\t|\n|\r|\r\n)|" +
            // \t
            @"(?<tab>\t)|" +
            // un-eval mode
            @"(?<uneval_mode>\@)|" +
            // eval mode
            @"(?<eval_mode>\!)|" +
            // forward
            @"(?<forward>-\>)|" +
            // comma, () and []
            @"(?<separator>,|\(|\)|\[|\])|" +
            // string " "
            @"(?<string>\"".*\"")|" +
            // pair of a:b
            @"(?<pair>[a-zA-Z0-9\\_\<\>$_]+:[a-zA-Z0-9,\\_\<\>\[\]$_]+)|" +

            // @instant_variable
            @"(?<instance_variable>\@[a-zA-Z0-9$_]+)|" +
            // $global_variable
            @"(?<global_variable>\$[a-zA-Z0-9$_]+)|" +

            // float 1f 2.0f
            @"(?<float>[-+]?[0-9]*\.?[0-9]+f)|" +
            // integer 120
            @"(?<integer>[+-]?[0-9]+)|" +
            // operators
            @"(?<operator>\+=|\-=|\=|\+|\-|\*|\/|\^)|" +
            // boolean
            @"(?<boolean_compare>[\>|\<|\==|\>=|\<=])|" +
            @"(?<boolean_condition>[\|\||\&\&])|" +

            // #Comments
            @"(?<comment>#.*\n)|" +

            // :Symbol
            @"(?<symbol>:[a-zA-Z0-9$_.]+)|" +
            // Label:
            @"(?<label>[a-zA-Z0-9$_]+\:)|" +
            // Literal   [a-zA-Z0-9\\_\<\>\[\]\-$_.]
            // without [] in its literal rule.
            @"(?<literal>[a-zA-Z0-9\\_\<\>\-$_.]+)|" +

            // the rest.
            @"(?<invalid>[^\s]+)";
        #endregion

        #region Reader: source -> token
        // Readline.

        public List<Token> Reader(string source, bool isDebugTransform = false, bool isDebugGrammar = false)
        {
            var token_list = new List<Token>();
            var regex_pattern = new Regex(pattern);
            var matches = regex_pattern.Matches(source);

            foreach (Match match in matches)
            {
                int i = 0;
                foreach (Group group in match.Groups)
                {

                    var match_value = group.Value;

                    var success = group.Success;

                    // ignore capture index 0 and 1 (general and WhiteSpace)
                    if (success && i > 1)
                    {
                        var group_name = regex_pattern.GroupNameFromNumber(i);
                        if (group_name != "tab")
                        {
                            token_list.Add(new Token(group_name, match_value));

                            //if (groupName != "newline") 
                            if (isDebugGrammar)
                                Console.WriteLine("{0} - {1}", group_name, match_value);
                        }
                    }
                    i++;
                }
            }
            return token_list;
        }

        #endregion

        #region Line Helpers 
        private List<string> _csharp_blocks = new List<string>();

        public static string nTimes(string ch, int time)
        {
            var acc = "";
            for (int i = 1; i < time; i++) acc += ch;
            return acc;
        }

        private void InspectLine(List<Token> line)
        {
            foreach (var word in line) Console.Write("{0} ", word);
            Console.WriteLine();
        }

        private string ViewLine(Token[] line)
        {
            var s = String.Empty;
            foreach (var word in line) s += String.Format("{0} ", word.Value);
            return s;
        }

        private void AddSource(string line)
        {
            _csharp_blocks.Add(line);
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

        #region Block
        public class Block
        {
            public object head;
            public object[] rest;
            public List<object> elements;
            public List<Block> asBlocks
            {
                get
                {
                    var blocks = new List<Block>();
                    foreach (var element in elements)
                        blocks.Add(element as Block);
                    return blocks;
                }
            }

            public Block(object[] args, bool isQuoted = false)
            {
                // we keep the original. //
                elements = new List<object>(args);
                if (args.Length != 0)
                {
                    if (!isQuoted)
                    {
                        rest = new object[args.Length - 1];
                        head = args[0];
                        // 1+ copying... //
                        for (int i = 1; i < args.Length; i++)
                        {
                            rest[i - 1] = args[i];
                        }
                        // done ! //
                    }
                    else
                    {
                        rest = new object[args.Length];
                        head = "quote";
                        // exactly copying... //
                        for (int i = 0; i < args.Length; i++)
                        {
                            rest[i] = args[i];
                        }
                    }
                }
            }

            public override string ToString()
            {
                var acc = "";
                foreach (var obj in rest) acc += obj.ToString() + " ";
                return string.Format("({0} {1})", head.ToString(), acc);
            }

        }

        #endregion

        #region Function

        public abstract class Function
        {
            public abstract object Eval(Stack<object> frame);
        }

        #endregion

        #region Repl

        public enum CompilerMode
        {
            Awake, Sleep
        }
        public CompilerMode compilerMode = CompilerMode.Awake; // by default. //
        public Stack<object> evaluationStack = new Stack<object>();
        public Stack<object> currentFrame = new Stack<object>();
        public Dictionary<string, Function> FunctionMap =
            new Dictionary<string, Function>()
            {
            };

        private void EatToken(Token token)
        {
            switch (token.Name)
            {
                case "literal":
                    if (FunctionMap.ContainsKey(token.Value))
                    {
                        switch (compilerMode)
                        {
                            case CompilerMode.Awake: // eval. 
                                FunctionMap[token.Value].Eval(evaluationStack);
                                break;
                            case CompilerMode.Sleep: // store!
                                evaluationStack.Push(token);
                                break;
                            default: break;
                        }
                    }
                    // to stack for now. //
                    else evaluationStack.Push(token);
                    break;
                default:
                    evaluationStack.Push(token.Value);
                    break;
            }
        }

        public void PrintStack()
        {
            Console.Write("[ ");
            foreach (var e in evaluationStack)
            {
                Console.Write("{0} ", e);
            }
            Console.Write(" ]");
        }

        public void Repl(string source)
        {
            var tokens = Reader(source);

            foreach (var token in tokens)
            {
                EatToken(token);
            }

            PrintStack();
        }

        #endregion
    }
}
