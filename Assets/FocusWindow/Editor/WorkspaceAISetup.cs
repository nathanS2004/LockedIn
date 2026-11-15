using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Assemblies;
using UnityEngine.Networking;
using UnityEngine.XR.OpenXR;

namespace LockedIn.Editor
{
    [InitializeOnLoad]
    internal static class WorkspaceAISetup
    {
        private const string Pending = "LockedIn.AISetup.Pending";
        private const string Deadline = "LockedIn.AISetup.Deadline";
        private const string Define = "LOCKEDIN_AI";
        internal const string ModelPath = "Assets/Resources/LockedIn/yolov9sentis.sentis";
        private const string ModelUrl = "https://raw.githubusercontent.com/oculus-samples/Unity-PassthroughCameraApiSamples/3baac1a1a53aa00af3f639425914f14238a97f84/Assets/PassthroughCameraApiSamples/MultiObjectDetection/SentisInference/Model/yolov9sentis.sentis";
        private const string ModelBlob = "926a4f3b79930c69d57d333dc767c54bcc7c625a";
        private static UnityWebRequest download;
        private static AddAndRemoveRequest packages;

        static WorkspaceAISetup() => EditorApplication.update += Tick;

        // Downloads occur only when the developer explicitly invokes this menu command.
        [MenuItem("LockedIn/Set Up AI Workspace Detection")]
        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                Debug.LogWarning("LockedIn: Stop Play Mode and let compilation finish before AI setup.");
                return;
            }
            if (download != null || packages != null || SessionState.GetBool(Pending, false)) return;
            try
            {
                if (File.Exists(ModelPath))
                {
                    if (!ValidModel(File.ReadAllBytes(ModelPath)))
                        throw new InvalidOperationException("A different model exists at " + ModelPath + ". Move it aside before running setup.");
                    InstallPackages();
                    return;
                }
                download = UnityWebRequest.Get(ModelUrl);
                download.timeout = 120;
                download.SendWebRequest();
                Debug.Log("LockedIn: Downloading the 2.4 MB pretrained YOLOv9 model from Meta's pinned sample.");
            }
            catch (Exception error) { Fail(error.Message); }
        }

        private static void InstallPackages()
        {
            AssetDatabase.Refresh();
            SessionState.SetBool(Pending, true);
            SessionState.SetFloat(Deadline, (float)EditorApplication.timeSinceStartup + 600f);
            var requested = new[] {
                "com.meta.xr.mrutilitykit@85.0.0", "com.unity.ai.inference@2.6.1",
                "com.unity.modules.assetbundle@1.0.0",
                "com.unity.modules.unitywebrequestaudio@1.0.0"
            };
            // Embedded SDKs contain our Unity 6.6 compatibility fixes. UPM cannot
            // add/update them; keep them and let the existing type checks validate import.
            var installable = requested.Where(package =>
                !File.Exists(Path.Combine("Packages", package.Split('@')[0], "package.json"))).ToArray();
            packages = installable.Length > 0 ? Client.AddAndRemove(installable) : null;
            Debug.Log("LockedIn: Preserving embedded packages and resolving remaining AI dependencies. Unity may reload scripts.");
        }

        private static void Tick()
        {
            try
            {
                if (download != null)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("LockedIn AI setup", "Downloading Meta's YOLO model", download.downloadProgress))
                    {
                        Fail("Model download cancelled. Existing manual selection is available.");
                        return;
                    }
                    if (!download.isDone) return;
                    if (download.result != UnityWebRequest.Result.Success) throw new IOException(download.error);
                    byte[] bytes = download.downloadHandler.data;
                    if (!ValidModel(bytes)) throw new InvalidDataException("Downloaded model failed its pinned size/hash check.");
                    Directory.CreateDirectory(Path.GetDirectoryName(ModelPath));
                    File.WriteAllBytes(ModelPath, bytes);
                    download.Dispose();
                    download = null;
                    EditorUtility.ClearProgressBar();
                    InstallPackages();
                }
                if (!SessionState.GetBool(Pending, false)) return;
                if (packages != null)
                {
                    if (!packages.IsCompleted) return;
                    if (packages.Status == StatusCode.Failure) throw new Exception(packages.Error.message);
                    packages = null;
                }
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                if (FindType("Meta.XR.PassthroughCameraAccess") == null || FindType("Unity.InferenceEngine.Worker") == null)
                {
                    if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Deadline, 0))
                        throw new TimeoutException("Package installation did not finish. Check Window > Package Manager, then run setup again.");
                    return;
                }
                Configure();
                SessionState.SetBool(Pending, false);
                Debug.Log("LockedIn AI setup complete. Wait for compilation, then Build and Run LockedInPassthrough. Choose Detect Workspace (AI).");
            }
            catch (Exception error) { Fail(error.Message); }
        }

        private static Type FindType(string name) => CurrentAssemblies.GetLoadedAssemblies()
            .Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);

        private static void Configure()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null) throw new InvalidOperationException("Enable Android OpenXR before AI setup.");
            var feature = settings.GetFeatures().FirstOrDefault(f => f != null && f.GetType().FullName == "Meta.XR.MetaXRFeature");
            if (feature == null) throw new InvalidOperationException("Meta XR Feature has not appeared yet. Let Unity finish importing and run setup again.");
            feature.enabled = true;
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            // Keep the current XR Origin, AR passthrough layer, and Unity raycast feature.
            var target = NamedBuildTarget.Android;
            var defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (!defines.Contains(Define))
            {
                defines.Add(Define);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
            }
        }

        internal static bool ValidModel(byte[] bytes)
        {
            if (bytes.Length != 2365288) return false;
            // Git's blob digest verifies the exact file retrieved from the pinned upstream commit.
            using var sha = SHA1.Create();
            byte[] header = Encoding.UTF8.GetBytes("blob " + bytes.Length + "\0");
            sha.TransformBlock(header, 0, header.Length, header, 0);
            sha.TransformFinalBlock(bytes, 0, bytes.Length);
            return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant() == ModelBlob;
        }

        private static void Fail(string message)
        {
            download?.Abort();
            download?.Dispose();
            download = null;
            packages = null;
            SessionState.SetBool(Pending, false);
            EditorUtility.ClearProgressBar();
            Debug.LogError("LockedIn AI setup: " + message);
        }
    }
}
