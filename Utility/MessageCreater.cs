using Google.Protobuf;
using ONetworkTalk.Codecs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Utility
{
    public class MessageCreater
    {
        public static MessagePacket CreateNormalMessage(string userID, string destID, int contractID, IMessage body = null)
        {
            return new MessagePacket(userID, destID, GloblParams.CurrentClientType, MessageType.Noraml, contractID, (body == null) ? null : MessageExtensions.ToByteArray(body));
        }
        public static MessagePacket CreateNormalMessage(string userID, int contractID, IMessage body = null)
        {
            return new MessagePacket(userID, GloblParams.CurrentClientType, MessageType.Noraml, contractID, (body == null) ? null : MessageExtensions.ToByteArray(body));
        }
        public static MessagePacket CreateAckMessage(string userID, string destID, int contractID, IMessage body = null)
        {
            return new MessagePacket(userID, destID, GloblParams.CurrentClientType, MessageType.Ack_Req, contractID, (body == null) ? null : MessageExtensions.ToByteArray(body));
        }
        public static MessagePacket CreateAckMessage(string userID, int contractID, IMessage body = null)
        {
            return new MessagePacket(userID, GloblParams.CurrentClientType, MessageType.Ack_Req, contractID, (body == null) ? null : MessageExtensions.ToByteArray(body));
        }
        public static MessagePacket CreateQueryMessage(string userID, string destID, int contractID, IMessage body = null)
        {
            return new MessagePacket(userID, destID, GloblParams.CurrentClientType, MessageType.Query_Req, contractID, (body == null) ? null : MessageExtensions.ToByteArray(body));
        }
        public static MessagePacket CreateQueryMessage(string userID, int contractID, IMessage body = null)
        {
            return new MessagePacket(userID, GloblParams.CurrentClientType, MessageType.Query_Req, contractID, (body == null) ? null : MessageExtensions.ToByteArray(body));
        }
    }
}
