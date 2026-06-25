using OpusSharp.Native;

namespace OpusSharp;

/// <summary>
/// Provides detailed metadata about raw Opus packets via libopus helpers.
/// Mirrors the diagnostics from the opus_inspect.c snippet to aid debugging.
/// </summary>
public static class OpusPacketInspector
{
    private const int MaxFrames = 48;

    /// <summary>
    /// Inspect the supplied Opus packet and return a detailed breakdown.
    /// </summary>
    /// <param name="packet">Raw Opus payload (no transport header).</param>
    /// <param name="sampleRate">Expected decoder sample rate for the stream (e.g. 16000, 48000).</param>
    /// <returns>An inspection result describing the packet.</returns>
    public static OpusPacketInspection InspectPacket(ReadOnlySpan<byte> packet, int sampleRate)
    {
        if (packet.IsEmpty)
        {
            return OpusPacketInspection.Failure("Packet was empty.");
        }

        if (sampleRate <= 0)
        {
            return OpusPacketInspection.Failure($"Invalid sample rate: {sampleRate}.");
        }

        unsafe
        {
            fixed (byte* packetPtr = packet)
            {
                var dataPtr = (IntPtr)packetPtr;

                var channels = OpusNative.opus_packet_get_nb_channels(dataPtr);
                if (channels < 0)
                {
                    return OpusPacketInspection.Failure($"opus_packet_get_nb_channels returned {channels}.");
                }

                var bandwidth = OpusNative.opus_packet_get_bandwidth(dataPtr);
                if (bandwidth < 0)
                {
                    return OpusPacketInspection.Failure($"opus_packet_get_bandwidth returned {bandwidth}.");
                }

                var frameCount = OpusNative.opus_packet_get_nb_frames(dataPtr, packet.Length);
                if (frameCount < 0)
                {
                    return OpusPacketInspection.Failure($"opus_packet_get_nb_frames returned {frameCount}.");
                }

                var samplesPerFrame = OpusNative.opus_packet_get_samples_per_frame(dataPtr, sampleRate);
                if (samplesPerFrame < 0)
                {
                    return OpusPacketInspection.Failure($"opus_packet_get_samples_per_frame returned {samplesPerFrame}.");
                }

                var totalSamples = OpusNative.opus_packet_get_nb_samples(dataPtr, packet.Length, sampleRate);
                if (totalSamples < 0)
                {
                    return OpusPacketInspection.Failure($"opus_packet_get_nb_samples returned {totalSamples}.");
                }

                byte toc = 0;
                int payloadOffset = 0;

                IntPtr* framePtrBuffer = stackalloc IntPtr[MaxFrames];
                short* frameSizeBuffer = stackalloc short[MaxFrames];

                byte* tocPtr = &toc;
                int* payloadOffsetPtr = &payloadOffset;

                var parsedFrames = OpusNative.opus_packet_parse(
                    dataPtr,
                    packet.Length,
                    (IntPtr)tocPtr,
                    (IntPtr)framePtrBuffer,
                    (IntPtr)frameSizeBuffer,
                    (IntPtr)payloadOffsetPtr);

                if (parsedFrames < 0)
                {
                    return OpusPacketInspection.Failure($"opus_packet_parse returned {parsedFrames}.");
                }

                var frames = new List<OpusFrameInspection>(parsedFrames);
                var packetBase = (nint)packetPtr;
                var totalFrameBytes = 0;

                var frameSizeSpan = new ReadOnlySpan<short>(frameSizeBuffer, parsedFrames);

                for (var i = 0; i < parsedFrames; i++)
                {
                    var size = frameSizeSpan[i];
                    totalFrameBytes += size;

                    var framePtr = framePtrBuffer[i];
                    var offset = framePtr != IntPtr.Zero
                        ? (int)((nint)framePtr - packetBase)
                        : 0;

                    var isVbr = i > 0 && frameSizeSpan[i] != frameSizeSpan[i - 1];

                    frames.Add(new OpusFrameInspection(
                        Index: i,
                        Offset: offset,
                        SizeBytes: size,
                        IsVariableBitRate: isVbr));
                }

                var padding = packet.Length - payloadOffset - totalFrameBytes;
                if (padding < 0)
                    padding = 0;

                var frameDurationMs = samplesPerFrame > 0
                    ? samplesPerFrame * 1000.0 / sampleRate
                    : 0.0;

                return new OpusPacketInspection(
                    Success: true,
                    Error: null,
                    Length: packet.Length,
                    Channels: channels,
                    Bandwidth: GetBandwidthLabel(bandwidth),
                    BandwidthRaw: bandwidth,
                    FrameCount: frameCount,
                    ParsedFrameCount: parsedFrames,
                    SamplesPerFrame: samplesPerFrame,
                    TotalSamples: totalSamples,
                    Toc: toc,
                    PayloadOffset: payloadOffset,
                    Padding: padding,
                    Packing: InferPacking(parsedFrames, frameSizeSpan),
                    FrameDurationMs: frameDurationMs,
                    Frames: frames);
            }
        }
    }

    private static string GetBandwidthLabel(int bandwidth) => bandwidth switch
    {
        1101 => "NB (4 kHz)",
        1102 => "MB (6 kHz)",
        1103 => "WB (8 kHz)",
        1104 => "SWB (12 kHz)",
        1105 => "FB (20 kHz)",
        _ => $"Unknown ({bandwidth})"
    };

    private static string InferPacking(int frameCount, ReadOnlySpan<short> frameSizes)
    {
        if (frameCount <= 0)
        {
            return "unknown";
        }

        if (frameCount == 1)
        {
            return "code 0 (1 frame)";
        }

        if (frameCount == 2)
        {
            return frameSizes[0] == frameSizes[1]
                ? "code 1 (2 frames, CBR)"
                : "code 2 (2 frames, VBR)";
        }

        var isVbr = false;
        for (var i = 1; i < frameCount; i++)
        {
            if (frameSizes[i] != frameSizes[i - 1])
            {
                isVbr = true;
                break;
            }
        }

        return isVbr ? "code 3 (N frames, VBR)" : "code 3 (N frames, CBR)";
    }
}

/// <summary>
/// Detailed metadata for an Opus packet.
/// </summary>
public sealed record OpusPacketInspection(
    bool Success,
    string? Error,
    int Length,
    int Channels,
    string Bandwidth,
    int BandwidthRaw,
    int FrameCount,
    int ParsedFrameCount,
    int SamplesPerFrame,
    int TotalSamples,
    byte Toc,
    int PayloadOffset,
    int Padding,
    string Packing,
    double FrameDurationMs,
    IReadOnlyList<OpusFrameInspection> Frames)
{
    public static OpusPacketInspection Failure(string message) => new(
        Success: false,
        Error: message,
        Length: 0,
        Channels: 0,
        Bandwidth: "Unknown",
        BandwidthRaw: 0,
        FrameCount: 0,
        ParsedFrameCount: 0,
        SamplesPerFrame: 0,
        TotalSamples: 0,
        Toc: 0,
        PayloadOffset: 0,
        Padding: 0,
        Packing: "unknown",
        FrameDurationMs: 0,
        Frames: Array.Empty<OpusFrameInspection>());
}

/// <summary>
/// Describes a single Opus frame within a packet.
/// </summary>
public sealed record OpusFrameInspection(
    int Index,
    int Offset,
    int SizeBytes,
    bool IsVariableBitRate);
