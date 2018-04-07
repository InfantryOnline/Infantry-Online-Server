using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

using InfServer.Network;
using InfServer.Protocol;

using Meebey.SmartIrc4net;

namespace InfServer.Game
{
    public partial class ZoneServer : Server
    {
        public IrcClient ircClient;
        public string IrcName;
        public bool IrcSayToggled;

        private string defaultAddress = "irc.synirc.net";
        private int defaultPort = 6667;
        private string defaultChannel = "#infantry";

        /// <summary>
        /// Initializes our irc client
        /// </summary>
        public void InitializeIrcClient()
        {
            ircClient = new IrcClient();
            ircClient.Encoding = System.Text.Encoding.UTF8;
            ircClient.ActiveChannelSyncing = true;

            ircClient.OnChannelMessage += new IrcEventHandler(OnChannelMessage);
            ircClient.OnQueryMessage += new IrcEventHandler(OnQueryMessage);
            ircClient.OnError += new ErrorEventHandler(OnError);
            ircClient.OnRawMessage += new IrcEventHandler(OnRawMessage);
            //ircClient.OnJoin += new JoinEventHandler(OnJoin);

            try
            {
                if (_config.Exists("irc/address") && _config.Exists("irc/port"))
                    ircClient.Connect(_config["irc/address"].Value, _config["irc/port"].intValue);
                else
                    ircClient.Connect(defaultAddress, defaultPort);

                if (ircClient.IsConnected)
                    Log.write(TLog.Normal, "Irc Client Connected.");
                else
                    Log.write(TLog.Normal, "Cannot connect to the IRC server.");
            }
            catch (ConnectionException e)
            {
                Log.write(TLog.Warning, "Could not connect to irc server.", e.Message);
            }

            try
            {
                IrcName = String.Join("", (from c in _name where !Char.IsWhiteSpace(c) && Char.IsLetterOrDigit(c) select c).ToArray());
                ircClient.Login(IrcName, IrcName + " Bot");

                string channel = defaultChannel;
                if (_config.Exists("irc/channel"))
                {
                    ircClient.RfcJoin(_config["irc/channel"].Value);
                    channel = _config["irc/channel"].Value;
                }
                else
                    ircClient.RfcJoin(defaultChannel);

                if (ircClient.IsJoined(channel))
                    Log.write(TLog.Normal, "Irc Client joined successfully.");
            }
            catch(ConnectionException e)
            {
                Log.write(TLog.Warning, "Could not join irc channel.", e.Message);
            }
            catch (Exception e)
            {
                Log.write(TLog.Warning, "Could not join irc channel.", e.Message);
            }
        }

        /// <summary>
        /// Handles incoming command messages
        /// </summary>
        public void OnQueryMessage(object sender, IrcEventArgs e)
        {
            var match = Regex.Match(e.Data.Message.Trim(), @"^:(?<Alias>.+):(?<Message>.+)$");

            if (match.Groups.Count > 2)
            {
                var player = _players.FirstOrDefault(x => x.Value._alias.ToLower() == match.Groups["Alias"].Value.ToLower()).Value;

                if (player != null)
                {
                    Helpers.Player_RouteChatRaw(player, "[IRC]" + e.Data.Nick, match.Groups["Message"].Value, 0);
                }

                return;
            }

            var firstString = e.Data.MessageArray[0];

            switch (firstString)
            {
                // debug stuff
                case "dump_channel":
                    string requested_channel = e.Data.MessageArray[1];
                    // getting the channel (via channel sync feature)
                    Channel channel = ircClient.GetChannel(requested_channel);

                    // here we send messages
                    ircClient.SendMessage(SendType.Message, e.Data.Nick, "<channel '" + requested_channel + "'>");

                    ircClient.SendMessage(SendType.Message, e.Data.Nick, "Name: '" + channel.Name + "'");
                    ircClient.SendMessage(SendType.Message, e.Data.Nick, "Topic: '" + channel.Topic + "'");
                    ircClient.SendMessage(SendType.Message, e.Data.Nick, "Mode: '" + channel.Mode + "'");
                    ircClient.SendMessage(SendType.Message, e.Data.Nick, "Key: '" + channel.Key + "'");
                    ircClient.SendMessage(SendType.Message, e.Data.Nick, "UserLimit: '" + channel.UserLimit + "'");

                    // here we go through all users of the channel and show their
                    // hashtable key and nickname 
                    string nickname_list = "";
                    nickname_list += "Users: ";
                    foreach (DictionaryEntry de in channel.Users)
                    {
                        string key = (string)de.Key;
                        ChannelUser channeluser = (ChannelUser)de.Value;
                        nickname_list += "(";
                        if (channeluser.IsOp)
                        {
                            nickname_list += "@";
                        }
                        if (channeluser.IsVoice)
                        {
                            nickname_list += "+";
                        }
                        nickname_list += ")" + key + " => " + channeluser.Nick + ", ";
                    }
                    ircClient.SendMessage(SendType.Message, e.Data.Nick, nickname_list);

                    ircClient.SendMessage(SendType.Message, e.Data.Nick, "</channel>");
                    break;
                case "gc":
                    GC.Collect();
                    break;
                case "auth":
                    break;

                // typical commands
                case "join":
                    ircClient.RfcJoin(e.Data.MessageArray[1]);
                    break;
                case "part":
                    ircClient.RfcPart(e.Data.MessageArray[1]);
                    break;
                case "die":
                    break;
            }
        }

