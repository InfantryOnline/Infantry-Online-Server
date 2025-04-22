using Microsoft.Extensions.Configuration;
using PipeComm;
using System.IO.Pipes;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Daemon
{
    internal class ClientConnection
    {
        public PipeStream Stream { get; set; }
    }
    internal class Program
    {
        static BaseConfiguration baseConfiguration = new BaseConfiguration();

        static async Task HandleClientAsync(PipeStream stream)
        {
            var connection = new ClientConnection { Stream = stream };

            var ss = new StreamReader(connection.Stream);

            var data = new char[4];

            await ss.ReadAsync(data, 0, 4);

            var type = (PacketTypes)int.Parse(data);

            await ss.ReadAsync(data, 0, 4);

            var length = int.Parse(data);

            var json = new char[length];

            await ss.ReadAsync(json, 0, length);

            var sw = new StreamWriter(connection.Stream);

            sw.AutoFlush = true;

            var messagePacket = new Message { Text = "Hello there!" };
            var payload = JsonSerializer.Serialize(messagePacket);

            sw.Write((int)PacketTypes.ConsoleMessage);
            sw.Write(payload.Length);
            sw.Write(payload);

            sw.Flush();

            connection.Stream.Close();
        }

        static NamedPipeServerStream CreatePipeServer()
        {
            return new NamedPipeServerStream(
                    baseConfiguration.DaemonPipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
        }

        static async Task Main(string[] args)
        {
            //
            // Load in configuration.
            //

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            config.GetSection("Base").Bind(baseConfiguration);

            //
            // Start the named pipe process to await queries and commands
            // from the console program as well as the zone servers. And any
            // other programs that wish to query.
            //

            var pipeServer = CreatePipeServer();

            Console.WriteLine("Awaiting client connections...");

            while (true)
            {
                try
                {
                    await pipeServer.WaitForConnectionAsync();

                    // Now spin off a new Task and handle the client.
                    var clientPipe = pipeServer;

                    _ = Task.Run(async () => {
                        await HandleClientAsync(clientPipe);
                    });
                }
                finally
                {
                    // Spin off a new pipe for any additional clients.
                    pipeServer = CreatePipeServer();
                }
            }
        }
    }
}
