using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Utility
{
    /// <summary>
    /// 生产/消费模型实现，可有多个/单个生产者，也可由多个/多个消费者
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WorkerEngine<T>
    {
        private Action<T> workProcess;
        private BlockingCollection<T> blockingCollection;
        private int workerThreadCount = 1;
        public int WorkerThreadCount
        {
            get
            {
                return this.workerThreadCount;
            }
            set
            {
                this.workerThreadCount = value;
            }
        }
        public WorkerEngine(Action<T> action) : this(1000, 1, action)
        {
        }
        public WorkerEngine(int capacity, int workThreadCount, Action<T> action)
        {
            this.blockingCollection = new BlockingCollection<T>(capacity);
            this.WorkerThreadCount = workThreadCount;
            this.workProcess = action;
        }
        public void Add(T t)
        {
            this.blockingCollection.Add(t);
        }
        public void Start()
        {
            for (int i = 0; i < this.WorkerThreadCount; i++)
            {
                Task.Factory.StartNew(new Action(this.Consume));
            }
        }
        private void Consume()
        {
            foreach (T current in this.blockingCollection.GetConsumingEnumerable())
            {
                this.workProcess(current);
            }
        }
        public void Stop()
        {
            this.blockingCollection.CompleteAdding();
        }
        public int GetLeftMessageCount()
        {
            return this.blockingCollection.Count<T>();
        }
    }
}
