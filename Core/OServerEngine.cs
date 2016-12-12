using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using ONetworkTalk.Handler;
using ONetworkTalk.Server;
using ONetworkTalk.Utility;
using ONetworkTalk.Codecs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ONetworkTalk.Core
{
    public class OServerEngine
    {
        public IBasicHandler basicHandler;
        public ICustomizeHandler handler;
        private RingObjManager<ChannelServerHandler> ringHandlers;
        private MultithreadEventLoopGroup bossGroup;
        private MultithreadEventLoopGroup workerGroup;
        private UserManager userManager;
        private int workThreadCount;
        private bool isStrictOrder;
        public BasicController BasicController { get; private set; }
        public OServerEngine(int workThreadCount = 100, bool isStrictOrder = false)
        {
            ThreadPool.SetMinThreads(workThreadCount, Environment.ProcessorCount * 2);
            this.workThreadCount = workThreadCount;
            this.isStrictOrder = isStrictOrder;
            this.userManager = new UserManager();
            this.BasicController = new BasicController(this.userManager);
        }

        public async Task RunEngineAsync(int port, IBasicHandler basicHandler, ICustomizeHandler handler)
        {
            bossGroup = new MultithreadEventLoopGroup(1);
            workerGroup = new MultithreadEventLoopGroup();
            MessageDispatcher dispatcher = new MessageDispatcher(this.BasicController, basicHandler, handler);
            List<ChannelServerHandler> handlers = new List<ChannelServerHandler>();
            if (isStrictOrder)//此处相比默认handler增加了工作线程，默认工作线程为cpu核心*2
            {
                for (int i = 0; i < workThreadCount; i++)
                {
                    WorkerEngine<RequestInfo> workerEngine = new WorkerEngine<RequestInfo>(3000, 1, dispatcher.Process);
                    handlers.Add(new ChannelServerHandler(this.userManager, workerEngine));
                }
                ringHandlers = new RingObjManager<ChannelServerHandler>(handlers);
            }
            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(bossGroup, workerGroup)
                .Channel<TcpServerSocketChannel>()
                //.Option(ChannelOption.SoBacklog, 100)
                //.Handler(new LoggingHandler(LogLevel.INFO))
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast(OFrameDecoder.NewOFrameDecoder());
                    if (isStrictOrder)
                    {
                        pipeline.AddLast(ringHandlers.GetNext());
                    }
                    else
                    {
                        pipeline.AddLast(new ChannelServerHandler(this.userManager));
                    }
                }));

            IChannel bootstrapChannel = await bootstrap.BindAsync(port);

        }
        public void Close()
        {
            Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
        }
    }
}
