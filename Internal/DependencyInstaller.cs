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
        private const string DependencyUrl = "https://github.com/Whinarn/UnityMeshSimplifier.git";
        private const string InstalledKey = "AutoLODGenerator_DependencyChecked";

        private static AddRequest _addRequest;

        static DependencyInstaller()
        {
            if (SessionState.GetBool(InstalledKey, false))
                return;

            SessionState.SetBool(InstalledKey, true);

            Debug.Log("[Auto LOD Generator] Checking dependencies...");
            _addRequest = Client.Add(DependencyUrl);
            EditorApplication.update += OnAddComplete;
        }

        private static void OnAddComplete()
        {
            if (!_addRequest.IsCompleted)
                return;

            EditorApplication.update -= OnAddComplete;

            if (_addRequest.Status == StatusCode.Success)
                Debug.Log("[Auto LOD Generator] UnityMeshSimplifier is ready.");
            else
                Debug.LogError(
                    "[Auto LOD Generator] Failed to install UnityMeshSimplifier. " +
                    "Please install manually: Window > Package Manager > + > Add package from git URL > " +
                    DependencyUrl + "\n" + _addRequest.Error?.message);
        }
    }
}
