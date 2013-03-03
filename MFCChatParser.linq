<Query Kind="Program">
  <Reference Relative="MFCChatClient\bin\Debug\MFCChatClient.dll">C:\Users\Bert\Dropbox\dev\MFCChatClient\MFCChatClient\bin\Debug\MFCChatClient.dll</Reference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <NuGetReference>WebSocket4Net</NuGetReference>
  <Namespace>MFCChatClient</Namespace>
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

	IList<Tip> AllTips = new List<Tip>();
	EventHandler<MFCTipEventArgs> handler = (s,e) => 
	{
		Util.ClearResults();
		AllTips.Add(e.Tip);
		AllTips.Dump();
//		(from tip in ((MFCModelRoom)s).Tips
//		 group tip by tip.Tipper into g
//		 select new 
//		 {
//		 	Tipper=g.Key, 
//			Count=g.Count(), 
//			Total=g.Sum(x=>x.Amount)
//		 }).Dump();
	};
	
	var mfc = new MFCModelRoom("AmyAsian");
	
	mfc.Tip += handler;

	
	//Overview of message types received
//	var msgCount = new Dictionary<MFCMessageType, int>();
//	mfc.Received += (s,e) => 
//	{
//		if (!msgCount.ContainsKey(e.Message.MessageType))
//			msgCount.Add(e.Message.MessageType, 1);
//		else
//			msgCount[e.Message.MessageType]++;
//		Util.ClearResults();
//		msgCount.Dump();
//	};

}


