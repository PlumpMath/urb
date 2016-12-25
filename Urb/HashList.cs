using System;
using System.Collections.Generic;
namespace Urb
{
    public class HashList<T> : List<T>
    {
        public HashList<T> AddUnique(T obj)
        {
            if (!Contains(obj)) base.Add(obj);
            return this;
        }
        public HashList() : base() { }
        public HashList(int capacity):base(capacity){}
        public HashList(IEnumerable<T> collection) : base(collection) { }
    }
}
