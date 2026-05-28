# Codex Instructions for Midnight Laundro-Cats

This is a Unity 6 C# game project.

## Project Goal

Build a playable prototype called Midnight Laundro-Cats.

Game concept:
Two cartoon cats try to do laundry at a spooky laundromat that is open at night only. When a cat starts a washing machine, laundry runs for 120 seconds. During that time, spooky lost sock enemies spawn and attack. The player wins if they survive until the laundry cycle finishes.

## Existing Assets

The project already contains:
- A laundromat scene/template.
- Cat character models.

Do not create, replace, rename, delete, or overwrite existing laundromat template assets or cat model assets.

## Safe Editing Area

Codex may create or modify files under:

Assets/_Project/Scripts/
Assets/_Project/Docs/

Codex should not modify:

ProjectSettings/
Packages/
Assets/ThirdParty/
Imported asset folders
Existing laundromat template assets
Existing cat model assets

Do not edit Unity scene files unless explicitly requested.

## Coding Standards

- Use Unity C#.
- Use namespaces under MidnightLaundroCats.
- Place scripts under Assets/_Project/Scripts.
- Use [SerializeField] private fields instead of public mutable fields.
- Keep scripts modular and easy to test.
- Avoid GameObject.Find in Update.
- Avoid unnecessary allocations in Update, FixedUpdate, and LateUpdate.
- Include null checks for serialized references where useful.
- Do not rename serialized fields unless necessary.
- Use old Input Manager-compatible input for the first prototype unless told otherwise.
- Use Unity built-in systems where possible.
- Do not require paid assets.
- Keep the prototype playable and easy to test.

## First Gameplay Systems to Build

Core:
- GameStateManager.cs
- LaundryCycleManager.cs
- Health.cs

Player:
- CatPlayerController.cs
- CatCombat.cs

Enemies:
- SockEnemyAI.cs
- SockSpawner.cs

Interactables:
- LaundryMachine.cs
- InteractablePrompt.cs

UI:
- PrototypeHUD.cs

## First Prototype Goal

The first playable prototype should prove:

1. Existing cat model moves around the laundromat.
2. Cat approaches a washing machine.
3. Player presses E to start laundry.
4. 120-second laundry timer starts.
5. Sock enemies spawn while timer runs.
6. Socks chase and attack the cat.
7. Cat attacks socks with paw swipe.
8. Player wins if timer reaches zero.
9. Player loses if cat health reaches zero.
10. UI updates during gameplay.

## Expected Response After Work

After making changes, summarize:
1. Files created or modified.
2. Purpose of each file.
3. Unity Editor setup steps.
4. Required prefab setup.
5. Required serialized field assignments.
6. Play Mode test checklist.
7. Any assumptions or limitations.