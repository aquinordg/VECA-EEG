# VECA-EEG

Open-source VR platform for multimodal cognitive assessment with integrated eye tracking and EEG synchronization. Implements the [VECA protocol](https://doi.org/10.1038/s41746-024-01206-5) in a virtual environment using gaze fixation on Areas of Interest (AOIs) as the response modality.

Available in **English** and **Portuguese (PT-BR)**.

---

## Download

Pre-built Windows releases (no Unity required):

| Version | Language | Link |
|---|---|---|
| v1.0.0 | English | [VECA-EEG_EN.zip](https://github.com/aquinordg/VECA-EEG/releases/tag/v1.0.0) |
| v1.0.0 | Português | [VECA-EEG_PTBR.zip](https://github.com/aquinordg/VECA-EEG/releases/tag/v1.0.0) |

---

## Requirements

### Pre-built releases

| Component | Requirement |
|---|---|
| OS | Windows 10/11 x64 |
| VR headset | **Required** — must support OpenXR Eye Gaze Interaction |
| OpenXR runtime | SteamVR (recommended) or Windows Mixed Reality |
| EEG amplifier | Optional — any LSL-compatible device |

> **Compatible headsets (eye tracking required):** HTC Vive Pro Eye, Varjo XR-3/4, HP Reverb G2 Omnicept, Pico 4 Enterprise, and any other headset supporting the OpenXR Eye Gaze Interaction extension.
>
> Headsets **without** eye tracking will render the VR environment but gaze-based AOI selection will not function — the assessment cannot be used clinically in this configuration.
>
> The application requires an active OpenXR runtime (SteamVR) to launch and **cannot run on a standard PC without a VR headset**.

### Software (source build)

| Component | Version |
|---|---|
| Unity | 6000.4.4f1 |
| SteamVR | latest |

> Testing without a physical headset is possible in the Unity Editor using the **XR Device Simulator** (included via XR Interaction Toolkit samples). This simulator is not available in standalone builds.

---

## Getting started (from source)

```bash
git clone https://github.com/aquinordg/VECA-EEG.git
```

1. Open **Unity Hub** and add the cloned folder as a project
2. Select Unity **6000.4.4f1** (install via Unity Hub if needed)
3. Let Unity import and compile — this may take a few minutes on first open
4. Open `Assets/Scenes/SampleScene.unity`
5. Press **Play**

All third-party free assets are included. No manual Asset Store imports required.

---

## Hardware setup

1. Start **SteamVR** and confirm the headset is detected and awake
2. Calibrate eye tracking via the headset's native calibration tool before each session
3. *(Source build only)* In Unity: **Project Settings → XR Plug-in Management → OpenXR → Features** → confirm **Eye Gaze Interaction** is enabled

---

## EEG synchronization (optional)

VECA-EEG sends LSL markers via a `"VECA-Markers"` outlet for millisecond-accurate alignment with EEG recordings.

**Pre-built release:** place `lsl.dll` (liblsl v1.17, [download here](https://github.com/sccn/liblsl/releases)) in the same folder as the `.exe`.

**Source build:** place `lsl.dll` at `Assets/Plugins/lsl.dll`.

In your EEG software (e.g., BrainVision Recorder + LiveAmp LSL Connector), subscribe to the `"VECA-Markers"` inlet. The application degrades silently if `lsl.dll` is absent — eye tracking and CSV export work without it.

**Marker format:**

| Marker | When |
|---|---|
| `session_start,<participantID>` | Session begins |
| `trial_start,<TaskName>` | Trial recording starts |
| `trial_end,<TaskName>` | Trial recording ends |
| `session_end,<participantID>` | Session ends |

Use the companion [EVA library](https://github.com/aquinordg/EVA) to preprocess EEG and segment epochs by task label.

---

## Localization

Language is selected via a `LocalizationConfig` ScriptableObject assigned to `TestManager.locConfig` in the Inspector.

| Asset | Language | Path |
|---|---|---|
| `Localization_PTBR` | Português (BR) | `Assets/_VECA-EEG/Localization/` |
| `Localization_EN` | English | `Assets/_VECA-EEG/Localization/` |

The config covers all UI strings, task names, instructions, button labels, and stimulus content. Leaving the field empty falls back to PT-BR defaults.

---

## Inspector configuration

| GameObject | Component | Key settings |
|---|---|---|
| TestManager | `TestManager` | `locConfig` → select language asset |
| EyeTracker | `EyeTracker` | `canvasRaycaster` → GraphicRaycaster of WorldCanvas; `vrCamera` → Main Camera |
| WorldCanvas | `Box Collider` | Size `(1920, 1080, 1)` — required for gaze physics raycast |
| Start / Got It / New Test buttons | `GazeDwellButton` | `eyeTracker` → EyeTracker; `dwellTime = 1.5`; `gazeColor` → highlight color when looking |
| XR Origin | `XR Origin` | Tracking Origin Mode = `Not Specified`; Camera Y Offset = `1.1176` |

Navigation buttons highlight in yellow when the user's gaze is on them and activate after 1.5 s of sustained fixation — no hand controllers required.

---

## Task sequence

| Order | Task | Features exported | Duration |
|---|---|---|---|
| 1 | Memory (3 trials) | `vr_mem8`, `vr_mem9`, `vr_mem10` | 8 s each |
| 2 | Attention | `vr_att` | 8 s |
| 3 | Abstraction (2 trials) | `vr_abs` | 8 s each |
| 4 | Calculation (3 trials) | `vr_calc4`, `vr_calc5`, `vr_calc6` | 8 s each |
| 5 | Execution | `vr_exec` | 8 s |
| 6 | Delayed Recall (3 trials) | `vr_recall` | 8 s each |

Each feature value is the proportion of fixation time on the correct AOI during the execution window (0–1).

---

## Output

Results are saved to `Results/VECA_<participantID>_<timestamp>.csv`:

```
participant_id,trial_start,trial_end,feature,value
XK4TW2,20260612_143201.123,20260612_143209.130,vr_mem8,0.8921
...
```

- `trial_start` / `trial_end`: UTC timestamps in `yyyyMMdd_HHmmss.fff` for EEG alignment
- `value`: fixation proportion (0–1)
- The `Results/` folder is excluded from git

---

## Project structure

```
Assets/_VECA-EEG/
  Localization/    Localization_PTBR.asset, Localization_EN.asset
  Scripts/
    Core/          EyeTracker.cs, TestManager.cs, LSLMarkerStream.cs,
                   LSLNative.cs, LocalizationConfig.cs
    UI/            UIManager.cs, AOI.cs, GazeDwellButton.cs
    Tasks/         TaskBase.cs, MemoryTask.cs, AttentionTask.cs,
                   AbstractionTask.cs, CalculationTask.cs,
                   ExecutionTask.cs, RecallTask.cs
  Prefabs/         AOI_Template.prefab
Assets/Scenes/     SampleScene.unity
```

---

## License

MIT License — see [LICENSE](LICENSE).

Cite the original VECA protocol:
> Goyal et al. (2024). *A virtual reality cognitive assessment*. npj Digital Medicine. https://doi.org/10.1038/s41746-024-01206-5
