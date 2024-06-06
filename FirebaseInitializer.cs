using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Functions;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    public static FirebaseAuth auth;
    public static DatabaseReference databaseReference;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                InitializeFirebase();
                FirebaseFunctions functions = FirebaseFunctions.DefaultInstance;
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {task.Exception}");
            }
        });
    }

    void InitializeFirebase()
    {
        FirebaseApp app = FirebaseApp.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        Debug.Log("Firebase Initialized");
    }
}
