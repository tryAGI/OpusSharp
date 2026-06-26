using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Xunit;

namespace OpusSharp.Tests;

public class PackageNativeAssetsTests
{
    [Fact]
    public void WindowsShimDependency_ShouldHavePackagedLibopusAlias()
    {
        var repoRoot = FindRepoRoot();
        var windowsNativeDir = Path.Combine(repoRoot, "natives", "windows");
        var opusDll = Path.Combine(windowsNativeDir, "opus.dll");
        var libopusAliasDll = Path.Combine(windowsNativeDir, "libopus-0.dll");
        var shimDll = Path.Combine(windowsNativeDir, "opus_sharp.dll");

        File.Exists(opusDll).Should().BeTrue();
        File.Exists(libopusAliasDll).Should().BeTrue();
        File.Exists(shimDll).Should().BeTrue();

        ContainsAscii(File.ReadAllBytes(shimDll), "libopus-0.dll").Should().BeTrue(
            "the Windows shim imports the MinGW libopus DLL by its native image name");
        SHA256.HashData(File.ReadAllBytes(libopusAliasDll)).Should().Equal(
            SHA256.HashData(File.ReadAllBytes(opusDll)),
            "the package ships libopus under both names so direct DllImport and shim dependency loading both work");

        File.ReadAllText(Path.Combine(repoRoot, "src", "libs", "OpusSharp", "OpusSharp.csproj"))
            .Should().Contain("runtimes/win-x64/native/libopus-0.dll");
    }

    [Fact]
    public void LinuxShimDependency_ShouldHavePackagedSonameAliasAndPublishRootProbe()
    {
        var repoRoot = FindRepoRoot();
        var linuxNativeDir = Path.Combine(repoRoot, "natives", "linux");
        var x64Opus = Path.Combine(linuxNativeDir, "libopus.so");
        var x64OpusSonameAlias = Path.Combine(linuxNativeDir, "libopus.so.0");
        var arm64Opus = Path.Combine(linuxNativeDir, "libopus-arm64.so");
        var arm64OpusSonameAlias = Path.Combine(linuxNativeDir, "libopus-arm64.so.0");
        var x64Shim = Path.Combine(linuxNativeDir, "libopus_sharp.so");
        var arm64Shim = Path.Combine(linuxNativeDir, "libopus_sharp-arm64.so");

        File.Exists(x64Opus).Should().BeTrue();
        File.Exists(x64OpusSonameAlias).Should().BeTrue();
        File.Exists(arm64Opus).Should().BeTrue();
        File.Exists(arm64OpusSonameAlias).Should().BeTrue();
        File.Exists(x64Shim).Should().BeTrue();
        File.Exists(arm64Shim).Should().BeTrue();

        ContainsAscii(File.ReadAllBytes(x64Opus), "libopus.so.0").Should().BeTrue(
            "the Linux x64 Opus binary advertises libopus.so.0 as its SONAME");
        ContainsAscii(File.ReadAllBytes(arm64Opus), "libopus.so.0").Should().BeTrue(
            "the Linux arm64 Opus binary advertises libopus.so.0 as its SONAME");
        ContainsAscii(File.ReadAllBytes(x64Shim), "libopus.so.0").Should().BeTrue(
            "the Linux x64 shim imports Opus by SONAME");
        ContainsAscii(File.ReadAllBytes(arm64Shim), "libopus.so.0").Should().BeTrue(
            "the Linux arm64 shim imports Opus by SONAME");
        SHA256.HashData(File.ReadAllBytes(x64OpusSonameAlias)).Should().Equal(
            SHA256.HashData(File.ReadAllBytes(x64Opus)),
            "the package ships Linux x64 libopus under its file name and SONAME");
        SHA256.HashData(File.ReadAllBytes(arm64OpusSonameAlias)).Should().Equal(
            SHA256.HashData(File.ReadAllBytes(arm64Opus)),
            "the package ships Linux arm64 libopus under its file name and SONAME");

        var projectFile = File.ReadAllText(Path.Combine(repoRoot, "src", "libs", "OpusSharp", "OpusSharp.csproj"));
        projectFile.Should().Contain("runtimes/linux-x64/native/libopus.so.0");
        projectFile.Should().Contain("runtimes/linux-arm64/native/libopus.so.0");

        var loaderFile = File.ReadAllText(Path.Combine(repoRoot, "src", "libs", "OpusSharp", "Native", "NativeLibraryLoader.cs"));
        loaderFile.Should().Contain("""Path.Combine(assemblyDir, "libopus_sharp.so")""");
        loaderFile.Should().Contain("""Path.Combine(assemblyDir, "libopus.so.0")""");
    }

    private static bool ContainsAscii(byte[] haystack, string needle)
    {
        var needleBytes = Encoding.ASCII.GetBytes(needle);
        return haystack.AsSpan().IndexOf(needleBytes) >= 0;
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "OpusSharp.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate OpusSharp repository root.");
    }
}
