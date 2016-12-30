using System;
using System.Text;
using Microsoft.CSharp;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using System.CodeDom;
using Token = Urb.Token;
using Atom = Urb.Atom;
using System.Security.Cryptography.X509Certificates;

namespace Urb
{
    public class GD
    {
        #region Syntax Table
        /********************
         *  Syntax Pattern. *
         ********************/
        private const string pattern =
            // \n and \r
            @"(?<newline>\n\t|\n|\r|\r\n)|" +
            // \t
            @"(?<tab>\t)|" +
            // quote
            @"(?<quote>\@)|" +
            // unquote
            @"(?<unquote>\!)|" +
            // forward
            @"(?<forward>\-\>)|" +
            // comma, () and []
            @"(?<separator>,|\(|\)|\[|\])|" +
            // string " "
            @"(?<String>\"".*?\"")|" +
            // pair of a:b
            @"(?<pair>[a-zA-Z0-9,\\_\<\>\[\]\-$_]+::[a-zA-Z0-9,\\_\<\>\[\]\-$_]+)|" +

            // @instant_variable
            @"(?<instance_variable>\@[a-zA-Z0-9$_]+)|" +
            // $global_variable
            @"(?<global_variable>\$[a-zA-Z0-9$_]+)|" +

            // float 1f 2.0f
            @"(?<float>[-+]?[0-9]*\.?[0-9]+f)|" +
            // double 1d 2.0d
            @"(?<Double>[-+]?[0-9]*\.?[0-9]+d)|" +
            // integer 120
            @"(?<Int32>[+-]?[0-9]+)|" +
            // true|false
            @"(?<bool>true|false)|" +

            // operators
            @"(?<operator>\+\=|\-\=|\>\>|\+|\-|\*|\/|\^)|" +
            // boolean
            @"(?<boolean_compare>\>|\<|\=\?|\>=|\<=)|" +
            @"(?<boolean_condition>\|\||\&\&)|" +

            // #Comments
            @"(?<comment>\=\=.*\n)|" +

            // :Symbol
            @"(?<symbol>:[a-zA-Z0-9$_.]+)|" +
            // Label:
            @"(?<label>[a-zA-Z0-9$_]+\:)|" +
            // Literal   [a-zA-Z0-9\\_\<\>\[\]\-$_.]
            // without [] in its literal rule.
            @"(?<literal>[a-zA-Z0-9\\_\<\>\[\],\-$_.]+)|" +

            // the rest.
            @"(?<invalid>[^\s]+)";
        #endregion

        #region Line Helper

        public static string nTimes(string ch, int time)
        {
            var acc = "";
            for (int i = 1; i < time; i++) acc += ch;
            return acc;
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

        #region Reader: source -> token
        // Readline.

        public static List<Token> Reader(string source, bool isDebugTransform = false, bool isDebugGrammar = false)
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

        #region List

        [Serializable]
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
                case "float":
                    return float.Parse(
              token.value.ToString().Substring(0,
              token.value.ToString().Length - 1));
                case "symbol":
                    return new Atom(token.type,
                                    token.value.Substring(1, token.value.Length - 1));
                default: return new Atom(token.type, token.value);
            }
        }

        #endregion

        #region Data

        public enum CompilerMode
        {
            Awake, Sleep
        }
        public CompilerMode compilerMode = CompilerMode.Awake; // by default. //
        public Stack<CompilerMode> compilerState = new Stack<CompilerMode>();
        public Stack<object> evaluationStack = new Stack<object>();
        public Stack<Stack<object>> Frames = new Stack<Stack<object>>();
        public Dictionary<string, object> userVars = new Dictionary<string, object>();

        #endregion

        public void Repl(string source)
        {
            var tokens = Reader(source);

            foreach (var token in tokens)
            {
                EatToken(token, userVars);
            }

            PrintStack();
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

        private void ChangeCompilerState(CompilerMode next_state)
        {
            // store current state.
            compilerState.Push(compilerMode);
            compilerMode = next_state;
            if (compilerMode == CompilerMode.Sleep)
                _print("[compiler] Slept.");
        }

        private void RestoreCompilerState(bool isForced = false)
        {
            if (isForced)
            {
                compilerMode = CompilerMode.Awake;
                compilerState.Clear();
                _print("[compiler] Awaken.");
                return;
            }
            if (compilerState.Count > 0)
                compilerMode = compilerState.Pop();
            else {
                compilerMode = CompilerMode.Awake;
                _print("[compiler] Awaken.");
            }
        }

        private void EatToken(Token token, Dictionary<string, object> env)
        {
            switch (token.type)
            {
                case "separator":
                    /// () []
                    switch (token.value)
                    {
                        case "(":
                            ChangeCompilerState(CompilerMode.Sleep);
                            NewStackFrame();
                            break;
                        case ")":
                            RestoreCompilerState();
                            CloseStackFrameToList();
                            break;

                        case "[": NewStackFrame(); break;
                        case "]": CloseStackFrameToStack(); break;
                        default: /// ignoring.
                            break;
                    }
                    break;

                //case "backward": break;
                //case "forward": break;

                case "boolean_compare":
                case "operator":
                case "literal": break;

                default:
                    evaluationStack.Push(BuildValueType(token));
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


    }
}
