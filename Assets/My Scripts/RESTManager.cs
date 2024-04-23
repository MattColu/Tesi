using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class RESTManager : MonoBehaviour {
    public static RESTManager Instance;

    private const string storageURL = "https://firebasestorage.googleapis.com/v0/b/tesi-10fe5.appspot.com/o";

    private const string recordingURL = storageURL + "/recordings";
    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static IEnumerator UploadRecording (byte[] recording, string extension, string trackName, int lap = -1) {
        string savePath;
        string shortName;
        if (lap == -1) {
            savePath = $"{MenuOptions.Instance.Name}_{MenuOptions.Instance.UID}%2F{trackName}.{extension}";
            shortName = $"{MenuOptions.Instance.Name}/{trackName}.{extension}";
        } else {
            savePath = $"{MenuOptions.Instance.Name}_{MenuOptions.Instance.UID}%2F{trackName}%2F{trackName}-{lap}.{extension}";
            shortName = $"{MenuOptions.Instance.Name}/{trackName}/{lap}.{extension}";
        }
        Task<string> t = Task.Run(() => {return Convert.ToBase64String(recording);});
        yield return new WaitUntil(() => t.IsCompleted);
        using (UnityWebRequest www = UnityWebRequest.Post($"{recordingURL}%2F{savePath}", t.Result, "application/octet-stream")) {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"Failed to upload {shortName}: {www.error}");
            } else {
                Debug.Log($"Successfully uploaded {shortName}: {www.result}");
            }
        }
    }

    public static IEnumerator GetAndWrite(string serverPath, string localPath, bool binary) {
        using (UnityWebRequest www = UnityWebRequest.Get($"{storageURL}/{serverPath}")) {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"Failed to retrieve {serverPath}: {www.error}");
            } else {
                string localFolder = Directory.GetParent(localPath).FullName;
                if (!Directory.Exists(localFolder)) {
                    Directory.CreateDirectory(localFolder);
                    Debug.Log($"Created folder {localFolder}");
                }
                using (FileStream fs = File.Create(localPath)) {
                    if (binary) {
                        fs.Write(Convert.FromBase64String(www.downloadHandler.text));
                    } else {
                        fs.Write(www.downloadHandler.data);
                    }
                }
            }
        }
    }
}
