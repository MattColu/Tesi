using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Firebase;
using Firebase.Storage;
using KartGame.Custom.Demo;
using Leguar.TotalJSON;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirebaseObject : MonoBehaviour
{
    public static FirebaseObject Instance;

    private StorageReference storageRef;

    void Awake() {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        DependencyStatus dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (dependencyStatus == DependencyStatus.Available) {
            Debug.Log("Firebase connection established");
            storageRef = FirebaseStorage.DefaultInstance.RootReference.Child("recordings");
        }
    }

    public static async void UploadRecording (byte[] recording, string extension) {
        string currentTrack = SceneManager.GetActiveScene().name;
        if (Instance == null) throw new NullReferenceException("Firebase was not set up");
        string savePath = $"{MenuOptions.Instance.Name} {MenuOptions.Instance.UID}/{currentTrack}.{extension}";
        string shortName = $"{MenuOptions.Instance.Name}/{currentTrack}.{extension}";
        await Instance.storageRef.Child(savePath)
            .PutBytesAsync(recording)
            .ContinueWith((Task<StorageMetadata> task) => {
                if (task.IsFaulted || task.IsCanceled) {
                    throw new Exception($"Failed to upload {shortName}: {task.Exception}");
                } else {
                    Debug.Log($"Successfully uploaded {shortName}: {task.Result.CreationTimeMillis} ms");
                }
            });
    }
    
}
