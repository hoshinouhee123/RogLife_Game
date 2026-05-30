using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuAchievementUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject achievementPanel; // 도감 창 패널
    public Transform contentParent;     // 슬롯들이 생성될 위치 (Scroll View -> Content)
    public GameObject achievementSlotPrefab; // 도감 슬롯 프리팹

    public void OpenAchievementPanel()
    {
        achievementPanel.SetActive(true);
        RefreshList();
    }

    public void CloseAchievementPanel()
    {
        achievementPanel.SetActive(false);
    }

    private void RefreshList()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (AchievementManager.Instance == null) return;

        foreach (AchievementInfo info in AchievementManager.Instance.achievementDatabase)
        {
            GameObject slotObj = Instantiate(achievementSlotPrefab, contentParent);

            Transform iconTr = slotObj.transform.Find("Icon");
            Transform titleTr = slotObj.transform.Find("Title");
            Transform descTr = slotObj.transform.Find("Desc");
            Transform lockTr = slotObj.transform.Find("LockOverlay");

            if (iconTr == null || titleTr == null || descTr == null || lockTr == null) continue;

            Image iconImg = iconTr.GetComponent<Image>();
            GameObject lockOverlay = lockTr.gameObject;

            // ★ [수정됨] 일반 Text인지 TextMeshPro인지 둘 다 확인해서 안전하게 글씨를 바꿉니다!
            Text titleTxt = titleTr.GetComponent<Text>();
            TextMeshProUGUI titleTMP = titleTr.GetComponent<TextMeshProUGUI>();

            Text descTxt = descTr.GetComponent<Text>();
            TextMeshProUGUI descTMP = descTr.GetComponent<TextMeshProUGUI>();

            bool isUnlocked = AchievementManager.Instance.IsUnlocked(info.id);

            // 넣을 글씨 결정
            string targetTitle = isUnlocked ? info.title : "???";
            string targetDesc = isUnlocked ? info.description : "조건이 밝혀지지 않았습니다.";

            // 일반 텍스트면 일반 텍스트 변경
            if (titleTxt != null) titleTxt.text = targetTitle;
            if (descTxt != null) descTxt.text = targetDesc;

            // TextMeshPro면 TextMeshPro 변경
            if (titleTMP != null) titleTMP.text = targetTitle;
            if (descTMP != null) descTMP.text = targetDesc;

            // 아이콘과 자물쇠 세팅
            iconImg.sprite = info.icon;
            lockOverlay.SetActive(!isUnlocked);
        }
    }
}