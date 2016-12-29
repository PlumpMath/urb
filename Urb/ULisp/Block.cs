using System.Collections.Generic;

namespace Urb
{
    public partial class ULisp
    {
        public class Block
        {
            public object head;
            public object[] rest;
            public List<object> elements;
            public List<Block> asBlocks
            {
                get
                {
                    var blocks = new List<Block>();
                    foreach (var element in elements)
                        blocks.Add(element as Block);
                    return blocks;
                }
            }
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
                        else if (element.GetType() == typeof(Quote))
                        {
                            acc.Add(element);
                        }
                        else if (element.GetType() == typeof(Block))
                        {
                            var e = _insertPrimitives((Block)element);
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
    }
}
