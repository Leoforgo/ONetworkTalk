using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using ONetworkTalk.Utility;
using ONetworkTalk.Codecs;
using ONetworkTalk.Contract;
using ONetworkTalk.Handler;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Client
{
    public class MessageBus
    {
        private HotDictionary<string, BlobItem> hotDic = new HotDictionary<string, BlobItem>();
        private ICustomizeHandler handler;
        private ConcurrentDictionary<int, TaskCompletionSource<byte[]>> messageBag = new ConcurrentDictionary<int, TaskCompletionSource<byte[]>>();
        internal IChannel channel { get; set; }
        public MessageBus(ICustomizeHandler handler)
        {
            this.handler = handler;
        }
        public void Process(RequestInfo requestInfo)
        {
            MessagePacket messagePacket = requestInfo.MessagePacket;
            try
            {
                if (messagePacket.MessageHeader.MessageType == MessageType.Noraml)
                {
                    this.handler.Handle(messagePacket.MessageHeader.SourceUserID, messagePacket.ContractID, messagePacket.BodyContent);
                }
                else
                {
                    if (messagePacket.MessageHeader.MessageType == MessageType.Blob)
                    {
                        BlobContract blobContract = BlobContract.Parser.ParseFrom(messagePacket.BodyContent);
                        string key = messagePacket.MessageHeader.SourceUserID + blobContract.BlobID.ToString();
                        BlobItem blobItem = this.hotDic.Get(key);
                        if (blobItem == null)
                        {
                            blobItem = new BlobItem();
                            this.hotDic.Add(key, blobItem);
                        }
                        blobItem.Add(blobContract.FragmentIndex, blobContract.Fragment);
                        bool isLast = blobContract.IsLast;
                        if (isLast)
                        {
                            BlobItem andRemove = this.hotDic.GetAndRemove(key);
                            MemoryStream memoryStream = new MemoryStream();
                            foreach (KeyValuePair<int, ByteString> current in andRemove)
                            {
                                current.Value.WriteTo(memoryStream);
                            }
                            byte[] info = memoryStream.ToArray();
                            memoryStream.Dispose();
                            this.handler.Handle(messagePacket.MessageHeader.SourceUserID, messagePacket.ContractID, info);
                        }
                    }
                    else
                    {
                        if (messagePacket.MessageHeader.MessageType==MessageType.Ack_Req)
                        {
                            this.ResponseAck(requestInfo.Channel, messagePacket);
                            this.handler.Handle(messagePacket.MessageHeader.SourceUserID, messagePacket.ContractID, messagePacket.BodyContent);
                        }
                        else
                        {
                            if (messagePacket.MessageHeader.MessageType == MessageType.Ack_Resp
                                || messagePacket.MessageHeader.MessageType == MessageType.Query_Resp 
                                || messagePacket.MessageHeader.MessageType == MessageType.LoginResp)
                            {
                                if (this.messageBag.ContainsKey(messagePacket.MessageHeader.MessageID))
                                {
                                    this.messageBag[messagePacket.MessageHeader.MessageID].SetResult(messagePacket.BodyContent);
                                }
                            }
                            else
                            {
                                if (messagePacket.MessageHeader.MessageType == MessageType.Query_Req)
                                {
                                    byte[] body = this.handler.HandleQuery(messagePacket.MessageHeader.SourceUserID, messagePacket.ContractID, messagePacket.BodyContent);
                                    this.ResponseQuery(requestInfo.Channel, messagePacket, body);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        internal void SendMessage(MessagePacket message)
        {
            this.channel.WriteAndFlushAsync(message.ToByteBuffer(this.channel.Allocator.Buffer()));
        }
        internal void SendMessage(List<MessagePacket> messages)
        {
            foreach (MessagePacket current in messages)
            {
                this.channel.WriteAsync(current.ToByteBuffer(this.channel.Allocator.Buffer()));
            }
            this.channel.Flush();
        }
        internal byte[] SendAckMessage(MessagePacket message)
        {
            TaskCompletionSource<byte[]> taskCompletionSource = new TaskCompletionSource<byte[]>();
            this.messageBag.TryAdd(message.MessageHeader.MessageID, taskCompletionSource);
            this.channel.WriteAndFlushAsync(message.ToByteBuffer(this.channel.Allocator.Buffer()));
            if (!taskCompletionSource.Task.Wait(GloblParams.WaitReplyTimeoutInSecs))
            {
                this.messageBag.TryRemove(message.MessageHeader.MessageID, out taskCompletionSource);
                throw new TimeoutException("timeout, cause by waiting for reply ！");
            }
            this.messageBag.TryRemove(message.MessageHeader.MessageID, out taskCompletionSource);
            return taskCompletionSource.Task.Result;
        }
        private void ResponseAck(IChannel channel, MessagePacket rawMessage)
        {
            MessagePacket messagePacket = new MessagePacket(rawMessage.MessageHeader.DestUserID, rawMessage.MessageHeader.SourceUserID, (ClientType)rawMessage.MessageHeader.Client, MessageType.Ack_Resp, rawMessage.ContractID, null);
            IByteBuffer byteBuffer = messagePacket.ToByteBuffer();
            channel.WriteAndFlushAsync(byteBuffer);
        }
        private void ResponseQuery(IChannel channel, MessagePacket rawMessage, byte[] body)
        {
            MessagePacket messagePacket = new MessagePacket(rawMessage.MessageHeader.DestUserID, rawMessage.MessageHeader.SourceUserID, (ClientType)rawMessage.MessageHeader.Client, MessageType.Query_Resp, rawMessage.ContractID, body);
            IByteBuffer byteBuffer = messagePacket.ToByteBuffer();
            channel.WriteAndFlushAsync(byteBuffer);
        }
    }
}
