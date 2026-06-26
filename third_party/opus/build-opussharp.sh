#!/usr/bin/env bash
# Build dynamic Opus libraries for .NET OpusSharp usage across platforms
set -euo pipefail

# Script location (expected at repo-root/third_party/opus/build-opussharp.sh)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# third_party/opus directory (this folder)
TP_OPUS_DIR="$SCRIPT_DIR"

# Repo root (two levels up from third_party/opus)
REPO_ROOT="$(cd "$TP_OPUS_DIR/../.." && pwd)"

# Find the vendored "opus-*" source dir inside third_party/opus (for example opus-1.5.2).
OPUS_SRC_DIR=""
for candidate in "$TP_OPUS_DIR"/opus-*; do
  [[ -d "$candidate" ]] || continue
  OPUS_SRC_DIR="$candidate"
done
if [[ -z "${OPUS_SRC_DIR}" ]]; then
  echo "❌ Could not find sources like third_party/opus/opus-*/"
  exit 1
fi

# Build/output dirs for OpusSharp
BUILD_DIR="$TP_OPUS_DIR/build-opussharp"
DIST_DIR="$TP_OPUS_DIR/dist-opussharp"

# OpusSharp natives directory
OPUSSHARP_NATIVES="$REPO_ROOT/natives"

# Minimum OS versions
MAC_MIN="${MAC_MIN:-12.0}"

echo "▶ Building Opus dynamic libraries for .NET OpusSharp"
echo "▶ Using Opus sources: $OPUS_SRC_DIR"
echo "▶ Output to: $OPUSSHARP_NATIVES"

rm -rf "$BUILD_DIR" "$DIST_DIR"
mkdir -p "$BUILD_DIR" "$DIST_DIR"

# Ensure source tree is clean (in case configure was run in-tree previously)
if [[ -f "$OPUS_SRC_DIR/config.status" || -f "$OPUS_SRC_DIR/Makefile" ]]; then
  echo "▶ Cleaning source tree (distclean)..."
  (cd "$OPUS_SRC_DIR" && make distclean) >/dev/null 2>&1 || true
fi

build_shared_one () {
  local PLATFORM="$1"      # Platform identifier
  local ARCH="$2"          # Architecture
  local MIN_FLAG="$3"      # Minimum version flag
  local HOST="$4"          # Host triplet
  local OUT_NAME="$5"      # Output directory name
  local LIB_NAME="$6"      # Library filename

  echo "▶ Building $OUT_NAME ($ARCH)..."

  local SDK; SDK="$(xcrun --sdk "$PLATFORM" --show-sdk-path 2>/dev/null || echo "")"
  local CC;  CC="$(xcrun --sdk "$PLATFORM" -f clang 2>/dev/null || echo "clang")"

  local BDIR="$BUILD_DIR/$OUT_NAME"
  rm -rf "$BDIR"; mkdir -p "$BDIR"
  pushd "$BDIR" >/dev/null

  export CC="$CC"
  if [[ -n "$SDK" ]]; then
    export CFLAGS="-arch $ARCH $MIN_FLAG -isysroot $SDK -O3 -DNDEBUG -fPIC"
    export LDFLAGS="-arch $ARCH -isysroot $SDK -Wl,-headerpad_max_install_names"
  else
    export CFLAGS="-O3 -DNDEBUG -fPIC"
    export LDFLAGS="-Wl,-headerpad_max_install_names"
  fi

  # Configure for shared lib
  "$OPUS_SRC_DIR/configure" \
    --host="$HOST" \
    --enable-shared \
    --disable-static \
    --disable-asm \
    --disable-doc \
    --disable-extra-programs

  make -j"$(sysctl -n hw.ncpu 2>/dev/null || nproc 2>/dev/null || echo 4)"
  mkdir -p "$DIST_DIR/$OUT_NAME"
  cp "./.libs/$LIB_NAME" "$DIST_DIR/$OUT_NAME/"

  popd >/dev/null
}

