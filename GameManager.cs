using System;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public FirebaseUser CurrentUser { get; private set; }
    public int Gold { get; set; }
    public List<ItemData> Inventory { get; set; } // set 접근자를 public으로 변경

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameManager Awake: Initialized.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeFirebase()
    {
        Debug.Log("Initializing Firebase...");
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        auth.StateChanged += OnAuthStateChanged;
        OnAuthStateChanged(this, null);
    }

    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        if (auth.CurrentUser != CurrentUser)
        {
            bool signedIn = CurrentUser != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && CurrentUser != null)
            {
                Debug.Log("Signed out " + CurrentUser.UserId);
            }
            CurrentUser = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + CurrentUser.UserId);
                LoadUserData();
            }
        }
    }

    public void LoadUserData()
    {
        if (CurrentUser == null)
        {
            Debug.LogError("CurrentUser is null");
            return;
        }

        var userDocRef = db.Collection("users").Document(CurrentUser.UserId);
        userDocRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                var userData = task.Result;
                Gold = userData.GetValue<int>("gold");
                LoadUserInventory();
            }
            else
            {
                Debug.LogError("Failed to load user data: " + task.Exception);
            }
        });
    }

    private void LoadUserInventory()
    {
        if (CurrentUser == null) return;

        var inventoryRef = db.Collection("users").Document(CurrentUser.UserId).Collection("inventory");
        inventoryRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Inventory = new List<ItemData>();
                foreach (var document in task.Result.Documents)
                {
                    var item = ScriptableObject.CreateInstance<ItemData>();
                    item.itemName = document.GetValue<string>("itemName");
                    item.attack = document.GetValue<int>("attack");
                    item.speed = document.GetValue<int>("speed");
                    item.defense = document.GetValue<int>("defense");
                    Inventory.Add(item);
                }
            }
            else
            {
                Debug.LogError("Failed to load user inventory: " + task.Exception);
            }
        });
    }

    public void SaveUserData()
    {
        if (CurrentUser == null) return;

        var userDocRef = db.Collection("users").Document(CurrentUser.UserId);
        userDocRef.UpdateAsync(new Dictionary<string, object> { { "gold", Gold } });
    }

    public void SaveUserInventory()
    {
        if (CurrentUser == null) return;

        var inventoryRef = db.Collection("users").Document(CurrentUser.UserId).Collection("inventory");

        // 기존 인벤토리를 초기화
        inventoryRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                var snapshot = task.Result;
                foreach (var document in snapshot.Documents)
                {
                    inventoryRef.Document(document.Id).DeleteAsync();
                }

                // 새로운 인벤토리 저장
                foreach (var item in Inventory)
                {
                    var itemData = new Dictionary<string, object>
                    {
                        { "itemName", item.itemName },
                        { "attack", item.attack },
                        { "speed", item.speed },
                        { "defense", item.defense }
                    };
                    inventoryRef.AddAsync(itemData);
                }
            }
            else
            {
                Debug.LogError("Failed to clear inventory: " + task.Exception);
            }
        });
    }
}
