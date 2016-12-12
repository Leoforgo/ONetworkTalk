using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Handler
{
    public interface IBasicHandler
    {
        bool VerifyUser(string userID, string password, string verCode, out string failureCause);
    }
}
