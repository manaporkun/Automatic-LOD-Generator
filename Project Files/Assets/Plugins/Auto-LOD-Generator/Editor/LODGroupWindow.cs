using System.Globalization;
using Plugins.Auto_LOD_Generator.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class LODGroupWindow : EditorWindow
    {
        private Texture _icon;
        private bool _objectSelected;
        private float _hSliderValue;
        private string _objPath;
        private GameObject _objectToSimplify;
        private const string _iconPath = "Assets/Plugins/Auto-LOD-Generator/Editor/icon.png";
        
        private void OnEnable()
        {
            _hSliderValue = 0.95f;
            _icon = (Texture)AssetDatabase.LoadAssetAtPath(_iconPath, typeof(Texture));
            _objectSelected = false;
            GetWindow(typeof(LODGroupWindow));
        }

        private void OnGUI()
        {
            
            GUILayout.BeginArea(new Rect(0f, 0f, 480f, 480f));
            GUILayout.BeginHorizontal(); //side by side columns
            

            GUILayout.BeginVertical(); //Layout objects vertically in each column
            GUILayout.Box(_icon, GUILayout.Height(240f), GUILayout.Width(240f));            

            if (_objectSelected)
            {
                GUILayout.Space(20);
                EditorGUILayout.LabelField("Quality Factor: ", GUILayout.Height(20f), GUILayout.Width(240f));
                var textFieldVal = float.Parse(EditorGUILayout.TextField(_hSliderValue.ToString(CultureInfo.InvariantCulture), GUILayout.Height(20f), GUILayout.Width(240f)));

                if (textFieldVal >= 0 && textFieldVal <= 1)
                {
                    _hSliderValue = textFieldVal;
                }
                else
                {
                    Debug.LogError("Quality factor number must be between 0 and 1");
                }
                
                
                _hSliderValue = GUILayout.HorizontalScrollbar(_hSliderValue, 0.01f, 0f, 1f, GUILayout.Height(20f), GUILayout.Width(240f));
                GUILayout.Space(20);
                EditorGUILayout.LabelField("Path:" + _objPath, GUILayout.Height(20f), GUILayout.Width(240f));
                GUILayout.Space(20);

                if (GUILayout.Button("Simplify", GUILayout.Height(20f), GUILayout.Width(240f))) { 
                    LODGenerator.Generator(_objectToSimplify, _hSliderValue);
                }
                GUILayout.Space(20);
            }
            
            GUILayout.Space(20);
            if (GUILayout.Button("Select Object", GUILayout.Height(20f), GUILayout.Width(240f))) { 
                _objPath = EditorUtility.OpenFilePanel("Select an FBX object", Application.dataPath, "fbx").Replace(Application.dataPath, "");

                if (_objPath.Length != 0)
                {
                    _objectSelected = true;
                    _objectToSimplify = AssetDatabase.LoadAssetAtPath("Assets/"+_objPath,typeof(GameObject)) as GameObject;

                }
            }
            
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            
            
        }
    }
}