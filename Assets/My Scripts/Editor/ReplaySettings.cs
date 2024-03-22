using System.Collections.Generic;
using KartGame.KartSystems;
using UnityEditor;
using UnityEngine;

namespace KartGame.Custom.Demo {   
    class ReplaySettings: ScriptableObject {
        public const string k_ReplaySettingsPath = "Assets/My Scripts/Editor/ReplaySettings.asset";

        [SerializeField]
        #pragma warning disable 0414
        private string m_TrackFolder;
        [SerializeField]
        private ArcadeKart m_DefaultKart;
        #pragma warning restore 0414

        internal static ReplaySettings GetOrCreateSettings() {
            var settings = AssetDatabase.LoadAssetAtPath<ReplaySettings>(k_ReplaySettingsPath);
            if (settings == null) {
                settings = CreateInstance<ReplaySettings>();
                settings.m_TrackFolder = "";
                settings.m_DefaultKart = null;
                AssetDatabase.CreateAsset(settings, k_ReplaySettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        internal static SerializedObject GetSerializedSettings() {
            return new SerializedObject(GetOrCreateSettings());
        }
    }

    static class ReplaySettingsProvider {
        [SettingsProvider]
        public static SettingsProvider CreateReplaySettingsProvider() {
            var provider = new SettingsProvider("Project/Replay", SettingsScope.Project) {
                label = "Replay Settings",
                guiHandler = (searchContext) => {
                    var settings = ReplaySettings.GetSerializedSettings();
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel("Track Scene Folder");
                        EditorGUILayout.LabelField(settings.FindProperty("m_TrackFolder").stringValue);
                        SerializedProperty trackFolder = settings.FindProperty("m_TrackFolder");
                        if (GUILayout.Button("Browse", GUILayout.MinWidth(10f))) {
                            trackFolder.stringValue = EditorUtility.OpenFolderPanel("Select Track scene folder", "", "");
                        }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.PropertyField(settings.FindProperty("m_DefaultKart"), new GUIContent("Default Kart Prefab"));
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },
                keywords = new HashSet<string>(new[] { "Track Scene Folder", "Default Kart Prefab" })
            };
            return provider;
        }
    }
}