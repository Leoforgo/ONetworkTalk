using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ONetworkTalk.Server
{
    public class ClientChannelGroup : IChannelGroup
    {
        static int nextId;
        readonly ConcurrentDictionary<IChannelId, IChannel> nonServerChannels = new ConcurrentDictionary<IChannelId, IChannel>();

        public ClientChannelGroup()
            : this($"group-{Interlocked.Increment(ref nextId):X2}")
        {
        }

        public ClientChannelGroup(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            this.Name = name;
        }

        public bool IsEmpty => this.nonServerChannels.Count == 0;

        public string Name { get; }

        public IChannel Find(IChannelId id)
        {
            IChannel channel;
            this.nonServerChannels.TryGetValue(id, out channel);
            return channel;
        }

        public Task WriteAsync(object message) => this.WriteAsync(message, ChannelMatchers.All());

        public Task WriteAsync(object message, IChannelMatcher matcher)
        {
            var futures = new Dictionary<IChannel, Task>();
            foreach (IChannel c in this.nonServerChannels.Values)
            {
                if (matcher.Matches(c))
                {
                    futures.Add(c, c.WriteAsync(SafeDuplicate(message)));
                }
            }

            ReferenceCountUtil.Release(message);
            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }

        public IChannelGroup Flush(IChannelMatcher matcher)
        {
            foreach (IChannel c in this.nonServerChannels.Values)
            {
                if (matcher.Matches(c))
                {
                    c.Flush();
                }
            }
            return this;
        }

        public IChannelGroup Flush() => this.Flush(ChannelMatchers.All());

        public int CompareTo(IChannelGroup other)
        {
            int v = string.Compare(this.Name, other.Name, StringComparison.Ordinal);
            if (v != 0)
            {
                return v;
            }

            return this.GetHashCode() - other.GetHashCode();
        }

        void ICollection<IChannel>.Add(IChannel item) => this.Add(item);

        public void Clear()
        {
            this.nonServerChannels.Clear();
        }

        public bool Contains(IChannel item)
        {
            IChannel channel;
            return this.nonServerChannels.TryGetValue(item.Id, out channel) && channel == item;
        }

        public void CopyTo(IChannel[] array, int arrayIndex) => this.ToArray().CopyTo(array, arrayIndex);

        public int Count => this.nonServerChannels.Count;

        public bool IsReadOnly => false;

        public bool Remove(IChannel channel)
        {
            IChannel ch;
            return this.nonServerChannels.TryRemove(channel.Id, out ch);
        }

        public IEnumerator<IChannel> GetEnumerator() => this.nonServerChannels.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.nonServerChannels.Values.GetEnumerator();

        public Task WriteAndFlushAsync(object message, IEnumerable<IChannel> channel)
        {
            var futures = new Dictionary<IChannel, Task>();
            foreach (IChannel c in this.nonServerChannels.Values)
            {
                futures.Add(c, c.WriteAndFlushAsync(SafeDuplicate(message)));
            }

            ReferenceCountUtil.Release(message);
            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }

        public Task DisconnectAsync() => this.DisconnectAsync(ChannelMatchers.All());

        public Task DisconnectAsync(IChannelMatcher matcher)
        {
            var futures = new Dictionary<IChannel, Task>();
            foreach (IChannel c in this.nonServerChannels.Values)
            {
                if (matcher.Matches(c))
                {
                    futures.Add(c, c.DisconnectAsync());
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }

        public Task CloseAsync() => this.CloseAsync(ChannelMatchers.All());

        public Task CloseAsync(IChannelMatcher matcher)
        {
            var futures = new Dictionary<IChannel, Task>();
            foreach (IChannel c in this.nonServerChannels.Values)
            {
                if (matcher.Matches(c))
                {
                    futures.Add(c, c.CloseAsync());
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }

        public Task DeregisterAsync() => this.DeregisterAsync(ChannelMatchers.All());

        public Task DeregisterAsync(IChannelMatcher matcher)
        {
            var futures = new Dictionary<IChannel, Task>();
            foreach (IChannel c in this.nonServerChannels.Values)
            {
                if (matcher.Matches(c))
                {
                    futures.Add(c, c.DeregisterAsync());
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }

        public Task NewCloseFuture() => this.NewCloseFuture(ChannelMatchers.All());

        public Task NewCloseFuture(IChannelMatcher matcher)
        {
            var futures = new Dictionary<IChannel, Task>();
            foreach (IChannel c in this.nonServerChannels.Values)
            {
                if (matcher.Matches(c))
                {
                    futures.Add(c, c.CloseCompletion);
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }

        static object SafeDuplicate(object message)
        {
            var buffer = message as IByteBuffer;
            if (buffer != null)
            {
                return buffer.Duplicate().Retain();
            }

            var byteBufferHolder = message as IByteBufferHolder;
            if (byteBufferHolder != null)
            {
                return byteBufferHolder.Duplicate().Retain();
            }

            return ReferenceCountUtil.Retain(message);
        }

        public override string ToString() => $"{this.GetType().Name}(name: {this.Name}, size: {this.Count})";

        public bool Add(IChannel channel)
        {
            bool added = this.nonServerChannels.TryAdd(channel.Id, channel);
            if (added)
            {
                channel.CloseCompletion.ContinueWith(x => this.Remove(channel));
            }
            return added;
        }

        public IChannel[] ToArray()
        {
            var channels = new List<IChannel>(this.Count);
            channels.AddRange(this.nonServerChannels.Values);
            return channels.ToArray();
        }

        public bool Remove(IChannelId channelId)
        {
            IChannel ch;
            if (this.nonServerChannels.TryRemove(channelId, out ch))
            {
                return true;
            }

            return false;
        }

        public bool Remove(object o)
        {
            var id = o as IChannelId;
            if (id != null)
            {
                return this.Remove(id);
            }
            else
            {
                var channel = o as IChannel;
                if (channel != null)
                {
                    return this.Remove(channel);
                }
            }
            return false;
        }

        public Task WriteAndFlushAsync(object message) => this.WriteAndFlushAsync(message, ChannelMatchers.All());

        public Task WriteAndFlushAsync(object message, IChannelMatcher matcher)
        {
            var futures = new Dictionary<IChannel, Task>();
            foreach (IChannel c in this.nonServerChannels.Values)
            {
                if (matcher.Matches(c))
                {
                    futures.Add(c, c.WriteAndFlushAsync(SafeDuplicate(message)));
                }
            }

            ReferenceCountUtil.Release(message);
            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }
    }
}
