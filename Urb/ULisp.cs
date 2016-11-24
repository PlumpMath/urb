using System;
using System.Text;
using Microsoft.CSharp;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace Urb
{
    public class ULisp
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

        public ULisp()
        {
            _print(" Urb :: a minimal lisp family language compiler ");
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
            // quote
            @"(?<quote>\@)|" +
            // forward
            @"(?<forward>\-\>)|" +
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
            @"(?<comment>;;.*\n)|" +

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


        private static void _print(string line)
        {
            Console.WriteLine(line);
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
        public List<Functional> Lexer(List<Token> token_list, bool isDebugTransform = false)
        {
            if (isDebugTransform) Console.WriteLine("Lexing..");

            /// Lexing Start... ///

            var acc = new List<Token>();
            _token_array = token_list.ToArray();
            if (isDebugTransform) Console.WriteLine("Token List Length: {0}", _token_array.Length);

            ///////////////////////////////////////////
            ///										///
            /// Eat up all tokens and processing... ///
            /// 									///
            ///////////////////////////////////////////
            TransformTokens(_token_array, acc, isDebugTransform);
            var functions = _refineExpressions(_expressions);
            return functions;
        }

        private string _transformIntoCSharp(List<Functional> functions, bool isDebugTransform = false)
        {
            foreach (var function in functions)
            {
                AddSource(function.CompileToCSharp());
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
                var e = BuildBlock(tokens, _transformerIndex);
                // accumulate all expressions: //
                if (e != null) _expressions.Add(e);
            }

            // Here we have a full parsed tree ! //
        }

        private int _open = 0;
        private int _close = 0;
        private bool _nextExpressionQuoted = false;
        private bool _nextAtomQuoted = false;
        private List<Block> _expressions = new List<Block>();

        private Block BuildBlock(Token[] tokens, int index)
        {
            var acc = new List<object>();
            var i = index;
            _transformerIndex = index;
            while (i < tokens.Length)
            {
                switch (tokens[i].Value)
                {
                    case ")":
                        _close++;
                        _print(" )");
                        //_print(" end#{0} \n", _transformerIndex);
                        _transformerIndex = i + 1;
                        var closedE = new Block(acc.ToArray(), _nextExpressionQuoted);
                        _nextExpressionQuoted = false;
                        return closedE;

                    case "(":
                        _open++;
                        _print("\n{0}#(", _expressions.Count);
                        var openE = BuildBlock(tokens, i + 1);
                        if (_open == _close) return openE;
                        // else just keep adding.. //
                        acc.Add(openE);
                        i = _transformerIndex;
                        continue;

                    case "@":
                        /*************************************
                         *                                   *
                         * 1. Quoted Expression -> List      *
                         * 2. Quoted Literal    -> Symbol    *
                         *                                   *
                         *************************************/
                        _nextExpressionQuoted = tokens[i + 1].Value == "(";
                        _nextAtomQuoted = !_nextExpressionQuoted;
                        i++;
                        break;

                    default:
                        /* Skip them all. */
                        if (tokens[i].Name == "newline" ||
                           tokens[i].Name == "comment")
                        {
                            i++;
                            continue;
                        }
                        // except special separator we eat all //
                        if (_nextAtomQuoted)
                        {
                            acc.Add(new Token("symbol", tokens[i].Value));
                        }
                        else acc.Add(tokens[i]);
                        /************************************
						 * 									*
						 * Would we transform token here ?	*
						 * 									*
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
                                                   
        private class LiteralForm : Functional
        {                                           
            private string _functionLiteral;
            public LiteralForm(object[] args) : base(args) {}
            public void Init(Atom function)
            {   
                _functionLiteral = function.value.ToString();
            }
            public override string CompileToCSharp()
            {                           
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
                    " {0} ({1})" 
                    //+ (isSingleStatement ? ";" : "")
                    ,
                    _functionLiteral, acc.ToString());
            }
        }

        private static string _nTimes(string ch, int time)
        {
            var acc = "";
            for (int i = 1; i < time; i++) acc += ch;
            return acc;
        }

        private static Functional _buildExpression(Block expression)
        {
            // We plugin all special forms here. //
            if (expression.head.GetType() == typeof(Token))
            {                       
                // transform it into primitive if possible //
                var token = (Token)expression.head;
                switch (token.Name)
                {
                    // All Primitives //
                    case "boolean_compare":
                    case "operator":
                    case "literal":
                        if (_specialForms.ContainsKey(token.Value))
                        {
                            // mean it's implemented primitive. //
                            return (Functional)Activator.CreateInstance(
                                _specialForms[token.Value],
                                new[] { expression.rest });
                        }
                        else if (_primitiveForms.ContainsKey(token.Value))
                        {
                            // mean it's implemented primitive. //
                            return (Functional)Activator.CreateInstance(
                                _primitiveForms[token.Value],
                                new[] { expression.evaluatedRest });
                        }
                        else {
                            // normal function or invoke. //   
                            var f = new LiteralForm(expression.evaluatedRest);           
                            f.Init(new Atom(token.Name, token.Value));             
                            return f;
                        }
                    //throw new NotImplementedException("Unknown form: " + token.Value);

                    default: throw new NotSupportedException(token.Name);
                }
            }
            return null;
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

        private List<Functional> _refineExpressions(List<Block> expressions)
        {
            /******************************************
			 * 									      *
			 * Phrase 2: Refine Expressions		   	  *
			 * Here we refactor tokenized expression. *
			 * 									   	  *
			 ******************************************/
            var result = new List<Functional>();
            foreach (var expression in expressions)
            {
                // continue building expression tree //
                var refinedExpression = _buildExpression(expression);
                result.Add(refinedExpression);
            }
            return result;
        }

        public List<Functional> BuildFunctionalTree
        (string source, bool isDebugTransform = false, bool isDebugGrammar = false)
        {
            _print(_nTimes("_", 80));
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
            public Functional(object[] _args)
            {
                args = _args;
            }
            //public abstract object Eval();
            public abstract string CompileToCSharp();
        }

        public class Block
        {
            public object head;
            public object[] rest;
            public List<object> elements;
            public object[] evaluatedRest
            {
                get
                {
                    var acc = new List<object>();
                    // transform all of them //
                    foreach (var element in rest)
                    {
                        if (element.GetType() == typeof(Token))
                        {
                            var e = _buildAtom((Token)element);
                            acc.Add(e);
                        }
                        else if (element.GetType() == typeof(Block))
                        {
                            var e = _buildExpression((Block)element);
                            acc.Add(e);
                        }
                    }
                    return acc.ToArray();
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

        #region Primitives
        // special form will custom-build its arguments.
        private static Dictionary<string, Type> _specialForms =
            new Dictionary<string, Type>()
            {
                {"def", typeof(DefForm)},
                {"class", typeof(ClassForm) },
                {"set", typeof(SetForm)}
            };

        // primitive form will default-build its arguments. //
        private static Dictionary<string, Type> _primitiveForms =
            new Dictionary<string, Type>()
            {
            {"require",  typeof(RequireForm)},
            {"import",  typeof(ImportForm)},
            {"inherit", typeof(InheritForm)},
            {"begin", typeof(BeginForm)},
            {"quote", typeof(QuoteForm)},
            {"new", typeof(NewForm)},
            {"override", typeof(DefoverrideForm)},
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
            public RequireForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                var ns = ((Atom)args[0]).ToString();
                references.Add(ns);
                return String.Format("using {0};", ns);
            }
        }

        private class ImportForm : Functional
        {
            public ImportForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return String.Format("using {0};", (Atom)args[0]);
            }
        }

        private class InheritForm : Functional
        {
            public InheritForm(object[] args) : base(args) { }
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
            /*****************************************************************
			 * 
			 * Class Form:
			 * 
			 *  (class (inherit ClassName Interface) 
             *         (:attributes) 
             *         (progn))
			 * 
			 *  1. name/inherit.
			 *  2. attributes.
			 *  3. body.
			 * 
			 *****************************************************************/
            var name = ((Token)args[0]).Value;
            var attributes = "";

            switch (args.Length)
            {
                case 3: // [attribute] class [name] {..} //
                    attributes = _buildAttributes(
                        ((Block)args[1]).elements);
                    break;
                case 2: // class [name] {..}             //
                    break;
                default: throw new Exception("Malform Class");
            }
            var title = args.Length == 2 ?
                String.Format("class {0}", name) :
                String.Format("{0} class {1}", attributes, name);
            var body = _buildExpression(args[args.Length - 1] as Block);
            /* adding newline before new class */
            return String.Format("\n{0}\n{1}", title, body.CompileToCSharp());

        }

        private class ClassForm : Functional
        {
            public ClassForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return DefineClass(args);
            }
        }

        private static int _beginLevel = 0;
        private class BeginForm : Functional
        {
            public BeginForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                _beginLevel++;
                var builder = new StringBuilder();
                foreach (Functional function in args)
                {
                    // just to hot fix //
                    if (function != null)
                        builder.AppendLine(function.CompileToCSharp() +
                        (function.GetType() == typeof(LiteralForm) ? ";": ""));
                }
                _beginLevel--;
                return String.Format("{{\n{0}}}", builder.ToString());
            }
        }

        private class QuoteForm : Functional
        {
            public QuoteForm(object[] args) : base(args)
            {
                this.args = args;
            }
            public override string CompileToCSharp()
            {
                return null;
            }
        }

        private class NewForm : Functional
        {
            public StringBuilder type = new StringBuilder();
            public NewForm(object[] args) : base(args)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].GetType() == typeof(Atom))
                        // normal one we get all //
                        type.Append((Atom)args[i]);
                }
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
                }
                return string.Format("new {0} ({1})", type.ToString(), constructorArgs.ToString());
            }
        }

        private static string SetVariable(object[] args, bool isStatic = false)
        {
            /*************************************************************
             * 
             * :: SET ::
             * 
             * (set name (:attributes) value)
             * 
             * 1. name
             * 2. attributes
             * 3. value
             * 
             *************************************************************/
            var name = ((Token)args[0]).Value;
            var attributes = 
                args.Length == 3 ?
                _buildAttributes(((Block)args[1]).elements) : "";
            var type = "";
            var binding = "";
            var body = args[args.Length - 1];
            if (body.GetType() == typeof(Block))
            {
                var _block = _buildExpression((Block)body);
                type = ((NewForm)_block).type.ToString();
                binding = ((NewForm)_block).CompileToCSharp();
            }
            else // just value //
            {
                type = ((Token)body).Name;
                binding = ((Token)body).Value;
            }
            return String.Format("{0} {1} {2} = {3};",
                attributes, type, name, binding);

        }

        private class SetForm : Functional
        {
            public SetForm(object[] args) : base(args) { }
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
			 * :: DEF FORM ::
			 * 
			 *  (def (name -> arg1 arg2)
			 *       (type -> type1 type2)
			 *       (:attribute)
			 *       (progn &body))
			 * 
			 * 1. name & arg names.
			 * 2. return type & arg types.
			 * 3. attributes. 
			 * 4. body.
			 * 
			 **************************************************/
            var names = ((Block)args[0]).elements;
            var types = ((Block)args[1]).elements;
            // validate arguments count: //
            if (!(names.Count == types.Count && names.Count > 1))
                throw new Exception("Malform def.");
            names.Remove(names[1]);
            types.Remove(types[1]);
            var name = ((Token)names[0]).Value;
            var returnType = ((Token)types[0]).Value;
            var arguments = new StringBuilder();
            // matching arguments...//
            if (names.Count > 1)
            {
                for (int i = 1; i < types.Count; i++)
                {
                    
                    var type = (Token)types[i];
                    if (type.Value != "_")
                        arguments.Append(string.Format(
                            "{0} {1},", 
                            type.Name=="pair" ? type.Value.Replace(':', ' ') : type.Value,
                            ((Token)names[i]).Value));
                }
                // remove last comma.
                if (arguments.Length > 0)
                    arguments.Remove(arguments.Length - 1, 1);
            }
            // collect attributes...//
            var _attributes = args.Length == 4 ? ((Block)args[2]).elements : null;
           
            // do some processing .. //
            var body = ((Functional)args[args.Length - 1]).CompileToCSharp();
            var commit = new string[] {
                _buildAttributes(_attributes),
                returnType == "ctor" ? "" : returnType,
                name,
                arguments.ToString(),
                body
                };
            /* we add newline before new function. */
            return String.Format("\n{0} {1} {2} ({3}) {4}", commit);

        }

        private static string _buildAttributes(List<object> _attributes)
        {
            var attributes = new StringBuilder();
            if (_attributes != null)
            {
                foreach (Token attribute in _attributes)
                {
                    attributes.Append(_buildAtom(attribute).value + " ");
                }
                attributes.Remove(attributes.Length - 1, 1);
            }
            return attributes.ToString();
        }

        private class DefForm : Functional
        {
            public bool isStatic = false;
            public DefForm(object[] args) : base(args)
            {
                // copying...           //
                this.args = args;
                // only build the body. //
                this.args[args.Length - 1] = _buildExpression(
                    (Block)this.args[args.Length - 1]);
            }
            public override string CompileToCSharp()
            {
                return BuildMethod(args);
            }
        }

        private class DefstaticForm : Functional
        {
            public bool isStatic = false;
            public DefstaticForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return BuildMethod(args, isStatic: true);
            }
        }

        private class DefoverrideForm : Functional
        {
            public bool isStatic = false;
            public DefoverrideForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return BuildMethod(args, isOverride: true);
            }
        }

        private class ReturnForm : Functional
        {
            public ReturnForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return String.Format("return {0};", SourceEnforce(args, 0));
            }
        }

        private class LabelForm : Functional
        {
            public LabelForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return String.Format("{0}:", (Atom)args[0]);
            }
        }

        private class VarForm : Functional
        {
            public VarForm(object[] args) : base(args) { }
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
            public IfForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {                                                                  
                var condition = SourceEnforce(args, 0);
                var body = SourceEnforce(args, 1);
                return String.Format("if ({0}) {{\n{1}\n}}", condition, body);
            }
        }

        private class OrForm : Functional
        {
            public OrForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("||", args, isClosure: true);
            }
        }

        private class AndForm : Functional
        {
            public AndForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("&&", args);
            }
        }

        #region Operators

        private class AssignmentForm : Functional
        {
            public AssignmentForm(object[] args) : base(args) { }
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
            public DivideOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("/", args);
            }
        }

        private class MultiplyOperatorForm : Functional
        {
            public MultiplyOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("*", args);
            }
        }

        private class SubOperatorForm : Functional
        {
            public SubOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("-", args);
            }
        }

        private class AddOperatorForm : Functional
        {
            public AddOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("+", args);
            }
        }

        private class DivideSelfOperatorForm : Functional
        {
            public DivideSelfOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("/=", args, isOnlyTwo: true) + ";";
            }
        }

        private class MultiplySelfOperatorForm : Functional
        {
            public MultiplySelfOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("*=", args, isOnlyTwo: true) + ";";
            }
        }

        private class SubSelfOperatorForm : Functional
        {
            public SubSelfOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("-=", args, isOnlyTwo: true) + ";";
            }
        }

        private class AddSelfOperatorForm : Functional
        {
            public AddSelfOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("+=", args, isOnlyTwo: true) + ";";
            }
        }

        private class LesserOperatorForm : Functional
        {
            public LesserOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("<", args, isOnlyTwo: true);
            }
        }

        private class BiggerOperatorForm : Functional
        {
            public BiggerOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree(">", args, isOnlyTwo: true);
            }
        }

        private class EqualOperatorForm : Functional
        {
            public EqualOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("==", args, isOnlyTwo: true);
            }
        }

        private class LesserEqualOperatorForm : Functional
        {
            public LesserEqualOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("<=", args, isOnlyTwo: true);
            }
        }

        private class BiggerEqualOperatorForm : Functional
        {
            public BiggerEqualOperatorForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree(">=", args, isOnlyTwo: true);
            }
        }

        #endregion

        private class JumpDirectiveForm : Functional
        {
            public JumpDirectiveForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return String.Format("goto {0};", (Atom)args[0]);
            }
        }

        #endregion

        #region Interpreter
        /********************************************************
         * 
         * :: INTERPRETER ::
         * 
         * we can have structure like this:
         * 
         *          [ REPL ]
         *             |
         *       [ INTERPRETER ]
         *             |
         *   [ HIGH LEVEL FUNCTION ]
         *             |
         *    [ MACROS EXPANSION ]
         *             |
         * [ LOW LEVEL FUNCTION TREE ]
         *             |
         *   [ CSHARP SOURCE CODE ]
         *             |
         *        [ ASSEMBLY ]
         *        
         *  So we completed infranstructure for 3 lowest levels,
         *  and continue to ground-up build above layers of eval.
         * 
         * 1. INTERPRETER
         * 
         * 2. HIGH LEVEL FUNCTION
         *    - won't relate much to C# primitives, but LISP.
         *      functions here mostly work w/ interpreter and
         *      compiler directive level.
         *    - as most of them are like macros and transformers.
         *    
         * 3. MACROS EXPANSION
         *    - expand high-level function into low-level tree. 
         * 
         **********************************************************/


        // experiment before actual improvement //
        private string _replSource = String.Empty;
        public void EvalTree(List<Functional> tree)
        {
            _print(_nTimes("_", 80));
            _print("* Evaluating tree.... *");
            var acc = "";
            var refs = string.Empty;
            foreach (var function in tree)
            {
                _print(EvalFunction(function) + "\n");
                _print(function.CompileToCSharp());
                if (function.GetType() != (typeof(RequireForm)) &&
                    function.GetType() != (typeof(ImportForm)))
                {
                    acc += function.CompileToCSharp();
                }
                else
                {
                    refs += function.CompileToCSharp();
                }
            }
            var asm = _compile_csharp_source(acc, "repl_env.dll", false, true);
            if (asm != null)
            {
                // mean no error. then we merge code. //
                _replSource += acc;
                if(refs != String.Empty)
                {
                    // refs > source reassignment.        //
                    string _final = refs + "\n" + _replSource;
                    _replSource = _final;
                }
                /*****************************************************
                 * 
                 * Actually, we can just modify the functional tree,
                 * and then re-compile the whole thing. So we can 
                 * have more control on this by:
                 * 
                 * 0. Improve references to manage lib/ns.
                 * 1. Improve class to manage its functions/vars.
                 * 2. Improve function to manage:
                 *      - its local variables.
                 *      - its statements.
                 *      - its expression.
                 *      so we can trace back when needed.
                 * 
                 *****************************************************/
            }
        }
        public string EvalFunction(Functional function)
        {
            var acc = new StringBuilder();
            var args = new StringBuilder();
            foreach (var arg in function.args)
            {
                //_print("{0} ", arg.GetType().Name);
                if (arg.GetType().IsSubclassOf(typeof(Functional)))
                {
                    args.Append("\n" + EvalFunction(arg as Functional) + " ");
                }
                else
                {
                    args.Append(arg.ToString() + " ");
                }
            }
            acc.Append(String.Format("\n[ {0} {1} ]",
                function.GetType().Name, args.ToString()));
            return acc.ToString();
        }
        public void ReplTest(string source)
        {
            EvalTree(BuildFunctionalTree(source));
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
                compiler_parameter.ReferencedAssemblies.Add(name + ".dll");

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
            _open = _close = 0;
            _beginLevel = 0;
            _token_array = null;
            _token_index = -1;
            _transformerIndex = -1;
        }

        #endregion
    }
}
