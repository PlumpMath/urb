using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Urb
{
public partial class ULisp    {

        #region Macro Expander

        private static List<Block> MacroExpand(List<Block> blocks)
        {
            _print("* Macros Expanding... *");
            /******************************************
			 * 									      *
			 *            MACROS EXPANDING            *
             *                          		   	  *
			 * Here we expand all macros & high level *
             * functions into low-level expressions.  *
             * terminate all of them.                 *
             *                                        *
             * For block that need to be evaluated at *
             * expanding-time, only interpreter-level *
             * implemented functions can be used.     *
			 * 									   	  *
             * - Macros components:                   *
             * . Quote: ok                            *
             * . Unquote                              *
             * . Defined Macros					      *		   	  
             * 									   	  *
			 ******************************************/
            //return blocks;
            var acc = new List<Block>();
            foreach (var block in blocks)
            {
                var b = MacroExpand(block);
                acc.Add(b);
            }
            return acc;
        }

        public static Block MacroExpand(Block block)
        {
            var _block = new List<object>();
            var i = 0;
            while (i < block.elements.Count)
            {

                var e = block.elements[i];
                if (e.GetType() == typeof(Token))
                {
                    var v = (e as Token).type;
                    switch (v)
                    {
                        case "quote":
                            var element = block.elements[i + 1];
                            var q = new Quote(element);
                            i += 2;
                            _block.Add(q);
                            break;
                        case "unquote": break;
                        default:
                            _block.Add(block.elements[i]);
                            i++;
                            break;
                    }
                }
                else
                {
                    var b = MacroExpand(e as Block);
                    _block.Add(b);
                    i++;
                }
            }
            return new Block(_block.ToArray());
        }

        #endregion

    }
}
