using FluentAssertions;
using Xunit;

namespace OpusSharp.Tests;

public class OpusRepacketizerTests
{
    [Fact]
    public void CombineAndSplit_Frames_ShouldRoundTrip()
    {
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
        using var encoder = new OpusEncoder(config);
        using var repacketizer = new OpusRepacketizer();

        var frameSize = SampleRate.Hz48k.FrameSize(FrameDuration.Ms20);
        var toneA = TestHelpers.GenerateTestTone(frameSize, 440f, 48000);
        var toneB = TestHelpers.GenerateTestTone(frameSize, 660f, 48000);

        var packetA = encoder.Encode(toneA, frameSize);
        var packetB = encoder.Encode(toneB, frameSize);

        repacketizer.AddPacket(packetA);
        repacketizer.AddPacket(packetB);

        var combinedBuffer = new byte[packetA.Length + packetB.Length + 16];
        var combinedLength = repacketizer.WritePacket(combinedBuffer);
        combinedLength.Should().BeGreaterThan(packetA.Length);

        repacketizer.Reset();
        repacketizer.AddPacket(combinedBuffer.AsSpan(0, combinedLength));

        repacketizer.GetFrameCount().Should().Be(2);

        var splitA = new byte[packetA.Length + 16];
        var splitALength = repacketizer.WriteFrame(0, splitA);
        splitALength.Should().Be(packetA.Length);
        splitA.AsSpan(0, splitALength).SequenceEqual(packetA).Should().BeTrue();

        var splitB = new byte[packetB.Length + 16];
        var splitBLength = repacketizer.WriteFrame(1, splitB);
        splitBLength.Should().Be(packetB.Length);
        splitB.AsSpan(0, splitBLength).SequenceEqual(packetB).Should().BeTrue();
    }

    [Fact]
    public void WriteRange_WithSmallBuffer_ShouldThrow()
    {
        var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
        using var encoder = new OpusEncoder(config);
        using var repacketizer = new OpusRepacketizer();

        var frameSize = SampleRate.Hz48k.FrameSize(FrameDuration.Ms20);
        var packet = encoder.Encode(TestHelpers.GenerateTestTone(frameSize, 440f, 48000), frameSize);

        repacketizer.AddPacket(packet);

        var dest = new byte[4];
        Action act = () => repacketizer.WriteFrame(0, dest);
        act.Should().Throw<OpusException>()
            .Where(ex => ex.ErrorCode == -2); // OPUS_BUFFER_TOO_SMALL
    }
}
