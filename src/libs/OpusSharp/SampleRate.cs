namespace OpusSharp;

/// <summary>
/// Supported Opus sample rates
/// </summary>
public enum SampleRate
{
    Hz8k = 8000,
    Hz12k = 12000,
    Hz16k = 16000,
    Hz24k = 24000,
    Hz48k = 48000
}

public static class SampleRateExtensions
{
    public static int FrameSize(this SampleRate sampleRate, FrameDuration duration)
    {
        return (int)sampleRate * (int)duration / 1000;
    }
}
