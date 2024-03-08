using System;
using System.IO;
using UnityEngine;
using KartGame.KartSystems;
using System.Collections.Generic;
using Leguar.TotalJSON;

namespace KartGame.Custom.Demo
{
    public class Recorder<T> : MonoBehaviour
    {
        public string userFilename;

        [Tooltip("Number of Fixed Timesteps to record (0 records until the editor is stopped)")]
        public float DemoDuration;
        public bool toDisk;

        protected int recordCounter;
        protected string filepath;
        protected string filename;
        protected DateTime startTime; 
        protected string demoType = "";

        protected ArcadeKart kart;
        
        protected Queue<T> queue;

        public event Action<Queue<T>> OnWriteQueue;

        protected virtual void Awake() {
            if (!TryGetComponent<ArcadeKart>(out kart)) {
                Debug.LogError($"No ArcadeKart component attached to this object ({name})");
            }

            queue = new();
        }

        protected virtual void Start()
        {
            startTime = DateTime.Now;
        }

        protected void OnEnable() {
            recordCounter = 0;
        }
        
        protected void OnDisable() {
            if (toDisk) {
                WriteToFileWrapper();
            } else {
                Write();
            }
        }

        protected virtual void FixedUpdate() {
            if (DemoDuration != 0 && recordCounter >= DemoDuration) {
                enabled = false;
                return;
            }
            recordCounter++;
        }

        protected virtual void Record(T queueElement) {
            queue.Enqueue(queueElement);
        }

        protected void WriteToFileWrapper() {
            filename = userFilename;
            filepath = Application.persistentDataPath + "/demos/";
            bool alreadyExists = false;

            if (File.Exists(Path.Combine(filepath, filename)) || File.Exists(Path.Combine(filepath, filename+$".{demoType}"))) {
                Debug.LogWarning($"File \"{filename}\" already exists");
                alreadyExists = true;
            }

            if (!Directory.Exists(filepath)) {
                Debug.LogWarning($"Folder {filepath} does not exist, it will be created.");
                Directory.CreateDirectory(filepath);
                Debug.Log("Done");
            }

            if (filename == "") {
                filename = startTime.ToString("yyyyMMdd'-'HHmmss") + $".{demoType}";
            }

            if (!filename.EndsWith($".{demoType}")) {
                if (alreadyExists) filename += "_";
                filename += $".{demoType}";
            } else {
                filename = filename.Split($".{demoType}")[0] + "_" + $".{demoType}";
            }

            Write();
            Debug.Log("Done");
        }

        protected virtual async void Write() {
            if (queue == null || queue.Count == 0) return;
            if (toDisk) {
                try {
                    await File.WriteAllTextAsync(Path.Join(filepath, filename), JArray.Serialize(queue.ToArray()).CreateString());
                } catch (SerializeException se) {
                    Debug.LogError($"Error serializing to JSON: {se}");
                    return;
                } catch (Exception e) {
                    Debug.LogError($"Error writing to \"{filename}\": {e}");
                    return;
                }
            }
            OnWriteQueue?.Invoke(new(queue));
            queue.Clear();
        }

        public static byte[] ConvertToByteArray(Queue<T> queue) {
            return System.Text.Encoding.UTF8.GetBytes(JArray.Serialize(queue.ToArray()).CreateString());
        }
    }
}