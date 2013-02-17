<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Bson</Namespace>
  <Namespace>Newtonsoft.Json.Converters</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>Newtonsoft.Json.Schema</Namespace>
  <Namespace>Newtonsoft.Json.Serialization</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Timers</Namespace>
</Query>

void Main()
{

	var mfc = new MFCClient();
//	mfc.Received += (s,e)=>
//	{
//		if (e.Message.MessageType == MFCMessageType.FCTYPE_CMESG)
//			e.Message.Dump();
//	};
	mfc.JoinModelChatRoom("JALYN", (s,e)=>{e.Message.Dump();});

}

public class MFCClient
{
	//private properties
	WebClient _client = new WebClient();
	//_connectKey is a guid generated by the chat server when you first make a request to the server
	//that is used on all subsequent calls to the server.  Defaults to "connect"
	//which is replaced by the guid as soon as we get it
	String _connectKey = "connect"; 
	//_sessionId is used to identify request from a logged in client.  After the FCTYPE_LOGIN message
	//has been sent, the server will reply with a sessionId for use on subsequent calls
	String _sessionId = "0";
	//url format for calls to the chat server.  TODO: replace the hardcoded xchat3.
	//There are about 6 xchat servers identified for ajax use in the client javascript
	String serverUrlFormat = "http://xchat3.myfreecams.com/zgw/?mh={0}&ms={1}&mk=0";
	//url format used for getting initial list of online models.  TODO: remove hardcoded xchat3
	String modelsUrlFormat = "http://www.myfreecams.com/mfc2/php/mobj.php?f={0}&s=xchat3";
	
	Queue _receivedMsgs = new Queue();
	Queue _msgsToSend = new Queue();
	System.Timers.Timer _heartbeat = new System.Timers.Timer(1000);
	
	//constructor
	public MFCClient()
	{
		//set up the message poll
		_heartbeat.Elapsed += handleMsgs;
		_heartbeat.Enabled = true;
	}
	
	//public properties
	public event MFCMessageEventHandler Received; //tap into the message feed
	public int SessionID { get {return Int32.Parse(_sessionId);}}
	public IEnumerable<ModelInfo> Models { get; set; } //initial list of online models.  Need to figure out how to update
	                                                   //it (likely FCTYPE_SESSIONSTATE messages)
	private Boolean _connected = false;
	public Boolean Connected { get { return _connected;} }
	private Boolean _loggedIn = false;
	public Boolean LoggedIn { get { return _loggedIn;} }
	private int _userId;
	public int UserId { get { return _userId; } } //MFCs identifer for the user
	private String _userName;
	public String UserName{ get { return _userName; } set { _userName = value; } }
	
	//public methods
	public void StopPolling()
	{
		_heartbeat.Enabled = false;
	}
	public void StartPolling()
	{
		_heartbeat.Enabled = true;
	}
	public void SendMessage(MFCMessage msg)
	{
		_msgsToSend.Enqueue(msg);
	}
	//JoinModelChatRoom takes a modelname and a handler for receiving messages
	public void JoinModelChatRoom(String modelName, MFCMessageEventHandler onReceived)
	{
		//wait until we actually have a model list
		while(null == Models){} //TODO: change this so we don't get into an infinite loop
		//figure out what the broadcasterid is for the model
		var info = Models.Where(m => m.nm == modelName).FirstOrDefault();
		if (null == info)
			throw new Exception("Model doesn't appear to be online.");
		//don't know why but the broadcaster id is always
		//prefixed with a 10
		var broadcasterId = Int32.Parse(("10" + info.uid).ToString());
		//Queue a join message
		SendMessage(new MFCMessage()
		{
			MessageType = MFCMessageType.FCTYPE_JOINCHAN,
			From = SessionID,
			To = 0,
			Arg1 = broadcasterId,                          
			Arg2 = 9 //join channel + history	
		});	
		
		//Set up a filtered handler that only sends chat room messages
		Received += (sender, e) => 
		{
			if (e.Message.MessageType == MFCMessageType.FCTYPE_CMESG && e.Message.To == broadcasterId)
				onReceived(sender,e);
		};
	}
	
