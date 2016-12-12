using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using ONetworkTalk.Codecs;
using ONetworkTalk.Contract;
using ONetworkTalk.Handler;
using ONetworkTalk.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using System.IO;
using ONetworkTalk.Server;

namespace ONetworkTalk.Server
{
    public class MessageDispatcher
    {
        private BasicController basicController;
        private IBasicHandler basicHandler;
        private ICustomizeHandler customizeHandler;
        private HotDictionary<string, BlobItem> hotDic;
        public MessageDispatcher(BasicController basicController, IBasicHandler basicHandler, ICustomizeHandler customizeHandler)
        {
            this.hotDic = new HotDictionary<string, BlobItem>();
            this.basicController = basicController;
            this.basicHandler = basicHandler;
            this.customizeHandler = customizeHandler;
        }

        public void Process(RequestInfo request)
        {
            var message = request.MessagePacket;
            try
            {
                #region Noraml
                if (message.MessageHeader.MessageType == MessageType.Noraml)
                {
                    if (message.MessageHeader.DestUserID == GloblParams.DefaultServerID)
                    {
                        customizeHandler.Handle(message.MessageHeader.SourceUserID, message.ContractID, message.BodyContent);
                    }
                    else
                    {
                        basicController.Send(message.MessageHeader.DestUserID, message);
                    }
                }
                #endregion

                #region Ack_Req
                else if (message.MessageHeader.MessageType == MessageType.Ack_Req)
                {
                    if (message.MessageHeader.DestUserID == GloblParams.DefaultServerID)
                    {
                        ResponseAck(request.Channel, message);
                        customizeHandler.Handle(message.MessageHeader.SourceUserID, message.ContractID, message.BodyContent);
                    }
                    else
                    {
                        basicController.Send(message.MessageHeader.DestUserID, message);
                    }
                }
                #endregion

                #region Blob
                else if (message.MessageHeader.MessageType == MessageType.Blob)
                {
                    if (message.MessageHeader.DestUserID == GloblParams.DefaultServerID)
                    {
                        BlobContract contract = BlobContract.Parser.ParseFrom(message.BodyContent);
                        string keyStr = message.MessageHeader.SourceUserID + contract.BlobID.ToString();
                        BlobItem item = hotDic.Get(keyStr);
                        if (item == null)
                        {
                            item = new BlobItem();
                            hotDic.Add(keyStr, item);
                        }
                        item.Add(contract.FragmentIndex, contract.Fragment);
                        if (contract.IsLast)
                        {
                            BlobItem blob = this.hotDic.GetAndRemove(keyStr);
                            MemoryStream stream = new MemoryStream();
                            foreach (var blobItem in blob)
                            {
                                blobItem.Value.WriteTo(stream);
                            }
                            byte[] bytes = stream.ToArray();
                            stream.Dispose();
                            customizeHandler.Handle(message.MessageHeader.SourceUserID, message.ContractID, bytes);
                        }
                    }
                    else
                    {
                        basicController.Send(message.MessageHeader.DestUserID, message);
                    }
                }
                #endregion

                #region Ack_Resp
                else if (message.MessageHeader.MessageType == MessageType.Ack_Resp)
                {
                    basicController.Send(message.MessageHeader.DestUserID, message);
                }
                #endregion

                #region Query_Req
                else if (message.MessageHeader.MessageType == MessageType.Query_Req)
                {
                    if (message.MessageHeader.DestUserID == GloblParams.DefaultServerID)
                    {
                        byte[] result = customizeHandler.HandleQuery(message.MessageHeader.SourceUserID, message.ContractID, message.BodyContent);
                        ResponseQuery(request.Channel, MessageType.Query_Resp, message, result);
                    }
                    else
                    {
                        basicController.Send(message.MessageHeader.DestUserID, message);
                    }
                }
                #endregion

                #region Query_Resp
                else if (message.MessageHeader.MessageType == MessageType.Query_Resp)
                {
                    basicController.Send(message.MessageHeader.DestUserID, message);
                }
                #endregion

                #region Login
                else if (message.MessageHeader.MessageType == MessageType.Login)
                {
                    LoginContract loginContract = LoginContract.Parser.ParseFrom(message.BodyContent);
                    string cause;
                    LoginResultContract result = null;
                    bool verResult = basicHandler.VerifyUser(loginContract.Uid, loginContract.Pwd, loginContract.VerCode, out cause);
                    this.basicController.userManager.Add(loginContract.Uid, request.Channel);
                    result = new LoginResultContract()
                    {
                        Result = verResult ? 0 : 1,
                        Failure = cause
                    };
                    ResponseQuery(request.Channel, MessageType.LoginResp, message, result.ToByteArray());
                }
                #endregion

                else
                {
                    //handler.HandleQuery(request);
                }
            }
            catch
            {

            }
        }

        private void ResponseAck(IChannel channel, MessagePacket message)
        {
            MessagePacket returnMessage = new MessagePacket(GloblParams.DefaultServerID, message.MessageHeader.SourceUserID, message.MessageHeader.MessageID, (ClientType)message.MessageHeader.Client, MessageType.Ack_Resp, message.ContractID);
            var buffer = returnMessage.ToByteBuffer();
            channel.WriteAndFlushAsync(buffer);
        }

        private void ResponseQuery(IChannel channel, byte type, MessagePacket rawMessage, byte[] body)
        {
            MessagePacket returnMessage = new MessagePacket(GloblParams.DefaultServerID, rawMessage.MessageHeader.SourceUserID,
                rawMessage.MessageHeader.MessageID, (ClientType)rawMessage.MessageHeader.Client, type,
                rawMessage.ContractID, body);

            var buffer = returnMessage.ToByteBuffer();
            channel.WriteAndFlushAsync(buffer);
        }

    }
}
