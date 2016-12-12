using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Codecs
{
    public class RequestInfo
    {
        public IChannel Channel { get; set; }
        public IChannelHandlerContext Ctx { get; set; }
        public MessagePacket MessagePacket { get; set; }

        public RequestInfo(IChannel channel, IChannelHandlerContext ctx, MessagePacket messagePacket)
        {
            this.Channel = channel;
            this.Ctx = ctx;
            this.MessagePacket = messagePacket;
        }
    }
}
