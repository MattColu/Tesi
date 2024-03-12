using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Storage;
using UnityEngine;

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

    public static Task UploadRecording (byte[] recording, string extension, string trackName) {
        if (Instance == null) throw new NullReferenceException("Firebase was not set up");
        string savePath = $"{MenuOptions.Instance.Name} {MenuOptions.Instance.UID}/{trackName}.{extension}";
        string shortName = $"{MenuOptions.Instance.Name}/{trackName}.{extension}";
        return Instance.storageRef.Child(savePath)
            .PutBytesAsync(recording)
            .ContinueWith((Task<StorageMetadata> task) => {
                if (task.IsFaulted || task.IsCanceled) {
                    throw new Exception($"Failed to upload {shortName}: {task.Exception}");
                } else {
                    Debug.Log($"Successfully uploaded {shortName}: {task.Result.CreationTimeMillis}");
                }
            });
    }

    
}
