using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button tradeButton;  // Button 타입으로 변경

    void Start()
    {
        // tradeButton의 onClick 이벤트 설정
        tradeButton.onClick.AddListener(OpenTradingPost);
    }

    void OpenTradingPost()
    {
        Debug.Log("Trading Post button clicked!");  // 디버깅 메시지 추가
        SceneManager.LoadScene("TradingPostScene");
    }
}
