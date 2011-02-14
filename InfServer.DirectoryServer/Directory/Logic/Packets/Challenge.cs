using InfServer.DirectoryServer.Directory.Protocol.Packets;

namespace InfServer.DirectoryServer.Directory.Logic.Packets
{
    public class Challenge
    {
        public static void Handle_CS_Initiate(CS_Initiate pkt, DirectoryClient client)
        {
            var packet = new SC_Initiate();

            packet.RandChallengeToken = pkt.RandChallengeToken;

            client.send(packet);
        }

        [Directory.RegistryFunc]
        static public void Register()
        {
            CS_Initiate.Handlers += Handle_CS_Initiate;
        }
    }
}
