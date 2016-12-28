using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Urb
{
public partial class ULisp {

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

        #region Eval Primitives

        public abstract class Evaluation:Exception
        {
            public object value;
            public Evaluation(object _value = null) { value = _value; }
            public override string ToString()
            {
                return string.Format("{0}:{1}",
                    value == null ? "nil" : value,
                    value == null ? "null" : value.GetType().Name);
            }
        }

        public class Nil : Evaluation { public Nil() : base() { } }
        public class Value : Evaluation { public Value(object value) : base(value) { } }
        public class Quote : Evaluation
        {
            public Quote(List<Block> block) { value = block; }
            public Quote(object value) : base(value) { }
            public override string ToString()
            {
                if (value.GetType() == typeof(List<Block>))
                {
                    var acc = new StringBuilder();
                    foreach (var block in (List<Block>)value)
                    {
                        acc.Append(
                            string.Format("[Quote {0} ]\n", block.ToString()));
                    }
                    return acc.ToString();
                }
                else
                {
                    return value.ToString();
                }
            }
        }
        public class Cons<T> : Evaluation
        {
            public List<T> elements;
            public Cons(List<T> elements)
            {
                elements = new List<T>(elements);
            }
            public override string ToString()
            {
                var acc = new StringBuilder();
                foreach (T t in elements)
                {
                    if (t.GetType() != typeof(Cons<T>))
                        acc.Append(t.ToString() + " ");
                    else
                    {

                    }
                }
                return acc.ToString();
            }
        }

        #endregion

        public static Evaluation Eval(List<Block> blocks)
        {
            var expansion = MacroExpand(blocks);
            var expressions = TokenTree2Expressions(expansion);
            foreach (var function in expressions)
            {
                /*******************************************
                 * 
                 * Use try/catch for parttern matching form.
                 * 
                 *******************************************/
                //try { throw function; }
                //catch (DefineForm d) { return ; }                   
                _print(
                    function.Eval(environment).ToString());
            }
            return null;
        }

        public static Evaluation Eval(List<Token> tokens)
        {
            // here is what it will be //
            switch (tokens.Count)
            {
                case 0: return new Nil(); // Nil? //
                case 1: return new Value(_buildAtom(tokens[0]).valueString);
                default:
                    // more than one is sign of block or expression    //
                    var _tree = Lexer(tokens);
                    if (_tree.Count == 0) return new Nil();

                    // Is a Quoted Value/Expression ?                  //
                    if (tokens[0].type == "quote")
                    {
                        if (_tree.Count == 0 && tokens.Count == 2) return new Quote(tokens[1].value);
                        if (_tree.Count == 1 && tokens.Count > 2) return new Quote(_tree);
                    }
                    // Eval an expression :                            //
                    var result = Eval(_tree);
                    return result;

            }
        }
        /************************************************************************ 
         * 
         * :: RULES :: 
         * 
         * 1. create new statement -> create/invoke a Main ().
         * 2. create new function -> into partial static class.
         * 3. create new class -> into our namespace (if not using).
         * 4. create new namespace -> assign as current namespace.
         * 
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
         *  :: Normal compiling process ::
         *  [ Source >> Reader >> Lexer >> TokensToExpression >> Expressions ]
         *  
         *  :: Compiling during evaluation ::
         *  [ Reader -> Tokens -> Lexer -> Expressor -> Compile -> Eval/invoke ]
         *  
         *  if we take the blackbox approach then throw anything at CSharp compiler,
         *  and wait for returning type to know anything.
         *  
         ***********************************************************************/
        public static Dictionary<string, object> environment =
            new Dictionary<string, object>();

        public static void ReplTest(string source)
        {
            var _tokens = Reader(source);
            var result = Eval(_tokens);
            _print("=> {0}", result);
        }

        public static void ReplSession()
        {
            environment = new Dictionary<string, object>();
            while (true)
            {
                _print("> ", null);
                var _input = Console.ReadLine();
                if (_input == "quit") break;

                var _tokens = Reader(_input);
                var result = Eval(_tokens);
                _print("=> {0}", result);

                // end line. //
                _print("\n" + _nTimes("_", 80) + "\n\n");
            }
        }

        #region Viewers

        public static void Traverse(List<Expression> tree)
        {
            _print(_nTimes("_", 80));
            _print("* Evaluating tree.... *");
            foreach (var function in tree)
            {
                _print(Traverse(function) + "\n");
            }
        }

        public static string Traverse(Expression function)
        {
            var acc = new StringBuilder();
            var args = new StringBuilder();
            foreach (var arg in function.args)
            {
                //_print("{0} ", arg.GetType().Name);
                if (arg.GetType().IsSubclassOf(typeof(Expression)))
                {
                    args.Append("\n" + Traverse(arg as Expression) + " ");
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

        public static void ViewTree(string source)
        {
            Traverse(Source2Expressions(source));
        }

        #endregion

        #endregion

    }
}
