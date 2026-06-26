# OpusSharp

High-performance C# bindings for the Opus audio codec with bundled native runtime libraries.

The NuGet package id is `tryAGI.OpusSharp`. The public namespace remains `OpusSharp`.

## Features

- Complete encoder and decoder API over libopus.
- PCM16 encode/decode with configurable sample rate, channel count, frame duration, application mode, bitrate, VBR, FEC, DTX, bandwidth, and complexity.
- Packet loss concealment, forward error correction, packet inspection, and repacketization helpers.
- Bundled native binaries for Windows x64, macOS universal x64/arm64, Linux x64, and Linux arm64.
- A small `opus_sharp` native shim for non-variadic encoder/decoder CTL calls.
- `net10.0` target with nullable reference types enabled.

## Installation

```bash
dotnet add package tryAGI.OpusSharp
```

## Basic Encoding

```csharp
using OpusSharp;

var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz16k);
using var encoder = new OpusEncoder(config);

var sampleRate = 16000;
var frameSize = config.SampleRate.FrameSize(config.FrameDuration);
var pcm = new short[frameSize];

for (var i = 0; i < frameSize; i++)
{
    var t = (float)i / sampleRate;
    pcm[i] = (short)(Math.Sin(2 * Math.PI * 440 * t) * short.MaxValue * 0.5f);
}

var encodedData = encoder.Encode(pcm, frameSize);
Console.WriteLine($"Encoded {frameSize} samples to {encodedData.Length} bytes");
```

## Basic Decoding

```csharp
using OpusSharp;

var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz16k);
using var decoder = new OpusDecoder(config);

var decodedPcm = decoder.Decode(encodedData, config.FrameDuration);
Console.WriteLine($"Decoded {encodedData.Length} bytes to {decodedPcm.Length} samples");
```

## Custom Configuration

```csharp
var config = new OpusConfig
{
    SampleRate = SampleRate.Hz48k,
    Channels = Channels.Stereo,
    FrameDuration = FrameDuration.Ms10,
    Application = Application.Audio,
    Complexity = 8,
    Bitrate = 128000,
    Fec = true,
};

using var encoder = new OpusEncoder(config);
using var decoder = new OpusDecoder(config);
```

## Encoder Presets

```csharp
using var voiceEncoder = new OpusEncoder(OpusConfig.VoiceMediumQualityMono(SampleRate.Hz16k));
using var musicEncoder = new OpusEncoder(OpusConfig.MusicStereo(SampleRate.Hz48k));
using var lowBandwidthEncoder = new OpusEncoder(OpusConfig.VoiceLowQualityMono(SampleRate.Hz16k));
```

## Advanced Features

### Forward Error Correction

```csharp
var config = OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k);
config.Fec = true;

using var encoder = new OpusEncoder(config);
using var decoder = new OpusDecoder(config);

var decodedWithFEC = decoder.DecodeWithFEC(currentPacket, useFEC: true, config.FrameDuration);
```

### Packet Loss Concealment

```csharp
var concealedAudio = decoder.GeneratePLC(FrameDuration.Ms20);
```

### Runtime Encoder Controls

```csharp
using var encoder = new OpusEncoder(OpusConfig.VoiceMediumQualityMono(SampleRate.Hz48k));

encoder.SetBitrate(24000);
encoder.SetComplexity(8);
encoder.SetVbr(enabled: true, constrained: false);
encoder.SetFec(enabled: true);
```

## Native Libraries

The package contains native runtime assets under NuGet `runtimes/<rid>/native/`:

- `runtimes/win-x64/native/opus.dll`
- `runtimes/win-x64/native/libopus-0.dll`
- `runtimes/win-x64/native/opus_sharp.dll`
- `runtimes/osx-x64/native/libopus.dylib`
- `runtimes/osx-x64/native/libopus_sharp.dylib`
- `runtimes/osx-arm64/native/libopus.dylib`
- `runtimes/osx-arm64/native/libopus_sharp.dylib`
- `runtimes/linux-x64/native/libopus.so`
- `runtimes/linux-x64/native/libopus.so.0`
- `runtimes/linux-x64/native/libopus_sharp.so`
- `runtimes/linux-arm64/native/libopus.so`
- `runtimes/linux-arm64/native/libopus.so.0`
- `runtimes/linux-arm64/native/libopus_sharp.so`

The native binaries are committed in `natives/` so consumers do not need a system libopus installation.

## Rebuilding Native Binaries

Vendored Opus 1.5.2 source lives in `third_party/opus/opus-1.5.2/`, with the .NET CTL shim in `third_party/opus/shim/opus_shim.c`.

Run the rebuild script from the repository root:

```bash
third_party/opus/build-opussharp.sh
```

The script writes refreshed binaries into `natives/`. macOS builds require Xcode command line tools. Linux builds use Docker. Windows x64 builds require `x86_64-w64-mingw32-gcc`.

## Build And Test

```bash
dotnet build OpusSharp.slnx
dotnet test src/tests/OpusSharp.Tests/OpusSharp.Tests.csproj
dotnet pack src/libs/OpusSharp/OpusSharp.csproj -c Release
```

## License

OpusSharp is licensed under MIT. Vendored Opus source and derived native binaries retain the Opus license; see `THIRD-PARTY-NOTICES.md`.
