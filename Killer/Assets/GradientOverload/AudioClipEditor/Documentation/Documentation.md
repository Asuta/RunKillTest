
# AudioClip Editor
Version 1.0 | March 23, 2025
## Table of Contents
- [AudioClip Editor](#audioclip-editor)
  * [Overview](#overview)
  * [Features](#features)
  * [Getting Started](#getting-started)
    + [Installation](#installation)
    + [Requirements](#requirements)
    + [Opening the Editor](#opening-the-editor)
  * [Using the Waveform Editor](#using-the-waveform-editor)
    + [Selecting an AudioClip](#selecting-an-audioclip)
    + [Trimming Audio](#trimming-audio)
    + [Adjusting Volume](#adjusting-volume)
    + [Applying Fade In/Out](#applying-fade-inout)
    + [Reset Clip](#reset-clip)
    + [Playback Controls](#playback-controls)
  * [Using the Batch Process Clips Editor](#using-the-batch-process-clips-editor)
    + [Selecting AudioClips](#selecting-audioclips)
    + [Batch Processing Options](#batch-processing-options)
  * [Restoring Original Clips](#restoring-original-clips)
  * [Performance Considerations](#performance-considerations)
  * [Troubleshooting](#troubleshooting)
  * [Support](#support)
  * [License](#license)

## Overview
AudioClip Editor allows you to modify AudioClips directly within the Unity Editor. With features like waveform visualization, batch processing, trimming, volume adjustment, and normalization, this tool is perfect for game developers and sound designers looking to streamline their audio workflow.

## Features
- **Waveform Visualization** – View real-time waveform updates when modifying an AudioClip.
- **Trim Audio** – Remove silence or unwanted sections from the beginning and end of clips.
- **Fade In & Fade Out** – Apply smooth fades with customizable curves.
- **Volume Adjustment** – Increase or decrease the volume of an AudioClip.
- **Normalization** – Auto-adjust the amplitude to fit within the -1 to 1 range.
- **Batch Processing** – Apply adjustments to multiple AudioClips at once.
- **Non-Destructive Editing** – Original clips are stored and can be restored at any time.
- Includes custom-made sound effects for the demo for you to practice on.

## Getting Started

### Installation
Import **AudioClip Editor** via Unity’s Package Manager. See [Package Manager docs](https://docs.unity3d.com/Manual/upm-ui-install.html) for details.

### Requirements
- **Unity 2021.3 LTS or later**.

### Opening the Editor

- **Waveform Editor** (single clip): Right-click an AudioClip in Project > **AudioClip Editor > Waveform Editor**
- **Batch Process Clips** (multi-clip): Right-click a folder/clips > **AudioClip Editor > Batch Process Clips**
- Or use: **Window > AudioClip Editor > [Waveform Editor / Batch Process Clips]**

## Using the Waveform Editor

### Selecting an AudioClip
While the editor is open, you can select an AudioClip in the **Project** tab. The editor will automatically load the selected clip. Alternatively, you can drag and drop an AudioClip from the Project tab into the "AudioClip" field.

### Trimming Audio
1. Adjust the **Trim Start** and **Trim End** sliders.
2. The waveform updates in real-time.

### Adjusting Volume
1. Use the **Volume Slider** to increase or decrease loudness.
2. Enable **Normalization** to automatically balance volume levels.

### Applying Fade In/Out
1. Drag the **Fade In/Out Sliders** to choose the duration of the fade.
2. Use the "Fade In/Out Curve" editors to customize the transition shape.

### Reset Clip
- Press the **Reset** button to revert any changes made in the editor.
- This will restore the original AudioClip from the `UneditedAudioClips` folder.

### Playback Controls
- **Play**: Press the play button to listen to the AudioClip.
- **Pause**: Press the pause button to stop the playback at any time.

## Using the Batch Process Clips Editor
### Selecting AudioClips
- Select a folder in the **Project** tab that contains AudioClips or select multiple AudioClips.
- Right-click and select **AudioClip Editor > Batch Process Clips**.
- Alternatively, you can access the Batch Process Clips editor from the Unity menu bar:
  - **Window > AudioClip Editor > Batch Process Clips**.

### Batch Processing Options
- Normalize: Automatically adjust the amplitude of all selected AudioClips.
- Denormalize: Remove normalization from all selected AudioClips.
- Set Volume: Adjust the volume of all selected AudioClips.
  - Press the "Set Volume For Selected Clips" button to apply the volume change.
- Silence Threshold: Set the threshold for silence detection.
  - Press the "Trim Silence" button to remove silence from the beginning and end of all selected AudioClips.
- Reset Clips: Restore the original AudioClips from the `UneditedAudioClips` folder.

## Restoring Clips
Originals are in `Assets/GradientOverload/AudioClipEditor/UneditedAudioClips/`. Use **Reset** to revert.

## Performance
- Long clips or large batches may slow processing.

## Troubleshooting
- **Waveform doesn't update:** Ensure the clip is not locked by another process.
- **Reset button isn't working:** Check if the original file exists in `UneditedAudioClips`.

## Support
Email [gradientoverload@gmail.com](mailto:gradientoverload@gmail.com) or visit [Gradient Overload](https://assetstore.unity.com/publishers/70692).

## License
This tool is provided under the Unity Asset Store End User License Agreement (EULA).