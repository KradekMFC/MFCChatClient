<Query Kind="Program">
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>WebSocket4Net</NuGetReference>
  <Namespace>Newtonsoft.Json</Namespace>
  <Namespace>Newtonsoft.Json.Bson</Namespace>
  <Namespace>Newtonsoft.Json.Converters</Namespace>
  <Namespace>Newtonsoft.Json.Linq</Namespace>
  <Namespace>Newtonsoft.Json.Schema</Namespace>
  <Namespace>Newtonsoft.Json.Serialization</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Timers</Namespace>
  <Namespace>WebSocket4Net</Namespace>
</Query>

void Main()
{
	var mfc = new MFCClient();
//	//testing multiple rooms
//	mfc.JoinModelChatRoom("Roxie18", (m)=>{ m.Dump(); });
//	//mfc.JoinModelChatRoom("AwesomeKate", (m)=>{if (m.IsTip) m.Dump();});
	mfc.JoinModelChatRoom("Brina_", (m)=>{if (m.IsTip) m.Dump();});
	mfc.JoinModelChatRoom("RachelSinger", (m)=>{if (m.IsTip) m.Dump();});
	mfc.JoinModelChatRoom("JALYN", (m)=>{if (m.IsTip) m.Dump();});
	mfc.JoinModelChatRoom("Bry_Anna", (m)=>{if (m.IsTip) m.Dump();});

}

public class MFCClient
{
	//private properties
	WebClient _client = new WebClient();
	WebSocket _socket = new WebSocket("ws://xchat8.myfreecams.com:8080/fcsl");
	
	String _sessionId = "0";
	//url format used for getting initial list of online models.  TODO: remove hardcoded xchat3
	String modelsUrlFormat = "http://www.myfreecams.com/mfc2/php/mobj.php?f={0}&s=xchat8";

	System.Timers.Timer _ping = new System.Timers.Timer(15000);
	
	//constructor
	public MFCClient()
	{
		//setup up the socket
		_socket.Opened += onSocketOpened;
		_socket.Error += onSocketError;
		_socket.MessageReceived += onSocketMessage;
		
		//set up a ping so the server doesn't close our connection
		_ping.Elapsed += onPing;
		_ping.Enabled = true;
		
		_socket.Open();
		
		Received += internalHandler;
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
	

	public void SendMessage(MFCMessage msg)
	{
		_socket.Send(msg.AsSocketMsg());
	}
	//JoinModelChatRoom takes a modelname and a handler for receiving messages
	public void JoinModelChatRoom(String modelName, ChatMessageHandler onMsg)
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
			//leave if this is not a message for our room
			if (e.Message.MessageType != MFCMessageType.FCTYPE_CMESG || e.Message.To != broadcasterId)
				return;
			
			//leave if there is no message data
			if ("" == e.Message.Data || null == e.Message.Data)
				return;

			try
			{
				var decoded = WebUtility.UrlDecode(e.Message.Data);
				var msg = JsonConvert.DeserializeObject<ChatMessage>(decoded); 
				onMsg(msg);
			}
			catch (Exception err)
			{
				err.Dump();
				e.Message.Dump();
			}
				
		};
	}
	
	//private methods
	protected virtual void OnReceived(MFCMessageEventArgs e)
	{
		if (null != Received)
			Received(this, e);
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
		e.Exception.Message.Dump();
	}
	
	void onSocketMessage(object sender, MessageReceivedEventArgs e)
	{
		var msg = e.Message;
		try
		{
			
			while (msg.Length > 0)
			{
				var dataLen = Int32.Parse(msg.Substring(0,4));
				var data = msg.Substring(4, dataLen);
				OnReceived(new MFCMessageEventArgs(){Message = new MFCMessage(data)});
	
				msg = msg.Substring(dataLen + 4);
			}
		}
		catch (Exception err)
		{
			msg.Dump();
		}
	}
	
	void onPing(object sender, EventArgs e)
	{
		SendMessage(new NullMessage());
	}
	
	void internalHandler(object sender, MFCMessageEventArgs e)
	{
		var msg = e.Message;
		
		switch (msg.MessageType)
		{
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
					msg.Dump();
					var fileno = JsonConvert.DeserializeObject<MetricsPayload>(WebUtility.UrlDecode(msg.Data)).fileno;
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
	}
}


