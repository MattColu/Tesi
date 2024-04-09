#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using KartGame.Custom.AI;
using UnityEditor;
using UnityEngine;

namespace KartGame.Custom.Training {
    [Serializable]
    public struct TrainingSettings {
        public Track track;
        public int trackInstances;
        public KartAgent agent;
        public int agentInstances;
        public string trainer;
        public string runId;

        public override readonly string ToString() {
            return $"Track: {track.name} - {trackInstances} instances\nAgent: {agent.name} - {agentInstances} instances\nTrainer:  {trainer}\nRunId: {runId}";
        }
    }

    [Serializable]
    public struct TrainingSession: IEnumerable<TrainingSettings> {
        public TrainingSettings[] settings;
        private string condaStartScript;

        public int Length {
            get {
                if (settings == null) {
                    return 0;
                } else {
                    return settings.Length;
                }
            }
        }

        public TrainingSettings this[int index] {
            get => settings[index];
            set => settings[index] = value;
        }

        public TrainingSession(TrainingSettings[] settings, string condaStartScript) {
            this.settings = settings;
            this.condaStartScript = condaStartScript;
        }

        public bool Check() {
            for (int i = 0; i < settings.Length; i++) {
                if (settings[i].agent == null) {
                    settings[i].agent = (KartAgent)DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_DefaultAgent").objectReferenceValue;
                }
                if (settings[i].trackInstances == 0) {
                    settings[i].trackInstances = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_DefaultTrackInstances").intValue;
                }
                if (settings[i].agentInstances == 0) {
                    settings[i].agentInstances = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_DefaultAgentInstances").intValue;
                }
                if (condaStartScript == "") {
                    condaStartScript = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_CondaActivateScript").stringValue;
                }
                if (settings[i].trainer == "") {
                    settings[i].trainer = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_DefaultTrainer").stringValue;
                }
                if (settings[i].track == null) throw new ArgumentNullException("Track");
                if (settings[i].agent == null) throw new ArgumentNullException("Agent");
                if (settings[i].trackInstances == 0) throw new ArgumentNullException("Track Instances");
                if (settings[i].agentInstances == 0) throw new ArgumentNullException("Agent Instances");
                if (settings[i].trainer == "") throw new ArgumentNullException("Trainer");
                if (settings[i].runId == "") throw new ArgumentNullException("RunID");
                if (condaStartScript == "") throw new ArgumentNullException("Conda activation script");
                if (!File.Exists(Path.Join($"{Directory.GetParent(Application.dataPath)}/Training/trainers", settings[i].trainer))) throw new FileNotFoundException(settings[i].trainer);
                if (!File.Exists(condaStartScript)) throw new FileNotFoundException(condaStartScript);
            }
            return true;
        }

        public string GetCondaScript() {
            if (condaStartScript == "") {
                condaStartScript = DefaultTrainingSettings.GetSerializedSettings().FindProperty("m_CondaActivateScript").stringValue;
            }
            return condaStartScript;
        }

        public static TrainingSession FromFile(string file) {
            TrainingSession s = new(null, "");
            object boxedS = s;                  //https://docs.unity3d.com/2023.2/Documentation/ScriptReference/EditorJsonUtility.FromJsonOverwrite.html
            try {
                EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(file), boxedS);
            } catch (Exception e) {
                Debug.LogError($"Error reading from \"{file}\": {e}");
            }
            return (TrainingSession)boxedS;
        }

        public readonly void ToFile(string file) {
            try {
                File.WriteAllText(file, EditorJsonUtility.ToJson(this));
            } catch (Exception e) {
                Debug.LogError($"Error writing to \"{file}\": {e}");
                return;
            }
        }

        public IEnumerator<TrainingSettings> GetEnumerator() {
            return ((IEnumerable<TrainingSettings>)settings).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public ref TrainingSettings ElementAt(int index)
        {
            throw new NotImplementedException();
        }
    }
}
#endif