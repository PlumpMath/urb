using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Urb
{
    public abstract class Expression:Exception
    {
        public ApplyCase abstractType = ApplyCase.Undefined;
        public object[] args;
        public Expression(object[] _args)
        {
            args = _args;
        }
        public virtual object Eval(Dictionary<string, object> env)
        {
            Console.WriteLine("Not yet overrided Evaluation function.");
            return null;
        }
        public abstract string CompileToCSharp();
    }
}
