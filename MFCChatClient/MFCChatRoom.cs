using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MFCChatClient
{
    public class MFCChatRoom
    {
        MFCClient _client = new MFCClient();
        String _userName = "";

        public MFCChatRoom(String userName)
        {
            _userName = userName;
            JoinByName();
        }

        public MFCClient Client { get { return _client; } }

        public event EventHandler<MFCChatMessageEventArgs> ChatMessageReceived;
        protected virtual void OnChatMessageReceived(MFCChatMessageEventArgs e)
        {
            if (null != ChatMessageReceived)
                ChatMessageReceived(this, e);
        }

        //JoinModelChatRoom takes a modelname and a handler for receiving messages
        void JoinByName()
        {
            //figure out what the broadcasterid is for the model
            var info = _client.Users.Select(u => u.Value).Where(n => n.Name == _userName).FirstOrDefault();
            if (null != info)
            {
                JoinByUserId((int)info.UserId);
                return;
            }

            //Ask the server what the userid is
            _client.SendMessage(new UserLookupMessage(_userName));

            //Handle lookup response
            EventHandler<MFCMessageEventArgs> lookupHandler = null;
            lookupHandler = (s, e) =>
            {
                if (e.Message.MessageType == MFCMessageType.FCTYPE_USERNAMELOOKUP)
                {
                    //remove the handler
                    _client.Received -= lookupHandler;

                    //was the user found?
                    if (e.Message.Arg2 == (int)MFCResponseType.FCRESPONSE_ERROR)
                        throw new Exception("Could not find a user by the name, " + e.Message.Data);

                    //join by id	
                    var userInfo = JsonConvert.DeserializeObject<User>(WebUtility.UrlDecode(e.Message.Data));
                    if (userInfo.Name == _userName)
                        JoinByUserId(e.Message.Arg2);
                }
            };
            _client.Received += lookupHandler;


        }

        void JoinByUserId(int userId)
        {
            //public channels for models are always their userid + 100000000
            //there are also session ids, but not sure what they are used for
            //session id is userid + 200000000
            var publicChannelId = 100000000 + userId;

            //Queue a join message
            _client.SendMessage(new MFCMessage()
            {
                MessageType = MFCMessageType.FCTYPE_JOINCHAN,
                From = _client.SessionID,
                To = 0,
                Arg1 = publicChannelId,
                Arg2 = (int)(MFCChatOpt.FCCHAN_JOIN | MFCChatOpt.FCCHAN_HISTORY)
            });

            //Set up a filtered handler that only sends chat room messages
            _client.Received += (sender, e) =>
            {
                //leave if this is not a message for our room
                if (e.Message.MessageType != MFCMessageType.FCTYPE_CMESG || e.Message.To != publicChannelId)
                    return;

                OnChatMessageReceived(new MFCChatMessageEventArgs() { ChatMessage = new MFCChatMessage(e.Message) });
            };
        }
    }
}
