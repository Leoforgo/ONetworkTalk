using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Utility.ObjectManagement
{
    public interface IPoolObject<T>
    {
        T Borrow();
        void Return(T buffer);
        void Return(IEnumerable<T> buffers);
        void Return(params T[] buffers);
    }

    /// <summary>
    /// 缓存对象池
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PoolObject<T> : IPoolObject<T> where T : class
    {
        private int maxTrial;
        private int minObjectCount;
        private int maxObjectCount;
        private Func<T> creatObjFun;
        private ConcurrentDictionary<T, T> busyDic;
        private readonly ConcurrentStack<T> idleObjs;

        /// <summary>
        /// Constructs
        /// </summary>
        /// <param name="creatObjFun">创建对象所使用方法</param>
        /// <param name="minObjectCount">最少缓存对象数量</param>
        /// <param name="maxObjectCount">最大缓存对象数量</param>
        public PoolObject(Func<T> creatObjFun, int minObjectCount = 100, int maxObjectCount = 200)
        {
            this.creatObjFun = creatObjFun;
            this.idleObjs = new ConcurrentStack<T>();
            this.busyDic = new ConcurrentDictionary<T, T>();
            this.maxTrial = 100;
            if (minObjectCount < 0)
            {
                throw new Exception("The minObjectCount must be greater than 0 !");
            }
            this.minObjectCount = minObjectCount;
            if (maxObjectCount <= 0)
            {
                throw new Exception("The maxObjectCount must be greater than 0 !");
            }
            this.minObjectCount = maxObjectCount;
            if (minObjectCount > maxObjectCount)
            {
                throw new Exception("The maxObjectCount can't be greater than minObjectCount !");
            }
            for (int i = 0; i < minObjectCount; i++)
            {
                idleObjs.Push(creatObjFun());
            }
        }

        public T Borrow()
        {
            T t;
            int trail = 0;
            while (trail < this.maxTrial)
            {
                if (idleObjs.TryPop(out t))
                {
                    this.busyDic.TryAdd(t, t);
                    return t;
                }
                CreateObj();
                trail++;
            }

            throw new UnableToAllocatePoolObjException();
        }

        public void Return(T t)
        {
            if (this.Validate(t))
            {
                T t1;
                this.busyDic.TryRemove(t, out t1);
                this.idleObjs.Push(t);
            }
        }

        public void Return(IEnumerable<T> buffers)
        {
            foreach (var item in buffers)
            {
                this.Return(item);
            }
        }

        public void Return(params T[] buffers)
        {
            foreach (var item in buffers)
            {
                this.Return(item);
            }
        }

        private bool Validate(T t)
        {
            return this.busyDic.ContainsKey(t);
        }

        private void CreateObj()
        {
            if (this.busyDic.Count < this.maxObjectCount)
            {
                idleObjs.Push(creatObjFun());
            }
        }
    }

    [Serializable]
    public class UnableToAllocatePoolObjException : Exception
    {
        public UnableToAllocatePoolObjException()
            : base("Cannot allocate  after few trials.")
        {
        }
    }
}
