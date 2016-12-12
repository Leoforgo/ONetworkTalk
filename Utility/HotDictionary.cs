using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ONetworkTalk.Utility
{
    public interface IHotItem
    {
        DateTime AddTime
        {
            get;
            set;
        }
    }

    public class BlobItem : SortedList<int, ByteString>, IHotItem
    {
        public DateTime AddTime { get; set; }
        public BlobItem()
        {
            this.AddTime = DateTime.Now;
        }
    }

    public class HotDictionary<Tkey, TValue> where TValue : class, IHotItem
    {
        private ConcurrentDictionary<Tkey, TValue> dic = new ConcurrentDictionary<Tkey, TValue>();
        private bool isStarCheck = false;
        private List<Tkey> kList = new List<Tkey>();
        public int KeepTimeInSecs { private get; set; }
        public HotDictionary() : this(60)
        {
        }
        public HotDictionary(int keepTimeInSecs)
        {
            this.KeepTimeInSecs = keepTimeInSecs;
        }
        public void Add(Tkey key, TValue value)
        {
            this.dic.TryAdd(key, value);
            if (!this.isStarCheck)
            {
                lock (this)
                {
                    if (!this.isStarCheck)
                    {
                        this.isStarCheck = true;
                        this.StartCheck();
                    }
                }
            }
        }
        public TValue Get(Tkey key)
        {
            TValue tValue;
            this.dic.TryGetValue(key, out tValue);
            bool flag = tValue == null;
            TValue result;
            if (flag)
            {
                result = default(TValue);
            }
            else
            {
                result = tValue;
            }
            return result;
        }
        public TValue GetAndRemove(Tkey key)
        {
            TValue tValue;
            this.dic.TryGetValue(key, out tValue);
            this.dic.TryRemove(key, out tValue);

            if (this.dic.Count == 0 && this.isStarCheck)
            {
                lock (this)
                {
                    this.isStarCheck = false;
                }
            }

            return tValue;
        }

        private void StartCheck()
        {
            Task.Run(() =>
            {
                while (this.isStarCheck)
                {
                    foreach (KeyValuePair<Tkey, TValue> current in this.dic)
                    {
                        bool flag = DateTime.Now.Subtract(current.Value.AddTime).TotalSeconds >= (double)this.KeepTimeInSecs;
                        if (flag)
                        {
                            this.kList.Add(current.Key);
                        }
                    }
                    foreach (Tkey current2 in this.kList)
                    {
                        TValue tValue;
                        this.dic.TryRemove(current2, out tValue);
                    }
                    if (this.dic.Count == 0 && this.isStarCheck)
                    {
                        lock (this)
                        {
                            this.isStarCheck = false;
                        }
                    }
                    this.kList.Clear();
                    Thread.Sleep(10000);
                }
            }
            );
        }
    }
}
