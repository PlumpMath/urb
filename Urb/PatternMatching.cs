using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Urb
{
    public partial class ULisp
    {
        /// Make Unions:
        public class Variable : Exception
        {
            public string Name { get; set; }
        }
        public class Constant : Exception
        {
            public int Value { get; set; }
        }
        public class Add : Exception
        {
            public Exception Left { get; set; }
            public Exception Right { get; set; }
        }
        public class Multiply : Exception
        {
            public Exception Left { get; set; }
            public Exception Right { get; set; }
        }
        public class Cons : Exception
        {
            public Exception Car { get; set; }
            public Exception Cdr { get; set; }
        }
        public class Nill : Exception
        {                
        }

        /// Pattern Matching :
        public static string Format(Exception e)
        {
            try { throw e; }
            catch (Constant n) { return n.Value.ToString(); }
            catch (Variable v) { return v.Name; }
            catch (Add a) { return $"({Format(a.Left)} + {Format(a.Right)})"; }
            catch (Multiply a) { return $"{Format(a.Left)} * {Format(a.Right)}"; }
            catch (Cons cons) { return $"({Format(cons.Car)} ${Format(cons.Cdr)})"; }
            catch (Nill) { return "nil"; }
        }

        /// Pattern Matching :
        public static int Evaluate(Exception e, IDictionary<string, int> vars)
        {
            int res;
            try { throw e; }
            catch (Constant n) { return n.Value; }
            catch (Variable v) when (vars.TryGetValue(v.Name, out res)) { return res; }
            catch (Variable _) { throw new ArgumentException("Variable not found!"); }
            catch (Add a) { return Evaluate(a.Left, vars) + Evaluate(a.Right, vars); }
            catch (Multiply a) { return Evaluate(a.Left, vars) * Evaluate(a.Right, vars); }      
        }

        public static void Test()
        {
            var expr = new Multiply
            {
                Left = new Variable { Name = "x" },
                Right = new Add
                {
                    Left = new Constant { Value = 1 },
                    Right = new Constant { Value = 2 }
                }
            };
            Console.WriteLine(Format(expr));

            Console.WriteLine(Evaluate(expr,
                new Dictionary<string, int> { { "x", 4 } }));
        }
    }
}
