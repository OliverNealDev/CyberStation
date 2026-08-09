# Shipping Cyber Station to itch.io as WebGL

Everything here is specific to itch.io's hosting, which is not a server you can
configure. The settings below are the ones that survive that constraint.

## Building

From the editor: **Build > WebGL (itch.io)**, which writes to `Builds/WebGL`.

From the command line:

```bash
Unity.exe -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod WebGLBuilder.BuildFromCommandLine -outputPath Builds/WebGL
```

Both go through `Assets/Editor/WebGLBuilder.cs`, so the scene list and target
cannot drift between a local build and a scripted one.

## Player settings that matter, and why

| Setting | Value | Reason |
|---|---|---|
| Compression Format | **Brotli** | itch.io reads the `.br` extension and sets `Content-Encoding: br` itself, so the browser decompresses natively. |
| Decompression Fallback | **Off** | On, Unity renames the build files to `.unityweb`. itch can no longer tell what they are, so a JavaScript decompressor does the work instead, the loader grows, and WebAssembly streaming compilation is lost entirely. |
| WebGL Template | **ItchIO** (`Assets/WebGLTemplates/ItchIO`) | Canvas is sized in percentages, so it fills whatever iframe itch gives it and follows itch's own fullscreen button instead of fighting it. |
| Enable Native C/C++ Multithreading | **Off** | Threads need `SharedArrayBuffer`, which needs COOP/COEP response headers. itch.io does not send them. |
| Static / Dynamic Batching | **On** | Draw calls are unusually expensive on WebGL. |
| Quality level for WebGL | **Mobile** | Already the per-platform default, and the right one: it points at `Mobile_RPAsset`. |

## itch.io page settings

- Kind of project: **HTML**
- Upload a zip of the **contents** of `Builds/WebGL`, so `index.html` sits at the
  root of the zip rather than inside a folder.
- Viewport: **1280 x 720**
- **Click to launch in fullscreen**: leave the fullscreen button enabled. The
  template's percentage-based canvas resizes with it.
- **SharedArrayBuffer support**: off, matching the threads setting above.
- Leave "automatically start on page load" **off**. The click that starts the game
  is also the user gesture browsers require before audio may play.

## Things the web build does differently

`DisplayModeController` does not touch `Screen.SetResolution` on the web. Calling it
would stamp a fixed pixel size over the canvas the page laid out, which is what
makes a game render at the wrong size inside an itch embed. F11 is left to the
browser, and fullscreen goes through `Screen.fullScreen`.

`SaveManager` calls into `Assets/Plugins/WebGL/FileSystemSync.jslib` after every
write and delete. `Application.persistentDataPath` is backed by IndexedDB on the
web, but a `File.WriteAllText` only reaches the in-memory layer; without an
explicit `FS.syncfs` the save is silently lost when the tab closes.

The debug hotkeys (`M` money, `P` passenger, `N` tier) are behind `#if UNITY_EDITOR`
and are not in any player build. `Y` to hide the UI is a real feature and ships.

## Still worth doing

Roughly in order of payoff. None of these are applied yet.

1. **Trim `Assets/TextMesh Pro/Examples & Extras`.** It sits in a `Resources`
   folder, so all 6.6 MB of it ships whether or not anything references it. Only
   `Roboto-Bold SDF.asset` is actually used, by the main scene and 16 prefabs. Move
   that one file to `Assets/TextMesh Pro/Resources/Fonts & Materials/` from inside
   the editor so the GUID and every reference follow it, then delete the rest.
2. **Managed Stripping Level to High** for WebGL. Test saving, loading and the
   build menu afterwards: the game leans on `Resources.LoadAll` and `JsonUtility`,
   both of which are reflection-driven and are what aggressive stripping breaks.
3. **Target WebAssembly 2023** in Player Settings. Enables SIMD and native
   exception handling, which makes exception support close to free.
4. **Raise Initial Memory Size** from 32 MB once you have measured the real heap.
   Every geometric growth step copies the entire heap, and those copies are visible
   as hitches during the opening minute.
