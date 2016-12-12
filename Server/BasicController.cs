using DotNetty.Transport.Channels;
using Google.Protobuf;
using ONetworkTalk.Utility;
using ONetworkTalk.Codecs;
using ONetworkTalk.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Server
{
    /// <summary>
    /// 服务端消息发送控制器
    /// </summary>
    public class BasicController
    {
        internal UserManager userManager;
        public BasicController(UserManager userManager)
        {
            this.userManager = userManager;
        }
        public void Send(string uid, int contractID, IMessage body = null)
        {
            IChannel channel = this.userManager.GetChannel(uid);
            MessagePacket messagePacket = MessageCreater.CreateNormalMessage(GloblParams.DefaultServerID, uid, contractID, body);
            channel.WriteAndFlushAsync(messagePacket.ToByteBuffer());
        }
        public void Send(IChannel channel, int contractID, IMessage body = null)
        {
            if (channel != null)
            {
                MessagePacket messagePacket = MessageCreater.CreateNormalMessage(GloblParams.DefaultServerID, GloblParams.CurrentClientID, contractID, body);
                channel.WriteAndFlushAsync(messagePacket.ToByteBuffer());
            }
        }
        public void Send2All(List<string> uids, int contractID, IMessage body = null)
        {
            MessagePacket messagePacket = MessageCreater.CreateNormalMessage(GloblParams.DefaultServerID, GloblParams.CurrentClientID, contractID, body);
            IEnumerable<IChannel> channel = this.userManager.GetChannel(uids);
            this.userManager.ClientChannelGroup.WriteAndFlushAsync(messagePacket.ToByteBuffer(), channel);
        }
        internal void Send(string uid, MessagePacket streamMessage)
        {
            IChannel channel = this.userManager.GetChannel(uid);
            if (channel != null)
            {
                channel.WriteAndFlushAsync(streamMessage.ToByteBuffer());
            }
        }
    }
}
