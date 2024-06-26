using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Data;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DB2
{
    public class ClientConnection
    {
        public DateTime ConnectionCreatedAt;

        public DateTime LastPacketReceivedAt;

        public DateTime LastPacketSentAt;

        public ClientConnection(IPEndPoint endpoint)
        {
            Init(endpoint);
        }

        private void Init(IPEndPoint endpoint)
        {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            socket.Connect(endpoint);
            socket.NoDelay = true;

            networkStream = new NetworkStream(socket);

            pipeReader = PipeReader.Create(networkStream);
            pipeWriter = PipeWriter.Create(networkStream);
        }

        public async Task Send(byte[] data, CancellationToken cancellationToken = default)
        {
            try
            {
                await sendingSemaphore.WaitAsync(cancellationToken);

                var len = BitConverter.GetBytes(data.Length + 4); // Length that includes the packet type.
                await pipeWriter.WriteAsync(len, cancellationToken);
                await pipeWriter.WriteAsync(BitConverter.GetBytes(3), cancellationToken);
                await pipeWriter.WriteAsync(data, cancellationToken);
            }
            finally
            {
                sendingSemaphore.Release();
            }
        }

        public List<byte[]> GetPendingInboundPackets()
        {
            List<byte[]> packets = null;

            packetSemaphore.Wait();

            packets = packetQueue;
            packetQueue = new List<byte[]>();

            packetSemaphore.Release();

            return packets;
        }

        public async Task ProcessAsync(CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(ReadPipe(cancellationToken), ProcessInbound());
        }

        public async Task ProcessInbound()
        {
            while(isActive)
            {
                var packets = GetPendingInboundPackets();

                Console.WriteLine("Here");

                foreach(var p in packets)
                {
                    Console.WriteLine(BitConverter.ToString(p));
                }
            }

            await Task.FromResult(true);
        }

        private async Task ReadPipe(CancellationToken cancellationToken = default)
        {
            while (isActive)
            {
                try
                {
                    var result = await pipeReader.ReadAsync();
                    var buffer = result.Buffer;

                    if (awaitingPacketLength == 0)
                    {
                        TryParseHeaderLength(ref buffer, out awaitingPacketLength);
                    }
                    
                    if (awaitingPacketLength != 0)
                    {
                        byte[] data;

                        if (TryParsePacketData(ref buffer, awaitingPacketLength, out data))
                        {
                            await packetSemaphore.WaitAsync();
                            packetQueue.Add(data);
                            packetSemaphore.Release();

                            awaitingPacketLength = 0;
                        }
                    }

                    LastPacketSentAt = DateTime.Now;

                    pipeReader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        sendingSemaphore.Release();
                        isActive = false;
                        break;
                    }
                    else
                    {
                        Console.WriteLine(ex.Message);
                        break;
                        // NICE: throw;
                    }
                }
            }

            pipeReader.Complete();
            pipeWriter.Complete();
        }

        bool TryParseHeaderLength(ref ReadOnlySequence<byte> buffer, out int length)
        {
            // If there's not enough space, the length can't be obtained.
            if (buffer.Length < 4)
            {
                length = 0;
                return false;
            }

            // Grab the first 4 bytes of the buffer.
            var lengthSlice = buffer.Slice(buffer.Start, 4);
            if (lengthSlice.IsSingleSegment)
            {
                // Fast path since it's a single segment.
                length = BinaryPrimitives.ReadInt32BigEndian(lengthSlice.First.Span);
            }
            else
            {
                // There are 4 bytes split across multiple segments. Since it's so small, it
                // can be copied to a stack allocated buffer. This avoids a heap allocation.
                Span<byte> stackBuffer = stackalloc byte[4];
                lengthSlice.CopyTo(stackBuffer);
                length = BinaryPrimitives.ReadInt32BigEndian(stackBuffer);
            }

            // Move the buffer 4 bytes ahead.
            buffer = buffer.Slice(lengthSlice.End);

            return true;
        }

        bool TryParsePacketData(ref ReadOnlySequence<byte> buffer, int length, out byte[] data)
        {
            if (buffer.Length < length)
            {
                data = null;
                return false;
            }

            var lengthSlice = buffer.Slice(buffer.Start, length);

            data = new byte[length];

            lengthSlice.CopyTo(data);

            buffer = buffer.Slice(lengthSlice.End);

            return true;
        }

        private List<byte[]> packetQueue = new List<byte[]>();

        private bool isActive = true;

        private int awaitingPacketLength = 0;

        private Socket socket;

        private NetworkStream networkStream;

        private SemaphoreSlim sendingSemaphore = new SemaphoreSlim(1);

        private SemaphoreSlim packetSemaphore = new SemaphoreSlim(1);

        private PipeReader pipeReader;

        private PipeWriter pipeWriter;
    }
}
