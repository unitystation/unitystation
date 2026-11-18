# Code scanning

The scanner is maintained in [unitystation/codescan](https://github.com/unitystation/codescan).
This project keeps the Unity tests and the sandbox policy in the root `CodeScanList.json`.

Run `Tests.ScanCode.BuildTest` to compile player assemblies, then `Tests.ScanCode.ScanCodeReport`
to scan them. When running the fixture together, the tests run in that order.
The security test compiles release player code without `UNITY_EDITOR` and stages the DLLs and
their precompiled dependencies in the layout expected by CodeScan. It skips scenes, assets,
shaders, build callbacks, and executable packaging; it does not validate a full playable build.
System dependencies use the editor installation's platform-specific Mono runtime assemblies.
Compiler-only framework facades are omitted, so reference metadata is not scanned as player code.
Compiler output is retained in `Library/CodeScan/PlayerAssemblies/<platform>` for incremental
compilation, while the staged scan input is refreshed each run.
Before each scan, the runner checks the latest stable GitHub release and downloads the matching
`win-x64.zip`, `linux-x64.zip`, or `osx-x64.zip` asset when it is missing or has changed.
These releases include the .NET runtime; a separate .NET installation is not required.

The extracted tool is cached in `UnityProject/Library/CodeScan/<runtime>` between runs.
Updates are extracted and validated before replacing the cached installation. If GitHub is
unavailable or an update fails, the runner logs a warning and uses the existing installation.
The first scan needs network access and fails if no usable release can be downloaded.
Delete `UnityProject/Library/CodeScan` to clear the cache.

The runner reads a fresh `errors.json` beside the executable on each scan and preserves whitelist
suggestions at `UnityProject/Library/CodeScan/CodeScanList.corrected.json`.

The editor's good-file build commands and `Tools/python prepare good files/PrepareGoodFile.py`
read `FilesToMoveToManaged.json` from the standalone repository and require network access.
