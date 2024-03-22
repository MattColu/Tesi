#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using KartGame.Custom.AI;
using Leguar.TotalJSON;
using UnityEditor;
using UnityEngine;

namespace KartGame.Custom {
    [Serializable]
    public struct TrainingSettings {
        public Track track;
        public int trackInstances;
        public KartAgent agent;
        public int agentInstances;
        public string trainer;
        public string runId;

        public TrainingSettings(SerializableTrainingSettings settings) {
            track = AssetDatabase.LoadAssetAtPath<Track>(settings.trackPath);
            trackInstances = settings.trackInstances;
            agent = AssetDatabase.LoadAssetAtPath<KartAgent>(settings.agentPath);
            agentInstances = settings.agentInstances;
            trainer = settings.trainer;
            runId = settings.runId;
        }
    }

    [Serializable]
    public struct SerializableTrainingSettings {
        public string trackPath;
        public int trackInstances;
        public string agentPath;
        public int agentInstances;
        public string trainer;
        public string runId;

        public SerializableTrainingSettings(string trackPath, int trackInstances, string agentPath, int agentInstances, string trainer, string runId) {
            this.trackPath = trackPath;
            this.trackInstances = trackInstances;
            this.agentPath = agentPath;
            this.agentInstances = agentInstances;
            this.trainer = trainer;
            this.runId = runId;
        }

        public SerializableTrainingSettings(TrainingSettings settings) {
            this.trackPath = AssetDatabase.GetAssetPath(settings.track);
            this.trackInstances = settings.trackInstances;
            this.agentPath = AssetDatabase.GetAssetPath(settings.agent);
            this.agentInstances = settings.agentInstances;
            this.trainer = settings.trainer;
            this.runId = settings.runId;
        }
    }
    public class TrainingSession {
        private TrainingSettings[] trainingSettings;

        public TrainingSession(TrainingSettings settings) {
            trainingSettings = new TrainingSettings[1];
            trainingSettings[0] = settings;
        }

        public TrainingSession(TrainingSettings[] settings) {
            trainingSettings = (TrainingSettings[])settings.Clone();
        }

        public static TrainingSettings[] FromFile(string file) {
            SerializableTrainingSettings[] settings;
            try {
                settings = JArray.ParseString(File.ReadAllText(file)).Deserialize<SerializableTrainingSettings[]>();
            } catch (ParseException e) {
                Debug.LogError($"Error parsing \"{file}\": {e}");
                settings = null;
            } catch (DeserializeException de) {
                Debug.LogError($"Error deserializing from JSON: {de}");
                settings = null;
            }
            
            return settings.Select(ser => new TrainingSettings(ser)).ToArray();
        }

        public async void SaveToFile(string file) {
            try {
                await File.WriteAllTextAsync(file, JArray.Serialize(trainingSettings.Select(notSer => new SerializableTrainingSettings(notSer)).ToArray()).CreateString());
            } catch (SerializeException se) {
                Debug.LogError($"Error serializing to JSON: {se}");
                return;
            } catch (Exception e) {
                Debug.LogError($"Error writing to \"{file}\": {e}");
                return;
            }
        }
    }
}
#endif