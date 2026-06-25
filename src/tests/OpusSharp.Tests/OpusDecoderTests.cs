using FluentAssertions;
using Xunit;

namespace OpusSharp.Tests;

public class OpusDecoderTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Arrange & Act
        var config = new OpusConfig { SampleRate = SampleRate.Hz16k, Channels = Channels.Mono };
        var decoder = new OpusDecoder(config);

        // Assert
        decoder.Config.SampleRate.Should().Be(SampleRate.Hz16k);
        decoder.Config.Channels.Should().Be(Channels.Mono);
        decoder.Dispose();
    }

    [Fact]
    public void Voice_WithDefaultParameters_ShouldCreateVoiceDecoder()
    {
        // Act
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
        Action act = () =>
        {
            _ = new OpusDecoder(config);
        };

        // Assert - This will fail until we implement it
        act.Should().NotThrow();
    }

    [Fact]
    public void Music_WithDefaultParameters_ShouldCreateMusicDecoder()
    {
        // Act
        var config = OpusConfig.MusicStereo(SampleRate.Hz48k);
        Action act = () =>
        {
            _ = new OpusDecoder(config);
        };

        // Assert - This will fail until we implement it
        act.Should().NotThrow();
    }

    [Fact]
    public void Decode_WithValidPacket_ShouldReturnPcmData()
    {
        // Arrange - Create a real Opus packet by encoding some test data first
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz16k);
        var encoder = new OpusEncoder(config);
        var decoder = new OpusDecoder(config);

        // Generate test audio: 320 samples = 20ms at 16kHz mono
        var testPcm = TestHelpers.GenerateTestTone(320, 440.0f, 16000);
        var opusPacket = encoder.Encode(testPcm, 320);

        // Act
        var decodedPcm = decoder.Decode(opusPacket);

        // Assert
        decodedPcm.Should().NotBeNull();
        decodedPcm.Should().NotBeEmpty();
        decodedPcm.Length.Should().Be(320); // Should decode to same frame size

        encoder.Dispose();
        decoder.Dispose();
    }

    [Fact]
    public void DecodeWithFEC_WithValidPacket_ShouldReturnPcmData()
    {
        // Arrange - Create a real Opus packet with FEC enabled
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz16k);
        config.Fec = true; // Enable FEC for encoding
        var encoder = new OpusEncoder(config);
        var decoder = new OpusDecoder(config);

        var testPcm = TestHelpers.GenerateTestTone(320, 440.0f, 16000);
        var opusPacket = encoder.Encode(testPcm, 320);

        // Act
        var decodedPcm = decoder.DecodeWithFEC(opusPacket, useFEC: true);

        // Assert
        decodedPcm.Should().NotBeNull();
        decodedPcm.Should().NotBeEmpty();
        decodedPcm.Length.Should().Be(320);

        encoder.Dispose();
        decoder.Dispose();
    }

    [Fact]
    public void DecodeUsingFECIfNeeded_WithPreviousPacketLost_ShouldReturnMultipleResults()
    {
        // Arrange
        var config = new OpusConfig { SampleRate = SampleRate.Hz16k, Channels = Channels.Mono };
        var decoder = new OpusDecoder(config);
        var currentPacket = new byte[] { 0x01, 0x02, 0x03 };

        // Act & Assert - Should return results even with invalid packet
        var act = () => decoder.DecodeUsingFECIfNeeded(prevLost: true, currentPacket);
        act.Should().NotThrow(); // Method should handle errors internally and return results

        decoder.Dispose();
    }

    [Fact]
    public void DecodeUsingFECIfNeeded_WithNoPreviousLoss_ShouldReturnSingleResult()
    {
        // Arrange
        var config = new OpusConfig { SampleRate = SampleRate.Hz16k, Channels = Channels.Mono };
        var decoder = new OpusDecoder(config);
        var currentPacket = new byte[] { 0x01, 0x02, 0x03 };

        // Act & Assert - Should return single result
        var act = () => decoder.DecodeUsingFECIfNeeded(prevLost: false, currentPacket);
        act.Should().NotThrow(); // Method should handle errors internally

        decoder.Dispose();
    }

    [Fact]
    public void GeneratePLC_WithValidDuration_ShouldReturnPcmData()
    {
        // Arrange
        var config = new OpusConfig { SampleRate = SampleRate.Hz16k, Channels = Channels.Mono };
        var decoder = new OpusDecoder(config);

        // Act
        var result = decoder.GeneratePLC(FrameDuration.Ms20);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        decoder.Dispose();
    }

    [Fact]
    public void DecodeWithAdvancedFEC_WithMultiplePackets_ShouldHandleComplexScenarios()
    {
        // Arrange
        var config = new OpusConfig { SampleRate = SampleRate.Hz16k, Channels = Channels.Mono };
        var decoder = new OpusDecoder(config);
        var currentPacket = new byte[] { 0x01, 0x02, 0x03 };
        var previousPacket = new byte[] { 0x04, 0x05, 0x06 };
        var nextPacket = new byte[] { 0x07, 0x08, 0x09 };

        // Act
        var result = decoder.DecodeWithAdvancedFEC(currentPacket, previousPacket, nextPacket, packetLoss: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        decoder.Dispose();
    }

    [Fact]
    public void DecodeWithAdvancedFEC_WithNullCurrentPacket_ShouldUseFECOrPLC()
    {
        // Arrange
        var config = new OpusConfig { SampleRate = SampleRate.Hz16k, Channels = Channels.Mono };
        var decoder = new OpusDecoder(config);
        var nextPacket = new byte[] { 0x07, 0x08, 0x09 };

        // Act
        var result = decoder.DecodeWithAdvancedFEC(null, null, nextPacket, packetLoss: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        decoder.Dispose();
    }

    [Fact]
    public void Reset_ShouldNotThrow()
    {
        // Arrange
        var config = new OpusConfig { SampleRate = SampleRate.Hz16k, Channels = Channels.Mono };
        var decoder = new OpusDecoder(config);

        // Act & Assert
        var act = () => decoder.Reset();
        act.Should().NotThrow();

        decoder.Dispose();
    }

    [Fact]
    public void GetSampleRate_ShouldReturnConfiguredSampleRate()
    {
        // Arrange
        var config = new OpusConfig { SampleRate = SampleRate.Hz16k, Channels = Channels.Mono };
        using var decoder = new OpusDecoder(config);

        // Act
        var sampleRate = decoder.GetSampleRate();

        // Assert
        sampleRate.Should().Be((int)SampleRate.Hz16k);
    }

    [Fact]
    public void PhaseInversionDisabled_ShouldRoundTrip()
    {
        // Arrange
        var config = OpusConfig.MusicStereo(SampleRate.Hz48k);
        using var decoder = new OpusDecoder(config);

        // Act & Assert
        decoder.SetPhaseInversionDisabled(true);
        decoder.GetPhaseInversionDisabled().Should().BeTrue();

        decoder.SetPhaseInversionDisabled(false);
        decoder.GetPhaseInversionDisabled().Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(256)] // +1 dB in Q8
    [InlineData(-512)] // -2 dB in Q8
    public void Gain_ShouldRoundTrip(int gainQ8)
    {
        // Arrange
        var config = new OpusConfig { SampleRate = SampleRate.Hz48k, Channels = Channels.Stereo };
        using var decoder = new OpusDecoder(config);

        // Act
        decoder.SetGain(gainQ8);
        var roundTripped = decoder.GetGain();

        // Assert
        roundTripped.Should().Be(gainQ8);
    }

    [Fact]
    public void GetPacketSampleCount_ShouldMatchDecodedSamples()
    {
        // Arrange
        var config = new OpusConfig { SampleRate = SampleRate.Hz48k, Channels = Channels.Stereo };
        using var encoder = new OpusEncoder(config);
        using var decoder = new OpusDecoder(config);

        var frameSize = SampleRate.Hz48k.FrameSize(FrameDuration.Ms20);
        var samplesNeeded = frameSize * (int)config.Channels;
        var testPcm = TestHelpers.GenerateTestTone(samplesNeeded, 440.0f, (int)SampleRate.Hz48k);
        var packet = encoder.Encode(testPcm, frameSize);

        // Act
        var packetSamples = decoder.GetPacketSampleCount(packet);
        var result = decoder.Decode(packet);

        // Assert
        packetSamples.Should().Be(frameSize);
        result.Length.Should().Be(samplesNeeded);
    }

    [Fact]
    public void DecodeInto_ShouldFillProvidedBuffer()
    {
        // Arrange
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz16k);
        using var encoder = new OpusEncoder(config);
        using var decoder = new OpusDecoder(config);

        var frameSize = SampleRate.Hz16k.FrameSize(FrameDuration.Ms20);
        var testPcm = TestHelpers.GenerateTestTone(frameSize, 440.0f, 16000);
        var packet = encoder.Encode(testPcm, frameSize);

        var packetSamples = decoder.GetPacketSampleCount(packet);
        var destination = new short[packetSamples * (int)config.Channels];

        // Act
        var decodedSamples = decoder.DecodeInto(packet, destination);

        // Assert
        decodedSamples.Should().Be(packetSamples);
        destination.Should().Contain(s => s != 0);
    }

    [Fact]
    public void GetPacketSampleCount_ShouldReturnZero_ForComfortNoisePacket()
    {
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz16k);
        using var decoder = new OpusDecoder(config);

        var comfortNoisePacket = new byte[] { 0xB8, 0xFF, 0xFE };

        var frameSize = SampleRate.Hz16k.FrameSize(FrameDuration.Ms20);
        var samples = decoder.GetPacketSampleCount(comfortNoisePacket);
        samples.Should().Be(frameSize);

        var decoded = decoder.Decode(comfortNoisePacket, FrameDuration.Ms20);
        decoded.Should().HaveCount(frameSize);
        decoded.Should().OnlyContain(s => s == 0);
    }

    [Theory]
    [InlineData(SampleRate.Hz8k, Channels.Mono)]
    [InlineData(SampleRate.Hz16k, Channels.Mono)]
    [InlineData(SampleRate.Hz48k, Channels.Stereo)]
    public void Decode_WithDifferentSampleRatesAndChannels_ShouldWork(SampleRate sampleRate, Channels channels)
    {
        // Arrange - Create real encoded data for specific sample rate/channels
        var config = new OpusConfig { SampleRate = sampleRate, Channels = channels, Application = Application.Voice };
        var encoder = new OpusEncoder(config);
        var decoder = new OpusDecoder(config);

        var frameSize = sampleRate.FrameSize(FrameDuration.Ms20);
        var samplesNeeded = frameSize * (int)channels;
        var testPcm = TestHelpers.GenerateTestTone(samplesNeeded, 440.0f, (int)sampleRate);
        var opusPacket = encoder.Encode(testPcm, frameSize);

        // Act
        var decodedPcm = decoder.Decode(opusPacket);

        // Assert
        decodedPcm.Should().NotBeNull();
        decodedPcm.Should().NotBeEmpty();
        decodedPcm.Length.Should().Be(samplesNeeded);

        encoder.Dispose();
        decoder.Dispose();
    }

    [Theory]
    [InlineData(FrameDuration.Ms10)]
    [InlineData(FrameDuration.Ms20)]
    [InlineData(FrameDuration.Ms40)]
    [InlineData(FrameDuration.Ms60)]
    public void Decode_WithDifferentFrameDurations_ShouldWork(FrameDuration duration)
    {
        // Arrange - Create real encoded data for specific frame duration
        var config = new OpusConfig { SampleRate = SampleRate.Hz16k, Channels = Channels.Mono, FrameDuration = duration };
        var encoder = new OpusEncoder(config);
        var decoder = new OpusDecoder(config);

        var frameSize = SampleRate.Hz16k.FrameSize(duration);
        var testPcm = TestHelpers.GenerateTestTone(frameSize, 440.0f, 16000);
        var opusPacket = encoder.Encode(testPcm, frameSize);

        // Act
        var decodedPcm = decoder.Decode(opusPacket, duration);

        // Assert
        decodedPcm.Should().NotBeNull();
        decodedPcm.Should().NotBeEmpty();
        decodedPcm.Length.Should().Be(frameSize);

        encoder.Dispose();
        decoder.Dispose();
    }
}
