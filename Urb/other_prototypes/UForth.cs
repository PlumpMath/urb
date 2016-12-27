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
namespace Forth
{
    public class UForth
    {
        #region Init 

        public UForth()
        {
            _print(" bailey :: a minimal forth family language ");
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

            // boolean
            @"(?<boolean_compare>[\>|\<|\=|\>=|\<=])|" +
            @"(?<boolean_condition>[\|\||\&\&])|" +
            // operators
            @"(?<operator>\+=|\-=|\+|\-|\*|\/|\^)|" +

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
                return true; // fix later.
                if (frame.Count < Signature.Length) return false;
                var isOk = false;
                var args = new object[Signature.Length];
                frame.CopyTo(args, frame.Count - Signature.Length - 1);

                for (int i = Signature.Length - 1; i > 0; i--)
                {
                    isOk = Signature[i] == args[i].GetType() || Signature[i] == typeof(object);
                    if (!isOk) return isOk;
                }
                return isOk;
            }
            public abstract object Eval(Stack<object> frame, UForth evaluator);
        }

        #region Operators

        enum Operator
        {
            Add, Sub, Div, Mul,
            AddSelf, SubSelf, DivSelf, MulSelf,
            LessThan, GreaterThan, LessEqual, GreaterEqual, Equal
        }

        static bool BuildBooleanOperator<T>(T a, T b, Operator op)
        {

            //TODO: re-use delegate!
            // declare the parameters
            ParameterExpression paramA = Expression.Parameter(typeof(T), "a"),
                                paramB = Expression.Parameter(typeof(T), "b");
            BinaryExpression body;
            switch (op)
            {
                case Operator.Equal: body = Expression.Equal(paramA, paramB); break;
                case Operator.LessThan: body = Expression.LessThan(paramA, paramB); break;
                case Operator.LessEqual: body = Expression.LessThanOrEqual(paramA, paramB); break;
                case Operator.GreaterThan: body = Expression.GreaterThan(paramA, paramB); break;
                case Operator.GreaterEqual: body = Expression.GreaterThanOrEqual(paramA, paramB); break;
                default: throw new NotSupportedException();
            }
            // compile it
            Func<T, T, bool> f = Expression.Lambda<Func<T, T, bool>>(body, paramA, paramB).Compile();
            // call it
            return f(a, b);
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

        private static bool ReturnBooleanBinary(Operator op, Stack<object> frame)
        {
            // a b -> b a
            var b = frame.Pop();
            var a = frame.Pop();

            if (a is int)
            {
                return BuildBooleanOperator<int>((int)a, (int)b, op);
            }
            else if (a is float)
            {
                return BuildBooleanOperator<float>((float)a, (float)b, op);
            }
            else if (a is double)
            {
                return BuildBooleanOperator<double>((double)a, (double)b, op);
            }
            throw new NotImplementedException();
        }

        private static object ReturnBinary(Operator op, Stack<object> frame)
        {
            // a b -> b a
            var b = frame.Pop();
            var a = frame.Pop();

            if (a is int)
            {
                return BuildOperator<int>((int)a, (int)b, op);
            }
            else if (a is float)
            {
                return BuildOperator<float>((float)a, (float)b, op);
            }
            else if (a is double)
            {
                return BuildOperator<double>((double)a, (double)b, op);
            }
            throw new NotImplementedException();
        }

        public class Add : Function
        {
            public Add(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                return ReturnBinary(Operator.Add, frame);
            }
        }
        public class Sub : Function
        {
            public Sub(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                return ReturnBinary(Operator.Sub, frame);
            }
        }
        public class Mul : Function
        {
            public Mul(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                return ReturnBinary(Operator.Mul, frame);
            }
        }
        public class Div : Function
        {
            public Div(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                return ReturnBinary(Operator.Div, frame);
            }
        }
        public class LessThan : Function
        {
            public LessThan(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                return ReturnBooleanBinary(Operator.LessThan, frame);
            }
        }
        public class LessEqual : Function
        {
            public LessEqual(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                return ReturnBooleanBinary(Operator.LessEqual, frame);
            }
        }
        public class GreaterThan : Function
        {
            public GreaterThan(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                return ReturnBooleanBinary(Operator.GreaterThan, frame);
            }
        }
        public class GreaterEqual : Function
        {
            public GreaterEqual(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                return ReturnBooleanBinary(Operator.GreaterEqual, frame);
            }
        }
        public class Equal : Function
        {
            public Equal(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                return ReturnBooleanBinary(Operator.Equal, frame);
            }
        }


        #endregion

        public class If : Function
        {
            public If(Type[] signature) : base(signature) { }
            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                /// bool -> object -> object
                var wrong_expr = frame.Pop() as List;
                var right_expr = frame.Pop() as List;
                var condition = frame.Pop() as List;
                evaluator.Eval(condition, userVars);
                var result = (bool)evaluator.evaluationStack.Pop();
                return result ? right_expr : wrong_expr;
            }
        }

