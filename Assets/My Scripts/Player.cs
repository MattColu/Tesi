using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using KartGame.KartSystems;
using Leguar.TotalJSON;

namespace KartGame.Custom.Demo
{
    public class Player<T> : MonoBehaviour
    {
        public string userFilename;
        protected bool doneReading = false;
        protected bool donePlaying = false;
        protected string filename;
        protected string filepath;
        protected string demoType = "";
        protected bool fromDisk = true;

        protected ArcadeKart kart;

        protected Queue<T> queue;

        protected virtual void Awake() {
            filepath = Application.persistentDataPath + "/demos/";
            if (!TryGetComponent<ArcadeKart>(out kart)) {
                Debug.LogError($"No ArcadeKart component attached to this object ({name})");
            }
        }

        protected virtual void Start() {
            if (fromDisk || userFilename != "") {
                ReadFromFileWrapper();
            }
            doneReading = true;
        }

        protected void FixedUpdate() {
            if (!doneReading) return;
            if (!donePlaying) {
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
            filename = userFilename;

            if (!CheckValidPath(filepath)) {
                return;
            }
            if (filename == "") {
                filename = Directory.EnumerateFileSystemEntries(filepath, $"*?.{demoType}").OrderByDescending(filename => File.GetLastWriteTime(filename)).First();
                filename = filename.Replace(filepath, "");
            }
            
            if (!filename.EndsWith($".{demoType}")) {
                filename += $".{demoType}";
            }
            if (!CheckValidFile(Path.Join(filepath, filename))) {
                return;
            }
            
            Debug.Log($"Reading from file {filename}...");
            
            ReadFromFile();

            Debug.Log("Done");
        }

        protected void ReadFromFile() {
            try {
                T[] data = JArray.ParseString(File.ReadAllText(Path.Join(filepath, filename))).Deserialize<T[]>();
                queue = new Queue<T>(data);
            } catch (ParseException e) {
                Debug.LogError($"Error parsing \"{filename}\": {e}");
            } catch (DeserializeException de) {
                Debug.LogError($"Error deserializing from JSON: {de}");
            }
        }

        public static T[] ReadFromFile(string fullpath) {
            T[] data;
            try {
                data = JArray.ParseString(File.ReadAllText(fullpath)).Deserialize<T[]>();
            } catch (ParseException e) {
                Debug.LogError($"Error parsing \"{fullpath}\": {e}");
                data = null;
            } catch (DeserializeException de) {
                Debug.LogError($"Error deserializing from JSON: {de}");
                data = null;
            }
            return data;
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