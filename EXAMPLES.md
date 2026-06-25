# OpusSharp Examples

## Voice Round Trip

```csharp
using OpusSharp;

var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz16k);
using var encoder = new OpusEncoder(config);
using var decoder = new OpusDecoder(config);

var frameSize = config.SampleRate.FrameSize(config.FrameDuration);
var pcm = new short[frameSize];

for (var i = 0; i < pcm.Length; i++)
{
    var t = i / (double)config.SampleRate;
    pcm[i] = (short)(Math.Sin(2 * Math.PI * 440 * t) * short.MaxValue * 0.25);
}

var packet = encoder.Encode(pcm, frameSize);
var decoded = decoder.Decode(packet, config.FrameDuration);

Console.WriteLine($"PCM samples: {pcm.Length}");
Console.WriteLine($"Opus bytes: {packet.Length}");
Console.WriteLine($"Decoded samples: {decoded.Length}");
```

## Music Preset

```csharp
using OpusSharp;

var config = OpusConfig.MusicStereo(SampleRate.Hz48k);
using var encoder = new OpusEncoder(config);
using var decoder = new OpusDecoder(config);

var frameSize = config.SampleRate.FrameSize(config.FrameDuration);
var stereoSamples = new short[frameSize * (int)config.Channels];

for (var i = 0; i < frameSize; i++)
{
    var t = i / (double)config.SampleRate;
    stereoSamples[i * 2] = (short)(Math.Sin(2 * Math.PI * 440 * t) * short.MaxValue * 0.25);
    stereoSamples[i * 2 + 1] = (short)(Math.Sin(2 * Math.PI * 554 * t) * short.MaxValue * 0.25);
}

var packet = encoder.Encode(stereoSamples, frameSize);
var decoded = decoder.Decode(packet, config.FrameDuration);
```

## Runtime Encoder Controls

```csharp
using OpusSharp;

var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
using var encoder = new OpusEncoder(config);

encoder.SetBitrate(24000);
encoder.SetComplexity(8);
encoder.SetBandwidth(Bandwidth.Wideband);
encoder.SetVbr(enabled: true, constrained: false);
encoder.SetFec(enabled: true);
encoder.SetDtx(enabled: true);
```

## Packet Loss Concealment

```csharp
using OpusSharp;

var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz16k);
using var decoder = new OpusDecoder(config);

var replacementAudio = decoder.GeneratePLC(config.FrameDuration);
```

## Forward Error Correction

```csharp
using OpusSharp;

var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
config.Fec = true;

using var encoder = new OpusEncoder(config);
using var decoder = new OpusDecoder(config);

var frameSize = config.SampleRate.FrameSize(config.FrameDuration);
var pcm = new short[frameSize];
var currentPacket = encoder.Encode(pcm, frameSize);

var recoveredPreviousFrame = decoder.DecodeWithFEC(currentPacket, useFEC: true, config.FrameDuration);
```

