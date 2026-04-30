using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Plugins.AutoLODGenerator.Editor
{
    /// <summary>
    /// Automatically installs required dependencies on first load.
    /// UPM does not resolve git URL dependencies from package.json,
    /// so this script handles it at editor startup.
    /// </summary>
    [InitializeOnLoad]
    internal static class DependencyInstaller
    {
        private const string GitUrl = "https://github.com/Whinarn/UnityMeshSimplifier.git";
        private const string RepoUrl = "https://github.com/Whinarn/UnityMeshSimplifier";
        private const string InstalledKey = "AutoLODGenerator_DependencyChecked";

        private static AddRequest _addRequest;

        static DependencyInstaller()
        {
            if (SessionState.GetBool(InstalledKey, false))
                return;

            SessionState.SetBool(InstalledKey, true);

            Debug.Log("[Auto LOD Generator] Checking dependencies...");
            _addRequest = Client.Add(GitUrl);
            EditorApplication.update += OnAddComplete;
        }

        private static void OnAddComplete()
        {
            if (!_addRequest.IsCompleted)
                return;

            EditorApplication.update -= OnAddComplete;

            if (_addRequest.Status == StatusCode.Success)
            {
                Debug.Log("[Auto LOD Generator] UnityMeshSimplifier is ready.");
                return;
            }

            var errorMessage = _addRequest.Error?.message ?? "unknown error";
            Debug.LogError(
                "[Auto LOD Generator] Failed to install UnityMeshSimplifier automatically. " +
                "Please install manually: Window > Package Manager > + > Add package from git URL > " +
                GitUrl + "\n" + errorMessage);

            if (Application.isBatchMode)
                return;

            EditorApplication.delayCall += () => ShowFailureDialog(errorMessage);
        }

        private static void ShowFailureDialog(string errorMessage)
        {
            var choice = EditorUtility.DisplayDialogComplex(
                "Auto LOD Generator — Dependency Install Failed",
                "Auto LOD Generator could not automatically install the required 'UnityMeshSimplifier' package.\n\n" +
                "Reason: " + errorMessage + "\n\n" +
                "To install manually:\n" +
                "1. Open Window > Package Manager\n" +
                "2. Click + > Add package from git URL\n" +
                "3. Paste the UnityMeshSimplifier git URL (button below copies it)",
                "Copy Git URL",
                "Dismiss",
                "Open in Browser");

            switch (choice)
            {
                case 0:
                    EditorGUIUtility.systemCopyBuffer = GitUrl;
                    Debug.Log("[Auto LOD Generator] UnityMeshSimplifier git URL copied to clipboard.");
                    break;
                case 2:
                    Application.OpenURL(RepoUrl);
                    break;
            }
        }
    }
}
