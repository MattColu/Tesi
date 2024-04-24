using System.Collections.Generic;
using KartGame.Custom.AI;
using UnityEditor;
using UnityEngine;

namespace KartGame.Custom.Training {   
    class DefaultEvaluationSettings: ScriptableObject {
        public const string k_EvaluationSettingsPath = "Assets/My Scripts/Editor/DefaultEvaluationSettings.asset";

        #pragma warning disable 0414
        
        [SerializeField]
        private ModelEvaluator m_DefaultEvaluator;
        [SerializeField]
        private KartAgent m_DefaultAgent;
        [SerializeField]
        private int m_DefaultNumberOfEvaluations;
        [SerializeField]
        private float m_DefaultTimescale;
        [SerializeField]
        private int m_DefaultSplitAmount;
        [SerializeField]
        private int m_DefaultSplitLength;

        #pragma warning restore 0414

        internal static DefaultEvaluationSettings GetOrCreateSettings() {
            var settings = AssetDatabase.LoadAssetAtPath<DefaultEvaluationSettings>(k_EvaluationSettingsPath);
            if (settings == null) {
                settings = CreateInstance<DefaultEvaluationSettings>();
                settings.m_DefaultEvaluator = null;
                settings.m_DefaultAgent = null;
                settings.m_DefaultNumberOfEvaluations = 10;
                settings.m_DefaultTimescale = 10f;
                settings.m_DefaultSplitAmount = 20;
                settings.m_DefaultSplitLength = 20;

                AssetDatabase.CreateAsset(settings, k_EvaluationSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        internal static SerializedObject GetSerializedSettings() {
            return new SerializedObject(GetOrCreateSettings());
        }
    }

    static class EvaluationSettingsProvider {
        [SettingsProvider]
        public static SettingsProvider CreateEvaluationSettingsProvider() {
            var provider = new SettingsProvider("Project/Evaluation", SettingsScope.Project) {
                label = "Evaluation Settings",
                guiHandler = (searchContext) => {
                    var settings = DefaultEvaluationSettings.GetSerializedSettings();
                    EditorGUILayout.ObjectField(settings.FindProperty("m_DefaultEvaluator"), new GUIContent("Default Evaluator Prefab"));
                    EditorGUILayout.ObjectField(settings.FindProperty("m_DefaultAgent"), new GUIContent("Default Agent Prefab"));
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("m_DefaultNumberOfEvaluations"), new GUIContent("Default Number of Evaluations"));
                    EditorGUILayout.PropertyField(settings.FindProperty("m_DefaultTimescale"), new GUIContent("Default Timescale"));
                    EditorGUILayout.PropertyField(settings.FindProperty("m_DefaultSplitAmount"), new GUIContent("Default Split Amount"));
                    EditorGUILayout.PropertyField(settings.FindProperty("m_DefaultSplitLength"), new GUIContent("Default Split Length (steps, 1 step = 20 ms)"));
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },
                keywords = new HashSet<string>(new[] { "Default Evaluator", "Default Agent", "Default Number of Evaluations", "Default Timescale", "Default Split Amount", "Default Split Length" })
            };
            return provider;
        }
    }
}