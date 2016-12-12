using DotNetty.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ONetworkTalk.Utility
{
    public class MessageType
    {
        public const byte Login = 1;
        public const byte LoginResp = 2;
        public const byte Noraml = 100;
        public const byte Ack_Req = 101;
        public const byte Ack_Resp = 102;
        public const byte Query_Req = 103;
        public const byte Query_Resp = 104;
        public const byte Blob = 105;
        public const byte HeartBeat_Req = 255;
    }

    public static class BufferExt
    {
        public static string ReadStrWithLen(this IByteBuffer buffer)
        {
            int num = buffer.ReadInt();
            bool flag = num <= 0;
            string result;
            if (flag)
            {
                result = string.Empty;
            }
            else
            {
                byte[] array = new byte[num];
                buffer.ReadBytes(array);
                result = Encoding.UTF8.GetString(array);
            }
            return result;
        }
        public static IByteBuffer WriteStrWithLen(this IByteBuffer buffer, string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            buffer.WriteInt(bytes.Length);
            buffer.WriteBytes(bytes);
            return buffer;
        }
        public static string ReadStrWithFixedLen(this IByteBuffer buffer, int fixedLen)
        {
            int num = (int)buffer.ReadByte();
            bool flag = num <= 0;
            string result;
            if (flag)
            {
                result = string.Empty;
            }
            else
            {
                bool flag2 = fixedLen < num;
                if (flag2)
                {
                    throw new Exception("the fixedLen must be more than the str length");
                }
                byte[] array = new byte[num];
                buffer.ReadBytes(array);
                buffer.SkipBytes(fixedLen - num);
                result = Encoding.UTF8.GetString(array);
            }
            return result;
        }
        public static IByteBuffer WriteStrWithFixedLen(this IByteBuffer buffer, string str, int fixedLen)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            buffer.WriteByte(bytes.Length);
            buffer.WriteBytes(bytes);
            bool flag = fixedLen < bytes.Length;
            if (flag)
            {
                throw new Exception("the fixedLen must be more than the str length");
            }
            buffer.WriteBytes(new byte[fixedLen - bytes.Length]);
            return buffer;
        }
    }

    public class GloblParams
    {
        private static int maxUserLength = 8;
        private static int maxFrameLength = 8192;
        internal const string DefaultServerID = "_0";
        internal static string CurrentClientID = "0_";
        internal const int DefaultContractID = 0;
        public static int HeartBeatInSecs = 10;
        public static int HeartBeatTimeOutInSecs = 300;
        public static ClientType CurrentClientType = ClientType.PC;
        public static int WaitReplyTimeoutInSecs = 15000;
        public static int MaxUserLength
        {
            get
            {
                return GloblParams.maxUserLength;
            }
            set
            {
                bool flag = value < 8;
                if (flag)
                {
                    throw new Exception("maxUserLength must be at least 8!");
                }
                bool flag2 = value % 4 > 0;
                if (flag2)
                {
                    throw new Exception("maxUserLenght must be a multiple of 4!");
                }
            }
        }
        public static int MaxFrameLength
        {
            get
            {
                return GloblParams.maxFrameLength;
            }
            set
            {
                GloblParams.maxFrameLength = value;
            }
        }
    }

    public static class MessageIDCreater
    {
        private static int count = 1;
        public static int GetNewID()
        {
            return Interlocked.Increment(ref MessageIDCreater.count);
        }
    }
}
