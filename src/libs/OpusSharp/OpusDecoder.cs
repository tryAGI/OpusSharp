using OpusSharp.Native;

namespace OpusSharp;

/// <summary>
/// High-level Opus decoder with FEC and PLC support
/// </summary>
public sealed class OpusDecoder : IDisposable
{
    private IntPtr _decoderPtr = IntPtr.Zero;
    private bool _disposed = false;

    public OpusConfig Config { get; private set; }

    public OpusDecoder(OpusConfig config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Initialize();
    }

    private void Initialize()
    {
        _decoderPtr = OpusNative.opus_decoder_create(
            (int)Config.SampleRate,
            (int)Config.Channels,
            out var error);

        if (_decoderPtr == IntPtr.Zero || error != OpusConstants.OPUS_OK)
        {
            throw new OpusException(error, $"Failed to create Opus decoder: {OpusNative.GetErrorMessage(error)}");
        }
    }

    /// <summary>
    /// Decode Opus packet to PCM16
    /// </summary>
    public short[] Decode(byte[] packet, FrameDuration expectedDuration = FrameDuration.Ms20)
    {
        return DecodeWithFEC(packet, useFEC: false, expectedDuration);
    }

    /// <summary>
    /// Get number of samples per channel contained in an Opus packet.
    /// </summary>
    public int GetPacketSampleCount(byte[] packet) => GetPacketSampleCount(packet.AsSpan());

    /// <summary>
    /// Get number of samples per channel contained in an Opus packet.
    /// </summary>
    public int GetPacketSampleCount(ReadOnlySpan<byte> packet)
    {
        ThrowIfDisposed();
        if (packet.IsEmpty)
        {
            throw new ArgumentException("Packet cannot be empty when querying sample count.", nameof(packet));
        }

        unsafe
        {
            fixed (byte* packetPtr = packet)
            {
                var count = OpusNative.opus_packet_get_nb_samples(
                    new IntPtr(packetPtr),
                    packet.Length,
                    (int)Config.SampleRate);

                if (count == OpusConstants.OPUS_INVALID_PACKET)
                {
                    return 0;
                }

                if (count < 0)
                {
                    throw new OpusException(count, $"Failed to retrieve packet sample count: {OpusNative.GetErrorMessage(count)}");
                }

                return count;
            }
        }
    }

    /// <summary>
    /// Decode an Opus packet directly into the provided PCM buffer.
    /// </summary>
    /// <param name="packet">Encoded Opus data.</param>
    /// <param name="destination">Destination buffer (interleaved PCM16). Must be large enough for <c>samplesPerChannel * channels</c> elements.</param>
    /// <param name="useFEC">Whether to enable in-band FEC recovery.</param>
    /// <returns>The number of samples per channel written to the destination.</returns>
    public int DecodeInto(ReadOnlySpan<byte> packet, Span<short> destination, bool useFEC = false)
    {
        ThrowIfDisposed();
        if (packet.IsEmpty)
        {
            throw new ArgumentException("Packet cannot be empty when decoding.", nameof(packet));
        }

        var samplesPerChannel = GetPacketSampleCount(packet);
        var requiredSamples = samplesPerChannel * (int)Config.Channels;
        if (destination.Length < requiredSamples)
        {
            throw new ArgumentException($"Destination buffer too small. Requires at least {requiredSamples} samples.", nameof(destination));
        }

        unsafe
        {
            fixed (byte* packetPtr = packet)
            fixed (short* pcmPtr = destination)
            {
                var result = OpusNative.opus_decode(
                    _decoderPtr,
                    new IntPtr(packetPtr),
                    packet.Length,
                    new IntPtr(pcmPtr),
                    samplesPerChannel,
                    useFEC ? 1 : 0);

                if (result < 0)
                {
                    throw new OpusException(result, $"Decoding failed: {OpusNative.GetErrorMessage(result)}");
                }

                return result;
            }
        }
    }

