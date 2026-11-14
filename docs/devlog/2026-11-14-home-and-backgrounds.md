# Home screen and backgrounds

## November 14, 2026

The adjustable focus window worked in the first Quest test. Added a LockedIn home screen to the same passthrough scene, with **Create Border** and **Choose Background**. **Create Border** opens [screen and keyboard calibration](2026-11-14-calibrated-border.md).

## Controls

- Use the right controller to aim. Pull the **trigger** to select a menu option.
- Open or close the menu using the right **grip**. **Back** returns from background choices to the home screen.
- Choose **Passthrough**, **Dim Room**, **Slate Blue**, or **Forest Green**. The style takes effect immediately. Before a border exists, the effect is a light preview over the full view. After placement, the two colors fill the area outside the border.
- During normal use, press **A** to switch between the selected effect and normal passthrough. With **Dim Room** selected and a border placed, use the right thumbstick to adjust the dimming. Press **B** to place the border again.

## Quest verification

1. Build `LockedInPassthrough` and check that the title and both buttons appear over live passthrough.
2. Pick each background and confirm that its preview is visible and the menu remains readable.
3. Choose **Create Border** and calibrate the screen. Verify that the inside remains clear and the selected style applies outside it.
4. Press **A** to restore normal visibility. Open the menu with the right grip, choose another background, and check that the border stays in place.

The home screen and new backgrounds still need to be tested in a Quest build.
