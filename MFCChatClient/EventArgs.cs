using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFCChatClient
{
    public class MFCMessageEventArgs : EventArgs
    {
        public MFCMessage Message { get; set; }
    }
 
    public class MFCChatMessageEventArgs : EventArgs
    {
        public MFCChatMessage ChatMessage { get; set; }
    }

    public class MFCTipEventArgs : EventArgs
    {
        public Tip Tip { get; set; }
    }

    public class SocketErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
    }
}
