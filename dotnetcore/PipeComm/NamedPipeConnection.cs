using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace PipeComm
{
    /// <summary>
    /// Allows for bi-directional communication using the .NET Named Pipes implementation.
    /// </summary>
    /// <remarks>
    /// This is used for localhost server-to-server control plane chatter.
    /// </remarks>
    public class NamedPipeConnection
    {
        private Dictionary<PacketTypes, Func<Packet, Task>> _handlers = new Dictionary<PacketTypes, Func<Packet, Task>>();

        private PipeStream? _stream = null;

        private BinaryReader? _streamReader = null;

        private BinaryWriter? _streamWriter = null;

        private byte[] _readBuffer = new byte[4096];

        public static async Task<NamedPipeConnection> CreateServerAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            var output = new NamedPipeConnection();

            var server = new NamedPipeServerStream(
                                name,
                                PipeDirection.InOut,
                                NamedPipeServerStream.MaxAllowedServerInstances,
                                PipeTransmissionMode.Byte,
                                PipeOptions.Asynchronous);

            await server.WaitForConnectionAsync();

            output._stream = server;

            output._streamReader = new BinaryReader(server);
            output._streamWriter = new BinaryWriter(server);

            return output;
        }

        public static async Task<NamedPipeConnection> CreateClientAsync(string ep, string name)
        {
            if (string.IsNullOrWhiteSpace(ep) || string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException();
            }

            var output = new NamedPipeConnection();

            var client = new NamedPipeClientStream(
                            ep,
                            name,
                            PipeDirection.InOut,
                            PipeOptions.Asynchronous);

            await client.ConnectAsync();

            output._stream = client;

            output._streamReader = new BinaryReader(client);
            output._streamWriter = new BinaryWriter(client);

            return output;
        }

        public static async Task RunAsync()
        {

        }

        public async Task WriteAsync(byte[] data)
        {
            _streamWriter.Write(data);
            _streamWriter.Flush();

            await Task.FromResult(data.Length);
        }

        public async Task<Packet> ReadAsync()
        {
            if (_streamReader.Read(_readBuffer, 0, 4) != 4)
            {
                // TODO: Problem
            }

            var type = (PacketTypes)BitConverter.ToInt32(_readBuffer);

            if (_streamReader.Read(_readBuffer, 0, 4) != 4)
            {
                // TODO: Problem
            }

            var length = BitConverter.ToInt32(_readBuffer);

            if (length > 4096)
            {
                // TODO: Problem.
            }

            _streamReader.Read(_readBuffer, 0, length);

            switch (type)
            {
                case PacketTypes.ConsoleMessage:
                    var msg = JsonSerializer.Deserialize<Message>(_readBuffer);
                    await _handlers[type](msg);
                    return msg;

                default:
                    throw new Exception("Not implemented");
            }

            
        }

        public void SetHandler<T>(PacketTypes type, Func<T, Task> h) where T : Packet
        {
            if (_handlers.ContainsKey(type))
            {
                throw new ArgumentException($"Type {type} already has a handler registered");
            }

            if (h == null)
            {
                throw new ArgumentNullException("h");
            }

            _handlers.Add(type, (Func<Packet, Task>)h);
        }

        public void Close()
        {
            _stream.Close();
        }
    }
}
