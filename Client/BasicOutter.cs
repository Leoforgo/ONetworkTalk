using Google.Protobuf;
using ONetworkTalk.Utility;
using ONetworkTalk.Codecs;
using ONetworkTalk.Contract;
using System.Collections.Generic;

namespace ONetworkTalk.Client
{
    /// <summary>
    /// 客户端消息发送
    /// </summary>
    public class BasicOutter
    {
        private MessageBus messageBus;

        public BasicOutter(MessageBus messageBus)
        {
            this.messageBus = messageBus;
        }

        public void Send(string destID, int contractID, IMessage body = null)
        {
            var message = MessageCreater.CreateNormalMessage(GloblParams.CurrentClientID, destID, contractID, body);

            messageBus.SendMessage(message);
        }

        public void Send(int contractID, IMessage body = null)
        {
            var message = MessageCreater.CreateNormalMessage(GloblParams.CurrentClientID, contractID, body);

            messageBus.SendMessage(message);
        }

        public void SendCertainly(string destID, int contractID, IMessage body = null)
        {
            var message = MessageCreater.CreateAckMessage(GloblParams.CurrentClientID, destID, contractID, body);

            messageBus.SendAckMessage(message);
        }

        public void SendCertainly(int contractID, IMessage body = null)
        {
            var message = MessageCreater.CreateAckMessage(GloblParams.CurrentClientID, contractID, body);

            messageBus.SendAckMessage(message);
        }

        public void SendBlob(string destID, int contractID, IMessage body, int blobSize = 1024)
        {
            List<MessagePacket> packets = new List<MessagePacket>();
            byte[] buffer = body.ToByteArray();
            int blobLength = buffer.Length / blobSize;
            if (blobLength != 0 && buffer.Length % blobLength > 0)
            {
                blobLength++;
            }
            int blobID = MessageIDCreater.GetNewID();
            if (blobLength == 0)
            {
                BlobContract contract = new BlobContract()
                {
                    BlobID = blobID,
                    FragmentIndex = 0,
                    Fragment = ByteString.CopyFrom(buffer),
                    IsLast = true
                };
                packets.Add(new MessagePacket(GloblParams.CurrentClientID, destID,
                                  GloblParams.CurrentClientType, MessageType.Blob, contractID, contract.ToByteArray()));
            }
            else
            {
                for (int i = 0; i < blobLength; i++)
                {
                    int startIndex = i * blobSize;
                    int endIndex = ((i + 1) * blobSize) > buffer.Length ? buffer.Length : ((i + 1) * blobSize);
                    int length = endIndex - startIndex;
                    byte[] sendBuffer = new byte[length];
                    System.Buffer.BlockCopy(buffer, startIndex, sendBuffer, 0, length);
                    BlobContract contract = new BlobContract()
                    {
                        BlobID = blobID,
                        FragmentIndex = i,
                        Fragment = ByteString.CopyFrom(sendBuffer),
                        IsLast = i == blobLength
                    };
                    packets.Add(new MessagePacket(GloblParams.CurrentClientID, destID,
                        GloblParams.CurrentClientType, MessageType.Blob, contractID, contract.ToByteArray()));
                }
            }


            messageBus.SendMessage(packets);
        }

        public void SendBlob(int contractID, IMessage body, int blobSize = 1024)
        {
            this.SendBlob(GloblParams.DefaultServerID, contractID, body, blobSize);
        }

        public byte[] Query(string destID, int contractID, IMessage body = null)
        {
            var message = MessageCreater.CreateQueryMessage(GloblParams.CurrentClientID, destID, contractID, body);

            return messageBus.SendAckMessage(message);
        }

        public byte[] Query(int contractID, IMessage body = null)
        {
            var message = MessageCreater.CreateQueryMessage(GloblParams.CurrentClientID, contractID, body);

            return messageBus.SendAckMessage(message);
        }

        internal byte[] Query(MessagePacket message)
        {
            return messageBus.SendAckMessage(message);
        }
    }
}
