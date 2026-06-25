using FluentAssertions;
using Xunit;

namespace OpusSharp.Tests;

public class OpusEncoderTests
{
    [Fact]
    public void Constructor_WithValidConfig_ShouldSucceed()
    {
        // Arrange
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);

        // Act & Assert
        var encoder = new OpusEncoder(config);
        encoder.Config.Should().Be(config);
        encoder.Dispose();
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OpusEncoder(null!));
    }

    [Fact]
    public void Voice_WithDefaultParameters_ShouldCreateVoiceEncoder()
    {
        // Act
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
        Action act = () =>
        {
            _ = new OpusEncoder(config);
        };

        // Assert - This will fail until we implement it
        act.Should().NotThrow();
    }

    [Fact]
    public void UltraLowBandwidth_WithDefaultParameters_ShouldCreateUltraLowEncoder()
    {
        // Act
        var config = OpusConfig.VoiceLowQualityMono(SampleRate.Hz16k);
        Action act = () =>
        {
            _ = new OpusEncoder(config);
        };

        // Assert - This will fail until we implement it
        act.Should().NotThrow();
    }

    [Fact]
    public void UltraLowBandwidth_WithCustomBitrate_ShouldCreateUltraLowEncoder()
    {
        // Act
        var config = OpusConfig.VoiceLowQualityMono(SampleRate.Hz16k);
        config.Fec = false;
        config.Bitrate = 8000;
        Action act = () =>
        {
            _ = new OpusEncoder(config);
        };

        // Assert - This will fail until we implement it
        act.Should().NotThrow();
    }

    [Fact]
    public void Music_WithDefaultParameters_ShouldCreateMusicEncoder()
    {
        // Act
        var config = OpusConfig.MusicStereo(SampleRate.Hz48k);
        Action act = () =>
        {
            _ = new OpusEncoder(config);
        };

        // Assert - This will fail until we implement it
        act.Should().NotThrow();
    }

    [Fact]
    public void Encode_WithValidPcm_ShouldReturnEncodedData()
    {
        // Arrange
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz16k);
        var encoder = new OpusEncoder(config);
        var frameSize = config.SampleRate.FrameSize(FrameDuration.Ms20);
        var pcm = TestHelpers.GenerateTestTone(frameSize, 440.0f, 16000);

        // Act
        var encodedData = encoder.Encode(pcm, frameSize);

        // Assert
        encodedData.Should().NotBeNull();
        encodedData.Should().NotBeEmpty();
        encodedData.Length.Should().BeGreaterThan(0);

        encoder.Dispose();
    }

    [Fact]
    public void Encode_WithOutputBuffer_ShouldReturnBytesWritten()
    {
        // Arrange
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz16k);
        var encoder = new OpusEncoder(config);
        var frameSize = config.SampleRate.FrameSize(FrameDuration.Ms20);
        var pcm = TestHelpers.GenerateTestTone(frameSize, 440.0f, 16000);
        var output = new byte[1500];

        // Act
        var bytesWritten = encoder.Encode(pcm, frameSize, output);

        // Assert
        bytesWritten.Should().BeGreaterThan(0);
        bytesWritten.Should().BeLessThanOrEqualTo(1500);

        encoder.Dispose();
    }

    [Fact]
    public void SetBitrate_WithValidBitrate_ShouldNotThrow()
    {
        // Arrange
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
        var encoder = new OpusEncoder(config);

        // Act & Assert
        var act = () => encoder.SetBitrate(32000);
        act.Should().NotThrow();

        encoder.Dispose();
    }

    [Fact]
    public void SetBandwidth_WithValidBandwidth_ShouldNotThrow()
    {
        // Arrange
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
        var encoder = new OpusEncoder(config);

        // Act & Assert
        var act = () => encoder.SetBandwidth(Bandwidth.Narrowband);
        act.Should().NotThrow();

        encoder.Dispose();
    }

    [Fact]
    public void SetVBR_WithValidParameters_ShouldNotThrow()
    {
        // Arrange
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
        var encoder = new OpusEncoder(config);

        // Act & Assert
        var act = () => encoder.SetVbr(true, constrained: false);
        act.Should().NotThrow();

        encoder.Dispose();
    }

    [Fact]
    public void SetDTX_WithValidParameter_ShouldNotThrow()
    {
        // Arrange
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
        var encoder = new OpusEncoder(config);

        // Act & Assert
        var act = () => encoder.SetDtx(true);
        act.Should().NotThrow();

        encoder.Dispose();
    }

    [Fact]
    public void SetFEC_WithValidParameter_ShouldNotThrow()
    {
        // Arrange
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
        var encoder = new OpusEncoder(config);

        // Act & Assert
        var act = () => encoder.SetFec(true);
        act.Should().NotThrow();

        encoder.Dispose();
    }

    [Fact]
    public void SetComplexity_WithValidComplexity_ShouldNotThrow()
    {
        // Arrange
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
        var encoder = new OpusEncoder(config);

        // Act & Assert
        var act = () => encoder.SetComplexity(5);
        act.Should().NotThrow();

        encoder.Dispose();
    }

    [Fact]
    public void Reset_ShouldNotThrow()
    {
        // Arrange
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
        var encoder = new OpusEncoder(config);

        // Act & Assert
        var act = () => encoder.Reset();
        act.Should().NotThrow();

        encoder.Dispose();
    }

    [Theory]
    [InlineData(SampleRate.Hz8k, FrameDuration.Ms20)]
    [InlineData(SampleRate.Hz16k, FrameDuration.Ms20)]
    [InlineData(SampleRate.Hz48k, FrameDuration.Ms40)]
    public void Encode_WithDifferentSampleRatesAndDurations_ShouldWork(SampleRate sampleRate, FrameDuration duration)
    {
        // Arrange
        var config = new OpusConfig
        {
            SampleRate = sampleRate,
            Channels = Channels.Mono,
            FrameDuration = duration
        };
        var encoder = new OpusEncoder(config);
        var frameSize = sampleRate.FrameSize(duration);
        var pcm = TestHelpers.GenerateTestTone(frameSize, 440.0f, (int)sampleRate);

        // Act
        var encodedData = encoder.Encode(pcm, frameSize);

        // Assert
        encodedData.Should().NotBeNull();
        encodedData.Should().NotBeEmpty();
        encodedData.Length.Should().BeGreaterThan(0);
        encoder.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Encode_WithVoiceHighDtxAndSilence_ShouldEmitDecodableCompactPackets(bool longFrames)
    {
        var config = longFrames
            ? OpusConfig.VoiceHighQualityLongMono(SampleRate.Hz24k)
            : OpusConfig.VoiceHighQualityMono(SampleRate.Hz24k);
        config.Dtx = true;

        using var encoder = new OpusEncoder(config);
        using var decoder = new OpusDecoder(config);

        var frameDuration = config.FrameDuration;
        var frameSize = config.SampleRate.FrameSize(frameDuration);
        var silentPcm = TestHelpers.GenerateSilence(frameSize);
        var tonePcm = TestHelpers.GenerateTestTone(frameSize, 440.0f, (int)config.SampleRate);

        var silentPacket = encoder.Encode(silentPcm, frameSize);
        var tonePacket = encoder.Encode(tonePcm, frameSize);

        silentPacket.Should().NotBeEmpty("DTX should emit a comfort-noise packet instead of a zero-byte payload");
        tonePacket.Should().NotBeEmpty();
        silentPacket.Length.Should().BeLessThan(tonePacket.Length);

        var decodedSilent = decoder.Decode(silentPacket, frameDuration);
        decodedSilent.Should().HaveCount(frameSize);
        decodedSilent.Select(sample => Math.Abs((int)sample)).Max().Should().BeLessThanOrEqualTo(8);
    }
}