    /// <summary>
    /// Decode with Forward Error Correction (FEC)
    /// </summary>
    public short[] DecodeWithFEC(byte[] packet, bool useFEC = false, FrameDuration expectedDuration = FrameDuration.Ms20)
    {
        ThrowIfDisposed();

        if (packet == null)
            throw new ArgumentNullException(nameof(packet));

        var frameSize = Config.SampleRate.FrameSize(expectedDuration);
        var outputSamples = frameSize * (int)Config.Channels;
        var pcmOutput = new short[outputSamples];

        unsafe
        {
            fixed (byte* packetPtr = packet)
            fixed (short* pcmPtr = pcmOutput)
            {
                var result = OpusNative.opus_decode(
                    _decoderPtr,
                    new IntPtr(packetPtr),
                    packet.Length,
                    new IntPtr(pcmPtr),
                    frameSize,
                    useFEC ? 1 : 0);

                if (result < 0)
                {
                    throw new OpusException(result, $"Decoding failed: {OpusNative.GetErrorMessage(result)}");
                }

                // Trim output to actual decoded samples
                if (result < frameSize)
                {
                    var actualSamples = result * (int)Config.Channels;
                    var trimmedOutput = new short[actualSamples];
                    Array.Copy(pcmOutput, trimmedOutput, actualSamples);
                    return trimmedOutput;
                }

                return pcmOutput;
            }
        }
    }

    /// <summary>
    /// Advanced FEC decoding matching Swift implementation
    /// Returns array of PCM results - first is recovered N-1 (if any), last is current packet N
    /// </summary>
    public List<short[]> DecodeUsingFECIfNeeded(bool prevLost, byte[] payloadN, FrameDuration expectedDuration = FrameDuration.Ms20)
    {
        ThrowIfDisposed();

        if (payloadN == null)
            throw new ArgumentNullException(nameof(payloadN));

        var results = new List<short[]>();

        if (prevLost)
        {
            // Step 1: Try to recover previous packet using FEC from current packet
            try
            {
                var recoveredPCM = DecodeWithFEC(payloadN, useFEC: true, expectedDuration);
                // Apply basic high-frequency filtering (simplified version of Swift implementation)
                var filteredPCM = ApplyHighFrequencyFilter(recoveredPCM);
                results.Add(filteredPCM);
            }
            catch (OpusException)
            {
                // FEC recovery failed, generate PLC instead
                var plcAudio = GeneratePLC(expectedDuration);
                results.Add(plcAudio);
            }
        }

        // Step 2: Decode current packet normally
        try
        {
            var currentPCM = DecodeWithFEC(payloadN, useFEC: false, expectedDuration);
            results.Add(currentPCM);
        }
        catch (OpusException)
        {
            // Current packet decoding failed, generate PLC
            var plcAudio = GeneratePLC(expectedDuration);
            results.Add(plcAudio);
        }

        return results;
    }

    /// <summary>
    /// Generate Packet Loss Concealment (PLC) for missing packet
    /// </summary>
    public short[] GeneratePLC(FrameDuration expectedDuration = FrameDuration.Ms20)
    {
        ThrowIfDisposed();

        var frameSize = Config.SampleRate.FrameSize(expectedDuration);
        var outputSamples = frameSize * (int)Config.Channels;
        var pcmOutput = new short[outputSamples];

        unsafe
        {
            fixed (short* pcmPtr = pcmOutput)
            {
                // Call opus_decode with null packet for PLC
                var result = OpusNative.opus_decode(
                    _decoderPtr,
                    IntPtr.Zero, // null packet triggers PLC
                    0,
                    new IntPtr(pcmPtr),
                    frameSize,
                    0);

                if (result < 0)
                {
                    throw new OpusException(result, $"PLC generation failed: {OpusNative.GetErrorMessage(result)}");
                }

                // Trim if necessary
                if (result < frameSize)
                {
                    var actualSamples = result * (int)Config.Channels;
                    var trimmedOutput = new short[actualSamples];
                    Array.Copy(pcmOutput, trimmedOutput, actualSamples);
                    return trimmedOutput;
                }

                return pcmOutput;
            }
        }
    }

