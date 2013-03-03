using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MFCChatClient
{
    public class MFCModelRoom : MFCChatRoom
    {
        public MFCModelRoom(String modelName)
            : base(modelName)
        {
            ChatMessageReceived += HandleTip;
        }

        public IList<Tip> Tips = new List<Tip>();

        public event EventHandler<MFCTipEventArgs> Tip;
        protected virtual void OnTip(MFCTipEventArgs e)
        {
            if (null != Tip)
                Tip(this, e);
        }

        void HandleTip(object sender, MFCChatMessageEventArgs e)
        {
            if (null != e.ChatMessage && !e.ChatMessage.IsTip)
                return;

            var tipMsg = e.ChatMessage.MessageData.Message;
            if (null != tipMsg && "" != tipMsg)
            {
                var match = Regex.Match(tipMsg, @"(\w*) has tipped (\w*) (\d*) tokens");
                var tip = new Tip() { Tipper = match.Groups[1].Value, Model = match.Groups[2].Value, Amount = Int32.Parse(match.Groups[3].Value) };
                Tips.Add(tip);
                OnTip(new MFCTipEventArgs() { Tip = tip });
            }
        }
    }
}
