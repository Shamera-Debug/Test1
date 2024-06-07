using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;  // TextMeshPro를 사용하기 위해 추가

public class MainMenu : MonoBehaviour
{
    public Button tradeButton;  // Button 타입으로 변경
    public Button addItemButton;  // 랜덤 아이템 추가 버튼
    public TMP_Text goldText;   // 골드를 표시할 TextMeshPro 텍스트

    void Start()
    {
        // tradeButton의 onClick 이벤트 설정
        tradeButton.onClick.AddListener(OpenTradingPost);

        // addItemButton의 onClick 이벤트 설정
        addItemButton.onClick.AddListener(AddRandomItem);

        // 유저의 골드를 가져와서 텍스트로 표시
        UpdateGoldText();
    }

    void OpenTradingPost()
    {
        Debug.Log("Trading Post button clicked!");  // 디버깅 메시지 추가
        SceneManager.LoadScene("TradingPostScene");
    }

    void UpdateGoldText()
    {
        // GameManager 인스턴스를 통해 유저의 골드를 가져와 표시
        goldText.text = "Gold: " + GameManager.Instance.Gold.ToString();
    }

    void AddRandomItem()
    {
        // 랜덤 아이템 생성
        var newItem = CreateRandomItem();

        // GameManager의 인벤토리에 아이템 추가
        GameManager.Instance.Inventory.Add(newItem);

        // GameManager를 통해 유저 데이터를 DB에 저장
        GameManager.Instance.SaveUserInventory();

        Debug.Log("Random item added to inventory.");
    }

    ItemData CreateRandomItem()
    {
        var item = ScriptableObject.CreateInstance<ItemData>();
        item.itemName = "Random Item " + Random.Range(1, 100);
        item.attack = Random.Range(1, 20);
        item.speed = Random.Range(1, 20);
        item.defense = Random.Range(1, 20);
        return item;
    }
}
