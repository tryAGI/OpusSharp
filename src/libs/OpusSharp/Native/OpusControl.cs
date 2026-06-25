namespace OpusSharp.Native;

/// <summary>
/// Helper methods for Opus encoder control (mirrors Swift opus_shim.c)
/// </summary>
internal static class OpusControl
{
    #region Encoder Control Helpers

    public static int SetComplexity(IntPtr encoder, int value)
    {
        // Try the direct int parameter approach
        return OpusNative.opus_encoder_ctl_int(encoder, OpusConstants.OPUS_SET_COMPLEXITY_REQUEST, value);
    }

    public static int GetComplexity(IntPtr encoder, out int value)
    {
        return OpusNative.opus_encoder_ctl_get(encoder, OpusConstants.OPUS_GET_COMPLEXITY_REQUEST, out value);
    }

    public static int SetBitrate(IntPtr encoder, int value)
    {
        return OpusNative.opus_encoder_ctl_int(encoder, OpusConstants.OPUS_SET_BITRATE_REQUEST, value);
    }

    public static int GetBitrate(IntPtr encoder, out int value)
    {
        return OpusNative.opus_encoder_ctl_get(encoder, OpusConstants.OPUS_GET_BITRATE_REQUEST, out value);
    }

    public static int SetVBR(IntPtr encoder, bool enabled)
    {
        return OpusNative.opus_encoder_ctl_int(encoder, OpusConstants.OPUS_SET_VBR_REQUEST, enabled ? 1 : 0);
    }

    public static int GetVBR(IntPtr encoder, out bool enabled)
    {
        var result = OpusNative.opus_encoder_ctl_get(encoder, OpusConstants.OPUS_GET_VBR_REQUEST, out int value);
        enabled = value != 0;
        return result;
    }

    public static int SetVBRConstraint(IntPtr encoder, bool enabled)
    {
        return OpusNative.opus_encoder_ctl_int(encoder, OpusConstants.OPUS_SET_VBR_CONSTRAINT_REQUEST, enabled ? 1 : 0);
    }

    public static int GetVBRConstraint(IntPtr encoder, out bool enabled)
    {
        var result = OpusNative.opus_encoder_ctl_get(encoder, OpusConstants.OPUS_GET_VBR_CONSTRAINT_REQUEST, out int value);
        enabled = value != 0;
        return result;
    }

    public static int SetInbandFEC(IntPtr encoder, bool enabled)
    {
        return OpusNative.opus_encoder_ctl_int(encoder, OpusConstants.OPUS_SET_INBAND_FEC_REQUEST, enabled ? 1 : 0);
    }

    public static int GetInbandFEC(IntPtr encoder, out bool enabled)
    {
        var result = OpusNative.opus_encoder_ctl_get(encoder, OpusConstants.OPUS_GET_INBAND_FEC_REQUEST, out int value);
        enabled = value != 0;
        return result;
    }

    public static int SetDTX(IntPtr encoder, bool enabled)
    {
        return OpusNative.opus_encoder_ctl_int(encoder, OpusConstants.OPUS_SET_DTX_REQUEST, enabled ? 1 : 0);
    }

    public static int GetDTX(IntPtr encoder, out bool enabled)
    {
        var result = OpusNative.opus_encoder_ctl_get(encoder, OpusConstants.OPUS_GET_DTX_REQUEST, out int value);
        enabled = value != 0;
        return result;
    }

    public static int SetPacketLossPercent(IntPtr encoder, int percent)
    {
        return OpusNative.opus_encoder_ctl_int(encoder, OpusConstants.OPUS_SET_PACKET_LOSS_PERC_REQUEST, percent);
    }

    public static int GetPacketLossPercent(IntPtr encoder, out int percent)
    {
        return OpusNative.opus_encoder_ctl_get(encoder, OpusConstants.OPUS_GET_PACKET_LOSS_PERC_REQUEST, out percent);
    }

    public static int SetBandwidth(IntPtr encoder, int bandwidth)
    {
        return OpusNative.opus_encoder_ctl_int(encoder, OpusConstants.OPUS_SET_BANDWIDTH_REQUEST, bandwidth);
    }

    public static int GetBandwidth(IntPtr encoder, out int bandwidth)
    {
        return OpusNative.opus_encoder_ctl_get(encoder, OpusConstants.OPUS_GET_BANDWIDTH_REQUEST, out bandwidth);
    }

