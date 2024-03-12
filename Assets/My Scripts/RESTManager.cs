using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class RESTManager : MonoBehaviour
{
    public static RESTManager Instance;

    private const string storageURL = "https://firebasestorage.googleapis.com/v0/b/tesi-10fe5.appspot.com/o/recordings";

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() {
        //StartCoroutine(CheckConnection());
    }

    IEnumerator CheckConnection() {
        using (UnityWebRequest www = UnityWebRequest.Get(storageURL)) {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"{www.error}");
            }
        }
    }

    public static IEnumerator UploadRecording (byte[] recording, string extension, string trackName) {
        if (Instance == null) throw new NullReferenceException("Firebase was not set up");
        string savePath = $"{MenuOptions.Instance.Name}_{MenuOptions.Instance.UID}%2F{trackName}.{extension}";
        string shortName = $"{MenuOptions.Instance.Name}/{trackName}.{extension}";
        using (UnityWebRequest www = UnityWebRequest.Post($"{storageURL}%2F{savePath}", Encoding.UTF8.GetString(recording), "application/octet-stream")) {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"Failed to upload {shortName}: {www.error}");
            } else {
                Debug.Log($"Successfully uploaded {shortName}: {www.result}");
            }
        }
    }
}
