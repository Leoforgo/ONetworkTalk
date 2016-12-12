using DotNetty.Buffers;
using DotNetty.Codecs;
using ONetworkTalk.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Codecs
{
    public class OFrameDecoder
    {
        public static LengthFieldBasedFrameDecoder NewOFrameDecoder()
        {
            return new LengthFieldBasedFrameDecoder(GloblParams.MaxFrameLength, 6, 4, (1 + GloblParams.MaxUserLength) * 2, 0, false);
        }
    }
}
