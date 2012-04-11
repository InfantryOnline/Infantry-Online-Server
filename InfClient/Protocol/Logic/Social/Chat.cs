using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;

using InfServer;
using InfServer.Network;
using InfServer.Protocol;

namespace InfClient.Protocol
{
    public partial class Chat
    {
        /// <summary>
        /// Handles a chat packet received from the server
        /// </summary>
        static public void Handle_SC_Chat(SC_Chat pkt, Client client)
        {
            switch (pkt.chatType)
            {
                case Helpers.Chat_Type.Arena:
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(String.Format("(Type={0}) {1}", 
                            pkt.chatType, pkt.message));
                    }
                    break;

                case Helpers.Chat_Type.Normal:
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(String.Format("(Type={0}) {1}> {2}",
                            pkt.chatType, pkt.from, pkt.message));
                    }
                    break;

                case Helpers.Chat_Type.Team:
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(String.Format("(Type={0}) {1}> {2}",
                            pkt.chatType, pkt.from, pkt.message));
                    }
                    break;

                case Helpers.Chat_Type.Whisper:
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(String.Format("(Type={0}) {1}> {2}",
                            pkt.chatType, pkt.from, pkt.message));
                    }
                    break;
            }

        }

        /// <summary>
        /// Registers all handlers
        /// </summary>
        [InfServer.Logic.RegistryFunc]
        static public void Register()
        {
            SC_Chat.Handlers += Handle_SC_Chat;
        }
    }
}
