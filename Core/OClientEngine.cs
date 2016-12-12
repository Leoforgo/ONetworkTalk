using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using ONetworkTalk.Client;
using ONetworkTalk.Utility;
using ONetworkTalk.Codecs;
using ONetworkTalk.Contract;
using ONetworkTalk.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Google.Protobuf;
using System.Threading.Tasks;

namespace ONetworkTalk.Core
{
    class OClientEngine
    {
        public BasicOutter BasicOutter { get; private set; }
        private ChannelClientHandler channelClientHandler;

        public async Task RunEngineAsync(string ip, int port, ICustomizeHandler handler)
        {
            this.channelClientHandler = new ChannelClientHandler(handler);
            this.BasicOutter = new BasicOutter(channelClientHandler.MessageBus);
            var group = new MultithreadEventLoopGroup();
            var bootstrap = new Bootstrap();
            bootstrap
                .Group(group)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    //pipeline.AddLast(new LoggingHandler());
                    pipeline.AddLast(OFrameDecoder.NewOFrameDecoder());
                    pipeline.AddLast();
                }));

            IChannel clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(long.Parse(ip), port));
        }

        public LoginResultContract Login(string uid, string pwd, string verCode)
        {
            LoginContract loginContract = new LoginContract
            {
                Uid = uid,
                Pwd = pwd,
                VerCode = verCode
            };
            MessagePacket message = new MessagePacket(uid, GloblParams.CurrentClientID, GloblParams.CurrentClientType, MessageType.Login, 0, loginContract.ToByteArray());
            byte[] array = null;
            try
            {
                array = this.BasicOutter.Query(message);
            }
            catch (TimeoutException e)
            {
                LoginResultContract loginResultContract = new LoginResultContract();
                loginResultContract.Result = 1;
                loginResultContract.Failure = "登录请求超时，检查网络或是服务器";
            }
            catch
            {
                LoginResultContract loginResultContract = new LoginResultContract();
                loginResultContract.Result = 2;
                loginResultContract.Failure = "unkonw cause";
            }
            LoginResultContract result;
            if (array == null)
            {
                result = null;
            }
            else
            {
                LoginResultContract loginResultContract = LoginResultContract.Parser.ParseFrom(array);
                if (loginResultContract.Result == 0)
                {
                    GloblParams.CurrentClientID = uid;
                    this.channelClientHandler.CanSendHeartMessage = true;
                }
                result = loginResultContract;
            }
            return result;
        }
    }
}
