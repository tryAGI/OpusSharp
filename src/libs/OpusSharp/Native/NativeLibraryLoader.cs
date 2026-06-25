using System.Reflection;
using System.Runtime.InteropServices;

namespace OpusSharp.Native;

/// <summary>
/// Handles loading of native Opus libraries across platforms
/// </summary>
internal static class NativeLibraryLoader
{
    private static readonly object _lock = new object();
    private static bool _initialized = false;

    /// <summary>
    /// Initialize native library loading
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
            return;

        lock (_lock)
        {
            if (_initialized)
                return;

            try
            {
                // Try to resolve the library name at runtime
                NativeLibrary.SetDllImportResolver(typeof(OpusNative).Assembly, ResolveDllImport);
                _initialized = true;
            }
            catch (Exception ex)
            {
                throw new OpusException(-1, $"Failed to initialize native library loader: {ex.Message}");
            }
        }
    }

    private static IntPtr ResolveDllImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // We resolve both libopus and our shim library here
        var isOpus = libraryName == "opus";
        var isShim = libraryName == "opus_sharp";
        if (!isOpus && !isShim)
            return IntPtr.Zero;

        // Try different library names and paths based on platform
        var libraryPaths = GetLibraryPaths(isShim);

        foreach (var path in libraryPaths)
        {
            try
            {
                if (NativeLibrary.TryLoad(path, out var handle))
                {
                    // Verify this is the expected library by checking for a known export
                    var expectedExport = isShim ? "opussharp_encoder_set_int" : "opus_get_version_string";
                    if (NativeLibrary.TryGetExport(handle, expectedExport, out _))
                    {
                        return handle;
                    }

                    // If the export isn't found, this is likely a stub/incorrect dylib; free and continue

                    try
                    { NativeLibrary.Free(handle); }
                    catch { /* ignore */ }
                }
            }
            catch
            {
                // Continue to next path
            }
        }

        throw new DllNotFoundException($"Could not load Opus native library. Tried paths: {string.Join(", ", libraryPaths)}");
    }

    private static string[] GetLibraryPaths(bool forShim)
    {
        var paths = new List<string>();
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (forShim)
            {
                paths.AddRange(new[]
                {
                    Path.Combine(assemblyDir, "runtimes", "win-x64", "native", "opus_sharp.dll"),
                    "opus_sharp.dll",
                    "opus_sharp"
                });
            }
            else
            {
                paths.AddRange(new[]
                {
                    Path.Combine(assemblyDir, "opus.dll"),
                    Path.Combine(assemblyDir, "libopus.dll"),
                    Path.Combine(assemblyDir, "runtimes", "win-x64", "native", "opus.dll"),
                    Path.Combine(assemblyDir, "runtimes", "win-x64", "native", "libopus.dll"),
                    "opus.dll",
                    "libopus.dll"
                });
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Prefer packaged runtimes and Homebrew canonical locations before any loose files
            if (forShim)
            {
                paths.AddRange(new[]
                {
                    Path.Combine(assemblyDir, "runtimes", "osx", "native", "libopus_sharp.dylib"),
                    Path.Combine(assemblyDir, "runtimes", "osx-x64", "native", "libopus_sharp.dylib"),
                    Path.Combine(assemblyDir, "runtimes", "osx-arm64", "native", "libopus_sharp.dylib"),
                    Path.Combine(assemblyDir, "libopus_sharp.dylib"),
                    "libopus_sharp.dylib",
                    "opus_sharp"
                });
            }
            else
            {
                paths.AddRange(new[]
                {
                    Path.Combine(assemblyDir, "runtimes", "osx", "native", "libopus.dylib"),
                    Path.Combine(assemblyDir, "runtimes", "osx-x64", "native", "libopus.dylib"),
                    Path.Combine(assemblyDir, "runtimes", "osx-arm64", "native", "libopus.dylib"),
                    "/opt/homebrew/opt/opus/lib/libopus.dylib", // Homebrew formula canonical path
                    "/opt/homebrew/lib/libopus.dylib",
                    "/usr/local/lib/libopus.dylib",
                    "libopus.dylib",
                    Path.Combine(assemblyDir, "libopus.dylib"),
                    "libopus"
                });
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (forShim)
            {
                paths.AddRange(new[]
                {
                    Path.Combine(assemblyDir, "runtimes", "linux-x64", "native", "libopus_sharp.so"),
                    Path.Combine(assemblyDir, "runtimes", "linux-arm64", "native", "libopus_sharp.so"),
                    "libopus_sharp.so",
                    "opus_sharp"
                });
            }
            else
            {
                paths.AddRange(new[]
                {
                    Path.Combine(assemblyDir, "libopus.so"),
                    Path.Combine(assemblyDir, "runtimes", "linux-x64", "native", "libopus.so"),
                    Path.Combine(assemblyDir, "runtimes", "linux-arm64", "native", "libopus.so"),
                    // Common multi-arch locations (Debian/Ubuntu)
                    "/usr/lib/x86_64-linux-gnu/libopus.so.0",
                    "/usr/lib/aarch64-linux-gnu/libopus.so.0",
                    "/lib/x86_64-linux-gnu/libopus.so.0",
                    "/lib/aarch64-linux-gnu/libopus.so.0",
                    "/usr/lib/libopus.so.0",
                    "/usr/local/lib/libopus.so",
                    "libopus.so.0",
                    "libopus.so",
                    "libopus"
                });
            }
        }

        return paths.ToArray();
    }
}
