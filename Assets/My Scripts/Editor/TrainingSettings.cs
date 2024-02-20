using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KartGame.Custom.Training {   
    class TrainingSettings: ScriptableObject {
        public const string k_TrainingSettingsPath = "Assets/My Scripts/Editor/TrainingSettings.asset";

        [SerializeField]
        #pragma warning disable 0414
        private string m_CondaActivateScript;
        [SerializeField]
        private string m_DefaultTrainer;
        #pragma warning restore 0414

        internal static TrainingSettings GetOrCreateSettings() {
            var settings = AssetDatabase.LoadAssetAtPath<TrainingSettings>(k_TrainingSettingsPath);
            if (settings == null) {
                settings = CreateInstance<TrainingSettings>();
                settings.m_CondaActivateScript = "";
                settings.m_DefaultTrainer = "trainer.yaml";
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
            var provider = new SettingsProvider("Project/Training", SettingsScope.Project) {
                label = "Training Settings",
                guiHandler = (searchContext) => {
                    var settings = TrainingSettings.GetSerializedSettings();
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel("Conda start script");
                        EditorGUILayout.LabelField(settings.FindProperty("m_CondaActivateScript").stringValue);
                        SerializedProperty condaScript = settings.FindProperty("m_CondaActivateScript");
                        if (GUILayout.Button("Browse", GUILayout.MinWidth(10f))) {
                            condaScript.stringValue = EditorUtility.OpenFilePanel("Select conda start script", "C:/", "bat");
                        }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.PropertyField(settings.FindProperty("m_DefaultTrainer"), new GUIContent("Default Trainer File"));
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },
                keywords = new HashSet<string>(new[] { "Conda Activate Script", "Default Trainer File" })
            };
            return provider;
        }
    }
}