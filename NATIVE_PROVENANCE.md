# Native provenance

`NATIVE_PROVENANCE.json` records SHA-256 and size for all 11 checked-in
native binaries, their configured NuGet paths, declared upstream components,
source revisions/archive hashes where available, and hashes of build inputs.
The baseline repository commit is the revision inspected before this manifest
was introduced. No native files were rebuilt or replaced during recording.

## Verify locally

From the tryAGI workspace:

```sh
python3 scripts/verify-native-provenance.py OpusSharp
python3 scripts/verify-native-provenance.py --require-complete OpusSharp
```

The first command checks integrity and inventory, and prints historical build
attestation gaps. The second fails on those gaps as well. A successful integrity
check does not prove that a binary was built from the declared upstream sources.

## Evidence and limitations

All 474 tracked files in the vendored Opus 1.5.2 tree match the downloaded Xiph release archive byte for byte. Its archive SHA-256 is recorded. The first-party shim and build script are included in the source-input hashes. Historical per-RID compiler/container identities and source-to-binary attestations are absent.

## Updating the baseline

Review upstream source/revision, license, download checksums, build environment,
patches and each output hash when rebuilding. Preserve the evidence alongside
the changed manifest. Do not regenerate hashes merely to silence a mismatch.
Unresolved source/build evidence must remain visible; never relabel it verified
without an actual attestation.
