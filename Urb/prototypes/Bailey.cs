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
    public class Bailey
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
            // compose head::tail
            @"(?<composer>\:\:)|" +
            // attractor
            @"(?<attractor>\:)|" +
            // arrow
            @"(?<arrow>\-\>)|" +
            // forward
            @"(?<forward>\>\>)|" +
            // backward
            @"(?<backward>\<\<)|" +
            // empty list
            @"(?<empty>\[\])|" +
            // comma, () and []
            @"(?<separator>,|\(|\)|\[|\])|" +
            // string " "
            @"(?<String>\"".*?\"")|" +
            
            // float 1f 2.0f
            @"(?<float>[-+]?[0-9]*\.?[0-9]+f)|" +
            // double 1d 2.0d
            @"(?<Double>[-+]?[0-9]*\.?[0-9]+d)|" +
            // integer 120
            @"(?<Int32>[+-]?[0-9]+)|" +
            // true|false
            @"(?<bool>true|false)|" +

            // operators
            @"(?<operator>\+\=|\-\=|\+|\-|\*|\/|\^)|" +

            // boolean
            @"(?<boolean_compare>\>|\<|\=\?|\>=|\<=)|" +
            @"(?<boolean_condition>\|\||\&\&)|" +

            // #Comments
            @"(?<comment>\=\=.*\n)|" +

            // symbol   [a-zA-Z0-9\\_\<\>\[\]\-$_.]
            @"(?<symbol>[a-zA-Z0-9\\_\<\>\/\[\],\-$_.]+)|" +

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
            print(line + "\n", new object[] { });
        }

        private static void print(string line, params object[] args)
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

        public class Integer : Exception { public int value { get; set; } }
        public class Double : Exception { public double value { get; set; } }
        public class Float : Exception { public float value { get; set; } }
        public class Boolean : Exception { public bool value { get; set; } }
        public class BString : Exception { public string value { get; set; } }
        public class Symbol : Exception { public string name { get; set; } }

        public Exception BuildValueType(Token token)
        {
            var value = token.value;
            switch (token.type)
            {
                case "integer": return new Integer() { value = Int32.Parse(value) };
                case "double": return new Double() {
                    value = double.Parse(value.Substring(0, value.Length - 1))
                };
                case "float": return new Float() {
                    value = float.Parse(value.Substring(0, value.Length - 1))
                };
                case "bool": return new Boolean() { value = value == "true" };
                case "string": return new BString() { value = value };
                case "symbol": return new Symbol() { name = value }; 
                default:
                    print("can't find so -> symbol {0}\n", token.type);
                    return new Symbol() { name = value };
                    //throw new NotImplementedException(token.type);
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

        #region Formatter

        public string Value2String(Exception e)
        {
            try { throw e; }
            catch (Integer n) {return string.Format("{0}", n.value); }
            catch (Float n) { return string.Format("{0}f", n.value); }
            catch (Double n) { return string.Format("{0}d", n.value); }
            catch (BString n) { return string.Format("{0}", n.value); }
            catch (Boolean n) { return string.Format("{0}", n.value); }
            catch (Symbol n) { return string.Format("{0}", n.name); }
        }

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

        public enum PreFixAnnotation
        {
            Create,
            PatternGuard
        }

        private void EatToken(Token token, Dictionary<string, object> env)
        {
            switch (token.type)
            {
                #region () []
                case "separator":
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
                #endregion

                case "attractor":
                    /// 1. Get next literal as signature.
                    /// 2. Assign name for this whole block.

                case "backward":
                case "forward":
                case "arrow":
                    
                case "boolean_compare":
                case "operator":
                case "literal":// break;

                default:
                    print("stack << {0}:'{1}'\n", token.type, token.value);
                    var value = BuildValueType(token);
                    evaluationStack.Push(value);
                    break;
            }
        }
        
        public void PrintStack()
        {
            Console.Write("[ ");
            foreach (var e in evaluationStack)
            {
                string acc = String.Empty;
                if (e.GetType().IsSubclassOf(typeof(Exception)))
                    acc = Value2String(e as Exception);
                else acc = e.ToString();
                Console.Write("{0} ", acc);
            }
            Console.Write(" ]");
        }


    }
}
