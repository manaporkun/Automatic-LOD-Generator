using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Plugins.AutoLODGenerator.Editor
{
    /// <summary>
    /// Main editor window for Auto LOD Generator.
    /// Provides a modern, unified interface for LOD generation and mesh simplification.
    /// </summary>
    public class AutoLODWindow : EditorWindow
    {
        #region Constants

        private const string WindowTitle = "Auto LOD Generator";
        private const float MinWindowWidth = 400f;
        private const float MinWindowHeight = 500f;
        private const string IconPath = "Assets/Plugins/Auto-LOD-Generator/Editor/icon.png";

        #endregion

        #region Private Fields

        // Tab state
        private int _selectedTab;
        private readonly string[] _tabNames = { "LOD Group", "Simplify Mesh", "Batch Process" };

        // Settings
        private LODGeneratorSettings _settings;
        private LODPreset _selectedPreset = LODPreset.Balanced;

        // Object selection
        private List<GameObject> _selectedObjects = new List<GameObject>();
        private Vector2 _objectListScrollPos;

        // Simplify mesh tab
        private float _simplifyQuality = 0.5f;
        private GameObject _singleObject;

        // Batch processing
        private bool _isProcessing;
        private float _processingProgress;
        private string _processingStatus = "";
        private BatchProcessingResult _lastBatchResult;

        // UI State
        private Vector2 _settingsScrollPos;
        private Vector2 _resultsScrollPos;
        private bool _showAdvancedSettings;
        private LODGenerationResult _lastResult;

        // Cached textures and styles
        private Texture2D _iconTexture;
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _statisticsStyle;
        private bool _stylesInitialized;

        #endregion

        #region Window Lifecycle

        [MenuItem("Tools/Auto LOD Generator", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<AutoLODWindow>(WindowTitle);
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            window.Show();
        }

        private void OnEnable()
        {
            _settings = new LODGeneratorSettings();
            _iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);

            // Subscribe to selection changes
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            // Auto-populate from hierarchy selection
            var selectedGameObjects = Selection.gameObjects
                .Where(go => LODGeneratorCore.ValidateForLODGeneration(go, out _))
                .ToList();

            if (selectedGameObjects.Count > 0)
            {
                _selectedObjects = selectedGameObjects;
                if (_selectedObjects.Count == 1)
                {
                    _singleObject = _selectedObjects[0];
                }
                Repaint();
            }
        }

        #endregion

        #region Main GUI

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.BeginVertical();

            DrawHeader();
            DrawTabs();

            EditorGUILayout.Space(10);

            switch (_selectedTab)
            {
                case 0:
                    DrawLODGroupTab();
                    break;
                case 1:
                    DrawSimplifyTab();
                    break;
                case 2:
                    DrawBatchTab();
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            _statisticsStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                wordWrap = true
            };

            _stylesInitialized = true;
        }

        #endregion

        #region Header and Tabs

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(_boxStyle);

            if (_iconTexture != null)
            {
                GUILayout.Label(_iconTexture, GUILayout.Width(48), GUILayout.Height(48));
            }

            EditorGUILayout.BeginVertical();
            GUILayout.Label(WindowTitle, _headerStyle);
            GUILayout.Label("Automatic Level of Detail Generator", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            EditorGUILayout.Space(5);
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
        }

        #endregion

        #region LOD Group Tab

        private void DrawLODGroupTab()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            // Object Selection Section
            DrawSectionHeader("Object Selection");
            DrawObjectSelection();

            EditorGUILayout.Space(10);

            // Settings Section
            DrawSectionHeader("LOD Settings");
            DrawLODSettings();

            EditorGUILayout.Space(10);

            // Preview Section
            if (_selectedObjects.Count > 0)
            {
                DrawSectionHeader("Preview");
                DrawLODPreview();
            }

            EditorGUILayout.Space(10);

            // Generate Button
            DrawGenerateButton();

            // Results Section
            if (_lastResult != null)
            {
                EditorGUILayout.Space(10);
                DrawResultsSection();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawObjectSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Drag and drop area
            var dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop GameObjects here\nor select in Hierarchy", EditorStyles.centeredGreyMiniLabel);

            HandleDragAndDrop(dropArea);

            // Single object field
            EditorGUILayout.Space(5);
            var newObject = EditorGUILayout.ObjectField("Target Object",
                _selectedObjects.Count == 1 ? _selectedObjects[0] : null,
                typeof(GameObject), true) as GameObject;

            if (newObject != null && newObject != (_selectedObjects.Count == 1 ? _selectedObjects[0] : null))
            {
                _selectedObjects.Clear();
                _selectedObjects.Add(newObject);
            }

            // Show selected objects count
            if (_selectedObjects.Count > 1)
            {
                EditorGUILayout.HelpBox($"{_selectedObjects.Count} objects selected", MessageType.Info);
            }

            // Validation message
            if (_selectedObjects.Count == 1)
            {
                if (!LODGeneratorCore.ValidateForLODGeneration(_selectedObjects[0], out string error))
                {
                    EditorGUILayout.HelpBox(error, MessageType.Warning);
                }
                else
                {
                    var stats = LODGeneratorCore.GetMeshStatistics(_selectedObjects[0]);
                    EditorGUILayout.LabelField($"Mesh: {stats.vertices:N0} vertices, {stats.triangles:N0} triangles");
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            var evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition)) return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();

                    _selectedObjects.Clear();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is GameObject go && LODGeneratorCore.ValidateForLODGeneration(go, out _))
                        {
                            _selectedObjects.Add(go);
                        }
                    }

                    evt.Use();
                    break;
            }
        }

        private void DrawLODSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Preset dropdown
            EditorGUI.BeginChangeCheck();
            _selectedPreset = (LODPreset)EditorGUILayout.EnumPopup("Preset", _selectedPreset);
            if (EditorGUI.EndChangeCheck())
            {
                _settings.ApplyPreset(_selectedPreset);
            }

            EditorGUILayout.Space(5);

            // LOD Level Count
            EditorGUI.BeginChangeCheck();
            _settings.lodLevelCount = EditorGUILayout.IntSlider("LOD Levels", _settings.lodLevelCount,
                LODGeneratorSettings.MinLODLevels, LODGeneratorSettings.MaxLODLevels);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedPreset = LODPreset.Custom;
            }

            // Culled level toggle
            EditorGUI.BeginChangeCheck();
            _settings.includeCulledLevel = EditorGUILayout.Toggle("Include Culled Level", _settings.includeCulledLevel);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedPreset = LODPreset.Custom;
            }

            // Advanced settings foldout
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings", true);

            if (_showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                DrawAdvancedSettings();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedSettings()
        {
            _settingsScrollPos = EditorGUILayout.BeginScrollView(_settingsScrollPos, GUILayout.MaxHeight(200));

            EditorGUILayout.LabelField("Quality Factors", EditorStyles.boldLabel);
            for (int i = 1; i < _settings.lodLevelCount; i++)
            {
                EditorGUI.BeginChangeCheck();
                _settings.qualityFactors[i] = EditorGUILayout.Slider($"LOD{i} Quality",
                    _settings.qualityFactors[i], 0.01f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedPreset = LODPreset.Custom;
                }
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Screen Transition Heights", EditorStyles.boldLabel);
            for (int i = 0; i < _settings.lodLevelCount; i++)
            {
                EditorGUI.BeginChangeCheck();
                _settings.screenTransitionHeights[i] = EditorGUILayout.Slider($"LOD{i} Transition",
                    _settings.screenTransitionHeights[i], 0.001f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedPreset = LODPreset.Custom;
                }
            }

            if (_settings.includeCulledLevel)
            {
                EditorGUI.BeginChangeCheck();
                _settings.culledTransitionHeight = EditorGUILayout.Slider("Culled Transition",
                    _settings.culledTransitionHeight, 0.001f, 0.1f);
                if (EditorGUI.EndChangeCheck())
                {
                    _selectedPreset = LODPreset.Custom;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLODPreview()
        {
            if (_selectedObjects.Count == 0) return;

            var targetObject = _selectedObjects[0];
            var stats = LODGeneratorCore.GetMeshStatistics(targetObject);
            if (stats.vertices < 0) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Estimated LOD Statistics:", EditorStyles.boldLabel);

            for (int i = 0; i < _settings.lodLevelCount; i++)
            {
                float quality = _settings.GetQualityFactor(i);
                int estimatedVerts = LODGeneratorCore.EstimateVertexCount(stats.vertices, quality);
                int estimatedTris = LODGeneratorCore.EstimateVertexCount(stats.triangles, quality);
                float reduction = (1f - quality) * 100f;

                string label = i == 0
                    ? $"LOD{i}: {estimatedVerts:N0} verts, {estimatedTris:N0} tris (Original)"
                    : $"LOD{i}: ~{estimatedVerts:N0} verts, ~{estimatedTris:N0} tris ({reduction:F0}% reduction)";

                EditorGUILayout.LabelField(label, _statisticsStyle);
            }

            if (_settings.includeCulledLevel)
            {
                EditorGUILayout.LabelField($"LOD{_settings.lodLevelCount}: Culled (100% reduction)", _statisticsStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGenerateButton()
        {
            GUI.enabled = _selectedObjects.Count > 0 && !_isProcessing;

            if (GUILayout.Button("Generate LOD Group", GUILayout.Height(30)))
            {
                GenerateLODForSelectedObjects();
            }

            GUI.enabled = true;
        }

        private void GenerateLODForSelectedObjects()
        {
            if (_selectedObjects.Count == 0) return;

            if (_selectedObjects.Count == 1)
            {
                _lastResult = LODGeneratorCore.GenerateLODGroup(_selectedObjects[0], _settings);

                if (_lastResult.Success)
                {
                    Selection.activeGameObject = _lastResult.GeneratedLODGroup;
                    Debug.Log($"[Auto LOD] Successfully generated LOD group for '{_selectedObjects[0].name}'");
                }
                else
                {
                    Debug.LogError($"[Auto LOD] Failed: {_lastResult.ErrorMessage}");
                }
            }
            else
            {
                // Switch to batch tab for multiple objects
                _selectedTab = 2;
            }

            Repaint();
        }

        private void DrawResultsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawSectionHeader("Last Result");

            if (_lastResult.Success)
            {
                EditorGUILayout.HelpBox("LOD Group generated successfully!", MessageType.Info);

                EditorGUILayout.LabelField("Statistics:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Original: {_lastResult.OriginalVertexCount:N0} vertices, {_lastResult.OriginalTriangleCount:N0} triangles");

                for (int i = 0; i < _lastResult.LODVertexCounts.Length; i++)
                {
                    float reduction = (1f - ((float)_lastResult.LODVertexCounts[i] / _lastResult.OriginalVertexCount)) * 100f;
                    EditorGUILayout.LabelField($"LOD{i}: {_lastResult.LODVertexCounts[i]:N0} verts ({reduction:F1}% reduction)");
                }

                if (GUILayout.Button("Select Generated LOD Group"))
                {
                    Selection.activeGameObject = _lastResult.GeneratedLODGroup;
                }
            }
            else
            {
                EditorGUILayout.HelpBox(_lastResult.ErrorMessage, MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Simplify Tab

        private void DrawSimplifyTab()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            DrawSectionHeader("Single Mesh Simplification");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Object field
            _singleObject = EditorGUILayout.ObjectField("Source Object", _singleObject, typeof(GameObject), true) as GameObject;

            // Validation
            if (_singleObject != null)
            {
                if (!LODGeneratorCore.ValidateForLODGeneration(_singleObject, out string error))
                {
                    EditorGUILayout.HelpBox(error, MessageType.Warning);
                }
                else
                {
                    var stats = LODGeneratorCore.GetMeshStatistics(_singleObject);
                    EditorGUILayout.LabelField($"Original: {stats.vertices:N0} vertices, {stats.triangles:N0} triangles");
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Quality slider
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawSectionHeader("Quality Settings");

            _simplifyQuality = EditorGUILayout.Slider("Quality Factor", _simplifyQuality, 0.01f, 0.99f);

            // Quality presets
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("25%")) _simplifyQuality = 0.25f;
            if (GUILayout.Button("50%")) _simplifyQuality = 0.50f;
            if (GUILayout.Button("75%")) _simplifyQuality = 0.75f;
            EditorGUILayout.EndHorizontal();

            // Preview
            if (_singleObject != null && LODGeneratorCore.ValidateForLODGeneration(_singleObject, out _))
            {
                var stats = LODGeneratorCore.GetMeshStatistics(_singleObject);
                int estimatedVerts = LODGeneratorCore.EstimateVertexCount(stats.vertices, _simplifyQuality);
                int estimatedTris = LODGeneratorCore.EstimateVertexCount(stats.triangles, _simplifyQuality);
                float reduction = (1f - _simplifyQuality) * 100f;

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Estimated result: ~{estimatedVerts:N0} verts, ~{estimatedTris:N0} tris ({reduction:F0}% reduction)");
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Simplify button
            GUI.enabled = _singleObject != null && LODGeneratorCore.ValidateForLODGeneration(_singleObject, out _);

            if (GUILayout.Button("Simplify Mesh", GUILayout.Height(30)))
            {
                var result = LODGeneratorCore.GenerateSimplifiedMesh(_singleObject, _simplifyQuality);
                _lastResult = result;

                if (result.Success)
                {
                    Selection.activeGameObject = result.GeneratedLODGroup;
                    Debug.Log($"[Auto LOD] Successfully simplified '{_singleObject.name}'");
                }
                else
                {
                    Debug.LogError($"[Auto LOD] Failed: {result.ErrorMessage}");
                }

                Repaint();
            }

            GUI.enabled = true;

            // Results
            if (_lastResult != null && _selectedTab == 1)
            {
                EditorGUILayout.Space(10);
                DrawSimplifyResults();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSimplifyResults()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawSectionHeader("Result");

            if (_lastResult.Success)
            {
                EditorGUILayout.HelpBox("Mesh simplified successfully!", MessageType.Info);
                EditorGUILayout.LabelField($"Original: {_lastResult.OriginalVertexCount:N0} vertices");
                EditorGUILayout.LabelField($"Simplified: {_lastResult.LODVertexCounts[0]:N0} vertices");

                float actualReduction = (1f - ((float)_lastResult.LODVertexCounts[0] / _lastResult.OriginalVertexCount)) * 100f;
                EditorGUILayout.LabelField($"Actual reduction: {actualReduction:F1}%");
            }
            else
            {
                EditorGUILayout.HelpBox(_lastResult.ErrorMessage, MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Batch Tab

        private void DrawBatchTab()
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            DrawSectionHeader("Batch Processing");

            // Object list
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Selected Objects: {_selectedObjects.Count}", EditorStyles.boldLabel);

            // Drag and drop area
            var dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop multiple GameObjects here", EditorStyles.centeredGreyMiniLabel);
            HandleDragAndDrop(dropArea);

            // Object list scroll view
            if (_selectedObjects.Count > 0)
            {
                _objectListScrollPos = EditorGUILayout.BeginScrollView(_objectListScrollPos, GUILayout.MaxHeight(150));

                for (int i = _selectedObjects.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(_selectedObjects[i], typeof(GameObject), true);
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        _selectedObjects.RemoveAt(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            // Selection helpers
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add from Selection"))
            {
                foreach (var go in Selection.gameObjects)
                {
                    if (LODGeneratorCore.ValidateForLODGeneration(go, out _) && !_selectedObjects.Contains(go))
                    {
                        _selectedObjects.Add(go);
                    }
                }
            }
            if (GUILayout.Button("Clear All"))
            {
                _selectedObjects.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Settings (reuse LOD settings)
            DrawSectionHeader("LOD Settings");
            DrawLODSettings();

            EditorGUILayout.Space(10);

            // Progress bar
            if (_isProcessing)
            {
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                    _processingProgress,
                    _processingStatus);
            }

            // Process button
            GUI.enabled = _selectedObjects.Count > 0 && !_isProcessing;

            if (GUILayout.Button($"Process {_selectedObjects.Count} Objects", GUILayout.Height(30)))
            {
                ProcessBatch();
            }

            GUI.enabled = true;

            // Batch results
            if (_lastBatchResult != null)
            {
                EditorGUILayout.Space(10);
                DrawBatchResults();
            }

            EditorGUILayout.EndVertical();
        }

        private void ProcessBatch()
        {
            _isProcessing = true;

            var result = LODGeneratorCore.ProcessBatch(
                _selectedObjects.ToArray(),
                _settings,
                (progress, status) =>
                {
                    _processingProgress = progress;
                    _processingStatus = status;
                    Repaint();
                });

            _lastBatchResult = result;
            _isProcessing = false;

            if (result.AllSucceeded)
            {
                Debug.Log($"[Auto LOD] Batch processing complete: {result.SuccessCount} objects processed in {result.ProcessingTimeSeconds:F2}s");
            }
            else
            {
                Debug.LogWarning($"[Auto LOD] Batch processing complete with errors: {result.SuccessCount} succeeded, {result.FailureCount} failed");
            }

            Repaint();
        }

        private void DrawBatchResults()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawSectionHeader("Batch Results");

            var msgType = _lastBatchResult.AllSucceeded ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox(
                $"Processed {_lastBatchResult.TotalObjects} objects in {_lastBatchResult.ProcessingTimeSeconds:F2}s\n" +
                $"Success: {_lastBatchResult.SuccessCount} | Failed: {_lastBatchResult.FailureCount}",
                msgType);

            // Show failures
            var failures = _lastBatchResult.Results.Where(r => !r.Success).ToList();
            if (failures.Count > 0)
            {
                EditorGUILayout.LabelField("Failures:", EditorStyles.boldLabel);
                _resultsScrollPos = EditorGUILayout.BeginScrollView(_resultsScrollPos, GUILayout.MaxHeight(100));

                foreach (var failure in failures)
                {
                    EditorGUILayout.LabelField($"- {failure.SourceObject?.name}: {failure.ErrorMessage}");
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Utility Methods

        private void DrawSectionHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        #endregion
    }
}
