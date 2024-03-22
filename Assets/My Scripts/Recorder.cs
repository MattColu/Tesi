using System;
using System.IO;
using UnityEngine;
using KartGame.KartSystems;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace KartGame.Custom.Demo
{
    public class Recorder<T> : MonoBehaviour
    {
        [Tooltip("Number of Fixed Timesteps to record (0 records until the editor is stopped)")]
        public float DemoDuration;
        public bool toDisk;

        protected int recordCounter;
        protected string demofolder;
        [SerializeField]
        public string filename;
        [SerializeField]
        protected string fullpath;
        protected DateTime startTime; 
        protected string demoType = "";

        protected ArcadeKart kart;
        
        protected Queue<T> queue;

        public event Action<Queue<T>> OnWriteQueue;

        protected virtual void Awake() {
            if (!TryGetComponent<ArcadeKart>(out kart)) {
                Debug.LogError($"No ArcadeKart component attached to this object ({name})");
            }
        }

        protected void OnEnable() {
            demofolder = Application.persistentDataPath + "/demos/";
            recordCounter = 0;
            queue = new();
            startTime = DateTime.Now;
        }
        
        protected void OnDisable() {
            Write();
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

        protected void SetupWriteToFile() {
            if (fullpath != "") {
                demofolder = Path.GetDirectoryName(fullpath);
                filename = Path.GetFileNameWithoutExtension(fullpath);
            }
            
            if (!Directory.Exists(demofolder)) {
                Debug.LogWarning($"Folder {demofolder} does not exist, it will be created.");
                Directory.CreateDirectory(demofolder);
            }

            filename += startTime.ToString("yyyyMMdd'-'HHmmss") + $".{demoType}";

            fullpath = Path.Combine(demofolder, filename);
        }

        protected virtual async void Write() {
            if (queue == null || queue.Count == 0) return;
            if (toDisk) {
                SetupWriteToFile();
                try {
                    await File.WriteAllTextAsync(fullpath, Convert.ToBase64String(ToByteArray(queue)));
                } catch (Exception e) {
                    Debug.LogError($"Error writing to \"{filename}\": {e}");
                    return;
                }
            }
            OnWriteQueue?.Invoke(new(queue));
            queue.Clear();
        }

        public void Split() {
            OnDisable();
            OnEnable();
        }

        public static byte[] ToByteArray(Queue<T> queue) {
            using (MemoryStream stream = new()) {
                new BinaryFormatter().Serialize(stream, queue.ToArray());
                return stream.ToArray();
            }
        }
    }
}