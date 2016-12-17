using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ONetworkTalk.Utility.ObjectManagement
{
    public sealed class RingObject<T>
    {
        private T[] items;

        private int currentPointerIndex = 0;

        public RingObject(IEnumerable<T> col)
        {
            items = new T[col.Count()];
            int tempIndex = 0;
            foreach (var item in col)
            {
                items[tempIndex] = item;
                tempIndex++;
            }
        }

        public T GetNext()
        {
            if (items != null && items.Length == 0)
            {
                return default(T);
            }
            int i = Interlocked.Increment(ref currentPointerIndex);
            return items[i % items.Length];
        }
    }
}
