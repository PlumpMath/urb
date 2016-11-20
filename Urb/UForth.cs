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

        private static string SourceEnforce(object[] args, int index)
        {
            return args[index].GetType().IsSubclassOf(typeof(Functional)) ?
                   ((Functional)args[index]).CompileToCSharp()
                                  : (string)((Atom)args[index]).value;
        }


        private static void _print(string line, params object[] args)
        {
            Console.Write(line, args);
        }

        #endregion

        #region Lexer
        private Token[] _token_array;
        private int _token_index = -1;

        // Well, I think we should play dirty :P 
        public List<Block> Lexer(List<Token> token_list, bool isDebugTransform = false)
        {
            if (isDebugTransform) Console.WriteLine("Lexing..");

            /// Lexing Start... ///

            var acc = new List<Token>();
            _token_array = token_list.ToArray();
            if (isDebugTransform) Console.WriteLine("Token List Length: {0}", _token_array.Length);

            ///////////////////////////////////////////
            ///                                     ///
            /// Eat up all tokens and processing... ///
            ///                                     ///
            ///////////////////////////////////////////
            TransformTokens(_token_array, acc, isDebugTransform);
            var functions = _refineExpressions(_expressions);
            return functions;
        }

        private string _transformIntoCSharp(List<Block> blocks, bool isDebugTransform = false)
        {
            foreach (var block in blocks)
            {
                /* NOT AVAILABLE NOW */
                //AddSource(block.CompileToCSharp());
            }
            /////////////////////////////////////////
            ///                                   ///
            /// Print transformed C# source code. ///
            ///                                   ///
            /////////////////////////////////////////
            if (isDebugTransform) Console.WriteLine("\n\n[Transformed C#] \n");
            var csharp_source = new StringBuilder();
            foreach (var line in _csharp_blocks)
            {
                if (isDebugTransform)
                    Console.WriteLine(line);
                csharp_source.Append(line);
                csharp_source.AppendLine();
            }
            return csharp_source.ToString();
        }

        #endregion

        #region Token Transformation

        private Token PeakNextToken(int steps = 0)
        {
            return _token_array[_token_index + steps];
        }

        private static List<string> references = new List<string>();
        private void TransformTokens(Token[] tokens, List<Token> acc, bool isDebugTransform)
        {
            _print("\nBuilding expressions from {0} tokens...\n", tokens.Length);
            _transformerIndex = 0;
            _expressions = new List<Block>();

            while (_transformerIndex < tokens.Length - 1)
            {
                // build expression: //
                var e = BuildExpression(tokens, _transformerIndex);
                // accumulate all expressions: //
                if (e != null) _expressions.Add(e);
            }

            // Here we have a full parsed tree ! //
        }

        private int _open = 0;
        private int _close = 0;
        private List<Block> _expressions = new List<Block>();

        private Block BuildExpression(Token[] tokens, int index)
        {
            var acc = new List<object>();
            var i = index;
            _transformerIndex = index;
            while (i < tokens.Length)
            {
                switch (tokens[i].Value)
                {
                    //case "]":
                    case ")":
                        _close++;
                        _print(" )");
                        //_print(" end#{0} \n", _transformerIndex);
                        _transformerIndex = i + 1;
                        //return new Block(acc.ToArray(), isList: tokens[i].Value == "]");
                        return new Block(acc.ToArray());
                    //case "[":
                    case "(":
                        _open++;
                        _print("\n{0}#(", _expressions.Count);
                        var e = BuildExpression(tokens, i + 1);
                        //e.isList = tokens[i].Value == "[";
                        if (_open == _close) return e;
                        // else just keep adding.. //
                        acc.Add(e);
                        i = _transformerIndex;
                        continue;

                    case "!":
                    case "@":
                    // what to do here ? //

                    default:
                        /* Skip them all. */
                        if (tokens[i].Name == "newline" ||
                           tokens[i].Name == "comment")
                        {
                            i++;
                            continue;
                        }
                        // except special separator we eat all //
                        acc.Add(tokens[i]);
                        /************************************
                         *                                  *
                         * Would we transform token here ?  *
                         *                                  *
                         ************************************/
                        _print(" {0}", tokens[i].Value);
                        break;
                }
                // cached index. //
                _transformerIndex = i;
                i++;
            }
            if (_open != _close)
                throw new Exception(string.Format(
                    "Not balanced parentheses at {0}!", _transformerIndex));
            // just a null expression. //
            return null;
        }

        #endregion

        #region Code Transformation

        private static bool _isLiteralForm = false;
        private static int _nestedLevel = 0;
        private class LiteralForm : Functional
        {
            public bool isSingleStatement = false;
            private string _functionLiteral;
            public LiteralForm() : base() { }
            public void Init(Atom function)
            {
                _functionLiteral = function.value.ToString();
            }
            public override string CompileToCSharp()
            {
                _isLiteralForm = true;
                if (!isSingleStatement) _nestedLevel++;
                var acc = new StringBuilder();
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i].GetType() == typeof(Atom) ?
                        " " + ((Atom)args[i]).ToString() :
                                     SourceEnforce(args, i);
                    // the commas part. 
                    var comma = (i + 1 < args.Length ? ", " : "");
                    acc.Append(arg + comma);
                }
                return string.Format(
                    _nTimes("    ", _nestedLevel) +
                    " {0} ({1})" + (isSingleStatement ? ";" : ""),
                    _functionLiteral, acc.ToString());
            }
        }

        private static string _nTimes(string ch, int time)
        {
            var acc = "";
            for (int i = 1; i < time; i++) acc += ch;
            return acc;
        }

        private static object _insertElement(object element, Block block)
        {
            var cast = (Token)element;
            // primitives form: //
            if (_primitiveForms.ContainsKey(cast.Value))
            {
                ///////////////////////////////////////////////////
                //                                               //
                // We have a evaluation stack already.           //
                // so this rev/rest seem to be not in-need.      //
                // along with block to have overload enforcement.//
                // 
                // 
                //                                               //
                ///////////////////////////////////////////////////
                return
                    (Functional)Activator.CreateInstance(
                        _primitiveForms[cast.Value]);
            }
            // special form: //
            else if (_specialForms.ContainsKey(cast.Value))
            {
                return
                    (Functional)Activator.CreateInstance(
                        _specialForms[cast.Value]);
            }
            // literal or atoms //
            else
            {
                return _buildAtom(cast);
            }
        }

        private static Block _insertPrimitives(Block block)
        {
            var acc = new List<object>();
            foreach (var element in block.elements)
            {
                // A Block //
                if (element.GetType() == typeof(Block))
                {
                    acc.Add(
                        _insertPrimitives((Block)element));

                }
                // An Atom //
                else if (element.GetType() == typeof(Token))
                {
                    acc.Add(
                        _insertElement(element, block));
                }
                // An List ? //
            }
            return new Block(acc.ToArray());
        }

        private static Atom _buildAtom(Token token)
        {
            switch (token.Name)
            {
                case "symbol":
                    return new Atom(token.Name,
                                    token.Value.Substring(1, token.Value.Length - 1));
                case "integer": return new Atom("Int32", Int32.Parse(token.Value));
                case "float":
                    var number = float.Parse(token.Value.ToString().Substring(0,
                      token.Value.ToString().Length - 1));
                    return new Atom("float", number);

                default: return new Atom(token.Name, token.Value);
            }
        }

        private List<Block> _refineExpressions(List<Block> expressions)
        {
            /******************************************
             *                                        *
             * Phrase 2: Refine Expressions           *
             * Here we refactor tokenized expression. *
             *                                        *
             ******************************************/
            var result = new List<Block>();
            foreach (var expression in expressions)
            {
                // continue building expression tree //
                var refinedExpression = _insertPrimitives(expression);
                result.Add(refinedExpression);
            }
            return result;
        }

        public List<Block> BuildFunctionalTree
        (string source, bool isDebugTransform = false, bool isDebugGrammar = false)
        {
            // We need to eat the token here with a Lexer !
            var token_list = Reader(source, isDebugTransform, isDebugGrammar);
            var tree = Lexer(token_list, isDebugTransform);
            return tree;
        }

        public string CompileIntoCSharp
        (string source, bool isDebugTransform = false, bool isDebugGrammar = false)
        {

            // We need to eat the token here with a Lexer !
            var token_list = Reader(source, isDebugTransform, isDebugGrammar);
            var tree = Lexer(token_list, isDebugTransform);
            var csharp_source = _transformIntoCSharp(tree, isDebugTransform);
            return csharp_source;
        }

        private int _transformerIndex = 0;

        private delegate Functional CreateFunction(object[] args);

        public abstract class Functional
        {
            public object[] args;

            public Functional() { }

            //public abstract object Eval();

            public abstract string CompileToCSharp();

            public override string ToString()
            {
                return string.Format("{0}", this.GetType().Name);
            }
        }

        public class Block
        {
            //public bool isList = false;
            public object[] elements;
            public object[] rest
            {
                get
                {
                    if (elements == null || elements.Length == 0) return null;
                    var lst = new List<object>(elements);
                    lst.Remove(elements[0]);
                    return lst.ToArray();
                }
            }
            public object[] revRest
            {
                get
                {
                    if (elements == null || elements.Length == 0) return null;
                    var lst = new List<object>(elements);
                    lst.Remove(elements[elements.Length - 1]);
                    return lst.ToArray();
                }
            }
            public object head
            {
                get
                {
                    return elements.Length > 0 ? elements[0] : null;
                }
            }
            public object last
            {
                get
                {
                    return elements.Length > 0 ? elements[elements.Length - 1] : null;
                }
            }
            public Block(object[] _elements, bool isList = false)
            {
                elements = _elements;
                //this.isList = isList;
            }

            public override string ToString()
            {
                var acc = "";
                foreach (var obj in elements) acc += obj.ToString() + " ";
                return string.Format("({0})", acc);
            }
        }

        public class List
        {
            public List<object> elements;
            public List(object[] args)
            {
                foreach (var arg in args)
                {
                    elements.Add(arg);
                }
            }
        }

        #endregion

        #region Primitive & Special Forms

        private static Dictionary<string, Type> _specialForms =
            new Dictionary<string, Type>()
            {
            {"def", typeof(DefForm)}
            };

        // primitive functions map //
        private static Dictionary<string, Type> _primitiveForms =
            new Dictionary<string, Type>()
            {
            {"require",  typeof(RequireForm)},
            {"import",  typeof(ImportForm)},
            {"inherit", typeof(InheritForm)},
            {"class", typeof(ClassForm)},
            {"endclass", typeof(EndClassForm)},
            {"new", typeof(NewForm)},
            {"set", typeof(SetForm)},

            {"return", typeof(ReturnForm)},
            {"label", typeof(LabelForm)},
            {"var", typeof(VarForm)},
            {"if", typeof(IfForm)},
            {"and", typeof(AndForm)},
            {"or", typeof(OrForm)},
            {"=", typeof(AssignmentForm)},
            {"/=", typeof(DivideSelfOperatorForm)},
            {"*=", typeof(MultiplySelfOperatorForm)},
            {"+=", typeof(AddSelfOperatorForm)},
            {"-=", typeof(SubSelfOperatorForm)},
            {"+", typeof(AddOperatorForm)},
            {"-", typeof(SubOperatorForm)},
            {"*", typeof(MultiplyOperatorForm)},
            {"/", typeof(DivideOperatorForm)},
            {"<", typeof(LesserOperatorForm)},
            {">", typeof(BiggerOperatorForm)},
            {"==", typeof(EqualOperatorForm)},
            {"<=", typeof(LesserEqualOperatorForm)},
            {">=", typeof(BiggerEqualOperatorForm)},
            {"jump", typeof(JumpDirectiveForm)},
            };

        private class RequireForm : Functional
        {
            public RequireForm() : base() { }
            public override string CompileToCSharp()
            {
                var ns = ((Atom)args[0]).ToString();
                references.Add(ns);
                return String.Format("using {0};", ns);
            }
        }

        private class ImportForm : Functional
        {
            public ImportForm() : base() { }
            public override string CompileToCSharp()
            {
                return String.Format("using {0};", (Atom)args[0]);
            }
        }

        private class InheritForm : Functional
        {
            public InheritForm() : base() { }
            public override string CompileToCSharp()
            {
                string _targets = "";
                if (args.Length > 2)
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        _targets += ((Atom)args[i]).ToString();
                        // Peak of next element existence. //
                        if (i + 1 < args.Length) _targets += ", ";
                    }
                }
                else {
                    _targets = ((Atom)args[1]).ToString();
                }
                return String.Format("{0} : {1}", (Atom)args[0], _targets);
            }
        }

        private static string DefineClass(object[] args, bool isStatic = false)
        {
            /*************************
                 * 
                 * Class Form:
                 * 
                 * 1. policy.
                 * 2. name/inherit.
                 * 3. body.
                 * 
                 *************************/
            var name = "";
            var policy = "";
            switch (args.Length)
            {
                case 3: // policy + name. //
                    policy = ((Atom)args[0]).ToString();
                    name = SourceEnforce(args, 1);
                    break;
                case 2: // ignore policy. //
                    name = SourceEnforce(args, 0);
                    break;
                default: throw new Exception("Malform Class");
            }
            var _static = isStatic ? "static" : "";
            var title = args.Length > 2 ?
                            String.Format("{0} {1} class {2}", policy, _static, name) :
                            String.Format("{0} class {1}", _static, name);
            var body = (Functional)args[args.Length - 1];
            /* adding newline before new class */
            return String.Format("\n{0}\n{1}", title, body.CompileToCSharp());

        }

        private class ClassForm : Functional
        {
            public ClassForm() : base() { }
            public override string CompileToCSharp()
            {
                return DefineClass(args);
            }
        }

        private class EndClassForm : Functional
        {
            public EndClassForm() : base() { }
            public override string CompileToCSharp()
            {
                return "}";
            }
        }

        private class NewForm : Functional
        {
            public StringBuilder type = new StringBuilder();
            public NewForm() : base()
            {
            }
            public override string CompileToCSharp()
            {
                var constructorArgs = new StringBuilder();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].GetType() != typeof(Atom))
                        // append all constructor args //
                        constructorArgs.Append(
                            ((Functional)args[i])
                                .CompileToCSharp() +
                            (i + 1 < args.Length ? ", " : ""));
                    else
                    {
                        type.Append((Atom)args[i]);
                    }
                }
                return string.Format("new {0} ({1})", type.ToString(), constructorArgs.ToString());
            }
        }

        private static string SetVariable(object[] args, bool isStatic = false)
        {
            // 3 args: policy, name & binding.
            var policy = args.Length == 3 ? ((Atom)args[0]).ToString() : "";
            var attribute = isStatic ? "static" : "";
            var type = "";
            var name = ((Atom)args[args.Length - 2]).ToString();
            var binding = "";
            if (args[args.Length - 1].GetType() == typeof(NewForm))
            {
                type = ((NewForm)args[args.Length - 1]).type.ToString();
                binding = ((NewForm)args[args.Length - 1]).CompileToCSharp();
            }
            else // just value //
            {
                type = ((Atom)args[args.Length - 1]).value.GetType().Name;
                binding = ((Atom)args[args.Length - 1]).ToString();
            }
            return String.Format("{0} {1} {2} {3} = {4};",
                policy, attribute, type, name, binding);

        }

        private class SetForm : Functional
        {
            public SetForm() : base() { }
            public override string CompileToCSharp()
            {
                return SetVariable(args);
            }
        }


        private static string[] Pair(Atom atom)
        {
            return atom.value.ToString().Split(new char[] { ':' });
        }

        private static string BuildMethod
        (object[] args, bool isStatic = false, bool isOverride = false)
        {
            /**************************************************
             * 
             * Defun:
             * 
             * 1. optional policy: private/protected/public.
             * 2. optional attributes: static/overload. 
             * 3. name:return-type.
             * 4. args.
             * last. body.
             * 
             **************************************************/
            var first = (Atom)args[0];
            var policy = first.type == "symbol" ? first.value.ToString() : "";
            var attribute = isStatic ? "static" : isOverride ? "override" : "";
            var pair = Pair(policy == String.Empty ? first : ((Atom)args[1]));
            var name = pair[0];
            var returnType = pair[1];
            var arguments = new StringBuilder();
            if (policy == string.Empty && args.Length > 2 ||
               policy != String.Empty && args.Length > 3)
                for (int i = policy == string.Empty ? 1 : 2;
                     i < args.Length - 1; i++)
                {
                    var argPair = Pair(((Atom)args[i]));
                    arguments.Append(string.Format(
                        "{0} {1}" + (i + 1 < args.Length - 1 ? ", " : ""),
                        argPair[1], argPair[0]));
                }
            var body = ((Functional)args[args.Length - 1]).CompileToCSharp();
            var commit = new string[] {
                policy,
                attribute,
                returnType == "ctor" ? "" : returnType,
                name,
                arguments.ToString(),
                body
            };
            /* we add newline before new function. */
            return String.Format("\n{0} {1} {2} {3} ({4}){5}", commit);

        }

        private class DefForm : Functional
        {
            public DefForm() : base() { }
            public bool isStatic = false;
            public override string CompileToCSharp()
            {
                return BuildMethod(args);
            }
        }


        private class ReturnForm : Functional
        {
            public ReturnForm() : base() { }
            public override string CompileToCSharp()
            {
                return String.Format("return {0};", SourceEnforce(args, 0));
            }
        }

        private class LabelForm : Functional
        {
            public LabelForm() : base() { }
            public override string CompileToCSharp()
            {
                return String.Format("{0}:", (Atom)args[0]);
            }
        }

        private class VarForm : Functional
        {
            public VarForm() : base() { }
            public override string CompileToCSharp()
            {
                var name = ((Atom)args[0]).ToString();
                var value = args[1].GetType() == typeof(Atom) ?
                    ((Atom)args[1]).value : SourceEnforce(args, 1);
                return String.Format("var {0} = {1};", name, value);
            }
        }

        private class IfForm : Functional
        {
            public IfForm() : base() { }
            public override string CompileToCSharp()
            {
                //_nestedLevel++;
                if (args[0].GetType() == typeof(LiteralForm))
                    ((LiteralForm)args[0]).isSingleStatement = false;

                var condition = SourceEnforce(args, 0);
                var body = SourceEnforce(args, 1);
                return String.Format("if ({0}) {{\n{1}\n}}", condition, body);
            }
        }

        private class OrForm : Functional
        {
            public OrForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("||", args, isClosure: true);
            }
        }

        private class AndForm : Functional
        {
            public AndForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("&&", args);
            }
        }

        #region Operators

        private class AssignmentForm : Functional
        {
            public AssignmentForm() : base() { }
            public override string CompileToCSharp()
            {
                var name = ((Atom)args[0]).ToString();
                var value = args[1].GetType() == typeof(Atom) ?
                    ((Atom)args[1]).value : SourceEnforce(args, 1);
                return String.Format("{0} = {1};", name, value);
            }
        }

        private static string OperatorTree(string _operator, object[] args, bool isClosure = false, bool isOnlyTwo = false)
        {
            // adding a warning about only 2 //
            if (isOnlyTwo && args.Length > 2)
            {
                throw new Exception("* warning: only 2 arguments for this operator.");
            }
            var acc = new StringBuilder();
            for (int i = 0; i < args.Length; i++)
            {
                var x =
                    args[i].GetType() == typeof(Atom) ?
                    ((Atom)args[i]).value : SourceEnforce(args, i);
                acc.Append(String.Format(
                    i + 1 < args.Length ? "{0} {1} " : "{0}", x, (i + 1 < args.Length ? _operator : "")));
            }
            return String.Format(isClosure ? "( {0} )" : "{0}", acc.ToString());
        }


        private class DivideOperatorForm : Functional
        {
            public DivideOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("/", args);
            }
        }

        private class MultiplyOperatorForm : Functional
        {
            public MultiplyOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("*", args);
            }
        }

        private class SubOperatorForm : Functional
        {
            public SubOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("-", args);
            }
        }

        private class AddOperatorForm : Functional
        {
            public AddOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("+", args);
            }
        }

        private class DivideSelfOperatorForm : Functional
        {
            public DivideSelfOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("/=", args, isOnlyTwo: true) + ";";
            }
        }

        private class MultiplySelfOperatorForm : Functional
        {
            public MultiplySelfOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("*=", args, isOnlyTwo: true) + ";";
            }
        }

        private class SubSelfOperatorForm : Functional
        {
            public SubSelfOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("-=", args, isOnlyTwo: true) + ";";
            }
        }

        private class AddSelfOperatorForm : Functional
        {
            public AddSelfOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("+=", args, isOnlyTwo: true) + ";";
            }
        }

        private class LesserOperatorForm : Functional
        {
            public LesserOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("<", args, isOnlyTwo: true);
            }
        }

        private class BiggerOperatorForm : Functional
        {
            public BiggerOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree(">", args, isOnlyTwo: true);
            }
        }

        private class EqualOperatorForm : Functional
        {
            public EqualOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("==", args, isOnlyTwo: true);
            }
        }

        private class LesserEqualOperatorForm : Functional
        {
            public LesserEqualOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree("<=", args, isOnlyTwo: true);
            }
        }

        private class BiggerEqualOperatorForm : Functional
        {
            public BiggerEqualOperatorForm() : base() { }
            public override string CompileToCSharp()
            {
                return OperatorTree(">=", args, isOnlyTwo: true);
            }
        }

        #endregion

        private class JumpDirectiveForm : Functional
        {
            public JumpDirectiveForm() : base() { }
            public override string CompileToCSharp()
            {
                return String.Format("goto {0};", (Atom)args[0]);
            }
        }

        #endregion

        #region Interpreter

        // eval stack.
        public Stack<object> EvaluationStack = new Stack<object>();


        public void EvalTree(List<Block> tree)
        {
            _print("\n" + _nTimes("_", 80) + "\n");
            _print("\n* Evaluating Tree...*\n");
            foreach (var block in tree)
            {
                _print(EvalBlock(block) + "\n");
            }
        }

        private Block Reduce(Block block)
        {
            var acc = new List<object>();
            var f = block.head;
            switch (f.GetType().Name)
            {
                case "DefForm":
                    var rest = block.rest;
                    ((DefForm)f).args = rest;
                    acc.Add(f);
                    break;
                default:
                    //if (block.isList) acc.Add(block);
                    //else
                    {
                        var flast = block.last;
                        var revRest = block.revRest;
                        ((Functional)flast).args = revRest;
                        acc.Add(flast);
                    }
                    break;
            }
            return new Block(acc.ToArray());
        }

        public string EvalBlock(Block block)
        {
            //var stack = new Stack<object>();
            var view = new StringBuilder();
            var args = new StringBuilder();
            //            var reduced = Reduce(block);
            foreach (var arg in block.elements)
            {
                //_print("{0} ", arg.GetType().Name);
                if (arg.GetType() == (typeof(Block)))
                {
                    // tree traveling... //
                    args.Append(EvalBlock(arg as Block) + " ");
                }
                else
                {
                    // where we start eval ? //
                    args.Append(arg.ToString() + " ");
                }
            }
            view.Append(String.Format("\n[ {0} ]", args.ToString()));
            return view.ToString();
        }
        public void ReplTest(string source)
        {
            EvalTree(BuildFunctionalTree(source));
            //var block = BuildFunctionalTree(source);
        }
        /****************************************
         * 
         * :: The REPL ::
         * 
         * This would be why it don't need IDE.
         * 
         * REPL mean to be evaluation of each
         * function we create, along w/ variables.
         * 
         ****************************************/
        public void ReplSession()
        {
            while (true)
            {
                _print("> ");
                /* get input */
                var _input = Console.ReadLine();
                if (_input == "quit") break;
                /* eval tree ? */

                /* 
                 * Do something here .. 
                 * like build tree and eval it.
                 */

                var _tree = BuildFunctionalTree(_input);
                //var _result = 
                EvalTree(_tree);
                //_print("=> {0}", _result == null ? "null" : _result.ToString());

                /************************************************************************ 
                 * 
                 * :: RULES :: 
                 * 
                 * 1. create new statement -> environment static function.
                 * 2. create new function -> environment partial static class.
                 * 3. create new class -> into our namespace (if not using).
                 * 4. create new namespace -> assign as current namespace.
                 *
                 *************************************************************************/
                _print("\n" + _nTimes("_", 80) + "\n\n");
            }
        }


        #endregion

        #region Compiling

        /*************************************************
         * 
         * :: COMPILER NOTE ::
         * 
         * 1) one thing is that we should compile 
         * each expression, one by one to be able to
         * interprete, debug and get intellisense 
         * in the future.
         * 
         *************************************************/

        public void Compile(string urb_source, string fileName, bool isExe = false, bool isDebugTransform = false, bool isDebugGrammar = false)
        {
            _reset_state();
            var _source = CompileIntoCSharp(urb_source, isDebugTransform, isDebugGrammar);
            _compile_csharp_source(_source, fileName, isExe);
        }

        private Assembly _compile_csharp_source(string source, string fileName, bool isExe = false, bool isInMemory = false)
        {
            var compiler_parameter = new CompilerParameters();
            compiler_parameter.GenerateExecutable = isExe;
            compiler_parameter.OutputAssembly = fileName;
            compiler_parameter.GenerateInMemory = isInMemory;
            foreach (var name in references)
                compiler_parameter.ReferencedAssemblies.Add(name);

            var compiler = new CSharpCodeProvider();

            var result = compiler.CompileAssemblyFromSource(compiler_parameter, new string[] { source });
            if (result.Errors.Count > 0)
            {
                // Display compilation errors.
                Console.WriteLine("Errors building  into {0}:",
                    result.PathToAssembly);
                foreach (CompilerError ce in result.Errors)
                {
                    Console.WriteLine("  {0}", ce.ToString());
                    Console.WriteLine();
                }
                throw new Exception("Can't compile code due to error.");
            }
            else
            {
                Console.WriteLine("Source built into {0} successfully.",
                result.PathToAssembly);

                return result.CompiledAssembly;
            }
        }

        public void CompileLoad(string source, string output)
        {
            // this part is invoked after we defined new method/class. 
            // reload our interpreter with new compiled part.
            var _csharp = CompileIntoCSharp(source);
            var _assembly = _compile_csharp_source(_csharp, output, false, true);
            AppDomain.CurrentDomain.Load(_assembly.GetName());
        }

        private void _reset_state()
        {
            _csharp_blocks.Clear();
            _expressions.Clear();
            _isLiteralForm = false;
            _nestedLevel = 0;
            _open = _close = 0;
            _token_array = null;
            _token_index = -1;
            _transformerIndex = -1;
        }

        #endregion
    }
}
