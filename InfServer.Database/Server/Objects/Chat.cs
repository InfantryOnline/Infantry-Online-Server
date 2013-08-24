using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Protocol;
using InfServer.Data;

namespace InfServer
{
    // Chat Class
    /// Represents a single private chat
    ///////////////////////////////////////////////////////
    public class Chat
    {
        DBServer _server;                               //Who we work for..
        public Dictionary<string, string[]> _players;    //The players in our chat..
        public string _name;                            //The name of our chat

        public Chat(DBServer server, string chat)
        {
            _server = server;
            _players = new Dictionary<string, string[]>();
            _name = chat;
            server._chats.Add(chat, this);
        }

        public void newPlayer(string player, string[] chats)
        {
            try
            {
                _players.Add(player, chats);
            }
            catch (ArgumentException)
            {
                Log.write(TLog.Error, "Player '{0}' already exists in chat '{1}'.", player, chats);
                return;
            }
            SC_JoinChat<Zone> join = new SC_JoinChat<Zone>();
            join.from = player;
            join.chat = _name;
            join.users = List();

            foreach (Zone z in _server._zones)
            {
                z._client.send(join);
            }
        }

        public void lostPlayer(string player)
        {
            if (!_players.Remove(player))
            {
                Log.write(TLog.Warning, "Lost player '{0}' that wasn't in chat '{1}'.", player, _name);
                return;
            }

            SC_LeaveChat<Zone> leave = new SC_LeaveChat<Zone>();
            leave.from = player;
            leave.chat = _name;
            leave.users = List();

            foreach (Zone z in _server._zones)
            {
                z._client.send(leave);
            }
        }

        public bool hasPlayer(string player)
        {
            if (_players.ContainsKey(player))
                return true;
            return false;
        }

        public void sendList(string player)
        {
            SC_JoinChat<Zone> reply = new SC_JoinChat<Zone>();
            reply.chat = _name;
            reply.from = player;
            reply.users = List();
            _server.getPlayer(player).zone._client.send(reply);
        }

        public string List()
        {
            List<string> members = new List<string>();
            foreach (var player in _players)
            {
                members.Add(player.Key);
            }
            return string.Join(", ", members);
        }
    }
}