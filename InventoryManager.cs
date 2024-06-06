using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Functions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public GameObject userInventoryPanel;
    public GameObject marketInventoryPanel;
    public GameObject itemDetailPanel;
    public TMP_InputField priceInputField;
    public Button registerButton;
    public Button closeButton;
    public Button refreshButton;
    public TMP_Text totalGoldText;

    public TMP_Text itemNameText;
    public TMP_Text itemAttackText;
    public TMP_Text itemSpeedText;
    public TMP_Text itemDefenseText;

    public List<ItemData> userItems; // ScriptableObject 리스트

    private Dictionary<string, ItemForSale> marketItems = new Dictionary<string, ItemForSale>();
    private ItemData selectedItem;
    private int totalGold;

    void Start()
    {
        Debug.Log("Start method called.");
        registerButton.onClick.AddListener(RegisterItem);
        closeButton.onClick.AddListener(CloseItemDetailPanel);
        refreshButton.onClick.AddListener(LoadMarketItems);

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            Debug.Log("Checking Firebase dependencies.");
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("Firebase dependencies are available.");
                FirebaseAuth.DefaultInstance.StateChanged += HandleAuthStateChanged;
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance; // Firestore 초기화

                if (FirebaseAuth.DefaultInstance.CurrentUser != null)
                {
                    Debug.Log("User is already logged in.");
                    Debug.Log("USER ID : " + FirebaseAuth.DefaultInstance.CurrentUser.UserId);
                    LoadUserGameInfo();
                }
                else
                {
                    Debug.Log("User is not logged in.");
                    // SceneManager.LoadScene("LoginScene"); // 다시 로그인 화면으로 이동
                }
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {task.Exception}");
            }
        });
    }

    private void HandleAuthStateChanged(object sender, EventArgs e)
    {
        Debug.Log("Auth state changed.");
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            Debug.Log("User is logged in.");
            Debug.Log("USER ID : " + FirebaseAuth.DefaultInstance.CurrentUser.UserId);
            LoadUserGameInfo();
        }
        else
        {
            Debug.Log("User is logged out.");
            // SceneManager.LoadScene("AuthScene"); // 다시 로그인 화면으로 이동
        }
    }

    private void LoadUserGameInfo()
    {
        try
        {
            Debug.Log("Loading user game info.");
            var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            var userDocRef = FirebaseFirestore.DefaultInstance.Collection("users").Document(userId);
            Debug.Log("USER ID : " + userId);

            userDocRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    var userData = task.Result;
                    totalGold = userData.GetValue<int>("gold");
                    UpdateTotalGoldText();
                    LoadUserInventory();
                }
                else
                {
                    Debug.LogError("Failed to load user data: " + task.Exception);
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception in LoadUserGameInfo: " + ex);
        }
    }

    private void LoadUserInventory()
    {
        try
        {
            Debug.Log("Loading user inventory.");
            var userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            var inventoryRef = FirebaseFirestore.DefaultInstance.Collection("users").Document(userId).Collection("inventory");

            inventoryRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    foreach (Transform child in userInventoryPanel.transform)
                    {
                        Destroy(child.gameObject);
                    }

                    var snapshot = task.Result;
                    userItems.Clear();
                    foreach (var document in snapshot.Documents)
                    {
                        var item = ScriptableObject.CreateInstance<ItemData>();
                        item.itemName = document.GetValue<string>("itemName");
                        item.attack = document.GetValue<int>("attack");
                        item.speed = document.GetValue<int>("speed");
                        item.defense = document.GetValue<int>("defense");
                        userItems.Add(item);

                        var itemButton = new GameObject(item.itemName).AddComponent<Button>();
                        var text = itemButton.gameObject.AddComponent<TextMeshProUGUI>();
                        text.text = item.itemName;
                        itemButton.onClick.AddListener(() => OpenItemDetailPanel(item));
                        itemButton.transform.SetParent(userInventoryPanel.transform);
                    }
                }
                else
                {
                    Debug.LogError("Failed to load user inventory: " + task.Exception);
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception in LoadUserInventory: " + ex);
        }
    }

    private void SaveUserGameInfo()
    {
        // GameManager의 데이터를 데이터베이스에 저장
        GameManager.Instance.SaveUserData();
        GameManager.Instance.SaveUserInventory();
    }

    void PopulateMarketInventory()
    {
        Debug.Log("Populating Market Inventory");
        foreach (Transform child in marketInventoryPanel.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (var kvp in marketItems)
        {
            var itemForSale = kvp.Value;

            // 아이템 패널 생성
            var itemPanel = new GameObject(itemForSale.item.itemName).AddComponent<HorizontalLayoutGroup>();

            // 아이템 이름 텍스트 추가
            var itemNameText = new GameObject("ItemName").AddComponent<TextMeshProUGUI>();
            itemNameText.text = $"{itemForSale.item.itemName} - {itemForSale.price} Gold";
            itemNameText.transform.SetParent(itemPanel.transform);

            // 구매 버튼 추가
            var purchaseButton = new GameObject("PurchaseButton").AddComponent<Button>();
            var buttonText = purchaseButton.gameObject.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Purchase";
            string itemKey = kvp.Key; // 아이템 키 저장
            purchaseButton.onClick.AddListener(() => BuyItem(itemKey));
            purchaseButton.transform.SetParent(itemPanel.transform);

            // 패널을 marketInventoryPanel에 추가
            itemPanel.transform.SetParent(marketInventoryPanel.transform);
        }
    }

    void OpenItemDetailPanel(ItemData item)
    {
        selectedItem = item;
        itemDetailPanel.SetActive(true);

        itemNameText.text = $"Name: {item.itemName}";
        itemAttackText.text = $"Attack: {item.attack}";
        itemSpeedText.text = $"Speed: {item.speed}";
        itemDefenseText.text = $"Defense: {item.defense}";
        Debug.Log($"Item detail panel opened for {item.itemName}");
    }

    void RegisterItem()
    {
        int price;
        if (selectedItem == null)
        {
            Debug.LogError("No item selected for registration.");
            return;
        }

        if (int.TryParse(priceInputField.text, out price))
        {
            var data = new Dictionary<string, object>
            {
                { "itemName", selectedItem.itemName },
                { "attack", selectedItem.attack },
                { "speed", selectedItem.speed },
                { "defense", selectedItem.defense },
                { "price", price }
            };

            FirebaseFunctions.DefaultInstance
                .GetHttpsCallable("registerItem")
                .CallAsync(data)
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsCompleted)
                    {
                        if (task.Exception == null)
                        {
                            Debug.Log("Item successfully registered.");
                            userItems.Remove(selectedItem); // 사용자의 인벤토리에서 아이템 제거
                            GameManager.Instance.Inventory = userItems; // GameManager의 인벤토리 업데이트
                            SaveUserGameInfo(); // 데이터베이스에 저장
                            LoadUserInventory(); // 사용자 인벤토리 UI 업데이트
                            itemDetailPanel.SetActive(false);
                            LoadMarketItems(); // MarketItems 재로드하여 업데이트된 데이터 반영
                        }
                        else
                        {
                            Debug.LogError("Failed to register item: " + task.Exception);
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to register item: " + task.Exception);
                    }
                });
        }
        else
        {
            Debug.LogError("Price input is not a valid number.");
        }
    }

    void CloseItemDetailPanel()
    {
        itemDetailPanel.SetActive(false);
        Debug.Log("Item detail panel closed");
    }

    void LoadMarketItems()
    {
        try
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            if (db == null)
            {
                Debug.LogError("FirebaseFirestore instance is null.");
                return;
            }

            db.Collection("marketItems").GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    if (task.Exception != null)
                    {
                        Debug.LogError("Error loading market items: " + task.Exception);
                        return;
                    }

                    var snapshot = task.Result;
                    if (snapshot == null)
                    {
                        Debug.LogError("Snapshot is null.");
                        return;
                    }
                    
                    marketItems.Clear();
                    foreach (var document in snapshot.Documents)
                    {
                        Debug.Log("Document data: " + document.ToDictionary());
                        try
                        {
                            // Firestore 문서에서 데이터를 추출하여 ItemForSale 객체를 생성합니다.
                            var itemForSale = new ItemForSale
                            {
                                item = ScriptableObject.CreateInstance<ItemData>(),
                                price = document.GetValue<int>("price"),
                                sellerId = document.GetValue<string>("sellerId")
                            };

                            itemForSale.item.itemName = document.GetValue<string>("item.itemName");
                            itemForSale.item.attack = document.GetValue<int>("item.attack");
                            itemForSale.item.speed = document.GetValue<int>("item.speed");
                            itemForSale.item.defense = document.GetValue<int>("item.defense");

                            Debug.Log("itemForSale: " + itemForSale.item.itemName);

                            marketItems.Add(document.Id, itemForSale);
                            Debug.Log("marketItems count: " + marketItems.Count);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("Error converting document to ItemForSale: " + e);
                        }
                    }
                    PopulateMarketInventory(); // UI 업데이트
                }
                else
                {
                    Debug.LogError("Failed to load market items: " + task.Exception);
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError("Exception in LoadMarketItems: " + e);
        }
    }

    void BuyItem(string itemId)
    {
        var data = new Dictionary<string, object>
        {
            { "itemId", itemId }
        };

        FirebaseFunctions.DefaultInstance
            .GetHttpsCallable("buyItem")
            .CallAsync(data)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    if (task.Exception == null)
                    {
                        var result = (IDictionary<string, object>)task.Result.Data;
                        if (result != null && result.ContainsKey("newGold"))
                        {
                            totalGold = Convert.ToInt32(result["newGold"]);
                            UpdateTotalGoldText();
                            GameManager.Instance.Gold = totalGold; // GameManager의 골드 업데이트
                        }

                        Debug.Log("Item successfully purchased.");
                        GameManager.Instance.LoadUserData(); // 유저 데이터 다시 로드
                        SaveUserGameInfo(); // 데이터베이스에 저장
                    }
                    else
                    {
                        Debug.LogError("Failed to purchase item: " + task.Exception);
                    }
                }
                else
                {
                    Debug.LogError("Failed to purchase item: " + task.Exception);
                }
            });
    }


    void UpdateTotalGoldText()
    {
        totalGoldText.text = $"Gold: {totalGold}";
        Debug.Log($"Total gold updated: {totalGold}");
    }
}
