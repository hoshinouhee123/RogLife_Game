using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuShopUI : MonoBehaviour
{
    [Header("메인 화면 연출")]
    public GameObject mainCharacterImage;
    public GameObject mainMenuButtonGroup;
    public GameObject merchantSpriteObject;

    [Header("상점 UI 연결")]
    public GameObject shopPanel;
    public TextMeshProUGUI totalCoinText;

    [Header("스탯/아이템 텍스트 UI")]
    public TextMeshProUGUI dmgPriceText;
    public TextMeshProUGUI itemPriceText;
    public TextMeshProUGUI dmgLevelText;
    public TextMeshProUGUI itemLevelText;

    // ==========================================
    // ★ [수정됨] 여러 개의 BGM 버튼들의 가격/보유 텍스트를 배열로 받습니다!
    // ==========================================
    [Header("BGM 상품 텍스트 UI (1번곡, 2번곡 순서대로 넣으세요)")]
    public TextMeshProUGUI[] bgmPriceTexts;
    public TextMeshProUGUI[] bgmLevelTexts;

    [Header("상인 대화 시스템")]
    public GameObject merchantSpeechBubble;
    public TextMeshProUGUI merchantSpeechText;
    public string[] randomClickLines;
    public string[] buySuccessLines;
    public string[] buyFailLines;

    // ==========================================
    // ★ [새로 추가됨] 이스터에그: 돈 부족 연타 업적!
    // ==========================================
    [Header("이스터에그 (돈 부족 연타)")]
    public string annoyedAchievementId = "AnnoyingCustomer"; // 해금할 업적 ID
    public int annoyedFailThreshold = 5;                     // 돈 없이 몇 번 눌러야 빡치는지
    public string[] annoyedFailLines;                        // 업적 달성 후 바뀌는 빡친(?) 대사들
    private int failClickCount = 0;                          // 클릭 횟수 기억용 변수
    // ==========================================

    public int baseCost = 50;

    private void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (merchantSpeechBubble != null) merchantSpeechBubble.SetActive(false);
        if (merchantSpriteObject != null) merchantSpriteObject.SetActive(false);
        UpdateShopUI();
    }

    public void OpenShop()
    {
        if (shopPanel != null) shopPanel.SetActive(true);
        if (mainCharacterImage != null) mainCharacterImage.SetActive(false);
        if (mainMenuButtonGroup != null) mainMenuButtonGroup.SetActive(false);
        if (merchantSpriteObject != null) merchantSpriteObject.SetActive(true);
        UpdateShopUI();
        if (randomClickLines.Length > 0) ShowMerchantDialogue("왔어? 물건 한 번 둘러봐.");
        
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.UnlockAchievement("Shop_Open");
        }

        // 상점을 켤 때마다 진상(?) 카운트 초기화 (원치 않으시면 지워주세요!)
        failClickCount = 0;
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (merchantSpeechBubble != null) merchantSpeechBubble.SetActive(false);
        if (mainCharacterImage != null) mainCharacterImage.SetActive(true);
        if (mainMenuButtonGroup != null) mainMenuButtonGroup.SetActive(true);
        if (merchantSpriteObject != null) merchantSpriteObject.SetActive(false);
    }

    public void UpdateShopUI()
    {
        if (PlayerDataManager.Instance != null && totalCoinText != null)
            totalCoinText.text = "보유 코인: " + PlayerDataManager.Instance.saveData.totalCoins;

        UpdatePriceText(dmgPriceText, "DMG");
        UpdatePriceText(itemPriceText, "Item");
        UpdateLevelText(dmgLevelText, "DMG");
        UpdateLevelText(itemLevelText, "Item");

        // BGM 텍스트들을 일괄 업데이트 합니다.
        for (int i = 0; i < bgmPriceTexts.Length; i++)
        {
            string bgmCode = "BGM_" + (i + 1); // BGM_1, BGM_2...
            UpdatePriceText(bgmPriceTexts[i], bgmCode);
            UpdateLevelText(bgmLevelTexts[i], bgmCode);
        }
    }

    private void UpdatePriceText(TextMeshProUGUI txtUI, string type)
    {
        if (txtUI == null) return;
        bool isMax; int cost = GetUpgradeCost(type, out isMax);
        if (isMax) txtUI.text = "MAX";
        else txtUI.text = cost.ToString() + " C";
    }

    private void UpdateLevelText(TextMeshProUGUI txtUI, string type)
    {
        if (txtUI == null || PlayerDataManager.Instance == null) return;
        PlayerSaveData data = PlayerDataManager.Instance.saveData;

        // ★ BGM이라면 레벨 대신 "보유중/미보유" 로 표시합니다!
        if (type.StartsWith("BGM_"))
        {
            int bgmIndex = int.Parse(type.Split('_')[1]); // "BGM_1"에서 1을 추출
            if (data.unlockedBgmList.Contains(bgmIndex)) txtUI.text = "보유중";
            else txtUI.text = "미보유";
            return;
        }

        int level = 0; bool isMax = false;
        if (type == "DMG") level = data.dmgLevel;
        else if (type == "Item") { level = data.startItemLevel; if (level >= 2) isMax = true; }

        if (isMax) txtUI.text = "Lv. MAX";
        else txtUI.text = "Lv. " + level;
    }

    private int GetUpgradeCost(string type, out bool isMaxLevel)
    {
        isMaxLevel = false;
        if (PlayerDataManager.Instance == null) return 0;
        PlayerSaveData data = PlayerDataManager.Instance.saveData;
        int cost = 0;

        if (type == "DMG") cost = 1 + (data.dmgLevel * 2);
        else if (type == "Item")
        {
            if (data.startItemLevel == 0) cost = 300;
            else if (data.startItemLevel == 1) cost = 600;
            else isMaxLevel = true;
        }
        else if (type.StartsWith("BGM_"))
        {
            int bgmIndex = int.Parse(type.Split('_')[1]);
            // 리스트에 이미 샀다는 기록이 있으면 만렙(MAX) 처리!
            if (data.unlockedBgmList.Contains(bgmIndex)) isMaxLevel = true;
            else cost = 350; // 각 브금 가격은 350원
        }
        return cost;
    }

    public void BuyUpgrade(string upgradeType)
    {
        if (PlayerDataManager.Instance == null) return;

        bool isMaxLevel;
        int cost = GetUpgradeCost(upgradeType, out isMaxLevel);

        if (isMaxLevel)
        {
            ShowMerchantDialogue("그건 이미 가지고 있는것 같은데?");
            return;
        }

        if (PlayerDataManager.Instance.SpendCoins(cost))
        {
            PlayerSaveData data = PlayerDataManager.Instance.saveData;

            if (upgradeType == "DMG") data.dmgLevel++;
            else if (upgradeType == "Item") data.startItemLevel++;
            else if (upgradeType.StartsWith("BGM_"))
            {
                // ★ 구매 성공 시 리스트에 해당 브금 번호를 추가!
                int bgmIndex = int.Parse(upgradeType.Split('_')[1]);
                data.unlockedBgmList.Add(bgmIndex);
            }

            PlayerDataManager.Instance.SaveData();
            UpdateShopUI();

            if (buySuccessLines != null && buySuccessLines.Length > 0)
                ShowMerchantDialogue(buySuccessLines[Random.Range(0, buySuccessLines.Length)]);
        }
        else
        {
            // ==========================================
            // ★ [이스터에그 발동 로직] 돈 부족한데 누를 때!
            // ==========================================
            bool isAnnoying = false;

            if (AchievementManager.Instance != null)
            {
                // 이미 이 업적을 깼는지(진상 손님인지) 기록을 확인합니다.
                isAnnoying = AchievementManager.Instance.IsUnlocked(annoyedAchievementId);
            }

            // 아직 안 깼다면 카운트를 1씩 올립니다.
            if (!isAnnoying)
            {
                failClickCount++;
                // 5번(목표치) 도달하면? 업적 해금!
                if (failClickCount >= annoyedFailThreshold)
                {
                    if (AchievementManager.Instance != null)
                    {
                        AchievementManager.Instance.UnlockAchievement(annoyedAchievementId);
                    }
                    isAnnoying = true; // 이제부터는 평생 빡친 대사만 나옵니다.
                }
            }

            // 업적을 깼다면(또는 방금 깨졌다면) 빡친 대사 무작위 출력!
            if (isAnnoying && annoyedFailLines != null && annoyedFailLines.Length > 0)
            {
                ShowMerchantDialogue(annoyedFailLines[Random.Range(0, annoyedFailLines.Length)]);
            }
            // 아직 안 깼다면 평범한 돈 부족 대사 출력
            else if (buyFailLines != null && buyFailLines.Length > 0)
            {
                ShowMerchantDialogue(buyFailLines[Random.Range(0, buyFailLines.Length)]);
            }
            // ==========================================
        }
    }

    public void OnClickMerchant()
    {
        if (randomClickLines != null && randomClickLines.Length > 0)
            ShowMerchantDialogue(randomClickLines[Random.Range(0, randomClickLines.Length)]);
    }

    private void ShowMerchantDialogue(string textMeshText)
    {
        StopAllCoroutines();
        StartCoroutine(SpeechRoutine(textMeshText));
    }

    private System.Collections.IEnumerator SpeechRoutine(string textMeshText)
    {
        if (merchantSpeechBubble != null) merchantSpeechBubble.SetActive(true);
        if (merchantSpeechText != null) { merchantSpeechText.gameObject.SetActive(true); merchantSpeechText.text = textMeshText; }
        yield return new WaitForSecondsRealtime(2.5f);
        if (merchantSpeechBubble != null) merchantSpeechBubble.SetActive(false);
        if (merchantSpeechText != null) { merchantSpeechText.text = ""; merchantSpeechText.gameObject.SetActive(false); }
    }
}