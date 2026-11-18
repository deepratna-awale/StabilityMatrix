# PR 1803 headless screenshot harness (throwaway, never commit to a PR branch)

1. Copy `ScratchScreenshotTests.cs` into `StabilityMatrix.UITests/`. No TestAppBuilder changes are needed.
2. Build a library: `python3 make_samples.py /tmp/shots/lib`, or point `SM_SHOTS_LIB` at a library folder with real models under `Models/`.
3. Run:
   ```
   SM_SHOTS_DIR=/tmp/shots/out SM_SHOTS_LIB=/tmp/shots/lib \
   dotnet test StabilityMatrix.UITests/StabilityMatrix.UITests.csproj --filter "FullyQualifiedName~ScratchScreenshotTests"
   ```
   Optional: `SM_SHOTS_NAME`, `SM_SHOTS_META_NAME` (output file names), `SM_SHOTS_META_FILE` (which .safetensors to show in View Metadata), `SM_SHOTS_META_TITLE`.
4. For the "before" shots, run the same test in a worktree of upstream `main`.

Notes:
- `RenderTargetBitmap` renders the model cards wrong in headless mode. Use `window.CaptureRenderedFrame()`, as the test does.
- `CheckpointsPageResizeFactor` defaults to 0 in a fresh settings file, which makes the cards render empty. The test sets it to 1.
- Running UITests rewrites `StabilityMatrix.UITests/Snapshots/*.received.*`. Run `git checkout -- StabilityMatrix.UITests/Snapshots` before committing anything.
- The cards use the library's real model files, so the window size (1720x720) fits 4 cards. Adjust it for more.
