using Editor;
using UnityEditor;
using UnityEngine;

namespace Plugins.Auto_LOD_Generator.Editor
{
    public class AutoLOD : UnityEditor.Editor
    {
        [MenuItem("AutoLOD/Create a simplified version", false, -1)]
        public static void SimplifiedVersion()
        {
            ShowSimplifier();
        }
        
        [MenuItem("AutoLOD/Create a LOD Group on the object", false, -1)]
        public static void AutoLODGenerator()
        {
            ShowLODGroupWindow();
        }

        private static void ShowSimplifier()
        {
            var popUp = CreateInstance<SimplifiedPopUp>();
            popUp.position = new Rect(Screen.width / 2.0f, Screen.height / 2.0f, 250, 100);
            popUp.maxSize = new Vector2(240, 480);
            popUp.minSize = new Vector2(240, 480);
            popUp.ShowPopup();
        }
        
        private static void ShowLODGroupWindow()
        {
            var popUp = CreateInstance<LODGroupWindow>();
            popUp.position = new Rect(Screen.width / 2.0f, Screen.height / 2.0f, 250, 100);
            popUp.maxSize = new Vector2(240, 480);
            popUp.minSize = new Vector2(240, 480);
            popUp.ShowPopup();
        }
    }
}
