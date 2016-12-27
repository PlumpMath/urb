using System;
using System.Text;
using Microsoft.CSharp;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
namespace Urb
{
    public class Bailey
    {
        #region Init 

        public Bailey()
        {
            _print(" bailey :: a minimal assembly family language ");
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
            // quote
            @"(?<quote>\')|" +
            // forward
            @"(?<forward>\-\>)|" +
            // backward
            @"(?<backward>\<\-)|" +
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
            // double 1.0 2.0
            @"(?<double>[-+]?[0-9]*\.[0-9]+)|" +
            // integer 120
            @"(?<integer>[+-]?[0-9]+)|" +

            // operators
            @"(?<operator>\+=|\-=|\=|\+|\-|\*|\/|\^)|" +
            // boolean
            @"(?<boolean_compare>[\>|\<|\==|\>=|\<=])|" +
            @"(?<boolean_condition>[\|\||\&\&])|" +

            // #Comments
            @"(?<comment>;;;.*\n)|" +

            // :Symbol
            @"(?<symbol>:[a-zA-Z0-9$_.]+)|" +
            // Label:
            @"(?<label>[a-zA-Z0-9$_]+\:)|" +
            // Literal   [a-zA-Z0-9\\_\<\>\[\]\-$_.]
            // without [] in its literal rule.
            @"(?<literal>[a-zA-Z0-9\\_\<\>\-\?$_.]+)|" +

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

        #region Lexer/EatToken


        private void EatToken(Token token, Dictionary<string, object> env)
        {
            switch (token.type)
            {
                case "eval_mode":
                    compilerMode = CompilerMode.Awake;
                    _print("[compiler] Awaken.");
                    break;

                case "uneval_mode":
                    compilerMode = CompilerMode.Sleep;
                    _print("[compiler] Slept.");
                    break;

                case "separator":
                    /// () []
                    switch (token.value)
                    {
                        case "(":
                            compilerMode = CompilerMode.Sleep;
                            NewStackFrame();
                            break;
                        case ")":
                            compilerMode = CompilerMode.Awake;
                            CloseStackFrameToList();
                            break;

                        case "[": NewStackFrame(); break;
                        case "]": CloseStackFrameToStack(); break;
                        default: /// ignoring.
                            break;
                    }
                    break;

                //case "backward": functionMap[token.Value].Eval(evaluationStack); break;
                case "forward": break;

                //case "operator":
                case "literal":
                    switch (compilerMode)
                    {
                        case CompilerMode.Awake:
                            /// if it's primitives: 
                            if (env.ContainsKey(token.value))
                            {
                                Apply(token, env);
                            }
                            /// if it's defined:
                            else if (env.ContainsKey(token.value))
                            {
                                Apply(token, env);
                            }
                            /// if it's variable from env:
                            else if (env.ContainsKey(token.value))
                            {
                                evaluationStack.Push(env[token.value]);
                            }
                            break;
                        case CompilerMode.Sleep:
                            // to stack for now. //
                            evaluationStack.Push(token);
                            break;
                        default: throw new NotImplementedException();
                    }
                    break;

                default:
                    evaluationStack.Push(BuildValueType(token));
                    break;
            }
        }

        private List<object> Lexer(List<Token> tokens)
        {
            foreach (var token in tokens)
            {
                EatToken(token, env);
            }
            return evaluationStack.Pop() as List;
        }


        #endregion

        #region Line Helpers 

        private static string _nTimes(string ch, int time)
        {
            var acc = "";
            for (int i = 1; i < time; i++) acc += ch;
            return acc;
        }

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
            foreach (var word in line) s += String.Format("{0} ", word.value);
            return s;
        }

        private void AddSource(string line)
        {
            _csharp_blocks.Add(line);
        }

        private static void _print(string line)
        {
            _print(line + "\n", new object[] { });
        }

        private static void _print(string line, params object[] args)
        {
            var backup = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(line, args);
            Console.ForegroundColor = backup;
        }

        #endregion

        #region List

        public class List : List<object>
        {

            public List(object[] collection, bool isReversed = false)
            {
                // reverse mode.
                if (isReversed) this.AddRange(collection);
                else // reserved mode.
                    for (int i = collection.Length - 1; i > -1; i--)
                        this.Add(collection[i]);
            }

            public List(IEnumerable<object> collection) : base(collection)
            {
            }

            public override string ToString()
            {
                var acc = new StringBuilder();
                acc.Append("[");
                foreach (var e in this)
                {
                    acc.Append(string.Format(
                        "{0} ", e.ToString()));
                }
                acc.Append("]");
                return acc.ToString();
            }
        }

        #endregion

        #region Typing

        public object BuildValueType(Token token)
        {
            switch (token.type)
            {
                case "string": return token.value;
                case "integer": return Int32.Parse(token.value);
                case "double": return double.Parse(token.value);
                case "float": return float.Parse(
                                token.value.ToString().Substring(0,
                                token.value.ToString().Length - 1));
                case "symbol":
                    return new Atom(token.type,
                                    token.value.Substring(1, token.value.Length - 1));
                default: return new Atom(token.type, token.value);
            }
        }

        #endregion

        #region Stack Manipulators
        public enum CompilerMode
        {
            Awake, Sleep
        }
        public CompilerMode compilerMode = CompilerMode.Awake; // by default. //
        public Stack<object> evaluationStack = new Stack<object>();
        public Stack<Stack<object>> Frames = new Stack<Stack<object>>();

        public void PrintStack()
        {
            Console.Write("[ ");
            foreach (var e in evaluationStack)
            {
                Console.Write("{0} ", e);
            }
            Console.Write(" ]");
        }

        public void NewStackFrame()
        {
            /// create new stack frame.
            Frames.Push(evaluationStack);
            evaluationStack = new Stack<object>();
        }

        public void CloseStackFrameToList()
        {
            /// acc all current stack frame into a list.
            var lst = new List(evaluationStack.ToArray());
            evaluationStack = Frames.Pop();
            evaluationStack.Push(lst);
        }

        public void CloseStackFrameToStack()
        {
            var store = Frames.Pop();
            foreach (var o in evaluationStack)
            {
                store.Push(o);
            }
            evaluationStack = store;
        }

        #endregion

        #region Eval/Repl

        private void Apply (Token token, Dictionary<string,object> env)
        {

        }

        public Dictionary<string, object> env = new Dictionary<string, object>();

        public Atom Eval(List<object> tokens)
        {

            return new Atom("null",tokens);
        }

        public void ReplTest(string source)
        {
            var _tokens = Reader(source);
            var _tree = Lexer(_tokens);
            var result = Eval(_tree);
            _print("=> {0}", result);
        }

        public void ReplSession()
        {
            var environment = new Dictionary<string, object>();
            while (true)
            {
                _print("> ", null);
                var _input = Console.ReadLine();
                if (_input == "quit") break;

                var _tokens = Reader(_input);
                var _tree = Lexer(_tokens);
                var result = Eval(_tree);
                _print("=> {0}", result);

                // end line. //
                _print("\n" + _nTimes("_", 80) + "\n\n");
            }
        }

        #endregion
    }
}
