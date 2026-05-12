# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

VECA-EEG is a Unity VR eye-tracking research application. It presents stimuli in a virtual environment and records gaze metrics (fixation time, fixation count, first fixation time) on named Areas of Interest (AOIs) — likely for cognitive/EEG research experiments.

**Unity version:** 6000.4.4f1 — use only APIs available in this version.

## Development Environment

This is a Unity project. There are no CLI build commands — all development happens through the Unity Editor:

- **Open project:** Launch Unity Hub, open this folder with Unity 6000.4.4f1
- **Run:** Press Play in the Unity Editor
- **Build:** File > Build Settings > Build (target: Windows Standalone)
- **Tests:** Window > General > Test Runner (uses `com.unity.test-framework 1.6.0`)
- **Edit C#:** Open `VECA-EEG.sln` in Visual Studio or Rider

The XR Device Simulator (included via XR Interaction Toolkit samples) allows testing VR interactions without physical hardware.

## Architecture

### Script Organization (`Assets/_VECA-EEG/Scripts/`)

| Folder | Responsibility |
|--------|---------------|
| `UI/` | AOI components, visual feedback, UI event handling |
| `Core/` | Eye-tracking data management, scene flow control *(to be built)* |
| `Calibration/` | Eye-tracker calibration procedures *(to be built)* |
| `Tasks/` | Experimental task implementations *(to be built)* |

### AOI System (`Scripts/UI/AOI.cs`)

The central data type. Each AOI is a UI Image+Button+TextMeshPro combo that represents one selectable region in the experiment. It owns its own fixation metrics:
- `totalFixationTime`, `fixationCount`, `firstFixationTime` — populated by an eye-tracking manager (not yet implemented)
- `isCorrectAnswer` — set by the task controller to indicate the expected response
- Visual states: default gray → highlighted yellow → correct green / incorrect red

AOIs are instantiated from `Assets/_VECA-EEG/Prefabs/AOI_Template.prefab`.

### Rendering & XR

- **URP** (Universal Render Pipeline 17.4.0) — renderer configs live in `Assets/Settings/`
- **OpenXR** (`com.unity.xr.openxr 1.16.1`) with XR Management — loader settings in `Assets/XR/`
- **XR Interaction Toolkit** 3.4.1 — handles controller/hand interactions; samples are in `Assets/Samples/`
- Primary scene: `Assets/Scenes/SampleScene.unity`

### Input

Uses the new Unity Input System (`com.unity.inputsystem 1.19.0`). Input bindings are defined in `Assets/InputSystem_Actions.inputactions`, not in legacy Input Manager.

## Key Conventions

- **Language:** Code comments and `[Header(...)]` labels are written in Portuguese (Brazilian).
- **AOI IDs:** AOIs are identified by string `aoiID` — the task controller uses this to correlate gaze data with stimuli.
- **Data reset:** Call `AOI.ResetData()` between trials to clear fixation metrics before the next stimulus presentation.
