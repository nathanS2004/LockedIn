using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LockedIn.Editor
{
    [InitializeOnLoad]
    internal static class FocusInterfacePreview
    {
        private const string Request = "Temp/lockedin-ui-preview.request";
        static FocusInterfacePreview() { EditorApplication.update += CheckRequest; }
        private static void CheckRequest()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/LockedIn/Interface/Previews/secluded_beach.png") == null) return;
            File.Delete(Request); Capture();
        }
        [MenuItem("LockedIn/Interface/Capture presentation previews")]
        private static void Capture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            { Debug.LogWarning("Stop Play Mode and finish importing before capturing interface previews."); return; }
            Type type = Type.GetType("LockedIn.FocusHomeMenu, Assembly-CSharp");
            MethodInfo factory = type?.GetMethod("CreateEditorPreview", BindingFlags.Public | BindingFlags.Static);
            if (factory == null) { Debug.LogWarning("Recompile the interface before capturing previews."); return; }
            Directory.CreateDirectory("docs/previews");
            foreach (string page in new[] { "home", "setup", "collection", "guide" })
            {
                var root = new GameObject("LockedIn temporary preview camera") { hideFlags = HideFlags.HideAndDontSave };
                var camera = root.AddComponent<Camera>(); camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(40, 49, 44, 255);
                camera.fieldOfView = 34; camera.cullingMask = 1 << 31; camera.nearClipPlane = 0.01f;
                camera.stereoTargetEye = StereoTargetEyeMask.None;
                var target = new RenderTexture(1600, 1200, 24, RenderTextureFormat.ARGB32);
                target.antiAliasing = 4; camera.allowMSAA = true;
                target.Create(); camera.targetTexture = target;
                RenderTexture previous = RenderTexture.active;
                IDisposable menu = null; Texture2D screenshot = null;
                try
                {
                    if (page == "home") Debug.Log(type.GetMethod("ValidateEditorInteractions", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { camera }));
                    menu = (IDisposable)factory.Invoke(null, new object[] { camera, page });
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                    // Dynamic font atlases may resize as the last heading is created. Rebuild every
                    // active label after prewarming to avoid a one-frame capture of stale glyph UVs.
                    foreach (Text label in root.GetComponentsInChildren<Text>())
                        label.font.RequestCharactersInTexture(label.text, label.fontSize, label.fontStyle);
                    Canvas.ForceUpdateCanvases();
                    foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>()) graphic.SetAllDirty();
                    Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                    screenshot = new Texture2D(1600, 1200, TextureFormat.RGB24, false);
                    screenshot.ReadPixels(new Rect(0, 0, 1600, 1200), 0, 0); screenshot.Apply();
                    File.WriteAllBytes("docs/previews/lockedin-" + page + ".png", screenshot.EncodeToPNG());
                }
                finally
                {
                    menu?.Dispose(); RenderTexture.active = previous;
                    if (screenshot != null) UnityEngine.Object.DestroyImmediate(screenshot);
                    UnityEngine.Object.DestroyImmediate(root); target.Release(); UnityEngine.Object.DestroyImmediate(target);
                }
            }
            Debug.Log("LockedIn presentation previews captured in docs/previews. Sample LIVE state is editor-only.");
        }
    }
}
