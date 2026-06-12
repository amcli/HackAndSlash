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

```
Scripts/
  Bootstrap/        AppBootstrap (auto entry point) + SceneComposer (composition root)
  Core/             Cross-scene services: GameApp, GameSession, GameSettings,
                    SceneFlow, SceneId, UICursor/AppQuit
  Data/             ScriptableObjects: LoadoutDefinition, EnemyDefinition,
                    + GameContent (authored-asset-or-built-in-default provider)
  UI/               UIFactory + UITheme (DRY widget builders), UIScreen base,
                    MenuController router
    Screens/        MainMenu, Settings, SelectionScreen<T> base, Loadout/Enemy select
    Arena/          ArenaHUD, PauseScreen, ResultScreen
  Arena/            ArenaController, PlayerController, ThirdPersonCamera,
                    Health, EnemyActor, ActorVisualFactory
```

## Architecture notes (the engineering story)

- **One entry point, one composition root.** `AppBootstrap` runs automatically
  before the first scene (`[RuntimeInitializeOnLoadMethod]`), creates the
  persistent `GameApp` (session + settings), and hands each loaded scene to
  `SceneComposer`, which is the single place that maps a scene to its controller.
  No manually-wired manager objects in scenes.

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

1. **M1 — the parry feel loop**: replace the placeholder attack with perfect
   parry / block / dodge + i-frames / stamina / hitstop / stagger meter / riposte.
2. **M2** — move attacks into `AttackSO` frame data + animation events + input buffering.
3. **M3** — the one-brain, data-driven enemy AI reading `EnemyDefinition`.
