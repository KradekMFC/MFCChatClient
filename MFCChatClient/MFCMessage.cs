using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MFCChatClient
{
    //helper class for parsing/sending messages tot he chat server
    //MFC messages are of the format
    //  int int int int int payload
    //where the first int is the type of message
    //the second int is who the message is from (typically their session id)
    //the third int is the recipient of the msg (typically their session id)
    //the fourth and fifth int vary in meaning based on the message
    //the payload is a string that can represent many different things, but
    //is often supporting JSON information
    public class MFCMessage
    {
        //constructors
        public MFCMessage()
        {
        }
        public MFCMessage(String msg)
        {
            //split the string into msg and data
            var pos5 = IndexOfNth(msg, ' ', 5);
            String m1;
            if (pos5 > 0)
            {
                Data = msg.Substring(pos5 + 1);
                m1 = msg.Substring(0, pos5);
            }
            else
                m1 = msg;

            var m1p = m1.Split(' ');
            MessageType = (MFCMessageType)Enum.Parse(typeof(MFCMessageType), m1p[0]);
            From = Int32.Parse(m1p[1]);
            To = Int32.Parse(m1p[2]);
            Arg1 = Int32.Parse(m1p[3]);
            Arg2 = Int32.Parse(m1p[4]);
        }
        //public properties
        public MFCMessageType MessageType { get; set; }
        public int From { get; set; }
        public int To { get; set; }
        public int Arg1 { get; set; }
        public int Arg2 { get; set; }
        public String Data { get; set; }
        //public methods
        //Messages are sent to the server in a particular format; this is a helper method
        //for generating the correct format.  MFC's server supports sending more than one
        //message at a time, but for now we send one at a time
        public String AsMFCRequest() //for ajax connections
        {
            var format = "pk0={0}%20{1}%20{2}%20{3}%20{4}%20{5}";
            return String.Format(format, (int)MessageType, From, To, Arg1, Arg2, (null == Data) ? "-" : Data);
        }
        public String AsSocketMsg() //for websocket connections
        {
            var msg = String.Format("{0} {1} {2} {3} {4}", (int)MessageType, From, To, Arg1, Arg2);
            if ("" != Data)
                msg += (" " + Data);
            msg += "\n\0";
            return msg;
        }
        //private methods
        private int IndexOfNth(string str, char c, int n)
        {
            int remaining = n;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == c)
                {
                    remaining--;
                    if (remaining == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
    }

    //helper message classes
    public class NullMessage : MFCMessage
    {
        public NullMessage()
        {
            MessageType = MFCMessageType.FCTYPE_NULL;
            From = 0;
            To = 0;
            Arg1 = 1;
            Arg2 = 0;
        }
    }
    public class GuestLoginMessage : MFCMessage
    {
        public GuestLoginMessage()
        {
            MessageType = MFCMessageType.FCTYPE_LOGIN;
            From = 0;
            To = 0;
            Arg1 = 20071025;
            Arg2 = 0;
            Data = "guest:guest";
        }
    }

    public class UserLookupMessage : MFCMessage
    {
        public UserLookupMessage(string userName)
        {
            MessageType = MFCMessageType.FCTYPE_USERNAMELOOKUP;
            From = 0;
            To = 0;
            Arg1 = 20;
            Arg2 = 0;
            Data = userName;
        }
    }

    public class MFCChatMessage : MFCMessage
    {
        public MFCChatMessage(String msg)
            : base(msg)
        {
            parseData();
        }

        public MFCChatMessage(MFCMessage msg)
        {
            From = msg.From;
            To = msg.To;
            Arg1 = msg.Arg1;
            Arg2 = msg.Arg2;
            Data = msg.Data;
            parseData();
        }

        public User MessageData { get; set; }

        void parseData()
        {
            if (null != Data && "" != Data)
            {
                try
                {
                    var decoded = WebUtility.UrlDecode(Data);
                    MessageData = JsonConvert.DeserializeObject<User>(decoded);
                }
                catch (Exception err)
                {
                    throw;
                }
            }
        }

        public Boolean IsTip
        {
            get
            {
                if (null != MessageData)
                    return ("FCServer" == MessageData.Name && MessageData.Message.Contains("has tipped"));
                else
                    return false;
            }
        }
    }
}
