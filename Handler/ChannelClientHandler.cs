using DotNetty.Buffers;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using ONetworkTalk.Client;
using ONetworkTalk.Utility;
using ONetworkTalk.Codecs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Handler
{
    public class ChannelClientHandler : SimpleChannelInboundHandler<IByteBuffer>
    {
        internal MessageBus MessageBus { get; private set; }

        public ChannelClientHandler(ICustomizeHandler handler)
        {
            this.MessageBus = new MessageBus(handler);
        }

        private IChannel channel;
        public override void ChannelActive(IChannelHandlerContext context)
        {
            this.channel = context.Channel;
            this.MessageBus.channel = this.channel;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, IByteBuffer msg)
        {
            MessagePacket message = new MessagePacket(msg);
            MessageBus.Process(new RequestInfo(ctx.Channel, ctx, message));
        }

        internal bool CanSendHeartMessage { get; set; }
        private IByteBuffer heartBeatBuffer = null;
        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            // base.UserEventTriggered(context, evt);
            var eventState = evt as IdleStateEvent;
            if (eventState != null && eventState.State == IdleState.ReaderIdle)
            {
                context.Channel.CloseAsync();
            }
            else if (eventState != null && eventState.State == IdleState.WriterIdle)
            {
                if (CanSendHeartMessage)
                {
                    if (heartBeatBuffer == null)
                    {
                        heartBeatBuffer = new MessagePacket(GloblParams.CurrentClientID, GloblParams.CurrentClientType, MessageType.HeartBeat_Req, GloblParams.DefaultContractID).ToByteBuffer();
                    }
                    context.Channel.WriteAndFlushAsync(heartBeatBuffer.Duplicate().Retain());
                }
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            base.ExceptionCaught(context, exception);
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            CanSendHeartMessage = false;
        }
    }
}
