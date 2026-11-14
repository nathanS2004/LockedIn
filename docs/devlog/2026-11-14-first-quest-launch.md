# First Quest 3 launch

## November 14, 2026

1. Enabled Developer Mode and confirmed the Quest 3 appeared in Windows ADB with status `device`.
2. Enabled the Android OpenXR loader and Meta Quest Support, and added an XR Origin to the `FocusLensDemo` scene.
3. Changed Android Active Input Handling from **Both** to **Input System Package (New)**.
4. Selected the Quest 3 under **Run Device** and used **Build and Run**. Unity produced `LockedIn-Test.apk`, and the scene was visible inside the headset.

## Open the installed build

On the Quest, open **App Library → Unknown Sources** and select **FocusLens_Unity_Demo**. Use Unity's **Build and Run** again to deploy a newer version. [Meta's deployment guide](https://developers.meta.com/vr/documentation/unity/ts-mqdh-deploy-build/)

## Next milestone

Create a separate mixed-reality scene with native passthrough and verify that the real laptop screen is readable in the headset. Then add a controller toggle and a manually placed focus area that keeps the screen clear while dimming the surroundings. Head tracking quality and passthrough still need separate checks on the headset.
