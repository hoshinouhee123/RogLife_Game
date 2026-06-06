using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BGMSlotUI : MonoBehaviour
{
    public TextMeshProUGUI titleText; // BGM 이름
    public Image backgroundImage;     // 버튼 배경 (어둡게 만들 용도)
    public Button slotButton;         // 클릭 버튼

    private int myIndex;
    private SettingsBGMManager myManager;

    // 리스트가 생성될 때 세팅을 받습니다.
    public void Setup(int index, string bgmName, bool isUnlocked, SettingsBGMManager manager)
    {
        myIndex = index;
        myManager = manager;

        if (isUnlocked)
        {
            titleText.text = bgmName;
            titleText.color = Color.white;
            backgroundImage.color = new Color(1f, 1f, 1f, 1f); // 원래 밝기
            slotButton.interactable = true; // 클릭 가능!
        }
        else
        {
            titleText.text = "??? (잠김)";
            titleText.color = new Color(0.5f, 0.5f, 0.5f, 1f); // 회색 글씨
            backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 1f); // 배경 어둡게
            slotButton.interactable = false; // ★ 클릭 불가!
        }

        // 버튼 클릭 이벤트 연결
        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnClickSlot);
    }

    private void OnClickSlot()
    {
        // 매니저에게 나를 선택했다고 알림
        if (myManager != null) myManager.SelectBGM(myIndex);
    }
}