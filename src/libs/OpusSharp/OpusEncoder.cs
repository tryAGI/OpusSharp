using OpusSharp.Native;

namespace OpusSharp;

/// <summary>
/// High-level Opus encoder
/// </summary>
public sealed class OpusEncoder : IDisposable
{
    private IntPtr _encoderPtr = IntPtr.Zero;
    private bool _disposed;

    public OpusConfig Config { get; private set; }

    public OpusEncoder(OpusConfig config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Initialize();
    }

    private void Initialize()
    {
        _encoderPtr = OpusNative.opus_encoder_create(
            (int)Config.SampleRate,
            (int)Config.Channels,
            OpusControl.ApplicationToNative(Config.Application),
            out var error);

        if (_encoderPtr == IntPtr.Zero || error != OpusConstants.OPUS_OK)
        {
            throw new OpusException(error, $"Failed to create Opus encoder: {OpusNative.GetErrorMessage(error)}");
        }

        // Apply configuration
        ApplyConfiguration(Config);
    }

    private void ApplyConfiguration(OpusConfig config)
    {
        var operations = new (string Name, Func<int> Operation)[]
        {
            ("SetComplexity", () => OpusControl.SetComplexity(_encoderPtr, config.Complexity)),
            ("SetVBR", () => OpusControl.SetVBR(_encoderPtr, config.Vbr)),
            ("SetVBRConstraint", () => OpusControl.SetVBRConstraint(_encoderPtr, config.ConstrainedVbr)),
            ("SetInbandFEC", () => OpusControl.SetInbandFEC(_encoderPtr, config.Fec)),
            ("SetDTX", () => OpusControl.SetDTX(_encoderPtr, config.Dtx)),
            ("SetPacketLossPercent", () => OpusControl.SetPacketLossPercent(_encoderPtr, config.ExpectedLossPercent)),
            ("SetBitrate", () => OpusControl.SetBitrate(_encoderPtr, config.Bitrate))
        };

        foreach (var (name, operation) in operations)
        {
            var result = operation();
            if (result != OpusConstants.OPUS_OK)
            {
                // For now, log the error but continue - we need to investigate control API issues
                System.Diagnostics.Debug.WriteLine($"Warning: Failed to configure encoder ({name}): {OpusNative.GetErrorMessage(result)}");
            }
        }
    }

    /// <summary>
    /// Encode PCM16 audio data
    /// </summary>
    public byte[] Encode(short[] pcm, int frameSize)
    {
        ThrowIfDisposed();

        if (pcm == null)
            throw new ArgumentNullException(nameof(pcm));
        if (frameSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameSize));

        var maxPacketSize = 1500; // Conservative maximum
        var outputBuffer = new byte[maxPacketSize];
        var bytesEncoded = Encode(pcm, frameSize, outputBuffer);

        var result = new byte[bytesEncoded];
        Array.Copy(outputBuffer, result, bytesEncoded);
        return result;
    }

    /// <summary>
    /// Encode PCM16 audio data with specific output buffer
    /// </summary>
    public int Encode(short[] pcm, int frameSize, byte[] output)
    {
        ThrowIfDisposed();

        if (pcm == null)
            throw new ArgumentNullException(nameof(pcm));
        if (output == null)
            throw new ArgumentNullException(nameof(output));
        if (frameSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(frameSize));
        if (output.Length == 0)
            throw new ArgumentException("Output buffer cannot be empty", nameof(output));

        unsafe
        {
            fixed (short* pcmPtr = pcm)
            fixed (byte* outputPtr = output)
            {
                var result = OpusNative.opus_encode(
                    _encoderPtr,
                    new IntPtr(pcmPtr),
                    frameSize,
                    new IntPtr(outputPtr),
                    output.Length);

                if (result < 0)
                {
                    throw new OpusException(result, $"Encoding failed: {OpusNative.GetErrorMessage(result)}");
                }

                return result;
            }
        }
    }

    /// <summary>
    /// Set bitrate
    /// </summary>
    public void SetBitrate(int bitrate)
    {
        ThrowIfDisposed();
        var result = OpusControl.SetBitrate(_encoderPtr, bitrate);
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to set bitrate: {OpusNative.GetErrorMessage(result)}");
        }
        Config.Bitrate = bitrate;
    }

    /// <summary>
    /// Set bandwidth
    /// </summary>
    public void SetBandwidth(Bandwidth bandwidth)
    {
        ThrowIfDisposed();
        var result = OpusControl.SetBandwidth(_encoderPtr, OpusControl.BandwidthToNative(bandwidth));
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to set bandwidth: {OpusNative.GetErrorMessage(result)}");
        }
    }

    /// <summary>
    /// Set VBR mode
    /// </summary>
    public void SetVbr(bool enabled, bool constrained = false)
    {
        ThrowIfDisposed();
        var vbrResult = OpusControl.SetVBR(_encoderPtr, enabled);
        var constrainedResult = OpusControl.SetVBRConstraint(_encoderPtr, constrained);

        if (vbrResult != OpusConstants.OPUS_OK)
        {
            throw new OpusException(vbrResult, $"Failed to set VBR: {OpusNative.GetErrorMessage(vbrResult)}");
        }
        if (constrainedResult != OpusConstants.OPUS_OK)
        {
            throw new OpusException(constrainedResult, $"Failed to set VBR constraint: {OpusNative.GetErrorMessage(constrainedResult)}");
        }

        Config.Vbr = enabled;
        Config.ConstrainedVbr = constrained;
    }

    /// <summary>
    /// Set DTX (discontinuous transmission)
    /// </summary>
    public void SetDtx(bool enabled)
    {
        ThrowIfDisposed();
        var result = OpusControl.SetDTX(_encoderPtr, enabled);
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to set DTX: {OpusNative.GetErrorMessage(result)}");
        }
        Config.Dtx = enabled;
    }

    /// <summary>
    /// Set FEC (forward error correction)
    /// </summary>
    public void SetFec(bool enabled)
    {
        ThrowIfDisposed();
        var result = OpusControl.SetInbandFEC(_encoderPtr, enabled);
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to set FEC: {OpusNative.GetErrorMessage(result)}");
        }
        Config.Fec = enabled;
    }

    /// <summary>
    /// Set complexity (0-10)
    /// </summary>
    public void SetComplexity(int complexity)
    {
        ThrowIfDisposed();
        if (complexity < 0 || complexity > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(complexity), "Complexity must be between 0 and 10");
        }

        var result = OpusControl.SetComplexity(_encoderPtr, complexity);
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to set complexity: {OpusNative.GetErrorMessage(result)}");
        }
        Config.Complexity = complexity;
    }

    /// <summary>
    /// Reset encoder state
    /// </summary>
    public void Reset()
    {
        ThrowIfDisposed();
        var result = OpusNative.opus_encoder_init(_encoderPtr, (int)Config.SampleRate, (int)Config.Channels, OpusControl.ApplicationToNative(Config.Application));
        if (result != OpusConstants.OPUS_OK)
        {
            throw new OpusException(result, $"Failed to reset encoder: {OpusNative.GetErrorMessage(result)}");
        }
        ApplyConfiguration(Config);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(OpusEncoder));
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
            if (_encoderPtr != IntPtr.Zero)
            {
                OpusNative.opus_encoder_destroy(_encoderPtr);
                _encoderPtr = IntPtr.Zero;
            }
            _disposed = true;
        }
    }

    ~OpusEncoder()
    {
        Dispose(false);
    }
}
