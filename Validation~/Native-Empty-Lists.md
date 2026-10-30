# Native empty-list verification

The native libraries were built from `muxy/gamelink-cpp` commit `32fd80c0706709843ccd8a743e9cc73e13fd750f`. This branch starts from a 2023 native source revision selected to match the package's existing library generation; the exact original binary provenance is not recorded and includes the empty-list fix. The native source and automated tests are on the `codex/clear-empty-unity` branch.

- Mac: the universal CMake build passed the native unit suite and exercised the built library with populated and empty action and text lists. The build verifies both arm64 and x86_64 slices; the native tests ran on an arm64 GitHub-hosted Mac. Both slices export all entry points referenced by the Unity wrapper.
- Windows: the CMake build passed the native unit suite and shared-library tests. The package's P/Invoke declarations were exercised on Windows, including empty managed arrays. Unity 2022.3.62f3 and Unity 6000.0.85f1 batch-mode integration tests exercised the same empty and populated native calls.
- Linux: the CMake build passed the native unit suite and shared-library tests.
- The tests verify outgoing SDK payloads, not a live Twitch session or server-side application of the clear requests. The Intel Mac slice was checked for architecture and exports, but was not executed on an Intel Mac.

The CMake results are available in GitHub Actions run `41116080194` (Mac) and `41115387230` (Windows and Linux). Both builds use the same native source changes; later commits only changed verification workflows. The SDK package keeps the Unity plugin metadata unchanged.

