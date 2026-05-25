using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossUIManager : MonoBehaviour
{
    public static BossUIManager Instance;

    [Header("보스 컷신 UI")]
    public GameObject cutscenePanel;  // 전체를 덮을 검은/빨간 반투명 배경
    public Image bossImage;           // 보스 크게 보여줄 이미지
    public TextMeshProUGUI bossNameText;         // 보스 이름

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        cutscenePanel.SetActive(false); // 평소엔 숨겨둠
    }

    // 컷신 켜기
    public void ShowCutscene(EnemyData bossData)
    {
        if (bossData != null)
        {
            bossImage.sprite = bossData.enemySprite;
            bossNameText.text = "" + bossData.enemyName;
        }
        cutscenePanel.SetActive(true);
    }

    // 컷신 끄기
    public void HideCutscene()
    {
        cutscenePanel.SetActive(false);
    }
}