//Received event boilerplate
public delegate void MFCMessageEventHandler (object sender, MFCMessageEventArgs e);
public class MFCMessageEventArgs : EventArgs
{
	public MFCMessage Message { get; set; }
}
public delegate void ChatMessageHandler(ChatMessage m);

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
		//The first four characters of messages from the socket
		//server indicate the message length
		//var len = msg.Substring(0,4);
		//msg = msg.Substring(4); //remove the len
		
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
	public String AsSocketMsg()
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
public enum MFCVideoState
{
	FCVIDEO_TX_IDLE = 0,
	FCVIDEO_TX_RESET = 1,
	FCVIDEO_TX_AWAY = 2,
	FCVIDEO_TX_CONFIRMING = 11,
	FCVIDEO_TX_PVT = 12,
	FCVIDEO_TX_GRP = 13,
	FCVIDEO_TX_KILLMODEL = 15,
	FCVIDEO_RX_IDLE = 90,
	FCVIDEO_RX_PVT = 91,
	FCVIDEO_RX_VOY = 92,
	FCVIDEO_RX_GRP = 93,
	FCVIDEO_UNKNOWN = 127
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
//for future reference
//public enum MFCFonts
// 0: { name: 'Arial' },
// 2: { name: 'Comic Sans MS' },
// 3: { name: 'Courier New' },
// 4: { name: 'Georgia' },
// 5: { name: 'Lucida Console, Monaco' },
// 6: { name: 'Lucida Sans Unicode' },
// 7: { name: 'MS Sans Serif' },
// 8: { name: 'Palatino Linotype, Book Antiqua' },
// 9: { name: 'Tahoma, Geneva' },
// 10: { name: 'Times New Roman' },
// 11: { name: 'Helvetica' },
// 12: { name: 'Verdana' },
// 13: { name: 'Arial Narrow' },
// 15: { name: 'Book Antiqua' },
// 16: { name: 'Bookman Old Style' },
// 17: { name: 'Bradley Hand ITC' },
// 18: { name: 'Century Gothic' },
// 19: { name: 'Copperplate Gothic Bold', no_bold:1 },
// 20: { name: 'Copperplate Gothic Light' },
// 21: { name: 'Engravers MT' },
// 22: { name: 'Eras Demi ITC' },
// 23: { name: 'Eras Light ITC' },
// 24: { name: 'Estrangelo Edessa' },
// 25: { name: 'Eurostile' },
// 26: { name: 'Felix Titling' },
// 27: { name: 'Fixedsys' },
// 28: { name: 'Franklin Gothic Book' },
// 29: { name: 'Franklin Gothic Demi', no_bold:1 },
// 30: { name: 'Franklin Gothic Demi Cond' },
// 31: { name: 'Franklin Gothic Medium' },
// 32: { name: 'Franklin Gothic Medium Cond' },
// 33: { name: 'Garamond' },
// 35: { name: 'Kristen ITC' },
// 36: { name: 'Latha, Mangal' },
// 37: { name: 'Lucida Sans' },
// 38: { name: 'Lucida Sans Unicode,Lucida Grande' },
// 39: { name: 'Maiandra GD' },
// 41: { name: 'Microsoft Sans Serif' },
// 42: { name: 'Monospace' },
// 43: { name: 'Monotype Corsiva', no_bold:1 },
// 44: { name: 'MS Reference Sans Serif' },
// 45: { name: 'MS Serif,New York' },
// 46: { name: 'MV Boli' },
// 47: { name: 'OCR A Extended' },
// 48: { name: 'Papyrus' },
// 49: { name: 'Perpetua' },
// 50: { name: 'Raavi' },
// 51: { name: 'Rockwell' },
// 52: { name: 'Sans-serif' },
// 53: { name: 'Serif' },
// 54: { name: 'Shruti' },
// 55: { name: 'Sydnie' },
// 56: { name: 'Sylfaen' },
// 57: { name: 'System' },
// 58: { name: 'Tempus Sans ITC' },
// 60: { name: 'Times' },
// 61: { name: 'Trebuchet MS' }


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

public class UserChatOptions
{
	[JsonProperty(PropertyName = "chat_color")]
    public string ChatColor { get; set; }
	[JsonProperty(PropertyName = "chat_font")]
    public int ChatFont { get; set; }
}

public class ChatMessage
{
	[JsonProperty(PropertyName = "lv")]
    public int AccessLevel { get; set; }
	[JsonProperty(PropertyName = "msg")]
    public string Message { get; set; }
	[JsonProperty(PropertyName = "nm")]
    public string Name { get; set; }
	[JsonProperty(PropertyName = "sid")]
    public int SessionID { get; set; }
	[JsonProperty(PropertyName = "uid")]
    public int UserID { get; set; }
	[JsonProperty(PropertyName = "vs")]
    public MFCVideoState VideoState { get; set; } 
	[JsonProperty(PropertyName = "u")]
    public UserChatOptions ChatOptions { get; set; }
	[JsonIgnore]
	public Boolean IsTip
	{
		get
		{
			return ("FCServer" == Name && Message.Contains("has tipped"));
		}
	}
}