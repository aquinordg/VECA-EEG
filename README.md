# VECA-EEG

VR cognitive assessment application with eye tracking, designed for EEG research.  
Replicates the [VECA protocol](https://doi.org/10.1038/s41746-024-01206-5) in a virtual environment using gaze fixation on Areas of Interest (AOIs) as the response modality.

---

## Requirements

| Component | Version |
|---|---|
| Unity | 6000.4.4f1 |
| HTC Vive Pro Eye | — |
| SteamVR | latest |
| SRanipal SDK | installed and running |

> The project uses **OpenXR Eye Gaze Interaction** (`<EyeGaze>/pose`) via SRanipal as the OpenXR runtime backend.  
> Testing without a headset is possible using the **XR Device Simulator** (included via XR Interaction Toolkit samples).

---

## Getting started

```bash
git clone https://github.com/aquinordg/VECA-EEG.git
```

1. Open **Unity Hub** and add the cloned folder as a project
2. Select Unity version **6000.4.4f1** (install it via Unity Hub if needed)
3. Let Unity import and compile — this may take a few minutes on the first open
4. Open `Assets/Scenes/SampleScene.unity`
5. Press **Play**

---

## Hardware setup (Vive Pro Eye)

1. Start **SteamVR** and make sure the headset is awake
2. Confirm **SRanipal** is running in the Windows system tray
3. Calibrate eye tracking via the SteamVR Dashboard before each session
4. In Unity: **Project Settings → XR Plug-in Management → OpenXR → Features** — enable **Eye Gaze Interaction**

---

## Inspector configuration

| GameObject | Component | Key settings |
|---|---|---|
| EyeTracker | `EyeTracker` | `canvasRaycaster` → GraphicRaycaster of WorldCanvas; `vrCamera` → Main Camera |
| GazeCursor | `GazeCursor` | `eyeTracker` → EyeTracker; `defaultDistance = 2` |
| WorldCanvas | `Box Collider` | Size `(1920, 1080, 1)` — required for GazeCursor physics raycast |
| Start / Got It / New Assessment buttons | `GazeDwellButton` | `eyeTracker` → EyeTracker; `dwellTime = 1.5` |
| XR Origin | `XR Origin` | Tracking Origin Mode = `Not Specified`; Camera Y Offset = `1.1176` |

---

## Task sequence

| Order | Task | Features exported |
|---|---|---|
| 1 | Memory (3 trials) | `vr_mem8`, `vr_mem9`, `vr_mem10` |
| 2 | Attention | `vr_att` |
| 3 | Abstraction | `vr_abs` |
| 4 | Calculation (3 trials) | `vr_calc4`, `vr_calc5`, `vr_calc6` |
| 5 | Execution | `vr_exec` |
| 6 | Recall | `vr_recall` |

Each feature is the proportion of fixation time on the correct AOI during the execution window (0–1).

---

## Output

Results are saved to `Results/VECA_<participantID>_<timestamp>.csv`:

```
participant_id,trial_start,trial_end,feature,value
XK4TW2,20260612_143201.123,20260612_143209.130,vr_mem8,0.8921
...
```

- `trial_start` / `trial_end`: timestamps in `yyyyMMdd_HHmmss.fff` format for EEG synchronization
- `value`: fixation proportion (0–1)
- Results folder is excluded from git (`.gitignore`)

---

## Project structure

```
Assets/_VECA-EEG/
  Scripts/
    Core/        EyeTracker.cs, TestManager.cs
    UI/          UIManager.cs, AOI.cs, GazeCursor.cs, GazeDwellButton.cs
    Tasks/       TaskBase.cs, MemoryTask.cs, AttentionTask.cs,
                 AbstractionTask.cs, CalculationTask.cs,
                 ExecutionTask.cs, RecallTask.cs
  Prefabs/       AOI_Template.prefab
Assets/Scenes/   SampleScene.unity
```

---

## License

For academic use. Cite the original VECA protocol:  
> Goyal et al. (2024). *A virtual reality cognitive assessment*. npj Digital Medicine. https://doi.org/10.1038/s41746-024-01206-5
