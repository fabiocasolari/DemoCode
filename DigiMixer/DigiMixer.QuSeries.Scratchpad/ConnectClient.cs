﻿using DigiMixer.QuSeries.Core;
using System.Net;
using System.Net.Sockets;

namespace DigiMixer.QuSeries.Scratchpad;

internal class ConnectClient
{
    static async Task Main()
    {
        var address = IPAddress.Parse("192.168.1.60");
        var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        var localUdpPort = ((IPEndPoint) udpClient.Client.LocalEndPoint!).Port;
        udpClient.Close();

        udpClient = new UdpClient();
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, localUdpPort));

        bool finished = false;

        var tcpClient = new TcpClient { NoDelay = true };
        tcpClient.Connect(address, 51326);

        int? mixerUdpPort = null;
        var action = new Action<QuControlPacket>(LogPacket) + new Action<QuControlPacket>(AssignUdpPort);

        // We send keepalive messages via UDP.
        var udpLoop = StartUdpLoop();
        var loop = StartLoop(action);
        var stream = tcpClient.GetStream();

        //var packet1 = QuPacket.Create(type: 0, new byte[] { (byte) (port & 0xff), (byte) (port >> 8) });
        //packet1.WriteTo(stream);

        //await Task.Delay(100);
        //var packet2 = QuPacket.Create(type: 4, Decode("00 01"));
        //packet2.WriteTo(stream);

        
        var introPackets = new[]
        {
            QuControlPacket.Create(type: 0, new byte[] { (byte) (localUdpPort & 0xff), (byte) (localUdpPort >> 8) }),
            // This is required in order to get notifications from other clients.
            QuControlPacket.Create(type: 4, Decode("13 00 00 00  ff ff ff ff ff ff 9f 0f", "00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00", "00 e0 03 c0 ff ff ff 7f")),
            //QuPacket.Create(type: 4, Decode("14 00 00 00  ff 00 00 00 00 00 00 00", "00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00", "00 00 00 00 00 00 00 00")),
            //QuPacket.Create(type: 4, Decode("15 00 00 00  fa c1 23 06 04 00 00 28", "d1 04 1c 00 00 00 00 00  00 00 00 00 00 00 00 00", "00 00 00 00 00 00 00 00")),
            /*
            QuPacket.Create(type: 4, Decode("0d 00")),
            QuPacket.Create(type: 4, Decode("06 00")),
            QuPacket.Create(type: 4, Decode("0c 00")),
            QuPacket.Create(type: 4, Decode("0b 00")),
            QuPacket.Create(type: 4, Decode("08 00")),
            // This requests full data (~25K)
            QuPacket.Create(type: 4, Decode("02 00")),
            // Type 2 response to this, just 01 00
            QuPacket.Create(type: 4, Decode("01 00 00 00  09 0e 64 90")),
            // No response to this
            QuPacket.Create(type: 4, Decode("0f 00")),
            QuPacket.Create(type: 4, Decode("07 00")),*/
            //QuPacket.Create(type: 4, Decode("02 00")),
            QuControlPacket.Create(type: 4, Decode("02 00")),
        };

        foreach (var packet in introPackets)
        {
            stream.Write(packet.ToByteArray());
            await Task.Delay(100);
        }
        
        /*
        
        var p1 = QuPacket.Create(type: 4, Decode("13 00 00 00  ff ff ff ff ff ff 9f 0f", "00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00", "00 e0 03 c0 ff ff ff 7f"));
        var p2 = QuPacket.Create(type: 4, Decode("14 00 00 00  ff 00 00 00 00 00 00 00", "00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00", "00 00 00 00 00 00 00 00"));
        var p3 = QuPacket.Create(type: 4, Decode("15 00 00 00  fa c1 23 06 04 00 00 28", "d1 04 1c 00 00 00 00 00  00 00 00 00 00 00 00 00", "00 00 00 00 00 00 00 00"));

        p1.WriteTo(stream);
        p2.WriteTo(stream);
        p3.WriteTo(stream);
        
        //await Task.Delay(1000);
        var packet3 = QuPacket.Create(type: 4, Decode("0d 00"));
        packet3.WriteTo(stream);

        var packet4 = QuPacket.Create(type: 4, Decode("08 00"));
        packet4.WriteTo(stream);

        //await Task.Delay(200);
        var packet5 = QuPacket.Create(type: 4, Decode("02 00"));
        packet5.WriteTo(stream);
        */

        await Task.Delay(1000);
        finished = true;

        async Task StartLoop(Action<QuControlPacket> action)
        {
            var packetBuffer = new QuPacketBuffer(100_000);
            byte[] buffer = new byte[1024];
            Console.WriteLine($"Starting reading at {DateTime.UtcNow}");
            while (!finished)
            {
                var bytesRead = await tcpClient.GetStream().ReadAsync(buffer, 0, 1024);
                if (bytesRead == 0)
                {
                    Console.WriteLine($"Receiving stream broken at {DateTime.UtcNow}");
                    return;
                }
                packetBuffer.Process(buffer.AsSpan().Slice(0, bytesRead), action);
            }
        }

        async Task StartUdpLoop()
        {
            var pingGap = TimeSpan.FromSeconds(4);
            var nextPing = DateTime.UtcNow + pingGap;
            while (!finished)
            {
                var result = await udpClient.ReceiveAsync();
               
                //Console.WriteLine($"Received UDP packet, {result.Buffer.Length} bytes");
                var now = DateTime.UtcNow;
                if (now > nextPing)
                {
                    if (mixerUdpPort is null)
                    {
                        continue;
                    }
                    var endpoint = new IPEndPoint(address, mixerUdpPort.Value);
                    Console.WriteLine("Sending ping");
                    await udpClient.SendAsync(new byte[] { 0x7f, 0x25, 0, 0 }, 4, endpoint);
                    nextPing = now + pingGap;
                }
            }
        }

        void AssignUdpPort(QuControlPacket packet)
        {
            if (packet is not QuGeneralPacket { Type: 0 } qgp ||
                qgp.Data.Length != 2)
            {
                return;
            }
            mixerUdpPort = qgp.Data[0] + (qgp.Data[1] << 8);
            Console.WriteLine($"Mixer UDP port: {mixerUdpPort}");
        }
    }

    static void LogPacket(QuControlPacket packet) => Console.WriteLine($"Received: {packet}");

    private static byte[] Decode(params string[] hexData)
    {
        var allHex = string.Join("", hexData).Replace(" ", "");
        byte[] data = new byte[allHex.Length / 2];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = Convert.ToByte(allHex.Substring(i * 2, 2), 16);
        }
        return data;
    }
}
