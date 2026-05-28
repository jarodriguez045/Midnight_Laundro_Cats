# Unity Game Development Rules

This is a Unity 6 C# game project.

Project:
Midnight Laundro-Cats is a 3D Unity prototype about two cartoon cats doing laundry in a spooky laundromat at night. While laundry runs in 2-minute cycles, the cats fight spooky lost sock enemies.

Existing assets:
- The project already has a laundromat scene/template.
- The project already has cat models.
- Do not create, replace, rename, or overwrite existing environment art.
- Do not create, replace, rename, or overwrite existing cat model assets.
- Gameplay scripts should integrate with existing scene objects.

Rules:
- Use C# for Unity gameplay code.
- Use namespaces under MidnightLaundroCats.
- Place scripts under Assets/_Project/Scripts.
- Use [SerializeField] private fields instead of public fields.
- Keep systems modular and prototype-friendly.
- Avoid GameObject.Find in Update.
- Avoid unnecessary allocations in Update.
- Include null checks where useful.
- Do not modify ProjectSettings, Packages, imported models, imported materials, or original template scenes unless explicitly requested.
- Do not rename serialized fields unless necessary.
- Do not edit scene files unless explicitly requested.
- When adding gameplay systems, include Unity Editor setup instructions and a Play Mode test checklist.
- Prioritize a playable prototype over complex architecture.
