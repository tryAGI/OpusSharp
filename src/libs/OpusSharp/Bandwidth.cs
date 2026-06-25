namespace OpusSharp;

/// <summary>
/// Opus bandwidth settings
/// </summary>
public enum Bandwidth : int
{
    /// <summary>4 kHz bandpass (narrowband)</summary>
    Narrowband = 1101,

    /// <summary>6 kHz bandpass (mediumband)</summary>
    Mediumband = 1102,

    /// <summary>8 kHz bandpass (wideband)</summary>
    Wideband = 1103,

    /// <summary>12 kHz bandpass (super-wideband)</summary>
    SuperWideband = 1104,

    /// <summary>20 kHz bandpass (fullband)</summary>
    Fullband = 1105
}
