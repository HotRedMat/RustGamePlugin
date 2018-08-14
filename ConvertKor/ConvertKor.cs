using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Oxide.Plugins
{
    [Info("ConvertKor", "RedMat", "1.7.0", ResourceId = 0)]
    [Description("영문 한글 변환")]
     
    public class ConvertKor : RustPlugin
    {
        private enum MESSAGE_GET_LANG_MODE { USER, SERVER };

        private RustPlugin plugin;
        private Dictionary<ulong, bool> convertKorUserSet;

        private const string ENG_KEY = "rRseEfaqQtTdwWczxvgkoiOjpuPhynbml";
        private const string KOR_KEY = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎㅏㅐㅑㅒㅓㅔㅕㅖㅗㅛㅜㅠㅡㅣ";
        private const string CHO_DATA = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";
        private const string JUNG_DATA = "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
        private const string JONG_DATA = "ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";

        // 파라미터 1개 명령어 대문자로 설정
        Dictionary<String, String> cmdOneParamLists = new Dictionary<String, String>();
        // 파라미터 2개 명령어 대문자로 설정
        Dictionary<String, String> cmdTowParamLists = new Dictionary<String, String>();

        ForbiddenWordData forbiddenWordData;

        private void Init()
        {
            convertKorUserSet = new Dictionary<ulong, bool>();

            // 파라미터 한개 명령어
            cmdOneParamLists.Add("C", "클랜채팅");
            cmdOneParamLists.Add("R", "PrivateMessage 답변");

            // 파라미터 두개 명령어
            cmdTowParamLists.Add("PM", "PrivateMessage 귓말");
        }

        void Loaded()
        {
            SetDefaultConfig();
            LoadMessages();

            Plugin betterSay = plugins.Find("BetterSay");

            if (null != betterSay)
            {
                PrintWarning(GetMessage("duplicatePlugin", null, MESSAGE_GET_LANG_MODE.SERVER));

                if (null != betterSay && betterSay.IsLoaded)
                {
                    rust.RunServerCommand("plugin.unload", new object[] { "BetterSay" });
                }
            }

            foreach (BasePlayer basePlayer in BasePlayer.activePlayerList)
            {
                convertKorUserSet.Add(basePlayer.userID, true);

                DestroyCui(basePlayer, "Sttus");
                CreateCui(basePlayer, "Sttus");

                SendMessage(basePlayer, GetMessage("loadPlugin", basePlayer.UserIDString));

                PrintDefaultKorMessage(basePlayer);
            }

            permission.RegisterPermission("server.admin", this);

            forbiddenWordData = Interface.Oxide.DataFileSystem.ReadObject<ForbiddenWordData>("ConvertKor_ForbiddenWordData");
        }

        [HookMethod("OnPlayerInit")]
        void OnPlayerInit(BasePlayer basePlayer)
        {
            if (basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(2, () => OnPlayerInit(basePlayer));

                PrintDefaultKorMessage(basePlayer);
            }

            DestroyCui(basePlayer, "Sttus");
            CreateCui(basePlayer, "Sttus");
        }

        [ChatCommand("nsvr")]
        private void chatCommandChangeServerName(BasePlayer basePlayer, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(basePlayer.UserIDString, "server.admin"))
            {
                SendMessage(basePlayer, GetMessage("notPermission", basePlayer.UserIDString));
                return;
            }

            UpdateConfig("serverName", GetConvertKor(args?[0]));

            PrintWarning(String.Format(GetMessage("configSetServerName", "", MESSAGE_GET_LANG_MODE.SERVER), Config["serverName"]));
            SendMessage(basePlayer, String.Format(GetMessage("configSetServerName", basePlayer.UserIDString), Config["serverName"]));
        }

        [ConsoleCommand("server.name")]
        private void consoleCommandChangeServerName(ConsoleSystem.Arg arg)
        {
            String cmdFullName = arg?.cmd?.FullName;
            String serverName = GetConvertKor(arg?.Args[0]);
            BasePlayer basePlayer = arg?.Connection?.player as BasePlayer;

            if(null != basePlayer)
            {
                if (!permission.UserHasPermission(basePlayer.UserIDString, "server.admin"))
                {
                    SendMessage(basePlayer, GetMessage("notPermission", basePlayer.UserIDString));
                    return;
                }

                UpdateConfig("serverName", serverName);

                SendMessage(basePlayer, String.Format(GetMessage("configSetServerName", basePlayer.UserIDString), Config["serverName"]));
            }
            else
            {
                UpdateConfig("serverName", serverName);
            }

            PrintWarning(String.Format(GetMessage("configSetServerName", "", MESSAGE_GET_LANG_MODE.SERVER), Config["serverName"]));
        }

        [ChatCommand("cnsvr")]
        private void chatCommandChangeServerNameColor(BasePlayer basePlayer, String cmd, String[] args)
        {
            if (!permission.UserHasPermission(basePlayer.UserIDString, "server.admin"))
            {
                SendMessage(basePlayer, GetMessage("notPermission", basePlayer.UserIDString));
                return;
            }

            UpdateConfig("serverNameColor", args?[0]);

            PrintWarning(String.Format(GetMessage("configSetServerName", "", MESSAGE_GET_LANG_MODE.SERVER), Config["serverNameColor"]));
            SendMessage(basePlayer, String.Format(GetMessage("configSetServerNameColor", basePlayer.UserIDString), Config["serverNameColor"]));
        }

        [ConsoleCommand("server.name.color")]
        private void consoleCommandChangeServerNameColor(ConsoleSystem.Arg arg)
        {
            String cmdFullName = arg?.cmd?.FullName;
            String serverNameColor = arg?.Args[0];
            BasePlayer basePlayer = arg?.Connection?.player as BasePlayer;

            if (null != basePlayer)
            {
                if (!permission.UserHasPermission(basePlayer.UserIDString, "server.admin"))
                {
                    SendMessage(basePlayer, GetMessage("notPermission", basePlayer.UserIDString));
                    return;
                }

                UpdateConfig("serverNameColor", serverNameColor);

                SendMessage(basePlayer, String.Format(GetMessage("configSetServerNameColor", basePlayer.UserIDString), Config["serverNameColor"]));
            }
            else
            {
                UpdateConfig("serverNameColor", serverNameColor);
            }

            PrintWarning(String.Format(GetMessage("configSetServerNameColor", "", MESSAGE_GET_LANG_MODE.SERVER), Config["serverNameColor"]));
        }

        [ChatCommand("h")]
        private void chatCommandH(BasePlayer basePlayer, String cmd, String[] args)
        {
            PrintChangeKorMessage(basePlayer);

            DestroyCui(basePlayer, "Sttus");
            CreateCui(basePlayer, "Sttus");
        }

        private void PrintDefaultKorMessage(BasePlayer basePlayer)
        {
            String nowMode = "";
            String message = GetMessage("nowMode", basePlayer.UserIDString);
            if (isConvertKor(basePlayer))
            {
                nowMode = GetMessage("hangulMode", basePlayer.UserIDString);
            }
            else
            {
                nowMode = GetMessage("englishMode", basePlayer.UserIDString);
            }

            SendMessage(basePlayer, String.Format(message, nowMode));
            SendMessage(basePlayer, GetMessage("changeMode", basePlayer.UserIDString));
        }

        private void PrintChangeKorMessage(BasePlayer basePlayer)
        {
            String nowMode = "";
            String message = GetMessage("nowMode", basePlayer.UserIDString);
            if (isConvertKor(basePlayer))
            {
                convertKorUserSet[basePlayer.userID] = false;
                nowMode = GetMessage("englishMode", basePlayer.UserIDString);
            }
            else
            {
                convertKorUserSet[basePlayer.userID] = true;
                nowMode = GetMessage("hangulMode", basePlayer.UserIDString);
            }

            SendMessage(basePlayer, String.Format(message, nowMode));
        }

        private BasePlayer GetBasePlayer(ulong userID)
        {
            foreach (BasePlayer basePlayer in BasePlayer.activePlayerList)
            {
                if (basePlayer.userID == userID) return basePlayer;
            }

            return null;
        }

        private BasePlayer GetBasePlayer(String userName)
        {
            foreach (BasePlayer basePlayer in BasePlayer.activePlayerList)
            {
                if (basePlayer.IPlayer.Name == userName) return basePlayer;
            }

            return null;
        }

        private Boolean isConvertKor(BasePlayer basePlayer)
        {
            if (!convertKorUserSet.ContainsKey(basePlayer.userID)) convertKorUserSet.Add(basePlayer.userID, true);

            return convertKorUserSet[basePlayer.userID];
        }

        private void SendMessage(BasePlayer basePlayer, String message)
        {
            basePlayer.SendConsoleCommand("chat.add", "", "<color=" + Config["serverNameColor"] + ">" + Config["serverName"] + "</color> " + message);
        }

        private void DestroyCui(BasePlayer basePlayer, String cuiName)
        {
            CuiHelper.DestroyUi(basePlayer, cuiName + "Panel");
        }

        private void CreateCui(BasePlayer basePlayer, String cuiName)
        {
            CuiElementContainer cElementContainer = new CuiElementContainer();
            cElementContainer.Add(new CuiPanel
            {
                Image =
                    {
                        Color = "0.8 1 0.6 1"
                    },
                RectTransform =
                    {
                        AnchorMin = "0 0.125",
                        AnchorMax = "0.012 0.165"
                    },
                CursorEnabled = false
            }, "Hud", cuiName + "Panel");
            cElementContainer.Add(new CuiLabel
            {
                Text =
                    {
                        Color = "0.2 0 1 1.1",
                        FontSize = 15,
                        Align = UnityEngine.TextAnchor.MiddleCenter,
                        Text = convertKorUserSet[basePlayer.userID] ? "한" : "영"
                    },
                RectTransform =
                    {
                        AnchorMin = "0 0.138",
                        AnchorMax = "1 0.862"
                    }
            }, cuiName + "Panel", cuiName + "Label");

            CuiHelper.AddUi(basePlayer, cElementContainer);
        }

        [HookMethod("OnServerCommand")]
        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg?.cmd?.FullName == null || arg.cmd.FullName == "global.say")
            {
                String message = GetConvertKor(string.Join(" ", arg.Args));

                Server.Broadcast("<color=" + Config["serverNameColor"] + ">" + Config["serverName"] + "</color> " + message);
                Puts(Config["serverName"] + " " + message);

                Plugin betterSay = plugins.Find("BetterSay");

                if (null != betterSay)
                {
                    PrintWarning(GetMessage("duplicatePlugin", null, MESSAGE_GET_LANG_MODE.SERVER));

                    if (null != betterSay && betterSay.IsLoaded)
                    {
                        rust.RunServerCommand("plugin.unload", new object[] { "BetterSay" });
                    }
                }

                return true;
            }
            
            return arg;
        }

        private object OnPlayerCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer basePlayer = arg.Connection.player as BasePlayer;

            if ("chat.say" == arg.cmd.FullName && isConvertKor(basePlayer) && !isContainHangul(arg.FullString))
            {
                if (null != arg.FullString)
                {
                    String[] cmdMessage = arg.FullString.Replace("\"", "").Split(' ');
                    String cnvMessage = "";
                    String cmd = cmdMessage[0].ToUpper().Replace("/", "").Replace("\"", "");

                    if (null != cmdMessage && null != cmdMessage[0])
                    {
                        if (cmdOneParamLists.ContainsKey(cmd))
                        {
                            for (int i = 1; i < cmdMessage.Length; i++)
                            {
                                cnvMessage = cnvMessage + cmdMessage[i];
                            }

                            cnvMessage = cmdMessage[0] + " " + GetConvertKor(cnvMessage);
                        }
                        if (cmdTowParamLists.ContainsKey(cmd))
                        {
                            for (int i = 2; i < cmdMessage.Length; i++)
                            {
                                cnvMessage = cnvMessage + cmdMessage[i];
                            }

                            cnvMessage = cmdMessage[0] + " " + cmdMessage[1] + " " + GetConvertKor(cnvMessage);
                        }

                        if (cmdOneParamLists.ContainsKey(cmd) || cmdTowParamLists.ContainsKey(cmd))
                        {
                            BasePlayer cmdBasePlayer = arg.Connection.player as BasePlayer;
                            cmdBasePlayer.Command("chat.say", cnvMessage);

                            return false;
                        }
                    }
                }
            }

            return null;
        }

        private String GetConvertKor(String text)
        {
            if (!isContainHangul(text)) text = engTypeToKor(text);

            return text;
        }

        private void LogChatFile(String fileName, String text, String playerId, String playerName)
        {
            LogToFile(fileName, "[" + DateTime.Now.ToString("HH:mm:ss") + "]" + "[" + playerId + "] " + playerName + ": " + text, this);
        }

        [HookMethod("OnBetterChat")]
        private object OnBetterChat(Dictionary<string, object> data)
        {
            String inMessage = (String) data["Text"];
            String convertMessage = "";

            IPlayer player = (IPlayer)data["Player"];

            BasePlayer basePlayer = GetBasePlayer(ulong.Parse(player.Id));

            convertMessage = (isConvertKor(basePlayer) ? GetConvertKor(inMessage) : inMessage);

            LogChatFile("", convertMessage, player.Id, player.Name);

            data["Text"] = GetChangeForbiddenWord(convertMessage);

            return data;
        }

        [HookMethod("OnPlayerChat")]
        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            BasePlayer inputChatBasePlayer = arg.Connection.player as BasePlayer;
            String playerName       = arg.Connection.username;
            String message          = string.Join(" ", arg.Args);
            String convertMessage   = (isConvertKor(inputChatBasePlayer) ? GetConvertKor(message) : message);

            if (!isUsePlugin("BetterChat"))
            {
                LogChatFile("", convertMessage, arg.Connection.userid.ToString(), playerName);

                foreach (BasePlayer basePlayer in BasePlayer.activePlayerList)
                {
                    basePlayer.SendConsoleCommand("chat.add", new object[] { arg.Connection.userid, "<color=#ffaa55>" + playerName + ": </color>" + GetChangeForbiddenWord(convertMessage) });
                }

                Puts(playerName + ": " + convertMessage);
            }

            return arg;
        }

        private String GetChangeForbiddenWord(String text)
        {
            bool forbiddenWordAt = false;
            // 특문 제거
            String delCharMessage = Regex.Replace(text, @"[^a-zA-Z0-9가-힣]", "", RegexOptions.Singleline);

            String[] diffTexts = forbiddenWordData.word;

            foreach(String diffText in diffTexts)
            {
                if (delCharMessage.Contains(diffText))
                {
                    char[] diffTextArray = diffText.ToArray<char>();
                    char[] messageCharArray = text.ToArray<char>();

                    for (int i = 0; i < messageCharArray.Length; i++)
                    {
                        String cutText = Regex.Replace(text.Substring(i, text.Length - i), @"[^a-zA-Z0-9가-힣]", "", RegexOptions.Singleline);

                        if (cutText.Length < diffText.Length) break;

                        if (cutText.Substring(0, diffText.Length).Equals(diffText) && messageCharArray[i].Equals(diffTextArray[0]))
                        {
                            String endAt = "N";
                            int nextIndex = 0;

                            while ("N".Equals(endAt))
                            {
                                if (messageCharArray[i].Equals(diffTextArray[nextIndex]))
                                {
                                    forbiddenWordAt = true;
                                    messageCharArray[i] = char.Parse(Config["chatChangeChar"].ToString());
                                    nextIndex++;
                                }

                                if (nextIndex == diffText.Length) endAt = "Y";
                                i++;
                            }
                        }
                    }

                    text = new string(messageCharArray);

                    text = forbiddenWordAt ? "<color=#008000ff><b>[금지단어 사용]</b></color> " + text : text;
                }
            }

            return text;
        }

        private Boolean isUsePlugin(String pluginName)
        {
            foreach (Plugin plugin in plugins.GetAll())
            {
                if (pluginName == plugin.Name) return true;
            }

            return false;
        }

        public bool isContainHangul(string s)
        {
            char[] charArr = s.ToCharArray();
            foreach (char c in charArr)
            {
                if (char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.OtherLetter) return true;
            }
            return false;
        }

        private string engTypeToKor(string src)
        {
            var res = "";
            if (src.Length == 0)
                return res;

            Int32 nCho = -1, nJung = -1, nJong = -1;        // 초성, 중성, 종성

            int intNotChange = 0;

            for (int i = 0; i < src.Length; i++)
            {
                string ch = src[i].ToString();
                int p = ENG_KEY.IndexOf(ch);

                if (src.Length > 1 && (i + 2) <= src.Length)
                {
                    if ("##".Equals(src.Substring(i, 2)))
                    {
                        intNotChange = intNotChange + 1;
                        i += 1;
                        continue;
                    }
                }

                if (intNotChange % 2 != 0)
                {
                    res += src.Substring(i, 1);
                    continue;
                }

                int intMod = intNotChange % 2;

                if (p == -1)
                {               // 영자판이 아님
                                // 남아있는 한글이 있으면 처리
                    if (nCho != -1)
                    {
                        if (nJung != -1)                // 초성+중성+(종성)
                            res += makeHangul(nCho, nJung, nJong);
                        else                            // 초성만
                            res += CHO_DATA[nCho];
                    }
                    else {
                        if (nJung != -1)                // 중성만
                            res += JUNG_DATA[nJung];
                        else if (nJong != -1)           // 복자음
                            res += JONG_DATA[nJong];
                    }
                    nCho = -1;
                    nJung = -1;
                    nJong = -1;
                    res += ch;
                }
                else if (p < 19)
                {           // 자음
                    if (nJung != -1)
                    {
                        if (nCho == -1)
                        {                   // 중성만 입력됨, 초성으로
                            res += JUNG_DATA[nJung];
                            nJung = -1;
                            nCho = CHO_DATA.IndexOf(KOR_KEY[p]);
                        }
                        else {                          // 종성이다
                            if (nJong == -1)
                            {               // 종성 입력 중
                                nJong = JONG_DATA.IndexOf(KOR_KEY[p]);
                                if (nJong == -1)
                                {           // 종성이 아니라 초성이다
                                    res += makeHangul(nCho, nJung, nJong);
                                    nCho = CHO_DATA.IndexOf(KOR_KEY[p]);
                                    nJung = -1;
                                }
                            }
                            else if (nJong == 0 && p == 9)
                            {           // ㄳ
                                nJong = 2;
                            }
                            else if (nJong == 3 && p == 12)
                            {           // ㄵ
                                nJong = 4;
                            }
                            else if (nJong == 3 && p == 18)
                            {           // ㄶ
                                nJong = 5;
                            }
                            else if (nJong == 7 && p == 0)
                            {           // ㄺ
                                nJong = 8;
                            }
                            else if (nJong == 7 && p == 6)
                            {           // ㄻ
                                nJong = 9;
                            }
                            else if (nJong == 7 && p == 7)
                            {           // ㄼ
                                nJong = 10;
                            }
                            else if (nJong == 7 && p == 9)
                            {           // ㄽ
                                nJong = 11;
                            }
                            else if (nJong == 7 && p == 16)
                            {           // ㄾ
                                nJong = 12;
                            }
                            else if (nJong == 7 && p == 17)
                            {           // ㄿ
                                nJong = 13;
                            }
                            else if (nJong == 7 && p == 18)
                            {           // ㅀ
                                nJong = 14;
                            }
                            else if (nJong == 16 && p == 9)
                            {           // ㅄ
                                nJong = 17;
                            }
                            else {                      // 종성 입력 끝, 초성으로
                                res += makeHangul(nCho, nJung, nJong);
                                nCho = CHO_DATA.IndexOf(KOR_KEY[p]);
                                nJung = -1;
                                nJong = -1;
                            }
                        }
                    }
                    else {                              // 초성 또는 (단/복)자음이다
                        if (nCho == -1)
                        {                   // 초성 입력 시작
                            if (nJong != -1)
                            {               // 복자음 후 초성
                                res += JONG_DATA[nJong];
                                nJong = -1;
                            }
                            nCho = CHO_DATA.IndexOf(KOR_KEY[p]);
                        }
                        else if (nCho == 0 && p == 9)
                        {           // ㄳ
                            nCho = -1;
                            nJong = 2;
                        }
                        else if (nCho == 2 && p == 12)
                        {           // ㄵ
                            nCho = -1;
                            nJong = 4;
                        }
                        else if (nCho == 2 && p == 18)
                        {           // ㄶ
                            nCho = -1;
                            nJong = 5;
                        }
                        else if (nCho == 5 && p == 0)
                        {           // ㄺ
                            nCho = -1;
                            nJong = 8;
                        }
                        else if (nCho == 5 && p == 6)
                        {           // ㄻ
                            nCho = -1;
                            nJong = 9;
                        }
                        else if (nCho == 5 && p == 7)
                        {           // ㄼ
                            nCho = -1;
                            nJong = 10;
                        }
                        else if (nCho == 5 && p == 9)
                        {           // ㄽ
                            nCho = -1;
                            nJong = 11;
                        }
                        else if (nCho == 5 && p == 16)
                        {           // ㄾ
                            nCho = -1;
                            nJong = 12;
                        }
                        else if (nCho == 5 && p == 17)
                        {           // ㄿ
                            nCho = -1;
                            nJong = 13;
                        }
                        else if (nCho == 5 && p == 18)
                        {           // ㅀ
                            nCho = -1;
                            nJong = 14;
                        }
                        else if (nCho == 7 && p == 9)
                        {           // ㅄ
                            nCho = -1;
                            nJong = 17;
                        }
                        else {                          // 단자음을 연타
                            res += CHO_DATA[nCho];
                            nCho = CHO_DATA.IndexOf(KOR_KEY[p]);
                        }
                    }
                }
                else {                                  // 모음
                    if (nJong != -1)
                    {                       // (앞글자 종성), 초성+중성
                                            // 복자음 다시 분해
                        Int32 newCho;           // (임시용) 초성
                        if (nJong == 2)
                        {                   // ㄱ, ㅅ
                            nJong = 0;
                            newCho = 9;
                        }
                        else if (nJong == 4)
                        {           // ㄴ, ㅈ
                            nJong = 3;
                            newCho = 12;
                        }
                        else if (nJong == 5)
                        {           // ㄴ, ㅎ
                            nJong = 3;
                            newCho = 18;
                        }
                        else if (nJong == 8)
                        {           // ㄹ, ㄱ
                            nJong = 7;
                            newCho = 0;
                        }
                        else if (nJong == 9)
                        {           // ㄹ, ㅁ
                            nJong = 7;
                            newCho = 6;
                        }
                        else if (nJong == 10)
                        {           // ㄹ, ㅂ
                            nJong = 7;
                            newCho = 7;
                        }
                        else if (nJong == 11)
                        {           // ㄹ, ㅅ
                            nJong = 7;
                            newCho = 9;
                        }
                        else if (nJong == 12)
                        {           // ㄹ, ㅌ
                            nJong = 7;
                            newCho = 16;
                        }
                        else if (nJong == 13)
                        {           // ㄹ, ㅍ
                            nJong = 7;
                            newCho = 17;
                        }
                        else if (nJong == 14)
                        {           // ㄹ, ㅎ
                            nJong = 7;
                            newCho = 18;
                        }
                        else if (nJong == 17)
                        {           // ㅂ, ㅅ
                            nJong = 16;
                            newCho = 9;
                        }
                        else {                          // 복자음 아님
                            newCho = CHO_DATA.IndexOf(JONG_DATA[nJong]);
                            nJong = -1;
                        }
                        if (nCho != -1)         // 앞글자가 초성+중성+(종성)
                            res += makeHangul(nCho, nJung, nJong);
                        else                    // 복자음만 있음
                            res += JONG_DATA[nJong];

                        nCho = newCho;
                        nJung = -1;
                        nJong = -1;
                    }
                    if (nJung == -1)
                    {                       // 중성 입력 중
                        nJung = JUNG_DATA.IndexOf(KOR_KEY[p]);
                    }
                    else if (nJung == 8 && p == 19)
                    {            // ㅘ
                        nJung = 9;
                    }
                    else if (nJung == 8 && p == 20)
                    {            // ㅙ
                        nJung = 10;
                    }
                    else if (nJung == 8 && p == 32)
                    {            // ㅚ
                        nJung = 11;
                    }
                    else if (nJung == 13 && p == 23)
                    {           // ㅝ
                        nJung = 14;
                    }
                    else if (nJung == 13 && p == 24)
                    {           // ㅞ
                        nJung = 15;
                    }
                    else if (nJung == 13 && p == 32)
                    {           // ㅟ
                        nJung = 16;
                    }
                    else if (nJung == 18 && p == 32)
                    {           // ㅢ
                        nJung = 19;
                    }
                    else {          // 조합 안되는 모음 입력
                        if (nCho != -1)
                        {           // 초성+중성 후 중성
                            res += makeHangul(nCho, nJung, nJong);
                            nCho = -1;
                        }
                        else                        // 중성 후 중성
                            res += JUNG_DATA[nJung];
                        nJung = -1;
                        res += KOR_KEY[p];
                    }
                }
            }

            // 마지막 한글이 있으면 처리
            if (nCho != -1)
            {
                if (nJung != -1)            // 초성+중성+(종성)
                    res += makeHangul(nCho, nJung, nJong);
                else                        // 초성만
                    res += CHO_DATA[nCho];
            }
            else {
                if (nJung != -1)            // 중성만
                    res += JUNG_DATA[nJung];
                else {                      // 복자음
                    if (nJong != -1)
                        res += JONG_DATA[nJong];
                }
            }

            return res;
        }

        private string makeHangul(Int32 nCho, Int32 nJung, Int32 nJong)
        {
            return new String(new char[] { Convert.ToChar(0xac00 + nCho * 21 * 28 + nJung * 28 + nJong + 1) });
        }

        private String GetMessage(String messageKey, String userId = "", MESSAGE_GET_LANG_MODE MODE = MESSAGE_GET_LANG_MODE.USER)
        {
            try
            {
                if (MODE.Equals(MESSAGE_GET_LANG_MODE.USER))
                {
                    return lang.GetMessage(messageKey, this, userId);
                }
                else
                {
                    String serverLang = (String) Config["serverLang"];
                    Dictionary<String, String> messges = new Dictionary<String, String>();
                    if (null != serverLang) lang.SetServerLanguage(serverLang);
                    return lang.GetMessages(lang.GetServerLanguage(), this)[messageKey];
                }
            }
            catch(Exception e)
            {
                return e.Message;
            }
        }

        private void LoadMessages()
        {
            String kindFile = "";
            String sucessMessage = "";
            String ErrorMessage = "";

            try
            {
                lang.RegisterMessages(new Dictionary<string, string>
                {
                        ["loadPlugin"] = "Plugin loaded."
                    ,   ["messageFile"] = "Message file"
                    ,   ["configFile"] = "Configuration file"
                    ,   ["configSetServerName"] = "We changed the server name to a {0}."
                    ,   ["configSetServerNameColor"] = "We changed the color of the server name to {0}."
                    ,   ["createPrintSucessFile"] = "You have created a {0}."
                    ,   ["createPrintErrorFile"] = "Failed to create {0}."
                    ,   ["nowMode"] = "The current mode is {0} mode."
                    ,   ["hangulMode"] = "Korean"
                    ,   ["englishMode"] = "English"
                    ,   ["changeMode"] = "To change the input mode, enter the /h chat command."
                    ,   ["duplicatePlugin"] = "Unload BetterSay plugin in duplicate with BetterSay plugin."
                    ,   ["notPermission"] = "You do not have permission."
                }, this);

                lang.RegisterMessages(new Dictionary<string, string>
                {
                        ["loadPlugin"] = "플러그인이 로드 되었습니다."
                    ,   ["messageFile"] = "메시지 파일"
                    ,   ["configFile"] = "설정 파일"
                    ,   ["configSetServerName"] = "서버이름을 {0}로 변경하였습니다."
                    ,   ["configSetServerNameColor"] = "서버이름의 색깔을 {0}로 변경하였습니다."
                    ,   ["createPrintSucessFile"] = "{0}을 생성하였습니다."
                    ,   ["createPrintErrorFile"] = "{0} 생성에 실패하였습니다."
                    ,   ["nowMode"] = "현재모드는 {0}입니다."
                    ,   ["hangulMode"] = "한글"
                    ,   ["englishMode"] = "영문"
                    ,   ["changeMode"] = "입력모드 변경은 /h 채팅명령어를 입력해주세요."
                    ,   ["duplicatePlugin"] = "BetterSay 플러그인과 중복되어 BetterSay 플러그인을 UnLoad 합니다."
                    ,   ["notPermission"] = "권한이 없습니다."
                }, this, "ko");
                
                kindFile = GetMessage("messageFile", null, MESSAGE_GET_LANG_MODE.SERVER);
                sucessMessage = String.Format(GetMessage("createPrintSucessFile", "", MESSAGE_GET_LANG_MODE.SERVER), kindFile);
            }
            catch (Exception e)
            {
                kindFile = GetMessage("messageFile", null, MESSAGE_GET_LANG_MODE.SERVER);
                ErrorMessage = String.Format(GetMessage("createPrintErrorFile", "", MESSAGE_GET_LANG_MODE.SERVER), kindFile);
                PrintError(ErrorMessage);
            }

            PrintWarning(sucessMessage);
        }
        
        private void SetDefaultConfig()
        {
            Config.Clear();
            Config["serverLang"] = "ko";
            Config["serverName"] = "SERVER";
            Config["serverNameColor"] = "#a1ff46";
            Config["chatChangeChar"] = "#";
            SaveConfig();
        }

        private void UpdateConfig(String messageKey, String value = "")
        {
            if (null == Config[messageKey])
            {
                Config.Set(new object[] { messageKey, value });
            }
            else
            {
                Config[messageKey] = value;
            }

            SaveConfig();
        }
        
        class ForbiddenWordData
        {
            public String[] word = new[] {
                "씨팔", "개새끼", "애미", "에미", "애비", "에비"
            };

            public ForbiddenWordData()
            {
            }
        }

        protected override void LoadDefaultConfig() => PrintWarning("You have created a Configuration file.");
    }
}
