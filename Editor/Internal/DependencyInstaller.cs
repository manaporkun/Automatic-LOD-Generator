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
        private const string DependencyPackage = "com.whinarn.unitymeshsimplifier";
        private const string DependencyUrl = "https://github.com/Whinarn/UnityMeshSimplifier.git";
        private const string InstalledKey = "AutoLODGenerator_DependencyChecked";

        private static ListRequest _listRequest;
        private static AddRequest _addRequest;

        static DependencyInstaller()
        {
            // Only check once per editor session
            if (SessionState.GetBool(InstalledKey, false))
                return;

            SessionState.SetBool(InstalledKey, true);
            _listRequest = Client.List(true);
            EditorApplication.update += OnListComplete;
        }

        private static void OnListComplete()
        {
            if (!_listRequest.IsCompleted)
                return;

            EditorApplication.update -= OnListComplete;

            if (_listRequest.Status == StatusCode.Failure)
                return;

            foreach (var package in _listRequest.Result)
            {
                if (package.name == DependencyPackage)
                    return; // Already installed
            }

            Debug.Log($"[Auto LOD Generator] Installing required dependency: {DependencyPackage}");
            _addRequest = Client.Add(DependencyUrl);
            EditorApplication.update += OnAddComplete;
        }

        private static void OnAddComplete()
        {
            if (!_addRequest.IsCompleted)
                return;

            EditorApplication.update -= OnAddComplete;

            if (_addRequest.Status == StatusCode.Success)
                Debug.Log($"[Auto LOD Generator] Successfully installed {DependencyPackage}");
            else
                Debug.LogError($"[Auto LOD Generator] Failed to install {DependencyPackage}: {_addRequest.Error?.message}");
        }
    }
}