        /// <summary>
        /// Handles incoming channel messages from the IRC server
        /// </summary>
        public void OnChannelMessage(object sender, IrcEventArgs e)
        {
            var arena = _arenas.FirstOrDefault(x => x.Value.IrcName == e.Data.Channel).Value;

            if (e.Data.Message.StartsWith("'") || e.Data.Message.StartsWith("?sayspec"))
            {
                int startIndex = e.Data.Message.StartsWith("?sayspec") ? 8 : 1;
                var firstSpecPlayer = arena.Players.FirstOrDefault(x => x.IsSpectator);

                if (firstSpecPlayer != null)
                {
                    Helpers.Player_RouteChatRaw(firstSpecPlayer._team, "[IRC]" + e.Data.Nick, e.Data.Message.Substring(startIndex), 0, Helpers.Chat_Type.Team);
                }

                return;
            }

            if (IrcSayToggled && e.Data.MessageArray[0] != "?saytoggle" && arena != null)
            {
                Helpers.Player_RouteChatRaw(arena, "[IRC]" + e.Data.Nick, e.Data.Message, 0, Helpers.Chat_Type.Normal);
                return;
            }

            switch (e.Data.MessageArray[0])
            {
                case "?saytoggle":
                    IrcSayToggled = !IrcSayToggled;

                    var toggleMsg = IrcSayToggled ? "Chat is now toggled ON. Every message from here on will be sent to the arena. Turn it off with ?saytoggle."
                        : "Chat is now toggled OFF. Use ?say <message> to send a message, or use ?saytoggle to toggle it back on.";

                    ircClient.SendMessage(SendType.Message, e.Data.Channel, toggleMsg);
                    break;

                case "?say":
                    if (arena != null)
                    {
                        var msg = String.Join(" ", e.Data.MessageArray.Skip(1));
                        Helpers.Player_RouteChatRaw(arena, "[IRC]" + e.Data.Nick, msg, 0, Helpers.Chat_Type.Normal);
                    }

                    break;

                case "?players":
                    if (arena != null)
                    {
                        var players = String.Join(", ", arena.Players.Select(x => (x.IsSpectator ? x._alias + "[S]" : x._alias)).OrderBy(y => y));

                        ircClient.SendMessage(SendType.Message, e.Data.Channel, players);
                    }

                    break;

                case "?arenas":
                    var arenas = String.Join(", ", _arenas.Select(x => String.Format("{0}({1})", x.Value._name, x.Value.IrcName)));

                    if (arenas.Length > 0)
                    {
                        ircClient.SendMessage(SendType.Message, e.Data.Channel, arenas);
                    }
                    break;

                case "!pop":
                    int pop = 0;
                    foreach (var arena_ in _arenas)
                        pop += arena_.Value.TotalPlayerCount;

                    if (pop > 0)
                    {
                        ircClient.SendMessage(SendType.Message, e.Data.Channel, string.Format("Current Pop: {0}", pop));
                    }
                    break;
            }
        }

        
        /// <summary>
        /// Handles recieved "ERROR" message from the IRC server
        /// </summary>
        public void OnError(object sender, ErrorEventArgs e)
        {
            Log.write("Error: " + e.ErrorMessage);
        }

        /// <summary>
        /// Gets all IRC messages
        /// </summary>
        public void OnRawMessage(object sender, IrcEventArgs e)
        {

        }

        /// <summary>
        /// Sends a list of commands once someone joins the channel
        /// </summary>
        public void OnJoin(object sender, JoinEventArgs e)
        {
            string[] helpMsg = {"?arenas - Gets a list of any active arenas in each active zone bot\r\n",
                             "?players - Gets a list of players within the active arena\r\n",
                             "?say - Sends a message within the active arena to the public\r\n",
                             "?sayspec or ' - Sends a message within the active arena to only spectators\r\n",
                             "?saytoggle - Toggles automatic chat on or off within the active arena"};
            foreach(string msg in helpMsg)
                ircClient.SendMessage(SendType.Message, e.Data.Channel, msg);
        }

    }
}
