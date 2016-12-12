using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Handler
{
    public interface IIntegratedCustomizeHandler: ICustomizeHandler
    {
        /// <summary>
        /// 是否可处理此消息
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        bool CanHandle(int ContractaID);
    }
}
