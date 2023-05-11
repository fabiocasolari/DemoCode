﻿using DigiMixer.Mackie.Core;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;

namespace DigiMixer.MackieDump;

internal class InfoRequester
{
    internal static async Task ExecuteAsync(string address, int port, string file, List<byte> infoItems)
    {
        PacketCollection pc = new PacketCollection();
        using var controller = new MackieController(NullLogger.Instance, address, port);
        controller.PacketSent += (sender, packet) => RecordPacket(packet, true);
        controller.PacketReceived += (sender, packet) => RecordPacket(packet, false);

        controller.MapCommand(MackieCommand.ClientHandshake, _ => new byte[] { 0x10, 0x40, 0xf0, 0x1d, 0xbc, 0xa2, 0x88, 0x1c });
        controller.MapCommand(MackieCommand.GeneralInfo, _ => new byte[] { 0, 0, 0, 2, 0, 0, 0x40, 0 });
        controller.MapCommand(MackieCommand.ChannelInfoControl, packet => new MackiePacketBody(packet.Body.Data.Slice(0, 4)));
        await controller.Connect(default);
        controller.Start();

        // From MackieMixerApi.Connect
        CancellationToken cancellationToken = default;
        await controller.SendRequest(MackieCommand.KeepAlive, MackiePacketBody.Empty, cancellationToken);
        await controller.SendRequest(MackieCommand.ClientHandshake, MackiePacketBody.Empty, cancellationToken);

        foreach (var item in infoItems)
        {
            try
            {
                await controller.SendRequest(MackieCommand.GeneralInfo, new byte[] { 0, 0, 0, item }, cancellationToken);
            }
            catch (MackieResponseException)
            {
                Console.WriteLine($"Request failed - error response received.");
            }
        }

        Console.WriteLine($"Captured {pc.Packets.Count} packets");

        using var output = File.Create(file);
        pc.WriteTo(output);

        void RecordPacket(MackiePacket packet, bool outbound)
        {
            pc.Packets.Add(Packet.FromMackiePacket(packet, outbound, null));
            // Immediate uninterpreted display, truncated after 16 bytes of data.
            var padding = outbound ? "" : "    ";
            if (packet.Body.Data.Length == 0)
            {
                Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.ffffff} {padding} {packet.Sequence} {packet.Type} {packet.Command} (empty)");
            }
            else
            {
                var dataLength = $"({packet.Body.Data.Length} bytes)";
                var data = BitConverter.ToString(packet.Body.Data.ToArray()).Replace("-", " ");
                if (data.Length > 47)
                {
                    data = data.Substring(0, 47) + "...";
                }
                Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.ffffff} {padding} {packet.Sequence} {packet.Type} {packet.Command}: {dataLength}: {data}");
            }
        }
    }
}
