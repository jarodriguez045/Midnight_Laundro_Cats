# Current Feature Plan: Core Prototype

## Goal

Build the first playable gameplay loop for Midnight Laundro-Cats.

## Existing Assets

- Existing laundromat template scene.
- Existing cat models.

## Core Loop

1. Player controls one existing cat model.
2. Cat approaches a washing machine.
3. Player presses E to start laundry.
4. Laundry timer runs for 120 seconds.
5. Sock enemies spawn during the laundry cycle.
6. Socks chase and attack the cat.
7. Cat attacks socks with a paw swipe.
8. Player wins if timer reaches zero.
9. Player loses if cat health reaches zero.

## Scripts Needed

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

## Restrictions

- Do not modify existing laundromat template assets.
- Do not modify existing cat model assets.
- Do not edit scene files automatically.
- Do not modify ProjectSettings or Packages.
- Create scripts only under Assets/_Project/Scripts.
