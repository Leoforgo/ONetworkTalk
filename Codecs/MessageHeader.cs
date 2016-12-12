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
    /// 消息头
    /// Client         startIndex:0,endIndex:1
    /// MessageType    startIndex:2,endIndex:3
    /// MessageID      startIndex:4,endIndex:7
    /// MessageBodyLen startIndex:8,endIndex:11
    /// SourceUserID   startIndex:12,endIndex:19(当用户名最大长度为8)  具体取决于GloblParams.MaxUserLength
    /// DestUserID     startIndex:20,endIndex:27(当用户名最大长度为8)  具体取决于GloblParams.MaxUserLength
    /// </summary>
    public class MessageHeader
    {
        public byte Client { get; set; }
        public byte MessageType { get; set; }
        public int MessageID { get; set; }
        public int MessageBodyLen { get; set; }
        public string SourceUserID { get; set; }
        public string DestUserID { get; set; }
        public MessageHeader() { }
        public MessageHeader(IByteBuffer buffer)
        {
            this.Client = buffer.ReadByte();
            this.MessageType = buffer.ReadByte();
            this.MessageID = buffer.ReadInt();
            // this.ContactID = buffer.ReadInt();
            this.MessageBodyLen = buffer.ReadInt();
            this.SourceUserID = buffer.ReadStrWithFixedLen(GloblParams.MaxUserLength);
            this.DestUserID = buffer.ReadStrWithFixedLen(GloblParams.MaxUserLength);
        }
        public IByteBuffer Serialize()
        {
            IByteBuffer buffer = Unpooled.Buffer();
            buffer.WriteByte(this.Client);
            buffer.WriteByte(this.MessageType);
            buffer.WriteInt(this.MessageID);
            buffer.WriteInt(this.MessageBodyLen);
            buffer.WriteStrWithFixedLen(this.SourceUserID, GloblParams.MaxUserLength);
            buffer.WriteStrWithFixedLen(this.DestUserID, GloblParams.MaxUserLength);
            return buffer;
        }
        public IByteBuffer Serialize(IByteBuffer buffer)
        {
            buffer.WriteByte(this.Client);
            buffer.WriteByte(this.MessageType);
            buffer.WriteInt(this.MessageID);
            //  buffer.WriteInt(this.ContactID);
            buffer.WriteInt(this.MessageBodyLen);
            buffer.WriteStrWithFixedLen(this.SourceUserID, GloblParams.MaxUserLength);
            buffer.WriteStrWithFixedLen(this.DestUserID, GloblParams.MaxUserLength);
            return buffer;
        }
    }
}
