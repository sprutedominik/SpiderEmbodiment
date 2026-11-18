# SpiderEmbodiment

A Unity VR study in which participants control a spider body instead of facing a spider. Built for my master's thesis at Karolinska Institutet's Digital Psychiatry Lab.

![Third-person view: the participant steers the spider through the scene](docs/images/third-person.png)

## Why

Classic exposure for spider phobia puts a person in front of the feared animal. This project asks a different question: what happens when you *are* the spider? If people feel that the spider body is their own, embodiment might become a gentler entry into exposure. The study measures how strongly people embody the avatar, how present they feel, and how afraid they are, scene by scene.

## What a session looks like

The participant wears a standalone headset (Meta Quest) and goes through:

1. A welcome scene with a start button.
2. Six blocks in random order. Each block is three Unity scenes:
   - a neutral washout room with a short countdown,
   - a 90-second spider scene,
   - a questionnaire inside VR with three sliders from 0 to 100: embodiment, fear, presence.
3. A final washout scene.

The six spider scenes cross three tasks with two viewpoints:

| Task | First person | Third person |
| --- | --- | --- |
| Labyrinth: walk the spider through a maze | 0.4 | 0.1 |
| Mirror: see the spider body move in a mirror | 1.0 | 0.7 |
| Light target: follow a moving light | 1.6 | 1.3 |

The numbers are the scene prefixes in `Assets/Scenes SPIDEREMB_/`.

| | |
| --- | --- |
| ![Mirror scene, first person](docs/images/mirror-first-person.png) | ![Mirror scene, third person](docs/images/mirror-third-person.png) |
| *Mirror, first person* | *Mirror, third person* |
| ![Labyrinth seen from above in the editor](docs/images/labyrinth-overview.png) | ![Light target scene in the editor](docs/images/light-target.png) |
| *Labyrinth, editor view* | *Light target, editor view* |
| ![Welcome scene](docs/images/welcome.png) | ![Washout room](docs/images/washout.png) |
| *Welcome scene* | *Washout room* |

## How it works

- **Sequencing.** `SequencedSceneController.cs` scans the build list for the pattern washout, spider, questionnaire, finds the six blocks, shuffles them once per session (Fisher-Yates) and moves through them. Washout scenes run on a button plus a countdown, spider scenes on a timer, questionnaires on a "next" button.
- **Embodiment.** The spider's front legs follow the VR controllers through inverse kinematics (Unity Animation Rigging). The body moves with the left thumbstick. In first person the camera sits in the spider's head; in third person it follows from behind and orbits with the participant's head.
- **Comfort.** A tunneling vignette narrows the view during turns (optionally also during movement) to reduce motion sickness. The spider is stylised on purpose.
- **Logging.** Two CSV files are written to the headset's app data folder:
  - `StudyVRResults/survey_results.csv`: UTC timestamp, rated scene, task and viewpoint, embodiment, fear, presence.
  - `HeadRotationData.csv`: per scene the total yaw and pitch in degrees, head movement in metres, and time spent looking at a target, if one is set in the scene.
- **Ethics.** `Ethical Review/` holds the proposal documents: exposé, participant information, consent form, data handling plan, risk assessment and the in-VR questionnaire items.

## Result

The paradigm was used for data collection in my thesis (M.Sc., 2026). A manuscript is in preparation, so I do not report findings here yet.

## How to run

Third-party models and animations from the Unity Asset Store (spider models, locomotion and character animations) are not included in this repository, because their license does not allow redistribution. Scenes that use them will show missing references until equivalent assets are imported.


1. Install Unity **6000.1.5f1** with Android build support.
2. Open the project folder. Main packages: Universal Render Pipeline 17.1, XR Interaction Toolkit 3.1.2, XR Hands, OpenXR, Animation Rigging, AI Navigation.
3. In **File > Build Settings**, add the scenes from `Assets/Scenes SPIDEREMB_/` in prefix order, from `0..00 Welcome` to `1.8 3rdDummyWashOutNew 5`. The sequencer relies on this order.
4. Switch the platform to Android and build to the headset.

## Tech

Unity 6 · C# · URP · XR Interaction Toolkit · OpenXR · Animation Rigging · NavMesh

## Status

Data collection finished. The repository is kept as the record of the study paradigm. Some scenes in the scene folder are earlier iterations and not part of the study build.
