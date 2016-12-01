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
    public class UForth
    {
        #region Init 

        public UForth()
        {
            _print(" uForth :: a minimal lisp family language compiler ");
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
            @"(?<comment>#.*\n)|" +

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

        #region List

        public class List: List<object> {

            public List(object[] collection, bool isReversed = false)
            {
                // reverse mode.
                if (isReversed) this.AddRange(collection);
                else // reserved mode.
                for (int i = collection.Length - 1; i > -1; i--)
                    this.Add(collection[i]);
            }

            public List(IEnumerable<object> collection):base(collection)
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

        #region Function

        public abstract class Function
        {
            public Type[] Signature;
            public Function(Type[] signature)
            {
                Signature = signature;
            }
            public bool TypeCheck(Stack<object> frame)
            {
                var isOk = false;
                var args = new object[Signature.Length];
                frame.CopyTo(args, frame.Count-Signature.Length);

                for(int i = Signature.Length-1; i > 0 ; i--)
                {
                    isOk = Signature[i] == args[i].GetType();
                    if (!isOk) return isOk;
                }
                return isOk;
            }
            public abstract object Eval(Stack<object> frame);
        }

        #region Operators

        enum Operator
        {
            Add, Sub, Div, Mul,
            AddSelf, SubSelf, DivSelf, MulSelf
        }

        static T BuildOperator<T>(T a, T b, Operator op)
        {
            //TODO: re-use delegate!
            // declare the parameters
            ParameterExpression paramA = Expression.Parameter(typeof(T), "a"),
                paramB = Expression.Parameter(typeof(T), "b");
            // add the parameters together
            BinaryExpression body;
            switch (op)
            {
                case Operator.Add: body = Expression.Add(paramA, paramB); break;
                case Operator.Sub: body = Expression.Subtract(paramA, paramB); break;
                case Operator.Div: body = Expression.Divide(paramA, paramB); break;
                case Operator.Mul: body = Expression.Multiply(paramA, paramB); break;
                case Operator.AddSelf: body = Expression.AddAssign(paramA, paramB); break;
                case Operator.SubSelf: body = Expression.SubtractAssign(paramA, paramB); break;
                case Operator.MulSelf: body = Expression.MultiplyAssign(paramA, paramB); break;
                case Operator.DivSelf: body = Expression.DivideAssign(paramA, paramB); break;
                default: throw new NotImplementedException();
            }
            // compile it
            Func<T, T, T> f = Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
            // call it
            return f(a, b);
        }

        private static object ReturnBinary(Operator op, Stack<object> frame)
        {
            // a b -> b a
            var b = frame.Pop();
            var a = frame.Pop();
            if (a.GetType() == typeof(int))
            {
                return BuildOperator<int>((int)a, (int)b, op);
            }
            else if (a.GetType() == typeof(float))
            {
                return BuildOperator<float>((float)a, (float)b, op);
            }
            else if (a.GetType() == typeof(double))
            {
                return BuildOperator<double>((double)a, (double)b, op);
            }
            throw new NotImplementedException();
        }

        public class Add : Function
        {
            public Add(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame)
            {
                return ReturnBinary(Operator.Add, frame);
            }
        }
        public class Sub : Function
        {
            public Sub(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame)
            {
                return ReturnBinary(Operator.Sub, frame);
            }
        }
        public class Mul : Function
        {
            public Mul(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame)
            {
                return ReturnBinary(Operator.Mul, frame);
            }
        }
        public class Div : Function
        {
            public Div(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame)
            {
                return ReturnBinary(Operator.Div, frame);
            }
        }

        #endregion

        public class TypeQuestion : Function
        {
            public TypeQuestion(Type[] signature) : base(signature)   {  }

            public override object Eval(Stack<object> frame)
            {
                var name = frame.Pop().GetType().Name;
                return name;
            }
        }

        public class Flush : Function
        {
            public Flush(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame)
            {
                frame.Clear();
                return null;
            }
        }

        public class Drop : Function
        {
            public Drop(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame)
            {
                frame.Pop();
                return null;
            }
        };

        public class Dup : Function
        {
            public Dup(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame)
            {
                var o = frame.Peek();
                return o;
            }
        };

        public class Pop : Function
        {
            public Pop(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame)
            {
                return frame.Pop();
            }
        }

        public class Exit : Function
        {
            public Exit(Type[] signature) : base(signature){}

            public override object Eval(Stack<object> frame)
            {
                Environment.Exit(0);
                return null;
            }
        }

        #endregion

        #region Typing

        public object BuildValueType(Token token)
        {
            switch (token.Name)
            {
                case "string": return token.Value;
                case "integer": return Int32.Parse(token.Value);
                case "double": return double.Parse(token.Value);
                case "float": return float.Parse(
                        token.Value.ToString().Substring(0,
                        token.Value.ToString().Length - 1));
                case "symbol":
                    return new Atom(token.Name,
                                    token.Value.Substring(1, token.Value.Length - 1));
                default: return new Atom(token.Name, token.Value);
            }
        }

        #endregion

        #region Data / Modes

        public enum CompilerMode
        {
            Awake, Sleep
        }
        public CompilerMode compilerMode = CompilerMode.Awake; // by default. //
        public Stack<object> evaluationStack = new Stack<object>();
        public Stack<Stack<object>> Frames = new Stack<Stack<object>>();
        public Dictionary<string, object> userVars = new Dictionary<string, object>();
        public Dictionary<string, Function> userFunctions = new Dictionary<string, Function>();
        public Dictionary<string, Function> functionMap =
            new Dictionary<string, Function>()
            {
                
                // operators
                { "add", new Add(new Type[] { typeof(object), typeof(object)})},
                { "sub", new Sub(new Type[] { typeof(object), typeof(object)})},
                { "div", new Div(new Type[] { typeof(object), typeof(object)})},
                { "mul", new Mul(new Type[] { typeof(object), typeof(object)})},
                
                // typing
                { "type?", new TypeQuestion(new Type[] {typeof(object)})},
                
                // destructor
                { "flush", new Flush(null) },
                { "drop", new Drop(null) },
                { "dup", new Dup(null) },
                { "pop", new Pop(null) },

                // interpreter functions
                { "exit", new Exit(null)}
            };

        #endregion

        #region Repl

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
            foreach(var o in evaluationStack)
            {
                store.Push(o);
            }
            evaluationStack = store;
        }

        private void EatToken(Token token)
        {
            switch (token.Name)
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
                    switch (token.Value)
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

                //case "operator":
                case "literal":
                    if (functionMap.ContainsKey(token.Value))
                    {
                        switch (compilerMode)
                        {
                            case CompilerMode.Awake: // eval.
                                var result = functionMap[token.Value].Eval(evaluationStack);
                                if(result!=null) evaluationStack.Push(result); // push result >> stack.
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
