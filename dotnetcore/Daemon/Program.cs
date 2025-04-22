using Microsoft.Extensions.Configuration;
using System.IO.Pipes;
using System.Security.Cryptography.X509Certificates;

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

            using (var sw = new StreamWriter(connection.Stream))
            {
                sw.AutoFlush = true;
                sw.WriteLine("Hello from Daemon proc!");
            }

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
