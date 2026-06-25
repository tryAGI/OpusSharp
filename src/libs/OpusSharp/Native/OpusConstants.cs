namespace OpusSharp.Native;

/// <summary>
/// Opus native constants (matching opus_defines.h)
/// </summary>
internal static class OpusConstants
{
    // Error codes
    public const int OPUS_OK = 0;
    public const int OPUS_BAD_ARG = -1;
    public const int OPUS_BUFFER_TOO_SMALL = -2;
    public const int OPUS_INTERNAL_ERROR = -3;
    public const int OPUS_INVALID_PACKET = -4;
    public const int OPUS_UNIMPLEMENTED = -5;
    public const int OPUS_INVALID_STATE = -6;
    public const int OPUS_ALLOC_FAIL = -7;

    // Control parameters
    public const int OPUS_SET_APPLICATION_REQUEST = 4000;
    public const int OPUS_GET_APPLICATION_REQUEST = 4001;
    public const int OPUS_SET_BITRATE_REQUEST = 4002;
    public const int OPUS_GET_BITRATE_REQUEST = 4003;
    public const int OPUS_SET_MAX_BANDWIDTH_REQUEST = 4004;
    public const int OPUS_GET_MAX_BANDWIDTH_REQUEST = 4005;
    public const int OPUS_SET_VBR_REQUEST = 4006;
    public const int OPUS_GET_VBR_REQUEST = 4007;
    public const int OPUS_SET_BANDWIDTH_REQUEST = 4008;
    public const int OPUS_GET_BANDWIDTH_REQUEST = 4009;
    public const int OPUS_SET_COMPLEXITY_REQUEST = 4010;
    public const int OPUS_GET_COMPLEXITY_REQUEST = 4011;
    public const int OPUS_SET_INBAND_FEC_REQUEST = 4012;
    public const int OPUS_GET_INBAND_FEC_REQUEST = 4013;
    public const int OPUS_SET_PACKET_LOSS_PERC_REQUEST = 4014;
    public const int OPUS_GET_PACKET_LOSS_PERC_REQUEST = 4015;
    public const int OPUS_SET_DTX_REQUEST = 4016;
    public const int OPUS_GET_DTX_REQUEST = 4017;
    public const int OPUS_SET_VBR_CONSTRAINT_REQUEST = 4020;
    public const int OPUS_GET_VBR_CONSTRAINT_REQUEST = 4021;
    public const int OPUS_SET_SIGNAL_REQUEST = 4024;
    public const int OPUS_GET_SIGNAL_REQUEST = 4025;
    public const int OPUS_GET_SAMPLE_RATE_REQUEST = 4029;
    public const int OPUS_SET_GAIN_REQUEST = 4034;
    public const int OPUS_GET_GAIN_REQUEST = 4045;
    public const int OPUS_SET_PHASE_INVERSION_DISABLED_REQUEST = 4046;
    public const int OPUS_GET_PHASE_INVERSION_DISABLED_REQUEST = 4047;

    // Applications
    public const int OPUS_APPLICATION_VOIP = 2048;
    public const int OPUS_APPLICATION_AUDIO = 2049;
    public const int OPUS_APPLICATION_RESTRICTED_LOWDELAY = 2051;

    // Bandwidth
    public const int OPUS_BANDWIDTH_NARROWBAND = 1101;
    public const int OPUS_BANDWIDTH_MEDIUMBAND = 1102;
    public const int OPUS_BANDWIDTH_WIDEBAND = 1103;
    public const int OPUS_BANDWIDTH_SUPERWIDEBAND = 1104;
    public const int OPUS_BANDWIDTH_FULLBAND = 1105;
    public const int OPUS_BANDWIDTH_AUTO = 1111;

    // Signal types
    public const int OPUS_SIGNAL_VOICE = 3001;
    public const int OPUS_SIGNAL_MUSIC = 3002;
    public const int OPUS_SIGNAL_AUTO = 3111;

    // Bitrate constants
    public const int OPUS_BITRATE_AUTO = -1000;
    public const int OPUS_BITRATE_MAX = -1;
}