        public class TypeQuestion : Function
        {
            public TypeQuestion(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                var name = frame.Pop().GetType().Name;
                return name;
            }
        }

        [Serializable]
        public class Lambda
        {
            public List body;
            public List parameters;
            public string name = String.Empty;
            public Lambda(List _body, List _parameters, string _name)
            {
                this.name = _name;
                this.body = _body;
                this.parameters = _parameters;
            }
        }

        public class Defun : Function
        {

            public Defun(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                if (base.TypeCheck(frame))
                {
                    /// body params name.
                    var body = (frame.Pop() as List);
                    var parameters = (frame.Pop() as List);
                    var name = (frame.Pop() as Token).value;
                    /// override current function.
                    if (userFunctions.ContainsKey(name)) userFunctions.Remove(name);

                    userFunctions.Add(name, new Lambda(body, parameters, name));
                    /// log:
                    _print("defined function {0}.\n", name);
                    return null;
                }
                throw new NotImplementedException();
            }
        }

        public static void CreateVariable(Stack<object> frame)
        {
            /// 1 A var.
            var name = (frame.Pop() as Token).value;
            var value = frame.Pop();
            /// override.
            if (userVars.ContainsKey(name)) userVars.Remove(name);
            userVars.Add(name, value);
            /// log:
            _print("defined variable {0}.\n", name);
        }

        public class Var : Function
        {
            public Var(Type[] signature) : base(signature)
            {
            }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {

                if (base.TypeCheck(frame))
                {
                    CreateVariable(frame);
                    return null;
                }
                throw new Exception("bad typing.");
            }
        }

        public class Print : Function
        {
            public Print(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                var backup = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(frame.Pop().ToString());
                Console.ForegroundColor = backup;
                return null;
            }
        }

        public class Flush : Function
        {
            public Flush(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                frame.Clear();
                return null;
            }
        }

        public class Drop : Function
        {
            public Drop(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                frame.Pop();
                return null;
            }
        };

        public class Dup : Function
        {
            public Dup(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                var o = frame.Peek();
                return o;
            }
        };

        public class Pop : Function
        {
            public Pop(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                return frame.Pop();
            }
        }

        public class Exit : Function
        {
            public Exit(Type[] signature) : base(signature) { }

