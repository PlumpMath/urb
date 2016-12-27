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
    public partial class ULisp
    { 
        #region Primitives
        // special form will custom-build its arguments.
        private static Dictionary<string, Type> _specialForms =
            new Dictionary<string, Type>()
            {
                {"member", typeof(MemberForm)},
                {"define", typeof(DefineForm)},
                {"class", typeof(ClassForm) },
            };

        // primitive form will default-build its arguments. //
        private static Dictionary<string, Type> _primitiveForms =
            new Dictionary<string, Type>()
            {
            {"load",  typeof(LoadForm)},
            {"using",  typeof(UsingForm)},
            {"extends", typeof(InheritForm)},
            {"attr", typeof(AttributeForm)},
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
            {"compile", typeof(CompileDirectiveForm)}
            };

        private static StringBuilder _referenceStatements = new StringBuilder();
        private static List<string> _loadedReferences = new List<string>() { "mscorlib" };
        private static List<string> _usingNamespaces = new List<string>();

        private class LoadForm : Expression
        {
            public LoadForm(object[] args) : base(args)
            {
                _loadedReferences.Add(args[0].ToString());
                _usingNamespaces.Add(args[0].ToString());
                ///TODO: load into appDomain anyway, and watch this.
                var asm = Assembly.Load(args[0].ToString());
                AppDomain.CurrentDomain.Load(asm.GetName());
            }
            public override string CompileToCSharp()
            {
                var ns = ((Atom)args[0]).ToString();
                references.Add(ns);
                _referenceStatements.Append(
                    Environment.NewLine +
                    String.Format("using {0};", ns));

                /// compiled goto _referenceStatement //
                return String.Empty;
            }
        }

        private class UsingForm : Expression
        {
            public UsingForm(object[] args) : base(args)
            {
                _usingNamespaces.Add(args[0].ToString());
            }
            public override string CompileToCSharp()
            {
                /// compiled goto _referenceStatement //        
                _referenceStatements.Append(
                    Environment.NewLine +
                String.Format("using {0};", (Atom)args[0]));
                return String.Empty;
            }
        }

        private static StringBuilder _classInheritances = new StringBuilder();

        /// Class-related form should be treat specially as internal task.
        private class InheritForm : Expression
        {
            /*************************************************
             * 
             * (inherit :Object)
             * 
             *************************************************/
            public InheritForm(object[] args) : base(args) { }

            public override string CompileToCSharp()
            {
                string _targets = "";
                if (args.Length > 1)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        _targets += ((Atom)args[i]).ToString();
                        // Peak of next element existence. //
                        if (i + 1 < args.Length) _targets += ", ";
                    }
                }
                else
                {
                    _targets = ((Atom)args[0]).ToString();
                }
                _classInheritances.Append(_targets);

                return String.Empty;
            }
        }

        private static StringBuilder _classAttributes = new StringBuilder();

        /// Class-related form should be treat specially as internal task.
        private class AttributeForm : Expression
        {
            /*************************************************
             * 
             * (attribute :public :static)
             * 
             *************************************************/
            public AttributeForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                var attributes = String.Empty;
                if (args.Length > 1)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        var attribute = ((Atom)args[i]).ToString();
                        /******************************************
                         * 
                         * We should filter attributes there.
                         * 
                         *****************************************/
                        if (_compilingOptions.Contains(attribute))
                        {
                            _compilingExe = attribute == _compilingOptions[0];
                        }
                        else
                        {
                            attributes += attribute;
                            // Peak of next element existence. //
                            if (i + 1 < args.Length) attributes += " ";
                        }
                    }
                }
                else
                {
                    attributes = ((Atom)args[0]).ToString();
                }
                _classAttributes.Append(attributes);

                return String.Empty;
            }
        }


        private class LiteralForm : Expression
        {
            private string _functionLiteral;
            public LiteralForm(object[] args) : base(args) { }
            public void Init(Atom function)
            {
                _functionLiteral = function.valueString;
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
            var name = ((Token)args[0]).value;
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
            var body = _insertPrimitives(args[args.Length - 1] as Block);
            /* adding newline before new class */
            return String.Format("\n{0}\n{1}", title, body.CompileToCSharp());

        }

        private class ClassForm : Expression
        {
            public ClassForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return DefineClass(args);
            }
        }

        private static int _beginLevel = 0;
        private class BeginForm : Expression
        {
            public BeginForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                _beginLevel++;
                var builder = new StringBuilder();
                foreach (Expression function in args)
                {
                    // just to hot fix //
                    if (function != null)
                        builder.AppendLine(function.CompileToCSharp() +
                        (function.GetType() == typeof(LiteralForm) ? ";" : ""));
                }
                _beginLevel--;
                return String.Format("{{\n{0}}}", builder.ToString());
            }
        }

        private class NewForm : Expression
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
                            ((Expression)args[i])
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
            var name = _buildAtom(args[0] as Token).value;
            var attributes = isStatic ? "public static" : "public";
            var type = "";
            var binding = "";
            var body = args[args.Length - 1];
            if (body.GetType() == typeof(Block))
            {
                var _block = _insertPrimitives((Block)body);
                type = ((NewForm)_block).type.ToString();
                binding = ((NewForm)_block).CompileToCSharp();
            }
            else // just value //
            {
                type = ((Token)body).type;
                binding = ((Token)body).value;
            }
            return String.Format("{0} {1} {2} = {3};",
                attributes, type, name, binding);

        }

        private class SetForm : Expression
        {
            public bool isStatic = false;
            public SetForm(object[] args, bool _static) : base(args) { this.isStatic = _static; }
            public override string CompileToCSharp()
            {
                return SetVariable(args, isStatic);
            }
        }

        private static string[] _pair(Token token)
        {
            var acc = token.value.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < acc.Length; i++)
            {
                if (acc[i].Contains("-"))
                {
                    acc[i] = acc[i].Replace("-", " ");
                }
            }
            return acc;
        }

        private static string[] _pair(Atom atom)
        {
            return atom.valueString.Split(new char[] { ':' });
        }

        private static string _buildMethod
        (object[] args, Dictionary<string, Token> inferenceMap, bool isStatic = false, bool isOverride = false)
        {
            /**************************************************
			 * 
			 * :: DEFINE FORM ::
			 * 
			 *  (define (name::type name2::type2) body)
			 * 
			 * 1. args.
			 * N. body.
			 * 
			 **************************************************/
            var signature = ((Block)args[0]);
            var headSign = signature.head as Token;
            string name = String.Empty;
            string returnType = String.Empty;

            if (headSign.type == "pair")
            {
                var _head = _pair(headSign);
                name = _head[0];
                returnType = _head[1];
            }
            else if (inferenceMap.ContainsKey(headSign.value))
            {
                /// calling inference-map for help:
                var _functionNameToken = inferenceMap[headSign.value];
                name = _functionNameToken.type;
                returnType = _functionNameToken.value;
            }
            else
            {
                /// simple:
                name = headSign.value;
                returnType = "void";
            }
            /// Build parameters..
            var arguments = new StringBuilder();
            if (signature.elements.Count > 1)
            {
                signature.elements.Remove(signature.head);
                foreach (Token parameter in signature.elements)
                {
                    var result = String.Empty;
                    if (parameter.type == "pair")
                    {
                        result = string.Format(" {1} {0},", _pair(parameter));
                    }
                    else if (inferenceMap.ContainsKey(parameter.value))
                    {

                        var _token = inferenceMap[parameter.value];

                        result = string.Format(" {1} {0},", _token.type, _token.value);
                    }
                    arguments.Append(result);
                }
            }
            // remove last comma.
            if (arguments.Length > 0)
                arguments.Remove(arguments.Length - 1, 1);
            /// Body processing... 
            var body = _buildBody(args);
            /// Finalizing ..  
            var commit = new string[] {
                "public" + (isStatic ? " static" : ""),
                returnType == "ctor" ? "" : returnType,
                name,
                arguments.ToString(),
                body.ToString()
                };
            /* we add newline before new function. */
            return String.Format("\n{0} {1} {2} ({3}) {{{4}\n}}", commit);

        }

        private static string _buildBody(Block list, int startIndex = 1)
        {
            return _buildBody(list.elements.ToArray(), startIndex);
        }

        private static string _buildBody(object[] list, int startIndex = 1)
        {
            /// Body processing... ///
            var body = new StringBuilder();
            for (int i = startIndex; i < list.Length; i++)
            {
                var _function = _insertPrimitives(list[i] as Block);
                body.Append(_buildStatement(_function));
            }
            return body.ToString();
        }

        private static string _buildStatement(Expression _function)
        {
            return Environment.NewLine +
                    _function.CompileToCSharp() +
                    ///TODO: We ignore some forms here ///
                    (_function is LabelForm ? "" : ";");
        }

        private static string _buildAttributes(List<object> _attributes)
        {
            if (_attributes.Count == 0) return string.Empty;
            var attributes = new StringBuilder();
            if (_attributes != null)
            {
                foreach (Token attribute in _attributes)
                {
                    attributes.Append(_buildAtom(attribute).valueString + " ");
                }
                attributes.Remove(attributes.Length - 1, 1);
            }
            return attributes.ToString();
        }

        private class MemberForm : Expression
        {
            /// Is non-static by default. ///
            public bool isVariable = false;
            private SetForm _setExpression;

            public MemberForm(object[] args) : base(args)
            {
                /// Member is not static. ///
                // copying...           //
                this.args = args;
                // only build the body. //
                var body = this.args[args.Length - 1];
                if (body is Block)
                {
                    /// This is method ! ///
                    this.args[args.Length - 1] = _insertPrimitives((Block)body);
                }
                else
                {
                    /// just variable ///
                    isVariable = true;
                    _setExpression = new SetForm(args, _static: false);
                }
            }
            public override string CompileToCSharp()
            {
                if (isVariable) return _setExpression.CompileToCSharp();
                else return _buildMethod(args, null); // leave support later.
            }
        }

        private class DefineForm : Expression
        {
            public bool isVariable = false;
            private SetForm _setExpression;
            private Dictionary<string, Token> _inferenceMap;
            private Block _body;
            public DefineForm(object[] args) : base(args)
            {
                if (!(args[0] is Block))
                {
                    _setExpression = new SetForm(args, _static: true);
                    isVariable = true;
                }
                else
                {
                    /// filtering data
                    _inferenceMap = new Dictionary<string, Token>();
                    var _count = 0;
                    var _functionName = String.Empty;
                    foreach (Token parameter in (args[0] as Block).elements)
                    {
                        ///TODO: consider it's pretty unknown.
                        if (parameter.type != "pair")
                        {    //&& parameter.Value!= ((args[0] as Block).head as Token).Value)
                            /// We allow function return type to join :
                            _inferenceMap.Add(parameter.value, parameter);
                            if (_count == 0) _functionName = parameter.value;
                        }
                        else
                        {
                            var _ppair = _pair(_buildAtom(parameter));
                            _parameterDict.Add(_ppair[0],
                                new PInfo()
                                {
                                    exactType = _ppair[2],
                                    isVerified = true
                                });
                        }
                        _count++;
                    }

                    _body = new Block(args);

                    /// Now replacing with type inference:
                    _inferenceMap = _typeInference(_inferenceMap, _body, _functionName);
                }

            }

            public override string CompileToCSharp()
            {
                if (isVariable)
                {
                    return _setExpression.CompileToCSharp();
                }
                else
                {
                    return _buildMethod(args, _inferenceMap, isStatic: true);
                }
            }
        }

        private class DefoverrideForm : Expression
        {
            public bool isStatic = false;
            public DefoverrideForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return _buildMethod(args, null, isOverride: true);
            }
        }

        private class ReturnForm : Expression
        {
            public ReturnForm(object[] args) : base(args) { abstractType = ApplyCase.Return; }
            public override string CompileToCSharp()
            {
                return String.Format("return {0}", SourceEnforce(args, 0));
            }
        }

        private class LabelForm : Expression
        {
            public LabelForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return String.Format("{0}:", (Atom)args[0]);
            }
        }

        private class VarForm : Expression
        {
            public VarForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                if (args.Length != 2)
                    throw new Exception("malform var: not enough arguments !");

                var name = ((Atom)args[0]).ToString();
                var value = args[1] is Atom ?
                    ((Atom)args[1]).valueString :
                    _insertPrimitives((Block)args[1]).CompileToCSharp();
                return String.Format("var {0} = {1}", name, value);
            }
        }

        private class IfForm : Expression
        {
            /**************************************************
             * 
             * IF FORM:
             * 
             * (if (cond) 
             *     (true) 
             *     (false))
             * 
             **************************************************/
            public IfForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                /// Condition:
                var condition = SourceEnforce(args, 0);
                var trueExpression = string.Empty;
                var falseExpression = string.Empty;
                /// True/False case
                if (args.Length > 1)
                {
                    trueExpression = _buildStatement(args[1] as Expression);
                }
                if (args.Length > 2)
                {
                    falseExpression = _buildStatement(args[2] as Expression);
                }
                if (args.Length > 3)
                {
                    throw new Exception("Malform If clause (if cond true false)");
                }
                return String.Format(
                    "if ({0}) {{{1}\n}}" +
                    Environment.NewLine +
                    (args.Length == 3 ? "else {{{2}\n}}" : ""),
                    condition, trueExpression, falseExpression);
            }
        }

        private class OrForm : Expression
        {
            public OrForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("||", args, isClosure: true);
            }
        }

        private class AndForm : Expression
        {
            public AndForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return OperatorTree("&&", args);
            }
        }

        #region Operators

        private class AssignmentForm : Expression
        {
            public AssignmentForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                var name = ((Atom)args[0]).ToString();
                var value = args[1].GetType() == typeof(Atom) ?
                    ((Atom)args[1]).valueString : SourceEnforce(args, 1);
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
                    ((Atom)args[i]).valueString : SourceEnforce(args, i);
                acc.Append(String.Format(
                    i + 1 < args.Length ? "{0} {1} " : "{0}", x, (i + 1 < args.Length ? _operator : "")));
            }
            return String.Format(isClosure ? "( {0} )" : "{0}", acc.ToString());
        }


        private class DivideOperatorForm : Expression
        {
            public DivideOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree("/", args);
            }
        }

        private class MultiplyOperatorForm : Expression
        {
            public MultiplyOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree("*", args);
            }
        }

        private class SubOperatorForm : Expression
        {
            public SubOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree("-", args);
            }
        }

        private class AddOperatorForm : Expression
        {
            public AddOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree("+", args);
            }
            public override object Eval(Dictionary<string, object> env)
            {
                var acc = new StringBuilder();
                foreach (var arg in args)
                    acc.Append(" " + arg.ToString());

                return string.Format("({0} {1})", "+", acc);
            }
        }

        // need a type analyzing... //
        private static Type Analyze(Expression f)
        {
            return typeof(object);
        }

        enum Operator
        {
            Add, Sub, Div, Mul,
            AddSelf, SubSelf, DivSelf, MulSelf
        }

        static T BuildOperator<T>(T a, T b, Operator op)
        {
            //TODO: re-use delegate!
            // declare the parameters
            ParameterExpression paramA = System.Linq.Expressions.Expression.Parameter(typeof(T), "a"),
                paramB = System.Linq.Expressions.Expression.Parameter(typeof(T), "b");
            // add the parameters together
            BinaryExpression body;
            switch (op)
            {
                case Operator.Add: body = System.Linq.Expressions.Expression.Add(paramA, paramB); break;
                case Operator.Sub: body = System.Linq.Expressions.Expression.Subtract(paramA, paramB); break;
                case Operator.Div: body = System.Linq.Expressions.Expression.Divide(paramA, paramB); break;
                case Operator.Mul: body = System.Linq.Expressions.Expression.Multiply(paramA, paramB); break;
                case Operator.AddSelf: body = System.Linq.Expressions.Expression.AddAssign(paramA, paramB); break;
                case Operator.SubSelf: body = System.Linq.Expressions.Expression.SubtractAssign(paramA, paramB); break;
                case Operator.MulSelf: body = System.Linq.Expressions.Expression.MultiplyAssign(paramA, paramB); break;
                case Operator.DivSelf: body = System.Linq.Expressions.Expression.DivideAssign(paramA, paramB); break;
                default: throw new NotImplementedException();
            }
            // compile it
            Func<T, T, T> f = System.Linq.Expressions.Expression.Lambda<Func<T, T, T>>(body, paramA, paramB).Compile();
            // call it
            return f(a, b);
        }

        private class DivideSelfOperatorForm : Expression
        {
            public DivideSelfOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree("/=", args, isOnlyTwo: true);
            }
        }

        private class MultiplySelfOperatorForm : Expression
        {
            public MultiplySelfOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree("*=", args, isOnlyTwo: true);
            }
        }

        private class SubSelfOperatorForm : Expression
        {
            public SubSelfOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree("-=", args, isOnlyTwo: true);
            }
        }

        private class AddSelfOperatorForm : Expression
        {
            public AddSelfOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree("+=", args, isOnlyTwo: true);
            }
        }

        private class LesserOperatorForm : Expression
        {
            public LesserOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree("<", args, isOnlyTwo: true);
            }
        }

        private class BiggerOperatorForm : Expression
        {
            public BiggerOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree(">", args, isOnlyTwo: true);
            }
        }

        private class EqualOperatorForm : Expression
        {
            public EqualOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree("==", args, isOnlyTwo: true);
            }
        }

        private class LesserEqualOperatorForm : Expression
        {
            public LesserEqualOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree("<=", args, isOnlyTwo: true);
            }
        }

        private class BiggerEqualOperatorForm : Expression
        {
            public BiggerEqualOperatorForm(object[] args) : base(args) { abstractType = ApplyCase.Map; }
            public override string CompileToCSharp()
            {
                return OperatorTree(">=", args, isOnlyTwo: true);
            }
        }

        #endregion

        private class JumpDirectiveForm : Expression
        {
            public JumpDirectiveForm(object[] args) : base(args) { }
            public override string CompileToCSharp()
            {
                return String.Format("goto {0}", (Atom)args[0]);
            }
        }

        private class CompileDirectiveForm : Expression
        {
            public CompileDirectiveForm(object[] args) : base(args) { }

            public override object Eval(Dictionary<string, object> env = null)
            {
                var body = ((args[0] as Quote).value as Block).asBlocks;

                var asm = Compile(body);
                Load(asm);

                return asm;
            }

            public override string CompileToCSharp()
            {
                // ignore ? //
                return null;
            }
        }
        #endregion

        #region Class Template

        public static string MakeClass(string name, string body /*some attributes ?*/)
        {
            var acc = new StringBuilder();
            acc.Append(_referenceStatements);
            /// Title 
            var title = string.Format(
                Environment.NewLine +
                "{0} class {1}", _classAttributes, name);
            /// Inheritances
            if (_classInheritances.Length != 0)
            {
                title += string.Format(": {0}", _classInheritances);
            }
            acc.Append(title);
            /// Body
            acc.Append(string.Format("\n{{{0}\n}}", body));

            _clearClassMetaData();

            return acc.ToString();
        }

        private static void _clearClassMetaData()
        {
            _referenceStatements.Clear();
            _classInheritances.Clear();
            _classAttributes.Clear();
        }

        #endregion

        #region Main () collector
        private static string _buildMain(List<Expression> statements)
        {
            if (statements.Count == 0) return String.Empty;

            var body = new StringBuilder();

            foreach (var statement in statements)
            {
                body.Append(
                    _buildStatement(statement));
            }

            var final = string.Format(
                "public static void Main(string[] args)"
                + Environment.NewLine +
                "{{" +
                    "{0}"
                + Environment.NewLine +
                "}}", body.ToString());

            /// Flush cache.
            statements.Clear();

            return final;
        }

        #endregion

    }
}
