namespace OpusSharp;

/// <summary>
/// Opus encoder configuration
/// </summary>
public class OpusConfig
{
    /// <summary>
    /// Medium quality voice: mono, ~12-16 kbps, 20ms frames
    /// Good balance between quality and bandwidth
    /// </summary>
    public const string VoiceMediumQualityMonoPrefix = "voice_medium_quality_mono";

    /// <summary>
    /// High quality voice: mono, ~32-64 kbps, 20ms frames
    /// Best quality for voice while still compressed
    /// </summary>
    public const string VoiceHighQualityMonoPrefix = "voice_high_quality_mono";

    /// <summary>
    /// Low quality voice: mono, ~6-8 kbps, 60ms frames (bundled)
    /// Optimized for poor network conditions
    /// </summary>
    public const string VoiceLowQualityMonoPrefix = "voice_low_quality_mono";

    /// <summary>
    /// High quality voice long frames: mono, ~32-64 kbps, 120ms frames
    /// Same quality as high quality but 6x fewer packets for Watch Connectivity efficiency
    /// </summary>
    public const string VoiceHighQualityLongMonoPrefix = "voice_high_quality_long_mono";

    /// <summary>
    /// Music quality: stereo, ~96-128 kbps, 20ms frames
    /// Optimized for high-quality music streaming
    /// </summary>
    public const string MusicStereoPrefix = "music_stereo";

    /// <summary>
    /// Logical preset identifier (see <see cref="OpusProfiles"/>).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Sampling rate for the encoder or decoder.
    /// </summary>
    public SampleRate SampleRate { get; init; } = SampleRate.Hz48k;

    /// <summary>
    /// Channel configuration used by the Opus stream.
    /// </summary>
    public Channels Channels { get; init; } = Channels.Mono;

    /// <summary>
    /// Target application mode that guides Opus optimisations.
    /// </summary>
    public Application Application { get; init; } = Application.Voice;

    /// <summary>
    /// Encoder complexity (0-10) balancing quality and CPU cost.
    /// </summary>
    public int Complexity { get; set; } = 10;

    /// <summary>
    /// Enables variable bitrate (VBR) encoding.
    /// </summary>
    public bool Vbr { get; set; } = true;

    /// <summary>
    /// Constrains VBR to maintain a tighter bitrate envelope.
    /// </summary>
    public bool ConstrainedVbr { get; set; } = true;

    /// <summary>
    /// Enables in-band Forward Error Correction (FEC).
    /// </summary>
    public bool Fec { get; set; }

    /// <summary>
    /// Enables Discontinuous Transmission (DTX) to suppress silence frames.
    /// </summary>
    public bool Dtx { get; set; }

    /// <summary>
    /// Expected packet loss percentage for network conditions.
    /// </summary>
    public int ExpectedLossPercent { get; private init; }

    /// <summary>
    /// Nominal target bitrate expressed in bits per second.
    /// </summary>
    public int Bitrate { get; set; } = 32000;

    /// <summary>
    /// Frame duration expected by the encoder/decoder.
    /// </summary>
    public FrameDuration FrameDuration { get; init; } = FrameDuration.Ms20;

    /// <summary>
    /// Voice optimized preset (medium quality, 20ms frames, ~12-16 kbps).
    /// Encoder/decoder will accept any valid sample rate (8/12/16/24/48 kHz).
    /// Opus internally resamples to 48kHz for processing.
    /// </summary>
    public static OpusConfig VoiceMediumQualityMono(SampleRate sampleRate) =>
        new()
        {
            Name = VoiceMediumQualityMonoPrefix + sampleRate,
            SampleRate = sampleRate,
            Channels = Channels.Mono,
            Application = Application.Voice,
            Complexity = 10,
            Vbr = true,
            ConstrainedVbr = true,
            Fec = true,
            Dtx = true,
            ExpectedLossPercent = 10,
            Bitrate = 24000,
            FrameDuration = FrameDuration.Ms20
        };

    /// <summary>
    /// High quality voice preset (20ms frames, ~32-64 kbps).
    /// Encoder/decoder will accept any valid sample rate (8/12/16/24/48 kHz).
    /// Opus internally resamples to 48kHz for processing.
    /// </summary>
    public static OpusConfig VoiceHighQualityMono(SampleRate sampleRate) =>
        new()
        {
            Name = VoiceHighQualityMonoPrefix + sampleRate,
            SampleRate = sampleRate,
            Channels = Channels.Mono,
            Application = Application.Voice,
            Complexity = 10,
            Vbr = true,
            ConstrainedVbr = true,
            Fec = true,
            Dtx = true,
            ExpectedLossPercent = 5,
            Bitrate = 64000,
            FrameDuration = FrameDuration.Ms20
        };

    /// <summary>
    /// Low quality voice preset for poor network conditions (60ms frames, ~6-8 kbps).
    /// Encoder/decoder will accept any valid sample rate (8/12/16/24/48 kHz).
    /// Opus internally resamples to 48kHz for processing.
    /// </summary>
    public static OpusConfig VoiceLowQualityMono(SampleRate sampleRate) =>
        new()
        {
            Name = VoiceLowQualityMonoPrefix + sampleRate,
            SampleRate = sampleRate,
            Channels = Channels.Mono,
            Application = Application.Voice,
            Complexity = 2,
            Vbr = true,
            ConstrainedVbr = false,
            Fec = true,
            Dtx = true,
            ExpectedLossPercent = 5,
            Bitrate = 6000,
            FrameDuration = FrameDuration.Ms60
        };

    /// <summary>
    /// High quality voice long frames preset for Watch Connectivity transport and archival output (120ms frames, ~32-64 kbps).
    /// Same quality as VoiceHighQualityMono but uses 120ms frames for 6x packet reduction.
    /// Encoder/decoder will accept any valid sample rate (8/12/16/24/48 kHz).
    /// Opus internally resamples to 48kHz for processing.
    /// </summary>
    public static OpusConfig VoiceHighQualityLongMono(SampleRate sampleRate) =>
        new()
        {
            Name = VoiceHighQualityLongMonoPrefix + sampleRate,
            SampleRate = sampleRate,
            Channels = Channels.Mono,
            Application = Application.Voice,
            Complexity = 10,
            Vbr = true,
            ConstrainedVbr = true,
            Fec = true,
            Dtx = true,
            ExpectedLossPercent = 5,
            Bitrate = 64000,
            FrameDuration = FrameDuration.Ms120
        };

    /// <summary>
    /// Music optimized preset (stereo, 20ms frames, ~96-128 kbps).
    /// Encoder/decoder will accept any valid sample rate (8/12/16/24/48 kHz).
    /// Opus internally resamples to 48kHz for processing.
    /// </summary>
    public static OpusConfig MusicStereo(SampleRate sampleRate) =>
        new()
        {
            Name = MusicStereoPrefix + sampleRate,
            SampleRate = sampleRate,
            Channels = Channels.Stereo,
            Application = Application.Audio,
            Complexity = 9,
            Vbr = true,
            ConstrainedVbr = false,
            Fec = false,
            Dtx = false,
            ExpectedLossPercent = 0,
            Bitrate = 128000,
            FrameDuration = FrameDuration.Ms20
        };
}
