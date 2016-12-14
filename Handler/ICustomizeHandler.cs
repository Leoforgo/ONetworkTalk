using ONetworkTalk.Codecs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Handler
{
    /// <summary>
    /// string sourceUserID, int contractaID, byte[] info
    /// </summary>
    public interface ICustomizeHandler
    {
        void Handle(string sourceUserID, int contractID, byte[] info);

        byte[] HandleQuery(string sourceUserID, int contractID, byte[] info);
    }
}
