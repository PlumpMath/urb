using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Urb
{
    public partial class ULisp
    {

        #region TokenTree -> Expressions

        private static string _nTimes(string ch, int time)
        {
            var acc = "";
            for (int i = 1; i < time; i++) acc += ch;
            return acc;
        }

        private static Expression _insertPrimitives(Block block)
        {
            // We plugin all special forms here. //
            if (block.head.GetType() == typeof(Token))
            {
                // transform it into primitive if possible //
                var token = (Token)block.head;
                switch (token.type)
                {
                    // All Primitives //
                    case "boolean_compare":
                    case "operator":
                    case "literal":
                        if (_specialForms.ContainsKey(token.value))
                        {
                            // mean it's implemented primitive. //
                            return (Expression)Activator.CreateInstance(
                                _specialForms[token.value],
                                new[] { block.rest });
                        }
                        else if (_primitiveForms.ContainsKey(token.value))
                        {
                            // mean it's implemented primitive. //
                            return (Expression)Activator.CreateInstance(
                                _primitiveForms[token.value],
                                new[] { block.evaluatedRest });
                        }
                        else
                        {
                            // normal function or invoke. //   
                            var f = new LiteralForm(block.evaluatedRest);
                            f.Init(new Atom(token.type, token.value));
                            return f;
                        }
                    //throw new NotImplementedException("Unknown form: " + token.Value);

                    default: throw new NotSupportedException(token.type);
                }
            }
            return null;
        }

        private static Atom _buildAtom(Token token)
        {
            switch (token.type)
            {
                case "symbol":
                    return new Atom(token.type,
                                    token.value.Substring(1, token.value.Length - 1));
                case "Int32": return new Atom("Int32", Int32.Parse(token.value));
                case "Double":
                    var d = double.Parse(token.value);
                    return new Atom("Double", d);

                case "float":
                    var number = float.Parse(token.value.Substring(0,
                      token.value.ToString().Length - 1));
                    return new Atom("float", number);
                case "bool":
                    return new Atom("bool", token.value == "true");
                default: return new Atom(token.type, token.value);
            }
        }
        private static List<Expression> _mainBody = new List<Expression>();
        private static List<Expression> TokenTree2Expressions(List<Block> tree)
        {
            /******************************************
			 * 									      *
			 * Phrase 2: Refine Expressions		   	  *
			 * Here we refactor tokenized expression. *
			 * 									   	  *
			 ******************************************/
            var result = new List<Expression>();
            foreach (var block in tree)
            {
                // inserting primitives....               //
                var _codeBlock = _insertPrimitives(block);
                if (_codeBlock is LiteralForm)
                {
                    /// then it belong to Main ():
                    _mainBody.Add(_codeBlock);
                }
                else
                {
                    result.Add(_codeBlock);
                }
            }
            return result;
        }

        public static List<Expression> Source2Expressions
        (string source, bool isDebugTransform = false, bool isDebugGrammar = false)
        {
            _print(_nTimes("_", 80));
            // We need to eat the token here with a Lexer !
            var token_list = Reader(source, isDebugTransform, isDebugGrammar);
            var tree = Lexer(token_list, isDebugTransform);
            var expansion = MacroExpand(tree);
            var expression = TokenTree2Expressions(expansion);
            return expression;
        }

        private static int _transformerIndex = 0;

        #endregion

    }
}
