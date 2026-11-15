using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace LockedIn.Editor
{
    internal sealed class WorkspaceAIBuild : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        // Run after Meta's manifest callback (99999), which can rewrite permissions.
        public int callbackOrder => int.MaxValue;
        private static bool AIEnabled => PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android)
            .Split(';').Contains("LOCKEDIN_AI");

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            PlayerSettings.Android.forceInternetPermission = true;
            if (!AIEnabled)
            {
                Debug.LogWarning("LockedIn: AI detection is not installed. Run LockedIn > Set Up AI Workspace Detection to enable it. Manual and depth selection still work.");
                return;
            }
            if (!File.Exists(WorkspaceAISetup.ModelPath) || !WorkspaceAISetup.ValidModel(File.ReadAllBytes(WorkspaceAISetup.ModelPath)))
                throw new BuildFailedException("LockedIn YOLO model is missing or mismatched. Run LockedIn > Set Up AI Workspace Detection.");
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null || !settings.GetFeatures().Any(f => f != null && f.enabled && f.GetType().FullName == "Meta.XR.MetaXRFeature"))
                throw new BuildFailedException("LockedIn needs Android OpenXR > Meta XR Feature. Run LockedIn AI setup.");
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            var manifest = new XmlDocument();
            manifest.Load(manifestPath);
            const string ns = "http://schemas.android.com/apk/res/android";
            // Unity's launcher owns these resources. Meta adds references to the
            // library manifest, but AGP verifies each library's resources separately.
            // The launcher's manifest supplies the same app name/icon in the final APK.
            var application = manifest.SelectSingleNode("/manifest/application") as XmlElement;
            if (application != null)
            {
                if (application.GetAttribute("label", ns) == "@string/app_name")
                    application.RemoveAttribute("label", ns);
                if (application.GetAttribute("icon", ns) == "@mipmap/app_icon")
                    application.RemoveAttribute("icon", ns);
            }
            string[] permissions = AIEnabled
                ? new[] { "android.permission.INTERNET", "horizonos.permission.HEADSET_CAMERA", "com.oculus.permission.USE_SCENE" }
                : new[] { "android.permission.INTERNET" };
            foreach (string permission in permissions)
            {
                bool exists = manifest.GetElementsByTagName("uses-permission").OfType<XmlElement>()
                    .Any(element => element.GetAttribute("name", ns) == permission);
                if (exists) continue;
                XmlElement element = manifest.CreateElement("uses-permission");
                element.SetAttribute("name", ns, permission);
                manifest.DocumentElement.AppendChild(element);
            }
            manifest.Save(manifestPath);
        }
    }
}
