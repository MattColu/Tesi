using System;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using KartGame.KartSystems;
using System.Runtime.Serialization.Formatters.Binary;

namespace KartGame.Custom.Demo
{
    public class Player<T> : MonoBehaviour
    {
        protected bool doneReading;
        protected bool donePlaying;
        [SerializeField]
        protected string filename;
        protected string demofolder;
        [SerializeField]
        protected string fullpath;
        protected string demoType = "";
        protected bool fromDisk = true;

        protected ArcadeKart kart;

        protected Queue<T> queue;

        protected virtual void Awake() {
            demofolder = Application.persistentDataPath + "/demos/";
            if (!TryGetComponent<ArcadeKart>(out kart)) {
                Debug.LogError($"No ArcadeKart component attached to this object ({name})");
            }
        }

        protected virtual void OnEnable() {
            doneReading = false;
            donePlaying = false;
            if (fromDisk || filename != "" || fullpath != "") {
                ReadFromFileWrapper();
            }
            doneReading = true;
        }

        protected virtual void Start() {}

        protected void FixedUpdate() {
            if (!doneReading) return;
            if (!donePlaying) {
                if (queue == null) return;
                if (queue.TryDequeue(out var queueElement)) {
                    ExecuteStep(queueElement);
                } else {
                    Cleanup();
                    donePlaying = true;
                    enabled = false;
                }
            }
        }

        protected virtual void ExecuteStep(T queueElement) {}

        protected virtual void Cleanup() {}
        
        protected void ReadFromFileWrapper() {
            if (fullpath == "") {
                if (!CheckValidPath(demofolder)) {
                    return;
                }

                if (filename == "") {
                    filename = Directory.EnumerateFileSystemEntries(demofolder, $"*?.{demoType}").OrderByDescending(filename => File.GetLastWriteTime(filename)).First();
                    filename = filename.Replace(demofolder, "");
                }
                
                if (!filename.EndsWith($".{demoType}")) {
                    filename += $".{demoType}";
                }

                fullpath = Path.Join(demofolder, filename);
            }

            if (!CheckValidFile(fullpath)) {
                return;
            }
            
            Debug.Log($"Reading from file {filename}...");
            
            int lines = ReadFromFile();
            Debug.Log($"Read {lines} timesteps");
        }

        protected int ReadFromFile() {
            try {
                queue = new Queue<T>(ReadFromFile(fullpath));
                return queue.Count;
            } catch (Exception e){
                Debug.LogError($"Error reading from \"{filename}\": {e}");
                return 0;
            }
        }

        public void SetFullpath(string path) {
            fullpath = path;
        }

        public static T[] ReadFromFile(string fullpath) {
            using(MemoryStream stream = new(Convert.FromBase64String(File.ReadAllText(fullpath)))) {
                return (T[]) new BinaryFormatter().Deserialize(stream);
            }
        }
        
        public static bool CheckValidPath(string filepath) {
            if (!Directory.Exists(filepath)) {
                Debug.LogError($"Directory {filepath} does not exist.");
                return false;
            }
            if (Directory.EnumerateFiles(filepath).Count() == 0) {
                Debug.LogWarning($"Directory {filepath} is empty.");
                return false;
            }
            return true;
        }

        public static bool CheckValidFile(string filepath) {
            if (File.Exists(filepath)) return true;
            Debug.LogError($"File \"{filepath}\" does not exist");
            return false;
        }

        public static bool CheckValidFile(string filepath, string extension) {
            return CheckValidFile(filepath) && filepath.EndsWith($".{extension}");
        }
    }
}