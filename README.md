# Parry Arena

A parry-forward, Souls-like 1v1 arena duel (Lies of P model) — built as a portfolio vertical slice.
See [`parry-arena-plan.md`](parry-arena-plan.md) for the full design/architecture plan.

This repository currently contains the **game-framing slice**: the menus, selection
screens, and a playable greybox arena that the real combat loop plugs into next.

---

## What's in the build right now

| Screen / system        | State |
|------------------------|-------|
| Main menu (Play / Settings / Quit) | ✅ |
| Settings (volumes, sensitivity, invert-Y, fullscreen; persisted) | ✅ |
| Loadout selection (data-driven) | ✅ |
| Opponent selection (data-driven) | ✅ |
| Greybox arena, third-person player, camera | ✅ |
| In-game pause (reuses the Settings screen) | ✅ |
| Win / lose result screen (Retry / Quit) | ✅ |
| **Parry / dodge / stamina / stagger / riposte combat** | ⏳ next (plan M1) |
| Data-driven enemy AI brain | ⏳ later (plan M3) |

The attack in the arena is intentionally a one-button placeholder so the loop
*menu → loadout → opponent → fight → win/lose → menu* is playable end to end.
The plan front-loads combat feel (M1); this PR front-loads the framing the user
asked for first. The two meet at the arena.

---

## First-time setup

1. **Unity 6.4** (`6000.4.6f1`). Open the project folder. On first open Unity
   restores the re-added **uGUI / TextMeshPro** package (`com.unity.ugui`).
2. **Import TMP Essentials** (one time, required for any text to render):
   `Window ▸ TextMeshPro ▸ Import TMP Essential Resources`.
   If you skip this, the console prints a clear reminder and UI text is invisible.
3. Open **`Assets/Scenes/MainMenu.unity`** and press Play.

> Input uses the **legacy Input Manager** (the project's active input handler) and
> rendering uses the **built-in pipeline** — no extra packages needed for either.

### Optional cleanup (leftover URP-template cruft)
These are unused now that the project was stripped to bare-bones; safe to delete:
- `Assets/Scenes/SampleScene.unity` (orphaned URP camera/light/volume)
- `Assets/InputSystem_Actions.inputactions` (Input System package was removed)
- `Assets/Settings/` (URP render-pipeline assets; the project now uses built-in)

---

## Controls

| Action | Input |
|--------|-------|
| Move | `WASD` |
| Look | Mouse |
| Attack (placeholder) | Left mouse |
| Pause | `Esc` |
| Force win / lose (debug) | `F1` / `F2` |

---

## Project structure

All gameplay/UI content is **built from code** so the scene files stay empty and
free of fragile serialized references. Scripts live under `Assets/Scripts`:

Namespaces are area-level (`ParryArena.Core/.Data/.UI/.Arena`) and intentionally
broader than folders — the folders organise files; the namespaces are the
visibility boundaries.

```
Scripts/
  Bootstrap/          AppBootstrap (auto entry point) + SceneComposer (composition root)
  Core/               Cross-scene services: GameApp, GameSession, MatchResult,
                      GameSettings, SceneFlow, SceneId, UICursor, AppQuit
  Data/               ScriptableObjects: LoadoutDefinition, EnemyDefinition,
                      + GameContent (authored-asset-or-built-in-default provider)
  UI/                 UIFactory + UITheme (DRY widget builders), UIScreen, StatBar
    Menu/             MenuController router + MainMenu/Settings/SelectionScreen<T>/
                      Loadout/Enemy select
    Hud/              ArenaHUD, PauseScreen, ResultScreen
  Arena/              ArenaController (runs the match) + ArenaStage (greybox world)
                      / CombatantFactory (assembles the two fighters) / Combatants
    Actors/           Health, WeaponRig, ActorVisualFactory + AvatarParts
    Controllers/      PlayerController, EnemyController (the actor brains)
    Combat/           ActorCombat (FSM) + SwingProfile, CombatResolver, Hitbox,
                      Hurtbox, StaggerMeter, CombatTeam, CombatState
    Camera/           ThirdPersonCamera (orbit + lock-on)
    Feedback/         Hitstop, ScreenShake, CameraPunch, CombatAudio + CombatSound
                      (procedural SFX), ImpactVfx (spark/hit bursts),
                      CombatDebug + DebugVolume (box viz)
```

> Every public type lives in its own file named after it, so any class, enum, or
> struct is findable by filename.

## Architecture notes (the engineering story)

- **One entry point, one composition root.** `AppBootstrap` runs automatically
  before the first scene (`[RuntimeInitializeOnLoadMethod]`), creates the
  persistent `GameApp` (session + settings), and hands each loaded scene to
  `SceneComposer`, which is the single place that maps a scene to its controller.
  No manually-wired manager objects in scenes.

- **Construction split from runtime.** Each scene controller delegates *building*
  to stateless factories — the arena's world to `ArenaStage`, its fighters to
  `CombatantFactory`, and every UI panel to `UIFactory.CreateScreen` — so the
  controllers keep only runtime state and match flow.

- **Code-built UI, on purpose.** Scenes are empty stages; controllers construct
  their world/UI at runtime. This removes whole classes of "missing reference"
  bugs and makes the UI fully reviewable as code.

- **DRY by construction** (the "no duplicate functions" requirement):
  - `UIFactory` is the *only* place that builds canvases, buttons, sliders,
    toggles, and bars.
  - `ActorVisualFactory` builds the one greybox avatar used by **both** player
    and enemy.
  - `SelectionScreen<T>` holds all the list/highlight/description logic; the
    loadout and opponent screens only supply their data and copy.
  - The pause menu **reuses the same `SettingsScreen`** as the main menu.
  - `Health` is one component shared by the player and enemies.

- **Data-driven content.** Loadouts and opponents are `ScriptableObject`
  definitions. `GameContent` prefers assets found in a `Resources/` folder and
  falls back to built-in defaults, so the game is fully playable before any
  asset is authored — and a designer can override purely with data later. This
  is the seam the plan's AttackSO frame-data and one-brain AI grow into.

- **Centralised scene flow & state.** `SceneFlow` is the only caller of
  `SceneManager`, and always restores `Time.timeScale` so a scene never starts
  frozen. The arena owns a small playing/paused/over state machine.

## Next steps (per the plan)

1. ~~**M1 — the parry feel loop**~~ *(built)*: perfect parry / block / dodge +
   i-frames / stamina / hitstop / stagger meter → riposte / charged heavy strike,
   plus the feel layer — procedural SFX, spark + hit VFX, and input buffering.
2. **M2** — move attacks into `AttackSO` frame data + animation events.
3. **M3** — the one-brain, data-driven enemy AI reading `EnemyDefinition`.
