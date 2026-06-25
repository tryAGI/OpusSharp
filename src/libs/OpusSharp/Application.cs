namespace OpusSharp;

/// <summary>
/// Opus application type for encoder optimization
/// </summary>
public enum Application : int
{
    /// <summary>Optimize for voice/speech (VOIP)</summary>
    Voice = 2048,

    /// <summary>Optimize for music/general audio</summary>
    Audio = 2049,

    /// <summary>Optimize for low delay communication</summary>
    RestrictedLowDelay = 2051
}
