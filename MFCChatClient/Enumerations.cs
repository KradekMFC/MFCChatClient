using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFCChatClient
{
    //Enumerations are lifted straight from the MFC client code
    public enum MFCChatOpt
    {
        FCCHAN_NOOPT = 0,
        FCCHAN_JOIN = 1,
        FCCHAN_PART = 2,
        FCCHAN_BATCHPART = 64,
        FCCHAN_OLDMSG = 4,
        FCCHAN_HISTORY = 8,
        FCCHAN_CAMSTATE = 16,
        FCCHAN_WELCOME = 32
    }
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
        FCTYPE_TAGS = 64,
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

    public enum MFCLounges
    {
        Lounge = 486121,
        Lounge1000 = 486123,
        Lounge10000 = 486124
    }

    public enum MFCAccessLevel
    {
        Guest = 0,
        Basic = 1,
        Premium = 2,
        Model = 4,
        Admin = 5
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

}
