using System.Runtime.InteropServices;

namespace OpusSharp.Native;

/// <summary>
/// P/Invoke bindings for the native Opus library
/// </summary>
internal static class OpusNative
{
    // Library names for different platforms
    private const string LibraryName = "opus";
    private const string ShimLibraryName = "opus_sharp"; // our non-variadic shim

    // For development/testing, we'll try to load from different possible locations
    private const string LibraryNameWindows = "opus.dll";
    private const string LibraryNameLinux = "libopus.so";
    private const string LibraryNameMacOS = "libopus.dylib";

    static OpusNative()
    {
        try
        {
            NativeLibraryLoader.Initialize();
        }
        catch (Exception ex)
        {
            // Don't throw in static constructor, let individual method calls handle it
            System.Diagnostics.Debug.WriteLine($"Failed to initialize Opus native library: {ex.Message}");
        }
    }

    #region Encoder Functions

    /// <summary>
    /// Create a new Opus encoder
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr opus_encoder_create(int Fs, int channels, int application, out int error);

    /// <summary>
    /// Destroy an Opus encoder
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void opus_encoder_destroy(IntPtr st);

    /// <summary>
    /// Encode PCM16 audio
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_encode(IntPtr st, IntPtr pcm, int frame_size, IntPtr data, int max_data_bytes);

    /// <summary>
    /// Encode PCM32 float audio
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_encode_float(IntPtr st, IntPtr pcm, int frame_size, IntPtr data, int max_data_bytes);

    // NOTE: .NET cannot safely call C varargs. Use a small C shim instead.
    [DllImport(ShimLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "opussharp_encoder_set_int")]
    public static extern int opus_encoder_ctl_int(IntPtr st, int request, int value);

    [DllImport(ShimLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "opussharp_encoder_get_int")]
    public static extern int opus_encoder_ctl_get(IntPtr st, int request, out int value);

    /// <summary>
    /// Initialize/reset encoder
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_encoder_init(IntPtr st, int Fs, int channels, int application);

    #endregion

    #region Decoder Functions

    /// <summary>
    /// Create a new Opus decoder
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr opus_decoder_create(int Fs, int channels, out int error);

    /// <summary>
    /// Destroy an Opus decoder
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void opus_decoder_destroy(IntPtr st);

    /// <summary>
    /// Decode Opus packet to PCM16
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_decode(IntPtr st, IntPtr data, int len, IntPtr pcm, int frame_size, int decode_fec);

    /// <summary>
    /// Decode Opus packet to PCM32 float
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_decode_float(IntPtr st, IntPtr data, int len, IntPtr pcm, int frame_size, int decode_fec);

    // Non-variadic shims for decoder CTLs
    [DllImport(ShimLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "opussharp_decoder_set_int")]
    public static extern int opus_decoder_ctl_int(IntPtr st, int request, int value);

    [DllImport(ShimLibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "opussharp_decoder_get_int")]
    public static extern int opus_decoder_ctl_get(IntPtr st, int request, out int value);

    /// <summary>
    /// Get number of samples in Opus packet
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_decoder_get_nb_samples(IntPtr dec, IntPtr packet, int len);

    /// <summary>
    /// Initialize/reset decoder
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_decoder_init(IntPtr st, int Fs, int channels);

    #endregion

    #region Repacketizer Functions

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr opus_repacketizer_create();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void opus_repacketizer_destroy(IntPtr rp);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr opus_repacketizer_init(IntPtr rp);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_repacketizer_cat(IntPtr rp, IntPtr data, int len);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_repacketizer_out(IntPtr rp, IntPtr data, int maxLen);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_repacketizer_out_range(IntPtr rp, int begin, int end, IntPtr data, int maxLen);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_repacketizer_get_nb_frames(IntPtr rp);

    #endregion

    #region Utility Functions

    /// <summary>
    /// Get Opus version string
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr opus_get_version_string();

    /// <summary>
    /// Get error string from error code
    /// </summary>
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr opus_strerror(int error);

    #endregion

    #region Packet Analysis Functions

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_packet_get_nb_channels(IntPtr data);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_packet_get_bandwidth(IntPtr data);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_packet_get_nb_frames(IntPtr data, int len);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_packet_get_samples_per_frame(IntPtr data, int Fs);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_packet_get_nb_samples(IntPtr data, int len, int Fs);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int opus_packet_parse(
        IntPtr data,
        int len,
        IntPtr out_toc,
        IntPtr frames,
        IntPtr sizes,
        IntPtr payload_offset);

    #endregion

    #region Helper Methods

    /// <summary>
    /// Check if error code indicates success
    /// </summary>
    public static bool IsSuccess(int errorCode) => errorCode >= 0;

    /// <summary>
    /// Get error message from error code
    /// </summary>
    public static string GetErrorMessage(int errorCode)
    {
        try
        {
            var ptr = opus_strerror(errorCode);
            return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) ?? $"Unknown error: {errorCode}" : $"Unknown error: {errorCode}";
        }
        catch
        {
            return $"Error code: {errorCode}";
        }
    }

    /// <summary>
    /// Get Opus version
    /// </summary>
    public static string GetVersion()
    {
        try
        {
            var ptr = opus_get_version_string();
            return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) ?? "Unknown version" : "Unknown version";
        }
        catch
        {
            return "Unknown version";
        }
    }

    #endregion
}
