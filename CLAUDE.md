# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

VECA-EEG is a Unity VR eye-tracking research application. It presents stimuli in a virtual environment and records gaze metrics (fixation time, fixation count, first fixation time) on named Areas of Interest (AOIs) for cognitive/EEG research experiments.

**Unity version:** 6000.4.4f1 — use only APIs available in this version.

## Development Environment

This is a Unity project. There are no CLI build commands — all development happens through the Unity Editor:

- **Open project:** Launch Unity Hub, open this folder with Unity 6000.4.4f1
- **Run:** Press Play in the Unity Editor
- **Build:** File > Build Settings > Build (target: Windows Standalone x86_64)
- **Tests:** Window > General > Test Runner (uses `com.unity.test-framework 1.6.0`)
- **Edit C#:** Open `VECA-EEG.sln` in Visual Studio or Rider

The XR Device Simulator (included via XR Interaction Toolkit samples) allows testing VR interactions without physical hardware.

The repository is **clone-ready**: all third-party free assets are included. No manual Asset Store imports required.

## Architecture

### Script Organization (`Assets/_VECA-EEG/Scripts/`)

| Folder | Scripts |
|--------|---------|
| `Core/` | `EyeTracker.cs`, `TestManager.cs`, `LSLMarkerStream.cs`, `LSLNative.cs` |
| `UI/` | `AOI.cs`, `UIManager.cs`, `GazeCursor.cs`, `GazeDwellButton.cs` |
| `Tasks/` | `TaskBase.cs`, `MemoryTask.cs`, `AttentionTask.cs`, `AbstractionTask.cs`, `CalculationTask.cs`, `ExecutionTask.cs`, `RecallTask.cs` |
| `Calibration/` | *(reserved for future calibration procedures)* |

### AOI System (`Scripts/UI/AOI.cs`)

The central data type. Each AOI is a UI Image+Button+TextMeshPro combo that represents one selectable region in the experiment. It owns its own fixation metrics:
- `totalFixationTime`, `fixationCount`, `firstFixationTime` — populated by `EyeTracker`
- `isCorrectAnswer` — set by the task controller to indicate the expected response
- Visual states: default gray → highlighted yellow → correct green / incorrect red

AOIs are instantiated from `Assets/_VECA-EEG/Prefabs/AOI_Template.prefab`.

### Eye Tracking (`Scripts/Core/EyeTracker.cs`)

Uses OpenXR Eye Gaze Interaction (`<EyeGaze>/pose`). Fixation is detected via angular distance between frames (≤ `limiarAngularGraus`, default 1.5°) held for ≥ `duracaoMinimaFixacao` (0.12 s). Includes fallback for Vive Pro Eye driver quirk where `isTracked` is always false — falls back to checking `trackingState` Position|Rotation flags. Public API: `TryGetGazeRay()`, `ObterPosicaoGaze()`, `StartRecording()`, `StopRecording()`, `CurrentTrialLabel`, `RecordingStartTime`, `RecordingEndTime`.

### EEG Synchronization (`Scripts/Core/LSLMarkerStream.cs` + `LSLNative.cs`)

`LSLMarkerStream` is a singleton that opens a Lab Streaming Layer outlet named `"VECA-Markers"`. It degrades silently if `lsl.dll` is absent. `TestManager` sends `session_start/<session_end>,<participantID>` markers; `EyeTracker` sends `trial_start/<trial_end>,<CurrentTrialLabel>` at recording boundaries. The `lsl.dll` (liblsl v1.17) must be placed at `Assets/Plugins/lsl.dll` — it is **not** included in the repository.

### Task Sequence

`TestManager` orchestrates the full experiment: Memória → Atenção → Abstração → Cálculo (3 trials) → Execução → Recall. Results are exported to `Results/VECA_<participantID>_<timestamp>.csv` with 10 features and per-trial EEG timestamps.

### Rendering & XR

- **URP** (Universal Render Pipeline 17.4.0) — renderer configs live in `Assets/Settings/`
- **OpenXR** (`com.unity.xr.openxr 1.16.1`) with XR Management — loader settings in `Assets/XR/`
- **XR Interaction Toolkit** 3.4.1 — handles controller/hand interactions; samples are in `Assets/Samples/`
- Primary scene: `Assets/Scenes/SampleScene.unity`

### Third-Party Assets (included)

- `Assets/Furniture_ges1/` — room furniture (free)
- `Assets/Customizable Lights and Candles/` — lighting props (free)
- `Assets/Free Wood Door Pack/` — only Door_2/Yellow variant included; other 14 door variants are excluded via `.gitignore` to keep repo size manageable

### Input

Uses the new Unity Input System (`com.unity.inputsystem 1.19.0`). Input bindings are defined in `Assets/InputSystem_Actions.inputactions`, not in legacy Input Manager.

## Key Conventions

- **Language:** Code comments and `[Header(...)]` labels are written in Portuguese (Brazilian).
- **AOI IDs:** AOIs are identified by string `aoiID` — the task controller uses this to correlate gaze data with stimuli.
- **Data reset:** Call `AOI.ResetData()` between trials to clear fixation metrics before the next stimulus presentation.
- **Cursor visibility:** `GazeCursor` uses `cursorRenderer.enabled` to show/hide — never `SetActive(false)`, which would break re-activation.
- **`aoiCorreta`:** Not reset in `StartRecording()`. Must be set via `SetCurrentCorrectAOI()` before or after starting recording.