    public static int SetMaxBandwidth(IntPtr encoder, int bandwidth)
    {
        return OpusNative.opus_encoder_ctl_int(encoder, OpusConstants.OPUS_SET_MAX_BANDWIDTH_REQUEST, bandwidth);
    }

    public static int GetMaxBandwidth(IntPtr encoder, out int bandwidth)
    {
        return OpusNative.opus_encoder_ctl_get(encoder, OpusConstants.OPUS_GET_MAX_BANDWIDTH_REQUEST, out bandwidth);
    }

    public static int SetSignal(IntPtr encoder, int signal)
    {
        return OpusNative.opus_encoder_ctl_int(encoder, OpusConstants.OPUS_SET_SIGNAL_REQUEST, signal);
    }

    public static int GetSignal(IntPtr encoder, out int signal)
    {
        return OpusNative.opus_encoder_ctl_get(encoder, OpusConstants.OPUS_GET_SIGNAL_REQUEST, out signal);
    }

    public static int SetApplication(IntPtr encoder, int application)
    {
        return OpusNative.opus_encoder_ctl_int(encoder, OpusConstants.OPUS_SET_APPLICATION_REQUEST, application);
    }

    public static int GetApplication(IntPtr encoder, out int application)
    {
        return OpusNative.opus_encoder_ctl_get(encoder, OpusConstants.OPUS_GET_APPLICATION_REQUEST, out application);
    }

    #endregion

    #region Decoder Control Helpers

    public static int GetDecoderSampleRate(IntPtr decoder, out int sampleRate)
    {
        return OpusNative.opus_decoder_ctl_get(decoder, OpusConstants.OPUS_GET_SAMPLE_RATE_REQUEST, out sampleRate);
    }

    public static int SetDecoderPhaseInversionDisabled(IntPtr decoder, bool disabled)
    {
        return OpusNative.opus_decoder_ctl_int(decoder, OpusConstants.OPUS_SET_PHASE_INVERSION_DISABLED_REQUEST, disabled ? 1 : 0);
    }

    public static int GetDecoderPhaseInversionDisabled(IntPtr decoder, out bool disabled)
    {
        var result = OpusNative.opus_decoder_ctl_get(decoder, OpusConstants.OPUS_GET_PHASE_INVERSION_DISABLED_REQUEST, out int value);
        disabled = value != 0;
        return result;
    }

    public static int SetDecoderGain(IntPtr decoder, int gainQ8)
    {
        return OpusNative.opus_decoder_ctl_int(decoder, OpusConstants.OPUS_SET_GAIN_REQUEST, gainQ8);
    }

    public static int GetDecoderGain(IntPtr decoder, out int gainQ8)
    {
        return OpusNative.opus_decoder_ctl_get(decoder, OpusConstants.OPUS_GET_GAIN_REQUEST, out gainQ8);
    }

    #endregion

    #region Value Conversion Helpers

    public static int BandwidthToNative(Bandwidth bandwidth) => bandwidth switch
    {
        Bandwidth.Narrowband => OpusConstants.OPUS_BANDWIDTH_NARROWBAND,
        Bandwidth.Mediumband => OpusConstants.OPUS_BANDWIDTH_MEDIUMBAND,
        Bandwidth.Wideband => OpusConstants.OPUS_BANDWIDTH_WIDEBAND,
        Bandwidth.SuperWideband => OpusConstants.OPUS_BANDWIDTH_SUPERWIDEBAND,
        Bandwidth.Fullband => OpusConstants.OPUS_BANDWIDTH_FULLBAND,
        _ => OpusConstants.OPUS_BANDWIDTH_AUTO
    };

    public static int ApplicationToNative(Application application) => application switch
    {
        Application.Voice => OpusConstants.OPUS_APPLICATION_VOIP,
        Application.Audio => OpusConstants.OPUS_APPLICATION_AUDIO,
        Application.RestrictedLowDelay => OpusConstants.OPUS_APPLICATION_RESTRICTED_LOWDELAY,
        _ => OpusConstants.OPUS_APPLICATION_VOIP
    };

    public static int SignalToNative(int signal) => signal switch
    {
        1 => OpusConstants.OPUS_SIGNAL_VOICE,
        2 => OpusConstants.OPUS_SIGNAL_MUSIC,
        _ => OpusConstants.OPUS_SIGNAL_AUTO
    };

    #endregion
}
