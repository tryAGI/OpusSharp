using OpusSharp.Native;

namespace OpusSharp;

/// <summary>
/// Managed wrapper for the Opus repacketizer allowing frame-level manipulation of Opus packets.
/// </summary>
public sealed class OpusRepacketizer : IDisposable
{
    private IntPtr _handle = IntPtr.Zero;
    private bool _disposed;

    public OpusRepacketizer()
    {
        _handle = OpusNative.opus_repacketizer_create();
        if (_handle == IntPtr.Zero)
        {
            throw new OpusException(OpusConstants.OPUS_ALLOC_FAIL, "Failed to create Opus repacketizer.");
        }
    }

    /// <summary>
    /// Reset internal state so the repacketizer can be reused.
    /// </summary>
    public void Reset()
    {
        ThrowIfDisposed();
        var result = OpusNative.opus_repacketizer_init(_handle);
        if (result == IntPtr.Zero)
        {
            throw new OpusException(OpusConstants.OPUS_INTERNAL_ERROR, "Failed to reset Opus repacketizer.");
        }
    }

    /// <summary>
    /// Append all frames from the supplied Opus packet.
    /// </summary>
    public void AddPacket(ReadOnlySpan<byte> packet)
    {
        ThrowIfDisposed();
        if (packet.IsEmpty)
        {
            throw new ArgumentException("Packet cannot be empty.", nameof(packet));
        }

        unsafe
        {
            fixed (byte* packetPtr = packet)
            {
                var result = OpusNative.opus_repacketizer_cat(_handle, new IntPtr(packetPtr), packet.Length);
                if (result != OpusConstants.OPUS_OK)
                {
                    throw new OpusException(result, $"Failed to append packet: {OpusNative.GetErrorMessage(result)}");
                }
            }
        }
    }

    /// <summary>
    /// Get the number of frames currently buffered.
    /// </summary>
    public int GetFrameCount()
    {
        ThrowIfDisposed();
        var result = OpusNative.opus_repacketizer_get_nb_frames(_handle);
        if (result < 0)
        {
            throw new OpusException(result, $"Failed to query frame count: {OpusNative.GetErrorMessage(result)}");
        }
        return result;
    }

    /// <summary>
    /// Write all buffered frames into a single Opus packet.
    /// </summary>
    public int WritePacket(Span<byte> destination) => WriteRange(0, GetFrameCount(), destination);

    /// <summary>
    /// Write a single frame to the destination.
    /// </summary>
    public int WriteFrame(int frameIndex, Span<byte> destination) => WriteRange(frameIndex, frameIndex + 1, destination);

    /// <summary>
    /// Write frames in the half-open range [beginFrame, endFrame) to the destination.
    /// </summary>
    public int WriteRange(int beginFrame, int endFrame, Span<byte> destination)
    {
        ThrowIfDisposed();
        if (beginFrame < 0)
            throw new ArgumentOutOfRangeException(nameof(beginFrame));
        if (endFrame <= beginFrame)
            throw new ArgumentOutOfRangeException(nameof(endFrame), "End frame must be greater than begin frame.");
        if (destination.IsEmpty)
            throw new ArgumentException("Destination must be non-empty.", nameof(destination));

        unsafe
        {
            fixed (byte* destPtr = destination)
            {
                var result = OpusNative.opus_repacketizer_out_range(_handle, beginFrame, endFrame, new IntPtr(destPtr), destination.Length);
                if (result < 0)
                {
                    throw new OpusException(result, $"Failed to repacketize frames [{beginFrame},{endFrame}): {OpusNative.GetErrorMessage(result)}");
                }
                return result;
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(OpusRepacketizer));
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (_handle != IntPtr.Zero)
        {
            OpusNative.opus_repacketizer_destroy(_handle);
            _handle = IntPtr.Zero;
        }

        _disposed = true;
    }

    ~OpusRepacketizer()
    {
        Dispose(false);
    }
}
