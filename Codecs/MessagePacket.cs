using DotNetty.Buffers;
using ONetworkTalk.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Codecs
{
    /// <summary>
    /// 消息包，消息头+协议id+协议体
    /// </summary>
    public class MessagePacket
    {
        public MessageHeader MessageHeader { get; set; }

        public int ContractID { get; set; }

        public byte[] BodyContent { get; set; }

        public MessagePacket(IByteBuffer buffer)
        {
            this.MessageHeader = new MessageHeader(buffer);
            this.ContractID = buffer.ReadInt();
            //消息体由协议ID+协议序列化后的内容构成
            if (this.MessageHeader.MessageBodyLen > 4)
            {
                this.BodyContent = new byte[this.MessageHeader.MessageBodyLen - 4];

                buffer.ReadBytes(this.BodyContent);
            }
            else//保证protobuffer对象默认状态下可反序列化(protobuffer序列化对象默认值时，字节数组为0)
            {
                this.BodyContent = new byte[0];
            }
        }

        public MessagePacket(string userID, string destID, ClientType clientType, byte messsageType, int contractID, byte[] body = null)
        {
            this.BodyContent = body;
            this.ContractID = contractID;
            int bodyLen = BodyContent == null ? 4 : BodyContent.Length + 4;

            this.MessageHeader = new MessageHeader()
            {
                Client = (byte)clientType,
                MessageBodyLen = bodyLen,
                MessageType = messsageType,
                MessageID = MessageIDCreater.GetNewID(),
                SourceUserID = userID,
                DestUserID = destID
            };
        }

        public MessagePacket(string userID, string destID, int messageID, ClientType clientType, byte messsageType, int contractID, byte[] body = null)
        {
            this.BodyContent = body;
            this.ContractID = contractID;
            int bodyLen = BodyContent == null ? 4 : BodyContent.Length + 4;

            this.MessageHeader = new MessageHeader()
            {
                Client = (byte)clientType,
                MessageBodyLen = bodyLen,
                MessageType = messsageType,
                MessageID = messageID,
                SourceUserID = userID,
                DestUserID = destID
            };
        }

        public MessagePacket(string userID, ClientType clientType, byte messsageType, int contractID, byte[] body = null)
            : this(userID, GloblParams.DefaultServerID, clientType, messsageType, contractID, body)
        {

        }

        public IByteBuffer ToByteBuffer()
        {
            IByteBuffer buffer = this.MessageHeader.Serialize();
            buffer.WriteInt(this.ContractID);
            if (this.BodyContent != null)
            {
                buffer.WriteBytes(this.BodyContent);
            }
            return buffer;
        }

        public IByteBuffer ToByteBuffer(IByteBuffer buffer)
        {
            this.MessageHeader.Serialize(buffer);
            buffer.WriteInt(this.ContractID);
            if (this.BodyContent != null)
            {
                buffer.WriteBytes(this.BodyContent);
            }
            return buffer;
        }
    }

    public class NullBodyContract { }
}