            public override object Eval(Stack<object> frame, UForth evaluator)
            {
                Environment.Exit(0);
                return null;
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

        #region Data / Modes / functionMap

        public enum CompilerMode
        {
            Awake, Sleep
        }
        public CompilerMode compilerMode = CompilerMode.Awake; // by default. //
        public Stack<CompilerMode> compilerState = new Stack<CompilerMode>();
        public Stack<object> evaluationStack = new Stack<object>();
        public Stack<Stack<object>> Frames = new Stack<Stack<object>>();
        public static Dictionary<string, object> userVars = new Dictionary<string, object>();
        public static Dictionary<string, Lambda> userFunctions = new Dictionary<string, Lambda>();
        public Dictionary<string, Function> coreFunctions =
            new Dictionary<string, Function>()
            {
                // creation
                {"save", new Defun(new Type[] { typeof(Token), typeof(List), typeof(List)})},
                {"var", new Var(new Type[] { typeof(Token), typeof(object)})},

                // operators
                { "+", new Add(new Type[] { typeof(object), typeof(object)})},
                { "-", new Sub(new Type[] { typeof(object), typeof(object)})},
                { "/", new Div(new Type[] { typeof(object), typeof(object)})},
                { "*", new Mul(new Type[] { typeof(object), typeof(object)})},
                { ">", new GreaterThan(new Type[] { typeof(object), typeof(object)})},
                { "<", new LessThan(new Type[] { typeof(object), typeof(object)})},
                { ">=", new GreaterEqual(new Type[] { typeof(object), typeof(object)})},
                { "<=", new LessEqual(new Type[] { typeof(object), typeof(object)})},
                { "=", new Equal(new Type[] { typeof(object), typeof(object)})},
                
                // branching
                { "if", new If(new Type[]{typeof(List), typeof(List), typeof(List)})},

                // helpers
                { "type?", new TypeQuestion(new Type[] {typeof(object)})},
                { "print", new Print(new Type[] {typeof(object) })},

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

        #region Stack Manipulators

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
                case "eval_mode":
                    RestoreCompilerState(true);
                    break;

                case "uneval_mode":
                    ChangeCompilerState(CompilerMode.Sleep);
                    break;

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

                case "backward": coreFunctions[token.value].Eval(evaluationStack, this); break;
                case "forward": break;

                case "boolean_compare":
                case "operator":
                case "literal":
                    switch (compilerMode)
                    {
                        case CompilerMode.Awake:
                            /// if it's primitives: 
                            if (coreFunctions.ContainsKey(token.value))
                            {
                                ApplyPrimitives(token, env);
                            }
                            /// if it's defined:
                            else if (userFunctions.ContainsKey(token.value))
                            {
                                ApplyUserFunction(token, env);
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

        public void ApplyUserFunction(Token token, Dictionary<string, object> env)
        {
            var localVars = new Dictionary<string, object>();
            var function = userFunctions[token.value];
            foreach (Token t in function.parameters)
            {
                var parameter = t.value;
                localVars.Add(parameter as string, null);
                localVars[parameter as string] = evaluationStack.Pop();
            }
            /// eval body:
            var body = function.body;
            /// local + env:
            var modEnv = new Dictionary<string, object>();
            modEnv = Copy(modEnv, env);
            modEnv = Copy(modEnv, localVars);
            Eval(body, modEnv);
            //modEnv.Clear();
        }

        public static Dictionary<string, object> Copy
            (Dictionary<string, object> src,
              Dictionary<string, object> des,
             bool isOverwrite = false)
        {
            foreach (var pair in des)
            {
                if (src.ContainsKey(pair.Key))
                {
                    if (isOverwrite) src.Remove(pair.Key);
                    throw new Exception("ambitious variable " + pair.Key);
                }
                src.Add(pair.Key, pair.Value);
            }
            return src;
        }

        public void Eval(List body, Dictionary<string, object> env)
        {
            foreach (var o in body)
            {
                EatToken(o as Token, env);
            }
        }

        public void ApplyPrimitives(Token token, Dictionary<string, object> env)
        {
            switch (compilerMode)
            {
                case CompilerMode.Awake: // eval.
                    var result = coreFunctions[token.value].Eval(evaluationStack, this);
                    if (result != null) evaluationStack.Push(result); // push result >> stack.
                    break;
                case CompilerMode.Sleep: // store!
                    evaluationStack.Push(token);
                    break;
                default: break;
            }
        }

        public void Repl(string source)
        {
            var tokens = Reader(source);

            foreach (var token in tokens)
            {
                EatToken(token, userVars);
            }

            PrintStack();
        }

        #endregion
    }
}
