using DotNetty.Common.Concurrency;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using ONetworkTalk.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Server
{
    public class UserManager
    {
        internal ClientChannelGroup ClientChannelGroup { get; private set; }

        private UserSet userSet;

        public UserManager()
        {
            this.userSet = new UserSet();
            this.ClientChannelGroup = new ClientChannelGroup();
        }

        internal bool Add(string uid, IChannel channel)
        {
            bool added = this.ClientChannelGroup.Add(channel);
            if (added)
            {
                added = this.userSet.TryAdd(uid, channel);
            }
            return added;
        }

        public IChannel GetChannel(string uid)
        {
            return this.userSet.TryGetChannel(uid);
        }

        public string GetUid(IChannel channel)
        {
            return this.userSet.TryGetUserID(channel);
        }

        public IEnumerable<IChannel> GetChannel(ICollection<string> col)
        {
            foreach (var item in col)
            {
                yield return this.GetChannel(item);
            }
        }

        internal void Remove(string uid)
        {
            IChannel channel = this.userSet.TryGetChannel(uid);
            this.userSet.Remove(uid);
            this.ClientChannelGroup.Remove(channel);
        }

        internal void Remove(IChannel channel)
        {
            this.userSet.Remove(channel);
            this.ClientChannelGroup.Remove(channel);
        }
    }

    /// <summary>
    /// 客户端登录验证后集合
    /// </summary>
    public class UserSet
    {
        //<userID,IChannel>
        private ConcurrentDictionary<string, IChannel> uidChannel;

        //<IChannel,userID>
        private ConcurrentDictionary<IChannel, string> channelUid;

        public UserSet()
        {
            this.uidChannel = new ConcurrentDictionary<string, IChannel>();
            this.channelUid = new ConcurrentDictionary<IChannel, string>();
        }

        public bool TryAdd(string uid, IChannel channel)
        {
            if (!(this.uidChannel.TryAdd(uid, channel) && this.channelUid.TryAdd(channel, uid)))
            {
                string u;
                IChannel c;
                this.uidChannel.TryRemove(uid, out c);
                this.channelUid.TryRemove(channel, out u);

                return false;
            }
            return true;
        }

        public void Remove(string uid)
        {
            IChannel c;
            if (this.uidChannel.TryGetValue(uid, out c))
            {
                string u;
                this.channelUid.TryRemove(c, out u);
            }
        }

        public void Remove(IChannel channel)
        {
            string u;
            if (this.channelUid.TryRemove(channel, out u))
            {
                IChannel c;
                this.uidChannel.TryRemove(u, out c);
            }
        }

        public IChannel TryGetChannel(string uid)
        {
            IChannel channel;
            this.uidChannel.TryGetValue(uid, out channel);
            return channel;
        }

        public string TryGetUserID(IChannel channel)
        {
            string uid;
            this.channelUid.TryGetValue(channel, out uid);
            return uid;
        }
    }
}
