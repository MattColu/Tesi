using System.Collections.Generic;
using KartGame.Custom.AI;
using UnityEditor;
using UnityEngine;

namespace KartGame.Custom.Training {   
    class DefaultTrainingSettings: ScriptableObject {
        public const string k_TrainingSettingsPath = "Assets/My Scripts/Editor/DefaultTrainingSettings.asset";

        #pragma warning disable 0414
        
        [SerializeField]
        private string m_CondaActivateScript;
        [SerializeField]
        private string m_DefaultTrainer;
        [SerializeField]
        private int m_DefaultTrackInstances;
        [SerializeField]
        private int m_DefaultAgentInstances;
        [SerializeField]
        private KartAgent m_DefaultAgent;

        #pragma warning restore 0414

        internal static DefaultTrainingSettings GetOrCreateSettings() {
            var settings = AssetDatabase.LoadAssetAtPath<DefaultTrainingSettings>(k_TrainingSettingsPath);
            if (settings == null) {
                settings = CreateInstance<DefaultTrainingSettings>();
                settings.m_CondaActivateScript = "";
                settings.m_DefaultTrainer = "trainer.yaml";
                settings.m_DefaultTrackInstances = 0;
                settings.m_DefaultAgentInstances = 0;
                settings.m_DefaultAgent = null;
                AssetDatabase.CreateAsset(settings, k_TrainingSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        internal static SerializedObject GetSerializedSettings() {
            return new SerializedObject(GetOrCreateSettings());
        }
    }

    static class TrainingSettingsProvider {
        [SettingsProvider]
        public static SettingsProvider CreateTrainingSettingsProvider() {
            var provider = new SettingsProvider("Project/Kart Settings/Training Settings", SettingsScope.Project) {
                label = "Training Settings",
                guiHandler = (searchContext) => {
                    var settings = DefaultTrainingSettings.GetSerializedSettings();
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel("Conda start script");
                        EditorGUILayout.LabelField(settings.FindProperty("m_CondaActivateScript").stringValue);
                        SerializedProperty condaScript = settings.FindProperty("m_CondaActivateScript");
                        if (GUILayout.Button("Browse", GUILayout.MinWidth(10f))) {
                            condaScript.stringValue = EditorUtility.OpenFilePanel("Select conda start script", "C:/", "bat");
                        }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.PropertyField(settings.FindProperty("m_DefaultTrainer"), new GUIContent("Default Trainer File"));
                    EditorGUILayout.PropertyField(settings.FindProperty("m_DefaultTrackInstances"), new GUIContent("Default Track Instance Count"));
                    EditorGUILayout.PropertyField(settings.FindProperty("m_DefaultAgentInstances"), new GUIContent("Default Agent Instance Count"));
                    EditorGUILayout.ObjectField(settings.FindProperty("m_DefaultAgent"), new GUIContent("Default Agent Prefab"));
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },
                keywords = new HashSet<string>(new[] { "Conda Activate Script", "Default Trainer File", "Default Track Instance Count", "Default Agent Instance Count", "Default Agent Prefab" })
            };
            return provider;
        }
    }
}