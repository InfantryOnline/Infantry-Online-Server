using db.packets;
using Google.FlatBuffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace DB2
{
    public static class ClientServerPacketFactory
    {
        public static byte[] CreateAuthenticateClient(int msgId, string token)
        {
            var fbb = new FlatBufferBuilder(512);
            var str = fbb.CreateString(token);

            var ac = AuthenticateClient.CreateAuthenticateClient(fbb, msgId, str);
            fbb.Finish(ac.Value);
            
            return fbb.SizedByteArray();
        }
    }
}
