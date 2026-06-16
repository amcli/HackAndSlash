# Parry Arena

A parry-forward, Souls-like 1v1 arena duel (Sekiro/Lies of P).

This repository currently contains: the menus, selection
screens, and a playable placeholder arena that the real combat loop plugs into next.

---

## What's in the build right now

| Screen / system        | State |
|------------------------|-------|
| Main menu (Play / Settings / Quit) | done |
| Settings (volumes, sensitivity, invert-Y, fullscreen; persisted) | done |
| Loadout selection (data-driven) | done |
| Opponent selection (data-driven) | done |
| Greybox arena, third-person player, camera | done |
| In-game pause (reuses the Settings screen) | done |
| Win / lose result screen (Retry / Quit) | done |
| **Parry / dodge / stamina / stagger / riposte combat** | in progress |
| Enemy AI | not started |

The attack in the arena is currently a one-button placeholder so the loop
*menu → loadout → opponent → fight → win/lose → menu* is playable end to end.

---

## First-time setup

1. **Unity 6.4** (`6000.4.6f1`). Open the project folder. On first open Unity
   restores the re-added **uGUI / TextMeshPro** package (`com.unity.ugui`).
2. **Import TMP Essentials** (one time, required for any text to render):
   `Window ▸ TextMeshPro ▸ Import TMP Essential Resources`.
   If you skip this, the console prints a clear reminder and UI text is invisible.
3. Open **`Assets/Scenes/MainMenu.unity`** and press Play.

> Input uses the **legacy Input Manager** (the project's active input handler) and
> rendering uses the **built-in pipeline**.

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

Namespaces are area-level (`ParryArena.Core/.Data/.UI/.Arena`).

```
Scripts/
  Bootstrap/          AppBootstrap (auto entry point) + SceneComposer (composition root)
  Core/               Cross-scene services: GameApp, GameSession, MatchResult,
                      GameSettings, SceneFlow, SceneId, UICursor, AppQuit,
                      Persistent (DontDestroyOnLoad singleton helper)
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
                      (SFX), ImpactVfx (spark/hit bursts),
                      CombatDebug + DebugVolume (box viz)
```

> Every public type lives in its own file named after it, so any class, enum, or
> struct is findable by filename.