# Merge function for universal binaries
merge_universal() {
  local out="$1"; shift
  local inputs=("$@")
  mkdir -p "$(dirname "$out")"
  # Keep only inputs that actually exist
  local existing=()
  for f in "${inputs[@]}"; do
    [[ -f "$f" ]] && existing+=("$f")
  done
  if [[ ${#existing[@]} -eq 0 ]]; then
    echo "❌ merge_universal: no inputs for $out" >&2
    exit 1
  elif [[ ${#existing[@]} -eq 1 ]]; then
    cp "${existing[0]}" "$out"
  else
    lipo -create "${existing[@]}" -output "$out"
  fi
}

# Build for macOS (always available on macOS)
echo "▶ Building for macOS..."

# Build for both architectures
build_shared_one macosx arm64 "-mmacosx-version-min=$MAC_MIN" aarch64-apple-darwin macos-arm64 libopus.dylib
build_shared_one macosx x86_64 "-mmacosx-version-min=$MAC_MIN" x86_64-apple-darwin macos-x64 libopus.dylib

# Create universal dynamic library
merge_universal "$DIST_DIR/macos-universal/libopus.dylib" \
  "$DIST_DIR/macos-arm64/libopus.dylib" \
  "$DIST_DIR/macos-x64/libopus.dylib"

# Ensure the install name is relative so downstream bundles resolve via @rpath.
install_name_tool -id @rpath/libopus.dylib "$DIST_DIR/macos-universal/libopus.dylib"

# Copy to OpusSharp natives
mkdir -p "$OPUSSHARP_NATIVES/macos"
cp "$DIST_DIR/macos-universal/libopus.dylib" "$OPUSSHARP_NATIVES/macos/"

echo "✅ macOS libraries created in $OPUSSHARP_NATIVES/macos/"

# Build macOS shim (non-variadic wrapper) for both arch and merge
echo "▶ Building macOS shim library..."
SHIM_SRC_REPO="$TP_OPUS_DIR/shim/opus_shim.c"
if [[ ! -f "$SHIM_SRC_REPO" ]]; then
  echo "❌ Shim source not found at $SHIM_SRC_REPO"
  exit 1
fi
mkdir -p "$DIST_DIR/macos-arm64" "$DIST_DIR/macos-x64" "$DIST_DIR/macos-universal"

clang -arch arm64 -dynamiclib "$SHIM_SRC_REPO" \
  -I "$OPUS_SRC_DIR/include" \
  -L "$DIST_DIR/macos-arm64" -lopus \
  -o "$DIST_DIR/macos-arm64/libopus_sharp.dylib" \
  -install_name @rpath/libopus_sharp.dylib \
  -Wl,-rpath,@loader_path \
  -Wl,-headerpad_max_install_names

clang -arch x86_64 -dynamiclib "$SHIM_SRC_REPO" \
  -I "$OPUS_SRC_DIR/include" \
  -L "$DIST_DIR/macos-x64" -lopus \
  -o "$DIST_DIR/macos-x64/libopus_sharp.dylib" \
  -install_name @rpath/libopus_sharp.dylib \
  -Wl,-rpath,@loader_path \
  -Wl,-headerpad_max_install_names

# Ensure the shim links against the bundled libopus using a relative path.
install_name_tool -change /usr/local/lib/libopus.0.dylib @loader_path/libopus.dylib "$DIST_DIR/macos-arm64/libopus_sharp.dylib"
install_name_tool -change /usr/local/lib/libopus.0.dylib @loader_path/libopus.dylib "$DIST_DIR/macos-x64/libopus_sharp.dylib"

merge_universal "$DIST_DIR/macos-universal/libopus_sharp.dylib" \
  "$DIST_DIR/macos-arm64/libopus_sharp.dylib" \
  "$DIST_DIR/macos-x64/libopus_sharp.dylib"

# Ensure the merged shim keeps the relative dependency as well.
install_name_tool -change /usr/local/lib/libopus.0.dylib @loader_path/libopus.dylib "$DIST_DIR/macos-universal/libopus_sharp.dylib"

cp "$DIST_DIR/macos-universal/libopus_sharp.dylib" "$OPUSSHARP_NATIVES/macos/"
echo "✅ macOS shim created in $OPUSSHARP_NATIVES/macos/libopus_sharp.dylib"

# Build for Linux using Docker
if command -v docker >/dev/null 2>&1; then
  echo "▶ Building for Linux using Docker..."
  
  # Build Linux x64 (amd64)
  echo "  ▶ Building Linux x64..."
  if docker run --rm --platform linux/amd64 \
    -v "$TP_OPUS_DIR:/workspace" -w /workspace \
    ubuntu:22.04 bash -c "
      apt-get update -qq && apt-get install -y -qq build-essential autoconf automake libtool pkg-config &&
      cd opus-*/  &&
      make clean || true &&
      ./configure --enable-shared --disable-static --disable-asm --disable-doc --disable-extra-programs &&
      make -j\$(nproc) &&
      mkdir -p /workspace/dist-opussharp/linux-x64 &&
      cp .libs/libopus.so /workspace/dist-opussharp/linux-x64/ &&
      cp /workspace/dist-opussharp/linux-x64/libopus.so /workspace/dist-opussharp/linux-x64/libopus.so.0 &&
      gcc -shared -fPIC /workspace/shim/opus_shim.c -I /workspace/opus-*/include -L ./.libs -lopus -o /workspace/dist-opussharp/linux-x64/libopus_sharp.so &&
      echo 'Linux x64 build complete'
    "; then
    echo "    ✅ Linux x64 build successful"
  else
    echo "    ❌ Linux x64 build failed"
  fi
  
  # Build Linux ARM64
  echo "  ▶ Building Linux ARM64..."
  if docker run --rm --platform linux/arm64 \
    -v "$TP_OPUS_DIR:/workspace" -w /workspace \
    ubuntu:22.04 bash -c "
      apt-get update -qq && apt-get install -y -qq build-essential autoconf automake libtool pkg-config &&
      cd opus-*/ &&
      make clean || true &&
      ./configure --enable-shared --disable-static --disable-asm --disable-doc --disable-extra-programs &&
      make -j\$(nproc) &&
      mkdir -p /workspace/dist-opussharp/linux-arm64 &&
      cp .libs/libopus.so /workspace/dist-opussharp/linux-arm64/ &&
      cp /workspace/dist-opussharp/linux-arm64/libopus.so /workspace/dist-opussharp/linux-arm64/libopus.so.0 &&
      gcc -shared -fPIC /workspace/shim/opus_shim.c -I /workspace/opus-*/include -L ./.libs -lopus -o /workspace/dist-opussharp/linux-arm64/libopus_sharp.so &&
      echo 'Linux ARM64 build complete'
    "; then
    echo "    ✅ Linux ARM64 build successful"
  else
    echo "    ❌ Linux ARM64 build failed"
  fi
  
  # Copy to OpusSharp natives
  mkdir -p "$OPUSSHARP_NATIVES/linux"
  if [[ -f "$DIST_DIR/linux-x64/libopus.so" ]]; then
    cp "$DIST_DIR/linux-x64/libopus.so" "$OPUSSHARP_NATIVES/linux/"
    cp "$DIST_DIR/linux-x64/libopus.so.0" "$OPUSSHARP_NATIVES/linux/" 2>/dev/null || true
    echo "    ✅ Copied Linux x64 library"
  fi
  if [[ -f "$DIST_DIR/linux-arm64/libopus.so" ]]; then
    cp "$DIST_DIR/linux-arm64/libopus.so" "$OPUSSHARP_NATIVES/linux/libopus-arm64.so"
    cp "$DIST_DIR/linux-arm64/libopus.so.0" "$OPUSSHARP_NATIVES/linux/libopus-arm64.so.0" 2>/dev/null || true
    echo "    ✅ Copied Linux ARM64 library"
  fi
  # Copy shims
  if [[ -f "$DIST_DIR/linux-x64/libopus_sharp.so" ]]; then
    cp "$DIST_DIR/linux-x64/libopus_sharp.so" "$OPUSSHARP_NATIVES/linux/"
    echo "    ✅ Copied Linux x64 shim"
  fi
  if [[ -f "$DIST_DIR/linux-arm64/libopus_sharp.so" ]]; then
    cp "$DIST_DIR/linux-arm64/libopus_sharp.so" "$OPUSSHARP_NATIVES/linux/libopus_sharp-arm64.so"
    echo "    ✅ Copied Linux ARM64 shim"
  fi
  
  echo "✅ Linux libraries created in $OPUSSHARP_NATIVES/linux/"
else
  echo "⚠️  Docker not available for Linux cross-compilation"
  echo "    Install Docker Desktop to enable Linux builds"
fi

# Try to build for Windows (if cross-compilation tools available)
if command -v x86_64-w64-mingw32-gcc >/dev/null 2>&1; then
  echo "▶ Building for Windows..."
  # Clean source again before Windows build
  if [[ -f "$OPUS_SRC_DIR/config.status" || -f "$OPUS_SRC_DIR/Makefile" ]]; then
    (cd "$OPUS_SRC_DIR" && make distclean) >/dev/null 2>&1 || true
  fi
  
  build_windows() {
    local ARCH="$1"
    local HOST="$2"
    local CC_NAME="$3"
    local OUT_NAME="$4"
    
    echo "  ▶ Building Windows $ARCH..."
    
    local BDIR="$BUILD_DIR/windows-$OUT_NAME"
    rm -rf "$BDIR"; mkdir -p "$BDIR"
    pushd "$BDIR" >/dev/null

    export CC="$CC_NAME"
    export CFLAGS="-O3 -DNDEBUG"
    export LDFLAGS=""

    "$OPUS_SRC_DIR/configure" \
      --host="$HOST" \
      --enable-shared \
      --disable-static \
      --disable-asm \
      --disable-doc \
      --disable-extra-programs

    make -j"$(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 4)"
    mkdir -p "$DIST_DIR/windows-$OUT_NAME"
    cp ./.libs/libopus-0.dll "$DIST_DIR/windows-$OUT_NAME/libopus-0.dll" 2>/dev/null || \
    cp ./.libs/libopus.dll "$DIST_DIR/windows-$OUT_NAME/libopus-0.dll" 2>/dev/null || \
    cp ./.libs/opus.dll "$DIST_DIR/windows-$OUT_NAME/libopus-0.dll" 2>/dev/null || true
    cp "$DIST_DIR/windows-$OUT_NAME/libopus-0.dll" "$DIST_DIR/windows-$OUT_NAME/opus.dll" 2>/dev/null || true

    popd >/dev/null
  }
  
  # Build for Windows x64
  build_windows x64 x86_64-w64-mingw32 x86_64-w64-mingw32-gcc x64

  # Build Windows shim (requires headers and import lib in ./.libs)
  echo "  ▶ Building Windows x64 shim..."
  pushd "$BUILD_DIR/windows-x64" >/dev/null || true
  if [[ -d ./.libs ]]; then
    "$OPUS_SRC_DIR/configure" >/dev/null 2>&1 || true
    x86_64-w64-mingw32-gcc -shared -O3 -DNDEBUG \
      "$SHIM_SRC_REPO" -I "$OPUS_SRC_DIR/include" -L ./.libs -lopus \
      -o "$DIST_DIR/windows-x64/opus_sharp.dll" || true
    echo "    ✅ Windows x64 shim build attempted"
  else
    echo "    ⚠️  Skipping shim: ./.libs not found"
  fi
  popd >/dev/null || true

  # Copy to OpusSharp natives
  mkdir -p "$OPUSSHARP_NATIVES/windows"
  cp "$DIST_DIR/windows-x64/opus.dll" "$OPUSSHARP_NATIVES/windows/" 2>/dev/null || true
  cp "$DIST_DIR/windows-x64/libopus-0.dll" "$OPUSSHARP_NATIVES/windows/" 2>/dev/null || true
  cp "$DIST_DIR/windows-x64/opus_sharp.dll" "$OPUSSHARP_NATIVES/windows/" 2>/dev/null || true
  
  echo "✅ Windows libraries created in $OPUSSHARP_NATIVES/windows/"
else
  echo "⚠️  Windows cross-compilation tools not available (x86_64-w64-mingw32-gcc)"
  echo "    Install with: brew install mingw-w64"
fi

# Print summary
echo "▶ OpusSharp Dynamic Libraries Summary:"
for dir in "$OPUSSHARP_NATIVES"/*; do
  if [[ -d "$dir" ]]; then
    platform="$(basename "$dir")"
    echo " - $platform:"
    for lib in "$dir"/*opus*; do
      if [[ -f "$lib" ]]; then
        echo "   - $(basename "$lib")"
        if [[ "$platform" == "macos" ]]; then
          lipo -info "$lib" 2>/dev/null || true
        elif [[ "$platform" == "linux" ]]; then
          file "$lib" 2>/dev/null || true
        elif [[ "$platform" == "windows" ]]; then
          file "$lib" 2>/dev/null || true
        fi
      fi
    done
  fi
done

echo "✅ OpusSharp dynamic libraries build complete!"
