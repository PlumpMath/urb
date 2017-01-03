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
        public enum Annotation
        {
            None,
            InitCreation,
            DoneCreation,
            InitGuard,
            NextGuard
        }                                              
        private Annotation annotation = Annotation.None; 

        #region Evaluation

        private void Evaluate(List expression, Dictionary<string, Word> dict)
        {
            foreach (var e in expression.cells)
            {
                Evaluate(e, expression, dict);
                if (annotation == Annotation.DoneCreation) break;
            }
            // reset annotation.
            annotation = Annotation.None;                                    
        }

        private void Evaluate(Exception e, List body, Dictionary<string, Word> dict)
        {
            try { throw e; }
            catch (Word w)
            {
                /// Do something about it ?
                if (w.isPrimitive) w.primitiveBody.Invoke(evaluationStack, dict);
            }
            catch (Attractor a) { annotation = Annotation.InitCreation; }
            catch (Symbol s)
            {
                /// 1. check annotation: None ? Create ? Guard ?      
                /// 3. wonder if it's create mode ? => analyze signature.
                /// 3. wonder if dict got s ? => fetch value.
                /// 4. else just return it to stack.
                switch (annotation)
                {
                    case Annotation.None:
                        if (dict.ContainsKey(s.name))
                        {
                            /// Eval it !
                            Evaluate(dict[s.name], body, dict);
                        }
                        else
                        {
                            /// Consider it as undefined.
                            warning("undefined symbol: {0}.\n", s.name);
                        }
                        break;

                    case Annotation.InitCreation:
                        /// Analyze symbol:
                        var signature = s.name;
                        if (signature.Contains("/"))
                        {
                            /// mean it got type info.          
                            throw new NotImplementedException();
                        }
                        else
                        {
                            /// just normal.
                            var word = new Word() { customBody = new List(body.cells) };
                            word.customBody.cells.Remove(s);
                            dict.Add(signature, word);
                            evaluationStack.Push(word);
                            annotation = Annotation.DoneCreation;
                        }
                        break;

                    case Annotation.InitGuard:
                        throw new NotImplementedException();
                        break;
                }
            }
            //catch (Add op) { }
            //catch (Div op) { }
            //catch (Mul op) { }
            //catch (Sub op) { }

            catch (Integer i) { evaluationStack.Push(i); }
            catch (Double i) { evaluationStack.Push(i); }
            catch (Float i) { evaluationStack.Push(i); }
            catch (BString i) { evaluationStack.Push(i); }
            catch (Boolean i) { evaluationStack.Push(i); }
        }

        #endregion

        #region Primitives 

        public static void Dup(Stack<Exception> e, Dictionary<string,Word> dict)
        {
            e.Push(e.Peek());
        }

        #endregion

        #region Word

        public class Word : Exception
        {
            public bool isPrimitive = false;
            public List customBody { get; set; }
            public Action<Stack<Exception>, Dictionary<string, Word>> primitiveBody;

            public override string ToString()
            {
                return string.Format("word@{0}", base.GetHashCode().ToString()) ;
            }
        }

        #endregion

        #region Build Expression

        private Stack<Exception> BuildExpression(List<Token> tokens)
        {
            foreach (var token in tokens)
                switch (token.type)
                {
                    #region () []
                    case "separator":
                        switch (token.value)
                        {
                            case "(": NewStackFrame(); break;
                            case ")": CloseStackFrameToList(); break;
                            default: evaluationStack.Push(InsertPrimitive(token)); break;
                        }
                        break;
                    #endregion

                    default:
                        var value = InsertPrimitive(token);
                        evaluationStack.Push(value);
                        print("stack < {0}:'{1}'\n", value.GetType().Name, Formatter(value));
                        break;
                }
            /// Validate expression:
            if (_open != _close) throw new Exception("'(' open not equal to close ')' !");
            else
            {
                var built = new Stack<Exception>();
                var total = _open + 1;
                note("built total {0} expression{1}.\n", _open+1, _open > 1 ? "s" : "");
                _open = _close = -1; /// Clear state.
                while(total > 0)
                {
                    built.Push(evaluationStack.Pop());
                    total--;
                }
                return built;
            }
        }
        
        #endregion

        #region AST

        /// Compiler                              
        public class CompilerSleep : Exception { }
        public class CompilerAwake : Exception { }

        /// Specials                              
        public class Attractor : Exception { }
        public class Guard : Exception { }
        public class Arrow : Exception { }
        public class Backward : Exception { }

        /// Operations
        public class Add : Exception { }
        public class Sub : Exception { }
        public class Mul : Exception { }
        public class Div : Exception { }
        
        /// Boolean
        public class IsEqual : Exception { }
        public class LesserThan : Exception { }
        public class GreaterThan : Exception { }
        public class LesserThanEqual : Exception { }
        public class GreaterThanEqual : Exception { }
        public class Or : Exception { }
        public class And : Exception { }

        /// Values
        public class Integer : Exception { public int value { get; set; } }
        public class Double : Exception { public double value { get; set; } }
        public class Float : Exception { public float value { get; set; } }
        public class Boolean : Exception { public bool value { get; set; } }
        public class BString : Exception { public string value { get; set; } }
        public class Symbol : Exception { public string name { get; set; } }

        public class Empty : Exception { }
        public class EmptyList : Exception { }

        public Exception InsertPrimitive(Token token)
        {
            var value = token.value;
            switch (token.type)
            {
                case "separator":
                    switch (token.value)
                    {
                        case "[": return new CompilerSleep();
                        case "]": return new CompilerAwake();
                        default: throw new NotImplementedException();
                    }
                    break;
                case "attractor": return new Attractor();
                case "guard": return new Guard();
                case "arrow": return new Arrow();
                case "backward": return new Backward();
                                                                        
                case "operator":
                    switch (token.value)
                    {
                        case "+": return new Add();
                        case "-": return new Sub();
                        case "*": return new Mul();
                        case "/": return new Div();
                        default: throw new NotImplementedException();
                    }
                case "boolean_compare":
                    switch (token.value)
                    {
                        case ">": return new GreaterThan();
                        case "<": return new LesserThan();
                        case "=?": return new IsEqual();
                        case ">=": return new GreaterThanEqual();
                        case "<=": return new LesserThanEqual();
                        default: throw new NotImplementedException();
                    }
                case "boolean_condition":
                    switch (token.value)
                    {
                        case "||": return new And();
                        case "&&": return new Or();                  
                        default: throw new NotImplementedException();
                    }

                #region Values
                case "Int32": return new Integer() { value = Int32.Parse(value) };
                case "double": return new Double() {
                        value = double.Parse(value.Substring(0, value.Length - 1))
                    };
                case "float":return new Float() {
                        value = float.Parse(value.Substring(0, value.Length - 1))
                    };
                case "bool": return new Boolean() { value = value == "true" };
                case "string": return new BString() { value = value };
                case "symbol": return new Symbol() { name = value };

                case "empty": return new Empty();
                case "empty_list": return new EmptyList();
                #endregion
                
                default:
                    print("can't find so -> symbol {0} - '{1}'\n", token.type, value);
                    return new Symbol() { name = value };
                    //throw new NotImplementedException(token.type);
            }
        }

        #endregion
                
        #region Formatter

        public static string Formatter(Exception e)
        {
            try { throw e; }

            #region Data
            catch (Integer n) { return string.Format("{0}", n.value); }
            catch (Float n) { return string.Format("{0}f", n.value); }
            catch (Double n) { return string.Format("{0}d", n.value); }
            catch (BString n) { return string.Format("{0}", n.value); }
            catch (Boolean n) { return string.Format("{0}", n.value); }
            catch (Symbol n) { return string.Format("{0}", n.name); }
            catch (List n) { return string.Format("{0}", n.ToString()); }
            catch (EmptyList n) { return "[]"; }
            catch (Empty n) { return "_"; }
            #endregion

            /// Specials
            catch (CompilerSleep n) { return "sleep"; }
            catch (CompilerAwake n) { return "awake"; }

            catch (Attractor a) { return ":"; }
            catch (Guard g) { return "|"; }
            catch (Arrow a) { return "->"; }
            catch (Backward b) { return " <<"; }

            catch (Word w) { return w.ToString(); }

            /// Operations                                               
            catch (Add n) { return "+"; }
            catch (Sub n) { return "-"; }
            catch (Mul n) { return "*"; }
            catch (Div n) { return "/"; }
            catch (GreaterThan n) { return ">"; }
            catch (LesserThan n) { return "<"; }
            catch (GreaterThanEqual n) { return ">="; }
            catch (LesserThanEqual n) { return "<="; }
            catch (IsEqual n) { return "=?"; }

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
            // empty
            @"(?<empty>\\_)|" +
            // empty list
            @"(?<empty_list>\[\])|" +
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
            @"(?<operator>\+|\-|\*|\/|\^)|" +

            // boolean
            @"(?<boolean_compare>\>|\<|\=\?|\>=|\<=)|" +
            @"(?<boolean_condition>\|\||\&\&)|" +
            // guard
            @"(?<guard>\|)|" +

            // #Comments
            @"(?<comment>\=\=.*\n)|" +

            // symbol   [a-zA-Z0-9\\_\<\>\[\]\-$_.]
            @"(?<symbol>[a-zA-Z0-9\\_\<\>\/\[\],\-$_.]+)|" +

            // the rest.
            @"(?<invalid>[^\s]+)";
        #endregion
           
        #region REPL

        public void Repl(string source)
        {
            var tokens = Reader(source);

            var built = BuildExpression(tokens);

            foreach (List expression in built)
            {
                Evaluate(expression, definedWords);   
            }
            PrintStack();
        }

        public void PrintStack()
        {
            Console.Write("[ ");
            foreach (var e in evaluationStack)
            {
                var acc = Formatter(e as Exception);
                Console.Write("{0} ", acc);
            }
            Console.Write(" ]");
        }

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
        private static ConsoleColor _consoleColor = ConsoleColor.Green;
        private static void print(string line, params object[] args)
        {
            var backup = Console.ForegroundColor;
            Console.ForegroundColor = _consoleColor;
            Console.Write(line, args);
            Console.ForegroundColor = backup;
        }
        private static void note(string line, params object[] args)
        {
            _consoleColor = ConsoleColor.Green;
            print(line, args);
        }
        private static void warning(string line, params object[] args)
        {
            _consoleColor = ConsoleColor.Yellow;
            print(line, args);
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
        public class List : Exception
        {
            public List<Exception> cells { get; set; }
            public List(Exception[] collection, bool isReversed = false)
            {
                cells = new List<Exception>();
                // reverse mode.
                if (isReversed) cells.AddRange(collection);
                else // reserved mode.
                    for (int i = collection.Length - 1; i > -1; i--)
                        cells.Add(collection[i]);
            }

            public List(IEnumerable<Exception> collection)
            {
                cells = new List<Exception>(collection);
            }

            public override string ToString()
            {
                var acc = new StringBuilder();
                acc.Append("[");
                foreach (var e in cells)
                {
                    acc.Append(string.Format(
                        "{0} ", Formatter(e)));
                }
                acc.Append("]");
                return acc.ToString();
            }
        }

        #endregion
                
        #region Compiler Mode / Stack / Words

        public enum CompilerMode
        {
            Awake, Sleep
        }
        public CompilerMode compilerMode = CompilerMode.Awake; // by default. //
        public Stack<CompilerMode> compilerState = new Stack<CompilerMode>();
        public Stack<Exception> evaluationStack = new Stack<Exception>();
        public Stack<Stack<Exception>> Frames = new Stack<Stack<Exception>>();
        public Dictionary<string, Word> definedWords = new Dictionary<string, Word>()
        {
            { "dup", new Word() { isPrimitive = true, primitiveBody = Dup } }
        };

        #endregion

        #region Stack Frame

        private int _open = -1;
        private int _close = -1;

        public void NewStackFrame()
        {
            /// create new stack frame.
            Frames.Push(evaluationStack);
            evaluationStack = new Stack<Exception>();
            _open++;
        }

        public void CloseStackFrameToList()
        {
            if (_open <= _close) throw new Exception("Lacking open (..)");
            /// acc all current stack frame into a list.
            var lst = new List(evaluationStack.ToArray());
            evaluationStack = Frames.Pop();
            evaluationStack.Push(lst);
            _close++;
        }

        public void AppendStackFrameToStack()
        {
            var store = Frames.Pop();
            foreach (var o in evaluationStack)
            {
                store.Push(o);
            }
            evaluationStack = store;
        }

        #endregion

        #region Compiler State
                     
        private void ChangeCompilerState(CompilerMode next_state = CompilerMode.Sleep)
        {
            // store current state.
            compilerState.Push(compilerMode);
            compilerMode = next_state;
            note("<compiler_{0}>\n", compilerMode.ToString().ToLower());
        }

        private void RestoreCompilerState(bool isForced = false)
        {
            if (isForced)
            {
                compilerMode = CompilerMode.Awake;
                compilerState.Clear();           
                return;
            }
            else if (compilerState.Count > 0)
            {
                compilerMode = compilerState.Pop();
            }
            else
            {
                compilerMode = CompilerMode.Awake;     
            }                                      
            note("<compiler_{0}>\n", compilerMode.ToString().ToLower());
        }

        #endregion
    }
}
