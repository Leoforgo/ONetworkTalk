using ONetworkTalk.Handler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONetworkTalk.Handler
{
    public class ComplexCustomizeHandler : ICustomizeHandler
    {
        private Dictionary<int, IIntegratedCustomizeHandler> handlerDic = new Dictionary<int, IIntegratedCustomizeHandler>();
        private List<IIntegratedCustomizeHandler> handlers = new List<IIntegratedCustomizeHandler>();
        public ComplexCustomizeHandler(IEnumerable<IIntegratedCustomizeHandler> handler)
        {
            foreach (IIntegratedCustomizeHandler current in handler)
            {
                if (!this.handlers.Contains(current))
                {
                    this.handlers.Add(current);
                }
            }
        }
        public ComplexCustomizeHandler(params IIntegratedCustomizeHandler[] handler)
        {
            for (int i = 0; i < handler.Length; i++)
            {
                IIntegratedCustomizeHandler item = handler[i];
                if (!this.handlers.Contains(item))
                {
                    this.handlers.Add(item);
                }
            }
        }
        private ICustomizeHandler FindHandler(int contractaID)
        {
            if (!this.handlerDic.ContainsKey(contractaID))
            {
                foreach (IIntegratedCustomizeHandler current in this.handlers)
                {
                    if (current.CanHandle(contractaID))
                    {
                        this.handlerDic.Add(contractaID, current);
                        break;
                    }
                }
            }
            IIntegratedCustomizeHandler result;
            this.handlerDic.TryGetValue(contractaID, out result);
            return result;
        }
        public void Handle(string sourceUserID, int contractaID, byte[] info)
        {
            ICustomizeHandler customizeHandler = this.FindHandler(contractaID);
            if (customizeHandler != null)
            {
                customizeHandler.Handle(sourceUserID, contractaID, info);
            }
        }
        public byte[] HandleQuery(string sourceUserID, int contractaID, byte[] info)
        {
            ICustomizeHandler customizeHandler = this.FindHandler(contractaID);
            byte[] result;
            if (customizeHandler != null)
            {
                result = customizeHandler.HandleQuery(sourceUserID, contractaID, info);
            }
            else
            {
                result = null;
            }
            return result;
        }
    }
}