	//private methods
	protected virtual void OnReceived(MFCMessageEventArgs e)
	{
		if (null != Received)
			Received(this, e);
	}
	void queueMsgs(MFCChatResponse r)
	{
		foreach (var msg in r.Messages)
			_receivedMsgs.Enqueue(msg);
	}
	void handleMsgs(object sender, ElapsedEventArgs e)
	{
		//if there are no queued messages to send, send a null message
		//so that we get any new messages from the server
		if (_msgsToSend.Count == 0)
			SendMessage(new NullMessage());	
			
		//handle the send queue
		while (_msgsToSend.Count > 0)
		{
			//TODO: this should probably have some error handling for
			//problems connecting to MFC
			//TODO: handle multiple messages at a time
			var msgToSend = (MFCMessage)_msgsToSend.Dequeue();
			var response = new MFCChatResponse(_client.UploadString(String.Format(serverUrlFormat, _connectKey, _sessionId), msgToSend.AsMFCRequest()));
			queueMsgs(response);
		}

		//handle the received message queue
		while (_receivedMsgs.Count > 0)
		{
			var current = (MFCMessage)_receivedMsgs.Dequeue();
			
			//handle some of the messages now
			switch (current.MessageType)
			{
				case MFCMessageType.FCTYPE_CONNECTED:
				{
					//when we get a connected message, save the key for future requests
					_connectKey = current.Data;
					_connected = true;
					//kick off a guest login
					//TODO: allow authorized logins
					SendMessage(new GuestLoginMessage());
				}
				break;
				
				case MFCMessageType.FCTYPE_LOGIN:
				{
					//when we get a login message, if it's a success message, save the session id
					if (current.Arg1 == (int)MFCResponseType.FCRESPONSE_SUCCESS)
					{
						_loggedIn = true;
						_userId = current.Arg2;
						_userName = current.Data;
						_sessionId = current.To.ToString();
					}
					//not sure what to do here--for now, throw an exception
					if (current.Arg1 == (int)MFCResponseType.FCRESPONSE_ERROR)
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
					if (current.To == 20 && current.Arg1 == 0 && current.Arg2 > 0)
					{
						//figure out the file pointer
						var fileno = JsonConvert.DeserializeObject<MetricsPayload>(WebUtility.UrlDecode(current.Data)).fileno;
						//get the file
						var modelInfo = _client.DownloadString(String.Format(modelsUrlFormat, fileno));
						//strip out extraneous javascript
						modelInfo = modelInfo.Replace("var g_hModelData = ","");
						modelInfo = modelInfo.Replace("LoadModelsFromObject(g_hModelData);","");
						modelInfo = modelInfo.Replace("};","}");
						//attempt to deserialize the model JSON
						try
						{
							var m = JObject.Parse(modelInfo); //parse to a dynamic
							var n = m.Properties().Where(p=>!p.Name.StartsWith("tags")) //filter out the tags objects
							         .Children() //give us back JTokens
									 .Select(x => JsonConvert.DeserializeObject<ModelInfo>(x.ToString())); //serialize the individual model objects
							Models = n;
						}
						catch(Exception oops)
						{
							//eat any exceptions-we'll do without a model list if there's an issue
							//TODO: figure out something nicer to do here
						}
					}
				}
				break;
				
			}
			//fire the message received event for any subscribers
			OnReceived(new MFCMessageEventArgs(){ Message = current });
		}
	}
}

//Received event boilerplate
public delegate void MFCMessageEventHandler (object sender, MFCMessageEventArgs e);
public class MFCMessageEventArgs : EventArgs
{
	public MFCMessage Message { get; set; }
}

//helper class for parsing MFC responses
public class MFCChatResponse
{
	public MFCChatResponse(String response)
	{
		Messages = response.Split('"').Select(m => new MFCMessage(m));
	}
	
	public IEnumerable<MFCMessage> Messages {get; set;}
}

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
		var pos5 = IndexOfNth(msg,' ',5);
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
	public String AsMFCRequest()
	{
		var format = "pk0={0}%20{1}%20{2}%20{3}%20{4}%20{5}";
		return String.Format(format, (int)MessageType, From, To, Arg1, Arg2, (null == Data) ? "-" : Data);
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
		Data="guest:guest";
	}
}
public class JoinModelRoom : MFCMessage
{
	public JoinModelRoom(String modelName, int session)
	{
		MessageType = MFCMessageType.FCTYPE_JOINCHAN;
		From = session;
		To = 0;
		//Arg1 = ""; //"10" + modelid 
		Arg2 = 9;
	}
}

