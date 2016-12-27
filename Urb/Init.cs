using System;
using System.Text;
using System.Collections.Generic;

namespace Urb
{
    /***************************************************************************
     * 
     * NOTE:
     * 
     * - for new function -> compile into new partial class and load to domain.
     * - for new variable -> instance save on interpreter environment memory.
     * 
     ***************************************************************************/
    public partial class ULisp
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
    }
}
