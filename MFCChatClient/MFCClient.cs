using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace MFCChatClient
{
    public class MFCClient
    {
        //private properties
        WebClient _client = new WebClient();
        WebSocket _socket;
        String _sessionId = "0";
        System.Timers.Timer _ping = new System.Timers.Timer(15000);

        public MFCClient()
        {
            initialize();
        }

        public MFCClient(Boolean enableHeartBeat)
        {
            initialize();

            if (enableHeartBeat)
                ToggleHeartBeat();
        }

        void initialize()
        {
            _socket = new WebSocket(WebsocketServerUrl);

            //setup up the socket
            _socket.Opened += onSocketOpened;
            _socket.Error += onSocketError;
            _socket.MessageReceived += onSocketMessage;
            _socket.Closed += onSocketClosed;

            _socket.Open();

            Received += internalHandler;
        }

        //public properties
        public int SessionID { get { return Int32.Parse(_sessionId); } } //chat server session identifier
        public IEnumerable<User> Models
        {
            get
            {
                return _users.Where(u => u.Value.AccessLevel == MFCAccessLevel.Model).Select(m => m.Value);
            }
        }

        private Boolean _handleSessionState = false;
        public Boolean HandleSessionState { get { return _handleSessionState; } set { _handleSessionState = value; } }
        private Dictionary<int, User> _users = new Dictionary<int, User>();
        public IDictionary<int, User> Users { get { return _users; } }
        private Boolean _connected = false;
        public Boolean Connected { get { return _connected; } }
        private Boolean _loggedIn = false;
        public Boolean LoggedIn { get { return _loggedIn; } }
        private int _userId;
        public int UserId { get { return _userId; } } //MFCs identifer for the user
        private String _userName;
        public String UserName { get { return _userName; } set { _userName = value; } }
        private String _socketServerUrl;
        public String WebsocketServerUrl
        {
            get
            {
                if (null == _socketServerUrl)
                    _socketServerUrl = String.Format("ws://{0}.myfreecams.com:8080/fcsl", WebsocketServerName);
                return _socketServerUrl;
            }
        }
        private String _modelUrlFormat;
        public String ModelUrlFormat
        {
            get
            {
                if (null == _modelUrlFormat)
                    _modelUrlFormat = "http://www.myfreecams.com/mfc2/php/mobj.php?f={0}&s=" + WebsocketServerName;
                return _modelUrlFormat;
            }
        }

        Queue sendQueue = new Queue();
        public void SendMessage(MFCMessage msg)
        {
            sendQueue.Enqueue(msg);
            if (WebSocketState.Open == _socket.State)
            {
                while (sendQueue.Count > 0)
                {
                    var m = sendQueue.Dequeue();
                    _socket.Send(((MFCMessage)m).AsSocketMsg());
                }
            }
        }

        //public methods
        private Boolean _heartBeatEnabled = false;
        public void ToggleHeartBeat()
        {
            if (_heartBeatEnabled)
            {
                _ping.Enabled = false;
                _ping.Elapsed -= onPing;
                _heartBeatEnabled = false;
            }
            else
            {
                //set up a ping so the server doesn't close our connection
                //MFC does it randomly between 10 and 20s--not sure why--
                //here we're just doing it every 15s
                _ping.Elapsed += onPing;
                _ping.Enabled = true;
                _heartBeatEnabled = true;
            }
                
        }

        //events
        public event EventHandler<MFCMessageEventArgs> Received; //tap into the message feed
        protected virtual void OnReceived(MFCMessageEventArgs e)
        {
            if (null != Received)
                Received(this, e);
        }
        public event EventHandler<EventArgs> UsersProcessed;
        protected virtual void OnUsersProcessed(EventArgs e)
        {
            if (null != UsersProcessed)
                UsersProcessed(this, e);
        }
        public event EventHandler<EventArgs> SocketClosed;
        protected virtual void OnSocketClosed(EventArgs e)
        {
            if (null != SocketClosed)
                SocketClosed(this, e);
        }
        public event EventHandler<SocketErrorEventArgs> SocketError;
        protected virtual void OnSocketError(SocketErrorEventArgs e)
        {
            if (null != SocketError)
                SocketError(this, e);
        }

        //private methods
        void updateUserInfo(int userId, User info)
        {
            if (!_users.ContainsKey(userId))
                _users.Add(userId, info);
            else
                _users[userId].Update(info); //update the user
        }

        void onSessionState(MFCMessage msg)
        {
            //remove users that are offline
            if (msg.Arg1 == (int)MFCVideoState.FCVIDEO_UNKNOWN)
                _users.Remove(msg.Arg2);
            //convert the json
            var info = JsonConvert.DeserializeObject<User>(WebUtility.UrlDecode(msg.Data));
            //update the user
            updateUserInfo(msg.Arg2, info);
        }
        void onSocketOpened(object sender, EventArgs e)
        {
            var socket = (WebSocket)sender;
            _socket.Send("hello fcserver\n\0");
            _socket.Send(new GuestLoginMessage().AsSocketMsg()); //log in to the server
            _connected = true;
        }

        void onSocketError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            _ping.Enabled = false;
            OnSocketError(new SocketErrorEventArgs() { Exception = e.Exception });
        }

        String queued = "";
        void onSocketMessage(object sender, MessageReceivedEventArgs e)
        {
            //occasionally get messages with random newlines--strip them
            queued += e.Message.Replace("\r\n", "");

            while (queued.Length > 12)
            {
                //how long is the next message?
                var dataLen = Int32.Parse(queued.Substring(0, 4));

                //do we have that much data?
                if (queued.Length < dataLen + 4)
                    return; //wait for more data

                //get the data
                var data = queued.Substring(4, dataLen);

                //check for malformed packets
                if (dataLen != data.Length)
                    break;

                //handle the message	
                OnReceived(new MFCMessageEventArgs() { Message = new MFCMessage(data) });

                //get the next message
                queued = queued.Substring(dataLen + 4);
            }
            //reset the queue
            queued = "";
        }

        void onPing(object sender, EventArgs e)
        {
            SendMessage(new NullMessage());
        }

        void onSocketClosed(object sender, EventArgs e)
        {
            //TODO need to think about what we should really do here
            if (_heartBeatEnabled)
                _ping.Enabled = false;

            OnSocketClosed(new EventArgs());
        }

        public string Metrics { get; set; }
        void internalHandler(object sender, MFCMessageEventArgs e)
        {
            var msg = e.Message;

            switch (msg.MessageType)
            {
                case MFCMessageType.FCTYPE_SESSIONSTATE:
                    {
                        if (_handleSessionState)
                            onSessionState(msg);
                    }
                    break;
                case MFCMessageType.FCTYPE_LOGIN:
                    {
                        //when we get a login message, if it's a success message, save the session id
                        if (msg.Arg1 == (int)MFCResponseType.FCRESPONSE_SUCCESS)
                        {
                            _loggedIn = true;
                            _userId = msg.Arg2;
                            _userName = msg.Data;
                            _sessionId = msg.To.ToString();
                        }
                        //not sure what to do here--for now, throw an exception
                        if (msg.Arg1 == (int)MFCResponseType.FCRESPONSE_ERROR)
                            throw new Exception("Unable to log in to MFC.");
                    }
                    break;

                case MFCMessageType.FCTYPE_METRICS:
                    {
                        //this is a little hairy but what we're trying to do is get an initial list
                        //of models that are currently logged in to the server, primarily so we can
                        //know what their broadcaster ids are when we want to join channels.
                        //MFC sends a metrics message that contains a pointer to a javascript file
                        //that has the currently logged in models right after you log in to the chat server.
                        //They also send metrics messages that dont have the pointer, and I don't know what
                        //those are for yet, so here we only work with the msg that matches the if statement
                        //below.  Then we strip out the javascript code so we're left with JSON we can
                        //parse
                        if (msg.To == 20 && msg.Arg1 == 0 && msg.Arg2 > 0)
                        {
                            //figure out the file pointer
                            var fileno = JsonConvert.DeserializeObject<MetricsPayload>(WebUtility.UrlDecode(msg.Data)).fileno;
                            //get the file
                            var modelInfo = _client.DownloadString(String.Format(ModelUrlFormat, fileno));
                            Metrics = modelInfo;
                            //strip out extraneous javascript
                            modelInfo = modelInfo.Replace("var g_hModelData = ", "");
                            modelInfo = modelInfo.Replace("LoadModelsFromObject(g_hModelData);", "");
                            modelInfo = modelInfo.Replace("};", "}");
                            //attempt to deserialize the model JSON
                            try
                            {
                                var m = JObject.Parse(modelInfo); //parse to a dynamic
                                var n = m.Properties().Where(p => !p.Name.StartsWith("tags")) //filter out the tags objects
                                         .Children() //give us back JTokens
                                         .Select(x => JsonConvert.DeserializeObject<User>(x.ToString())); //serialize the individual model objects
                                foreach (var model in n)
                                    updateUserInfo(model.UserId ?? default(int), model);
                            }
                            catch (Exception oops)
                            {
                                //eat any exceptions-we'll do without a model list if there's an issue
                                //TODO: figure out something nicer to do here
                            }
                        }

                        OnUsersProcessed(new EventArgs());
                    }
                    break;

            }
        }

        class Server
        {
            public String Name { get; set; }
            public String Type { get; set; }
        }
        Server[] ServerList = new[]
	    {
		    new Server() { Name = "xchat11", Type = "hybi00" },
		    new Server() { Name = "xchat12", Type = "hybi00" },
		    new Server() { Name = "xchat20", Type = "hybi00" },
		    new Server() { Name = "xchat7", Type = "rfc6455" },
		    new Server() { Name = "xchat8", Type = "rfc6455" },
		    new Server() { Name = "xchat9", Type = "rfc6455" },
		    new Server() { Name = "xchat10", Type = "rfc6455" }
	    };
        private String _socketServer;
        public String WebsocketServerName
        {
            get
            {
                if (null == _socketServer)
                    _socketServer = ServerList[new Random().Next(ServerList.Length)].Name;
                return _socketServer;
            }
        }
    }
}
