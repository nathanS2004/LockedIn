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
        public int callbackOrder => 1000;
        private static bool AIEnabled => PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android)
            .Split(';').Contains("LOCKEDIN_AI");

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
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
            if (!AIEnabled) return;
            string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            var manifest = new XmlDocument();
            manifest.Load(manifestPath);
            const string ns = "http://schemas.android.com/apk/res/android";
            foreach (string permission in new[] { "horizonos.permission.HEADSET_CAMERA", "com.oculus.permission.USE_SCENE" })
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
