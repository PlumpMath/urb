using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Urb
{
     public partial class ULisp
    {

        #region Token Transformation

        private Token PeakNextToken(int steps = 0)
        {
            return _token_array[_token_index + steps];
        }

        private static List<string> references = new List<string>();
        private static void TransformTokens(Token[] tokens, List<Token> acc, bool isDebugTransform)
        {
            if (isDebugTransform)
                _print("\nBuilding expressions from {0} tokens...\n", tokens.Length);
            _transformerIndex = 0;
            _tokenTree = new List<Block>();

            while (_transformerIndex < tokens.Length - 1)
            {
                // build expression: //
                var e = BuildBlock(tokens, _transformerIndex, isDebugTransform);
                // accumulate all expressions: //
                if (e != null) _tokenTree.Add(e);
            }

            // Here we have a full parsed tree ! //
        }

        private static int _open = 0;
        private static int _close = 0;
        private static List<Block> _tokenTree = new List<Block>();

        private static Block BuildBlock(Token[] tokens, int index, bool isDebugTransform)
        {
            var acc = new List<object>();
            var i = index;
            _transformerIndex = index;
            while (i < tokens.Length)
            {
                switch (tokens[i].value)
                {
                    case ")":
                        _close++;
                        if (isDebugTransform) _print(" )");
                        //_print(" end#{0} \n", _transformerIndex);
                        _transformerIndex = i + 1;
                        var closedE = new Block(acc.ToArray());
                        return closedE;

                    case "(":
                        _open++;
                        if (isDebugTransform) _print("\n{0}#(", _tokenTree.Count);
                        var openE = BuildBlock(tokens, i + 1, isDebugTransform);
                        if (_open == _close) return openE;
                        // else just keep adding.. //
                        acc.Add(openE);
                        i = _transformerIndex;
                        continue;


                    default:
                        /* Skip them all. */
                        if (tokens[i].type == "newline" ||
                           tokens[i].type == "comment")
                        {
                            i++;
                            continue;
                        }
                        // except special separator we eat all //
                        acc.Add(tokens[i]);
                        /************************************
						 * 									*
						 * Would we transform token here ?	*
						 * 									*
						 ************************************/
                        if (isDebugTransform) _print(" {0}", tokens[i].value);
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

    }
}
