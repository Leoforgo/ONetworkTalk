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

    /// <summary>
    /// 短暂存储,非精准计时控制，最大存在时间可能为 KeepTimeInSecs+10s
    /// 检测时间未提供参数设置，目前为10s，可自行修改此值
    /// </summary>
    /// <typeparam name="Tkey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class HotDictionary<Tkey, TValue> where TValue : class, IHotItem
    {
        private ConcurrentDictionary<Tkey, TValue> dic;
        private bool isStarCheck = false;
        private List<Tkey> kList;
        public int KeepTimeInSecs { private get; set; }
        public HotDictionary() : this(60)
        {
        }
        public HotDictionary(int keepTimeInSecs)
        {
            this.KeepTimeInSecs = keepTimeInSecs;
            dic = new ConcurrentDictionary<Tkey, TValue>();
            kList = new List<Tkey>();
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
