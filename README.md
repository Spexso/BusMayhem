# Bus Mayhem

A hyper-casual mobile puzzle game built with Unity 2022 LTS, developed as a case project for **Rollic Games**.

Players tap stickman characters on a grid and route them to color-matching buses before time runs out or the waiting area overflows.

---

## Gameplay

**Core Loop**

1. Tap a house to spawn a colored stickman
2. Tap a stickman to send it walking toward the exit
3. The stickman boards the matching colored bus, or waits in the waiting area if no match is available
4. Buses depart when full; clear all passengers to win

**Win / Fail Conditions**

| Outcome | Condition |
|---------|-----------|
| Win | All stickmen boarded and all buses departed |
| Fail — Timer | Time expires before grid is cleared |
| Fail — Waiting Area Full | Waiting area overflows with no space remaining |

**Hidden Passengers** — A late-game mechanic where stickmen spawn disguised (shown in black). Their true color is revealed only when a clear path opens around them, adding a layer of uncertainty to planning.

> In-game screenshots are available in the `media/` directory at the repository root.

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Bus/            # BusController, BusManager
│   ├── Core/           # GameManager, LevelManager, InputManager,
│   │                   # TimerManager, PassengerRouter, WaitingAreaManager
│   ├── Data/           # LevelData, BusData, HouseData (ScriptableObjects)
│   ├── Editor/         # In-editor level creation and validation tools
│   ├── Grid/           # GridManager, GridCell, PathFinder
│   ├── House/          # HouseController
│   ├── Stickman/       # StickmanController
│   └── UI/             # GameplayScreenUI, StartScreenUI, EndScreenUI
├── Scenes/
│   ├── StartScene      # Main menu
│   ├── GameplayScene   # Core gameplay loop
│   └── EndScene        # Win / fail results
├── Levels/             # 15 LevelData ScriptableObject assets
├── Prefabs/            # Buses, stickmen, houses, UI
├── Animation/          # Stickman character animations
├── Art/                # Sprites and visual assets
├── Materials/          # Shader materials
└── Thirdparty/         # Third-party asset packs (see below)
```

---

## Architecture

### Managers (Singleton)

| Manager | Responsibility |
|---------|---------------|
| `GameManager` | Master state machine: `Idle → Playing → Win / Fail` |
| `LevelManager` | Level progression and persistence via PlayerPrefs |
| `InputManager` | Touch / click input with raycasting |
| `TimerManager` | Countdown timer with event callbacks |
| `GridManager` | Grid initialization, stickman placement, pathfinding orchestration |
| `BusManager` | Bus spawning, movement, boarding, and departure lifecycle |
| `WaitingAreaManager` | Passenger queue management |
| `PassengerRouter` | Decides whether a stickman goes to a bus or the waiting area |
| `SceneLoader` | Transition between Start, Gameplay, and End scenes |

### Controllers

| Controller | Responsibility |
|------------|---------------|
| `StickmanController` | Movement, color state, interaction, animations |
| `BusController` | Seat tracking, passenger boarding, color assignment |
| `HouseController` | Manages the stickman queue per spawner |
| `GridCell` | Tracks occupancy state of each grid node |

### Pathfinding

`PathFinder` implements **BFS (Breadth-First Search)** to find the shortest path from a stickman to the exit row. It runs both at runtime (avoiding occupied cells) and at design time via the Level Editor to validate that every level is solvable.

### Data Layer (ScriptableObjects)

| Class | Content |
|-------|---------|
| `LevelData` | Grid dimensions, timer duration, waiting area size, cell layout, house definitions, bus sequence |
| `BusData` | Bus color and passenger capacity |
| `HouseData` | Spawner grid position and passenger queue |
| `ColorMatchPalette` | Central color-to-`UnityEngine.Color` mapping |

### Design Patterns

- **Singleton** — All managers use a standard `Instance` guard in `Awake`
- **Observer / Events** — C# events (`OnGameStateChanged`, `OnBusDepart`, `OnWaitingAreaFull`) decouple systems
- **State Machine** — `GameState` enum drives the overall game flow
- **ScriptableObject Data** — Levels and configurations live outside code, enabling designer iteration without recompilation
- **BFS Pathfinding** — Grid traversal is deterministic and validated pre-shipping

---

## Level System

15 levels ship with the project, each defined as a `LevelData` ScriptableObject:

| Field | Description |
|-------|-------------|
| `gridWidth / gridHeight` | Puzzle grid dimensions |
| `timerDuration` | Time limit in seconds |
| `waitingAreaSize` | Maximum passenger queue (default: 3) |
| `cells[]` | Stickman placement per cell, including hidden flag |
| `houses[]` | Spawner positions and their passenger sequences |
| `busSequence[]` | Ordered list of buses arriving at the stop |

**Progression** is tracked via `PlayerPrefs ("CurrentLevelIndex")`. After a win the index increments; after the final level it loops.

An **in-editor Level Editor** window (`Assets/Scripts/Editor/`) lets designers place stickmen, define houses, and build bus sequences visually, with BFS validation to confirm solvability before saving.

---

## Persistence (PlayerPrefs)

| Key | Value |
|-----|-------|
| `CurrentLevelIndex` | Current level number |
| `LastGameResult` | Win / FailTimerExpired / FailWaitingAreaFull |
| `Coins` | Accumulated coin total |

Players earn **10 coins** per level win, displayed with a flying-coin UI animation.

---

## Visual & Audio Feedback

- Stickmen show an outline when selectable and dim when no path is available
- A **blocked icon** animates (scale pulse) when a tap is invalid
- Hidden stickmen display an indicator icon until revealed
- Sound effects (pop, block, success, fail) via `ButtonSoundManager` and `AudioSource`
- Background music on the Start and Gameplay scenes

---

## Third-Party Assets

| Asset | Usage |
|-------|-------|
| Cobra Games Studio — Low Poly Bus Pack | Bus models and road props |
| Hyper Casual Characters | Stickman meshes and animations |
| Camping Low Poly Pack Lite | Environment dressing |
| Casual Game Sounds U6 | SFX library |
| FREE MUSIC PACK (Aila Scott) | Menu and gameplay music |
| BOXOPHOBIC | Skybox and post-effects |
| FastMesh | Mesh optimization utilities |
| TextMesh Pro 3.0.6 | All in-game text |
| Unity Input System 1.7.0 | Modern input handling |

---

## Requirements

| Requirement | Version |
|-------------|---------|
| Unity | 2022.3.19f1 |
| Target Platform | Android (WebGL / PC also supported) |
| Scripting Backend | IL2CPP (Android) |
| Minimum Android API | 22 |

---

## Getting Started

1. Clone the repository
2. Open the project in **Unity 2022.3.19f1**
3. Open `Assets/Scenes/StartScene` and press Play
4. To build for Android, switch platform in **Build Settings** and build normally

To create or edit levels, open **BusMayhem → Level Editor** from the Unity menu bar.
