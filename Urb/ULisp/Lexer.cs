using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Urb
{
    public partial class ULisp
    {

        #region Lexer
        private static Token[] _token_array;
        private static int _token_index = -1;

        /// <summary>
        /// Construct S-Expression List from flat token list.
        /// </summary>
        /// <param name="token_list">The flat token list.</param>
        /// <param name="isDebugTransform">debug in progress ?</param>
        /// <returns></returns>
        public static List<Block> Lexer(List<Token> token_list, bool isDebugTransform = false)
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
            return _tokenTree;
        }

        private static string Expression2CSharp(List<Expression> functions, bool isDebugTransform = false)
        {
            foreach (var function in functions)
            {
                AddSource(function.CompileToCSharp());
            }
            var csharp_source = new StringBuilder();
            foreach (var line in _csharp_blocks)
            {
                csharp_source.Append(line);
                csharp_source.AppendLine();
            }
            return csharp_source.ToString();
        }

        #endregion

    }
}
