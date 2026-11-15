# Launch LockedIn on Quest 3

Unity Build And Run installs a standalone Android app. It remains on the Quest after you quit. The current installed title is `FocusLens_Unity_Demo`.

1. From Quest Home, reopen Library and look for Unknown Sources. Depending on the layout, check the Applications view and its filter.
2. Select `FocusLens_Unity_Demo`. If the category is missing, connect the headset to Wi-Fi and restart it, then reopen Library. Meta tracks a case where the category can disappear after an offline boot.
3. If it is still missing, confirm Developer Mode in the Meta Horizon phone app for this headset and check the headset Library again. The installed APK can be launched from Meta Quest Developer Hub over USB while diagnosing the Library.

[Meta's deployment guide](https://developers.meta.com/horizon/documentation/unity/ts-mqdh-deploy-build/) · [Meta's offline boot investigation](https://developers.meta.com/vr/support/investigations/1790042985642909/)

## Verification on November 14, 2026

ADB reported `com.DefaultCompany.FocusLens_Unity_Demo` installed, visible to Android and enabled for the active user. The package resolves both Android Launcher and Quest VR categories to `com.unity3d.player.UnityPlayerGameActivity`. The command below cold-started the existing installation successfully and the activity process was present afterward. Launching directly from the headset Library remains unverified.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe' shell am start -W -a android.intent.action.MAIN -c android.intent.category.LAUNCHER -n 'com.DefaultCompany.FocusLens_Unity_Demo/com.unity3d.player.UnityPlayerGameActivity'
```