    /// <summary>
    /// Decode with comprehensive FEC and PLC support
    /// </summary>
    public List<short[]> DecodeWithAdvancedFEC(byte[]? currentPacket, byte[]? previousPacket, byte[]? nextPacket, bool packetLoss = false)
    {
        var results = new List<short[]>();

        // Try to decode current packet first
        if (currentPacket != null && currentPacket.Length > 0)
        {
            try
            {
                var decoded = Decode(currentPacket);
                results.Add(decoded);
                return results;
            }
            catch (OpusException)
            {
                // Current packet failed, fall through to FEC/PLC
            }
        }

        // If current packet failed or missing, try FEC recovery
        if (packetLoss && nextPacket != null && nextPacket.Length > 0)
        {
            var fecResults = DecodeUsingFECIfNeeded(prevLost: true, nextPacket);
            results.AddRange(fecResults);
        }

        // If FEC failed or unavailable, generate PLC
        if (results.Count == 0)
        {
            var plc = GeneratePLC();
            results.Add(plc);
        }

        return results;
    }

    /// <summary>
    /// Get the sample rate (Hz) the decoder was initialised with.
    /// </summary>
    public int GetSampleRate()
    {
        ThrowIfDisposed();
        var result = OpusControl.GetDecoderSampleRate(_decoderPtr, out var sampleRate);
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to get decoder sample rate: {OpusNative.GetErrorMessage(result)}");
        }

        return sampleRate;
    }

    /// <summary>
    /// Enable or disable phase inversion suppression for stereo downmixing.
    /// </summary>
    public void SetPhaseInversionDisabled(bool disabled)
    {
        ThrowIfDisposed();
        var result = OpusControl.SetDecoderPhaseInversionDisabled(_decoderPtr, disabled);
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to set phase inversion disabled: {OpusNative.GetErrorMessage(result)}");
        }
    }

    /// <summary>
    /// Check whether phase inversion suppression is enabled.
    /// </summary>
    public bool GetPhaseInversionDisabled()
    {
        ThrowIfDisposed();
        var result = OpusControl.GetDecoderPhaseInversionDisabled(_decoderPtr, out var disabled);
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to get phase inversion state: {OpusNative.GetErrorMessage(result)}");
        }

        return disabled;
    }

    /// <summary>
    /// Apply gain in Q8 dB (value / 256.0f) at the decoder output.
    /// </summary>
    public void SetGain(int gainQ8)
    {
        ThrowIfDisposed();
        if (gainQ8 < short.MinValue || gainQ8 > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(gainQ8), "Gain must be between -32768 and 32767 (Q8 dB).");
        }

        var result = OpusControl.SetDecoderGain(_decoderPtr, gainQ8);
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to set decoder gain: {OpusNative.GetErrorMessage(result)}");
        }
    }

    /// <summary>
    /// Get the current decoder gain in Q8 dB (value / 256.0f).
    /// </summary>
    public int GetGain()
    {
        ThrowIfDisposed();
        var result = OpusControl.GetDecoderGain(_decoderPtr, out var gainQ8);
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to get decoder gain: {OpusNative.GetErrorMessage(result)}");
        }

        return gainQ8;
    }

    /// <summary>
    /// Apply simple high-frequency filtering (simplified version of Swift implementation)
    /// </summary>
    private short[] ApplyHighFrequencyFilter(short[] samples)
    {
        if (samples.Length <= 1)
            return samples;

        var filtered = new short[samples.Length];
        filtered[0] = samples[0];

        // Simple exponential smoothing filter
        const float alpha = 0.15f;

        for (int i = 1; i < samples.Length; i++)
        {
            var current = samples[i];
            var previous = filtered[i - 1];
            var smoothed = previous + alpha * (current - previous);
            filtered[i] = (short)Math.Clamp(smoothed, short.MinValue, short.MaxValue);
        }

        return filtered;
    }

    /// <summary>
    /// Reset decoder state
    /// </summary>
    public void Reset()
    {
        ThrowIfDisposed();
        var result = OpusNative.opus_decoder_init(_decoderPtr, (int)Config.SampleRate, (int)Config.Channels);
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to reset decoder: {OpusNative.GetErrorMessage(result)}");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(OpusDecoder));
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (_decoderPtr != IntPtr.Zero)
            {
                OpusNative.opus_decoder_destroy(_decoderPtr);
                _decoderPtr = IntPtr.Zero;
            }
            _disposed = true;
        }
    }

    ~OpusDecoder()
    {
        Dispose(false);
    }
}
