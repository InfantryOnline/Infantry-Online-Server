using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;

// TODO: Change Datagram reading/writing into streams.
// TODO: Look into using Async I/O.
// TODO: Centralize error checking.
// TODO: Centralize logging, use aaerox's logger?
// TODO: Try the provided Server class.

namespace DirectoryServer
{
    class Directory
    {
        public Directory()
        {
            var zones = new List<Zone>();

            // Sanity Test zones, lifted from SOE zone list.
            var z9 = new Zone(new byte[4] {64, 37, 134, 136}, 9669, "[I:Arcade] Frontlines", false, "Frontlines");
            var z6 = new Zone(new byte[4] {64, 37, 134, 136}, 9556, "[I:CQ] Faydon Lake", false, "Titan and Collective forces wage war against eachother for control of the Faydon Lake territory. A hybrid zone with CTF/SK/EOL inspiration.");
            var z3 = new Zone(new byte[4] {64, 37, 134, 136}, 9957, "[I:CTFX] CTF Extreme", false, "CTFX2 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z4 = new Zone(new byte[4] {64, 37, 134, 136}, 9318, "[I:RTS] Fleet!", false, "Fleet! 2 Space Navies attempt to destroy the other sides Command Post Satellite!");
            var z5 = new Zone(new byte[4] {64, 37, 134, 136}, 9466, "[I:SK] Mechanized Skirmish", false, "Mechanized SKIRMISH! Intermediate level zone. Two opposing forces fight over an area of Titan called \"Kliest's Ridge\". The first team to capture and hold the objectives wins.");
            var z1 = new Zone(new byte[4] {64, 37, 134, 136}, 9218, "[I:Sports] GravBall DvS", false, "GravBall is a soccer like sport for the weary soldier. This zone features the Devils vs. Suns.");
            var z7 = new Zone(new byte[4] {64, 37, 134, 136}, 9128, "[I:Sports] Soccer Brawl", false, "Soccer Brawl! Football Infantry style!");
            var z8 = new Zone(new byte[4] {64, 37, 134, 136}, 9578, "[League] CTFPL", false, "CTF Players League. Please visit http://www.ctfpl.org for details.");
            var z2 = new Zone(new byte[4] {64, 37, 134, 136}, 9124, "[League] GravBall League", false, "This zone is for members of the GravBall League to play matches, if you are not a member you can only spectate.");
            var z10 = new Zone(new byte[4] {64, 37, 134, 136}, 9126, "[League] SBL", false, "Soccer Brawl League! Point your www browser to www.sbleague.net for information!");
            var z11 = new Zone(new byte[4] {64, 37, 134, 136}, 9516, "[League] Skirmish League", false, "Skirmish League. Goto http://www.skirmishleague.com/ for details.");
            var z12 = new Zone(new byte[4] {64, 37, 134, 136}, 9224, "[League] Test Zone", true, "League testing zone for new maps");
            var z13 = new Zone(new byte[4] {64, 37, 134, 136}, 9857, "[League] Test Zone 2", true, "A cool zone");
            var z14 = new Zone(new byte[4] {64, 37, 134, 136}, 9182, "[League] USL", false, "Unified Skirmish League Deathmatch gameplay using unified skirmish settings and based on Mechanized Skirmish and Elsdragon Compound. http://www.uslzone.com");
            var z15 = new Zone(new byte[4] {64, 37, 134, 136}, 9628, "Test Zone 1", true, "Test zone 2 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z16 = new Zone(new byte[4] {64, 37, 134, 136}, 9222, "Test Zone 2", false, "Testing zone for new maps");
            var z17 = new Zone(new byte[4] {64, 37, 134, 136}, 9468, "Test Zone 3", true, "Test zone 3 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z18 = new Zone(new byte[4] {64, 37, 134, 136}, 9290, "Test Zone 4", true, "Test zone 4 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z19 = new Zone(new byte[4] {64, 37, 134, 136}, 9790, "Test Zone 5", true, "Test zone 5 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z20 = new Zone(new byte[4] {64, 37, 134, 136}, 9378, "Test Zone 6", true, "Test zone 6 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z21 = new Zone(new byte[4] {64, 37, 134, 136}, 9334, "Test Zone 7", true, "Test zone 7 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            var z22 = new Zone(new byte[4] {64, 37, 134, 136}, 9094, "Test Zone 9", true, "Test zone 9 part of the PCT! Submit your PCT entry at: stationgamesfeedback@soe.sony.com");
            zones.Add(z1);
            zones.Add(z2);
            zones.Add(z3);
            zones.Add(z4);
            zones.Add(z5);
            zones.Add(z6);
            zones.Add(z7);
            zones.Add(z8);
            zones.Add(z9);
            zones.Add(z10);
            zones.Add(z11);
            zones.Add(z12);
            zones.Add(z13);
            zones.Add(z14);
            zones.Add(z15);
            zones.Add(z16);
            zones.Add(z17);
            zones.Add(z18);
            zones.Add(z19);
            zones.Add(z20);
            zones.Add(z21);
            zones.Add(z22);

            _streams = new ZoneStream(zones);

            InitSocket();
        }

        public Directory(List<Zone> zones)
        {
            if(zones == null)
                throw new ArgumentNullException("zones cannot be null");

            _streams = new ZoneStream(zones);
            InitSocket();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Poll()
        {
            Console.WriteLine("Launching...");

            byte[] buf = new byte[MaxBufferLen];
            bool isRunning = true;

            while(isRunning)
            {
                int recvLen = _socket.ReceiveFrom(buf, ref _remotePoint);

                if(recvLen == -1)
                {
                    // Handle error
                    break;
                }

                if(!ProcessDatagram(buf, recvLen))
                {
                    // Handle error
                    break;
                }
            }
        }

        private bool ProcessDatagram(byte[] data, int recvLen)
        {
            // Sanity check
            if (data.Length == 0 || data[0] != 0)
                return false;

            byte type = data[1];

            switch(type)
            {
                case (byte)C2SPacketType.Challenge:
                    HandleChallenge(data, recvLen);
                    break;

                case (byte)C2SPacketType.ClientInfo:
                    break;

                case (byte)C2SPacketType.GetZoneList:
                    HandleGetZoneList(data, recvLen);
                    break;
                    
                case (byte)C2SPacketType.AcknowledgeZoneChunk:
                    HandleAcknowledgeZoneChunk(data, recvLen);
                    break;
            }

            return true;
        }

        private void InitSocket()
        {
            _localPoint = new IPEndPoint(IPAddress.Any, DirServPort);
            _remotePoint = new IPEndPoint(IPAddress.Any, 0);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                                 ProtocolType.Udp);

            _socket.Bind(_localPoint);
        }


        /// <summary>
        /// Sends the challenge token back to the client.
        /// </summary>
        /// <param name="data">Received datagram</param>
        /// <param name="recvLen"></param>
        private void HandleChallenge(byte[] data, int recvLen)
        {
            if(recvLen != 8)
            {
                // Handle error
                return;
            }

            Console.WriteLine("[Challenge Packet]");

            byte[] token = new byte[] {data[4], data[5], data[6], data[7]};

            SendChallengeResp(token);
        }

        /// <summary>
        /// Begins to send the zone list to the client in 512 byte chunks.
        /// 
        /// As each packet is sent, the server must wait for the 
        /// AcknowledgeZoneChunk packet to arrive before sending
        /// the next chunk.
        /// 
        /// A token is included in this packet that the server must echo
        /// back once all the zones have been listed.
        /// </summary>
        /// <param name="data">Received datagram</param>
        /// <param name="recvLen"></param>
        private void HandleGetZoneList(byte[] data, int recvLen)
        {
            if(recvLen != 28)
            {
                // Handle error
                return;
            }

            Console.WriteLine("[Get Zone List]");

            // Save the token so that we can send it once we list all the zones
            _zoneListEndToken = new[] {data[4], data[5], data[6], data[7]};

            // Begin enumerating the zones
            _chunksSent = 0;
            _lastChunkSent = 0;
            SendZoneListChunk(_chunksSent);
        }

        /// <summary>
        /// Sends the next zone chunk.
        /// </summary>
        /// <param name="data">Received datagram</param>
        /// <param name="recvLen"></param>
        private void HandleAcknowledgeZoneChunk(byte[] data, int recvLen)
        {
            if(recvLen != 8)
            {
                // Handle error
                return;
            }

            // Might be a ushort instead? 
            byte lastReceivedChunk = data[4];

            if (lastReceivedChunk != _lastChunkSent)
            {
                // Resend or Handle Error
                Console.WriteLine("Error: Invalid Ack. Resending the last one");
                _chunksSent = _lastChunkSent;
                SendZoneListChunk(_chunksSent);
                return;
            }

            Console.WriteLine("[ACK: {0}]", lastReceivedChunk);

            _chunksSent++;

            SendZoneListChunk(_chunksSent);
        }


        private void SendChallengeResp(byte[] token)
        {
            Console.WriteLine("[Challenge sent.]");
            byte[] response = new byte[]
                                  {
                                      0,
                                      (byte) S2CPacketType.ChallengeResp,
                                      0x42,
                                      0x0c,
                                      token[0],
                                      token[1],
                                      token[2],
                                      token[3],
                                      0,
                                      0,
                                      0,
                                      0
                                  };

            int sendLen = _socket.SendTo(response, _remotePoint);

            if(sendLen != response.Length)
            {
                // Handle error
            }
        }

        private void SendZoneListChunk(byte chunkNum)
        {
            if (chunkNum != _streams.Count)
            {
                Console.WriteLine("[Zone Chunk sent.]");
                byte[] header = { 00, 03, 00, 00, (byte)chunkNum, 00, 00, 00, 00, 0x08, 00, 00, 0x91, 0x0D, 00, 00 };
                byte[] zoneInfo = _streams[chunkNum];

                byte[] response = header.Concat(zoneInfo).ToArray();

                int sendLen = _socket.SendTo(response, _remotePoint);

                if (sendLen != response.Length)
                {
                    // Handle error
                }

                _lastChunkSent = chunkNum;
            }
            else
            {
                // TODO: Refactor this

                Console.WriteLine("[Zone Delimiter sent.]");

                // Terminate chunk sending.
                byte[] delim = {00, 0x0b, 00, 0x71, 00, 00, 00, 00};
                int DelimLen = _socket.SendTo(delim, _remotePoint);

                if (DelimLen != delim.Length)
                {
                    // Handle error
                }

                // End.
                Console.WriteLine("[Last packet sent.]");
                byte[] lastpacket = new byte[]
                                        {
                                            00, 06, 0x91, 00, _zoneListEndToken[0], _zoneListEndToken[1],
                                            _zoneListEndToken[2], _zoneListEndToken[3], 0x46, 0xf3, 0x1d, 0x11
                                        };

                _socket.SendTo(lastpacket, _remotePoint);
            }
        }


        /// <summary>
        /// Client-2-Server packet identifiers.
        /// </summary>
        private enum C2SPacketType
        {
            /// <summary>
            /// Opens the connection. This packet has a random token that the
            /// server must send back to the client with the S2C ChallengeResp
            /// packet.
            /// </summary>
            Challenge = 1,

            /// <summary>
            /// Must be an "Infantry" client with a valid version to use the 
            /// rest of the protocol.
            /// </summary>
            ClientInfo = 3,

            /// <summary>
            /// Requests the entire zone list. The zones are split int
            /// several packets of 512 bytes each.
            /// 
            /// A token is appended in this request. That token must be echoed
            /// after all the zones have been printed.
            /// </summary>
            GetZoneList = 5,

            /// <summary>
            /// Lets the server know that it has received a zone list chunk.
            /// </summary>
            AcknowledgeZoneChunk = 0x0b,
        }

        /// <summary>
        /// Server-2-Client packet identifiers.
        /// </summary>
        private enum S2CPacketType
        {
            /// <summary>
            /// Responds to the client with the same token that the client
            /// sent in the Challenge packet.
            /// </summary>
            ChallengeResp = 2,

            /// <summary>
            /// Sends any remaining zones.
            /// </summary>
            ZoneListResp = 3,

            /// <summary>
            /// Echoes the token given by C2S GetZoneList.
            /// </summary>
            ZoneListCompleted = 6,
        }

        private ZoneStream _streams;
        private byte _chunksSent;
        private byte _lastChunkSent;
        private byte[] _zoneListEndToken;
        private EndPoint _localPoint;
        private EndPoint _remotePoint;
        private Socket _socket;

        private const int MaxBufferLen = 512;
        private const int DirServPort = 4850;
    }
}
