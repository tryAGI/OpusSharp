namespace OpusSharp.Tests;

/// <summary>
/// Helper methods for tests
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Generate a test tone for testing purposes
    /// </summary>
    public static short[] GenerateTestTone(int samples, float frequency, int sampleRate)
    {
        var tone = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            var t = (float)i / sampleRate;
            var sample = Math.Sin(2 * Math.PI * frequency * t) * 0.5; // 50% amplitude
            tone[i] = (short)(sample * short.MaxValue);
        }
        return tone;
    }

    /// <summary>
    /// Generate silence for testing
    /// </summary>
    public static short[] GenerateSilence(int samples)
    {
        return new short[samples]; // All zeros
    }

    /// <summary>
    /// Generate white noise for testing
    /// </summary>
    public static short[] GenerateWhiteNoise(int samples, int seed = 42)
    {
        var random = new Random(seed);
        var noise = new short[samples];
        for (int i = 0; i < samples; i++)
        {
            noise[i] = (short)(random.Next(short.MinValue, short.MaxValue + 1));
        }
        return noise;
    }
}