//Enumerations are lifted straight from the MFC client code
public enum MFCResponseType
{
	FCRESPONSE_SUCCESS = 0,
  	FCRESPONSE_ERROR = 1,
  	FCRESPONSE_NOTICE = 2
}
public enum MFCMessageType
{
	FCTYPE_NULL = 0,
	FCTYPE_LOGIN = 1,
	FCTYPE_ADDFRIEND = 2,
	FCTYPE_PMESG = 3,
	FCTYPE_STATUS = 4,
	FCTYPE_DETAILS = 5,
	FCTYPE_TOKENINC = 6,
	FCTYPE_ADDIGNORE = 7,
	FCTYPE_PRIVACY = 8,
	FCTYPE_ADDFRIENDREQ = 9,
	FCTYPE_USERNAMELOOKUP = 10,
	FCTYPE_ANNOUNCE = 13,
	FCTYPE_STUDIO = 14,
	FCTYPE_INBOX = 15,
	FCTYPE_RELOADSETTINGS = 17,
	FCTYPE_HIDEUSERS = 18,
	FCTYPE_RULEVIOLATION = 19,
	FCTYPE_SESSIONSTATE = 20,
	FCTYPE_REQUESTPVT = 21,
	FCTYPE_ACCEPTPVT = 22,
	FCTYPE_REJECTPVT = 23,
	FCTYPE_ENDSESSION = 24,
	FCTYPE_TXPROFILE = 25,
	FCTYPE_STARTVOYEUR = 26,
	FCTYPE_SERVERREFRESH = 27,
	FCTYPE_SETTING = 28,
	FCTYPE_BWSTATS = 29,
	FCTYPE_SETGUESTNAME = 30,
	FCTYPE_SETTEXTOPT = 31,
	FCTYPE_MODELGROUP = 33,
	FCTYPE_REQUESTGRP = 34,
	FCTYPE_STATUSGRP = 35,
	FCTYPE_GROUPCHAT = 36,
	FCTYPE_CLOSEGRP = 37,
	FCTYPE_UCR = 38,
	FCTYPE_MYUCR = 39,
	FCTYPE_SLAVEVSHARE = 43,
	FCTYPE_ROOMDATA = 44,
	FCTYPE_NEWSITEM = 45,
	FCTYPE_GUESTCOUNT = 46,
	FCTYPE_MODELGROUPSZ = 48,
	FCTYPE_CMESG = 50,
	FCTYPE_JOINCHAN = 51,
	FCTYPE_CREATECHAN = 52,
	FCTYPE_INVITECHAN = 53,
	FCTYPE_KICKCHAN = 54,
	FCTYPE_BANCHAN = 56,
	FCTYPE_PREVIEWCHAN = 57,
	FCTYPE_SETWELCOME = 61,
	FCTYPE_LISTCHAN = 63,
	FCTYPE_UEOPT = 67,
	FCTYPE_METRICS = 69,
	FCTYPE_OFFERCAM = 70,
	FCTYPE_REQUESTCAM = 71,
	FCTYPE_MYWEBCAM = 72,
	FCTYPE_MYCAMSTATE = 73,
	FCTYPE_PMHISTORY = 74,
	FCTYPE_CHATFLASH = 75,
	FCTYPE_TRUEPVT = 76,
	FCTYPE_REMOTEPVT = 77,
	FCTYPE_ZGWINVALID = 95,
	FCTYPE_CONNECTING = 96,
	FCTYPE_CONNECTED = 97,
	FCTYPE_DISCONNECTED = 98,
	FCTYPE_LOGOUT = 99
}

//javascript deserialization classes
public sealed class ChatRoomMsg
{
	public String lv { get; set; }
	public String msg { get; set; }
	public String nm { get; set; }
	public String sid { get; set; }
	public String uid { get; set; }
}
public sealed class MetricsPayload
{
	public String fileno { get; set; }
}
public sealed class ModelInfo
{
	public int lv { get; set; }
	public string nm { get; set; }
	public int sid { get; set; }
	public int uid { get; set; }
	public int vs { get; set; }
	public U u { get; set; }
	public M m { get; set; }
}
public sealed class U
{
	public int age { get; set; }
	public int avatar { get; set; }
	public string blurb { get; set; }
	public int camserv { get; set; }
	public int chat_bg { get; set; }
	public string chat_color { get; set; }
	public int chat_opt { get; set; }
	public string city { get; set; }
	public string country { get; set; }
	public int creation { get; set; }
	public string ethnic { get; set; }
	public int photos { get; set; }
	public int profile { get; set; }
}
public sealed class M
{
	public double camscore { get; set; }
	public string continent { get; set; }
	public int flags { get; set; }
	public int kbit { get; set; }
	public int lastnews { get; set; }
	public int mg { get; set; }
	public int missmfc { get; set; }
	public int new_model { get; set; }
	public int rank { get; set; }
	public string topic { get; set; }
}



