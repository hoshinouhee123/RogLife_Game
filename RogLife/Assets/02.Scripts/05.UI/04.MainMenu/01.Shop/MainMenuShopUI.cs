using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuShopUI : MonoBehaviour
{
    [Header("메인 화면 연출")]
    public GameObject mainCharacterImage;
    public GameObject mainMenuButtonGroup;

    // ==========================================
    // ★ [새로 추가됨] 캔버스 밖에 있는 조명용 상인 스프라이트를 넣을 칸!
    // ==========================================
    [Header("상인 스프라이트 (조명용)")]
    public GameObject merchantSpriteObject;

    [Header("상점 UI 연결")]
    public GameObject shopPanel;
    public TextMeshProUGUI totalCoinText;

    [Header("상인 대화 시스템")]
    public GameObject merchantSpeechBubble;
    public TextMeshProUGUI merchantSpeechText;
    public string[] randomClickLines;
    public string[] buySuccessLines;
    public string[] buyFailLines;

    [Header("업그레이드 가격 설정")]
    public int baseCost = 50;

    private void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (merchantSpeechBubble != null) merchantSpeechBubble.SetActive(false);

        // ★ [수정됨] 시작할 때 텍스트 오브젝트도 확실하게 꺼버립니다!
        if (merchantSpeechText != null) merchantSpeechText.gameObject.SetActive(false);

        if (merchantSpriteObject != null) merchantSpriteObject.SetActive(false);

        UpdateShopUI();
    }

    public void OpenShop()
    {
        if (shopPanel != null) shopPanel.SetActive(true);

        if (mainCharacterImage != null) mainCharacterImage.SetActive(false);
        if (mainMenuButtonGroup != null) mainMenuButtonGroup.SetActive(false);

        // ==========================================
        // ★ 상점이 열리면 숨겨뒀던 상인 스프라이트를 켭니다!
        // ==========================================
        if (merchantSpriteObject != null) merchantSpriteObject.SetActive(true);

        UpdateShopUI();

        if (randomClickLines.Length > 0)
        {
            ShowMerchantDialogue("왔어? 물건 한 번 둘러봐.");
        }
    }

    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (merchantSpeechBubble != null) merchantSpeechBubble.SetActive(false);

        // ★ [수정됨] 상점 창을 닫을 때도 텍스트가 남지 않게 강제로 끕니다!
        if (merchantSpeechText != null) merchantSpeechText.gameObject.SetActive(false);

        if (mainCharacterImage != null) mainCharacterImage.SetActive(true);
        if (mainMenuButtonGroup != null) mainMenuButtonGroup.SetActive(true);
        if (merchantSpriteObject != null) merchantSpriteObject.SetActive(false);
    }

    public void UpdateShopUI()
    {
        if (PlayerDataManager.Instance != null && totalCoinText != null)
        {
            totalCoinText.text = "보유 코인: " + PlayerDataManager.Instance.saveData.totalCoins;
        }
    }

    public void OnClickMerchant()
    {
        if (randomClickLines.Length > 0)
        {
            string line = randomClickLines[Random.Range(0, randomClickLines.Length)];
            ShowMerchantDialogue(line);
        }
    }

    private void ShowMerchantDialogue(string textMeshText)
    {
        StopAllCoroutines();
        StartCoroutine(SpeechRoutine(textMeshText));
    }

    private System.Collections.IEnumerator SpeechRoutine(string textMeshText)
    {
        // 켤 때 배경과 텍스트 둘 다 켬
        if (merchantSpeechBubble != null) merchantSpeechBubble.SetActive(true);
        if (merchantSpeechText != null)
        {
            merchantSpeechText.gameObject.SetActive(true);
            merchantSpeechText.text = textMeshText;
        }

        yield return new WaitForSecondsRealtime(2.5f);

        // ★ [완벽 해결] 끌 때 배경과 텍스트 둘 다 끄고, 내용물도 공백으로 날려버림!
        if (merchantSpeechBubble != null) merchantSpeechBubble.SetActive(false);
        if (merchantSpeechText != null)
        {
            merchantSpeechText.text = ""; // 글씨 지우기
            merchantSpeechText.gameObject.SetActive(false); // 오브젝트 끄기
        }
    }

    public void BuyUpgrade(string upgradeType)
    {
        if (PlayerDataManager.Instance == null) return;

        PlayerSaveData data = PlayerDataManager.Instance.saveData;
        int currentLevel = 0;

        switch (upgradeType)
        {
            case "HP": currentLevel = data.hpLevel; break;
            case "DMG": currentLevel = data.dmgLevel; break;
            case "SPD": currentLevel = data.spdLevel; break;
            case "Item": currentLevel = data.startItemLevel; break;
            case "Coin": currentLevel = data.startCoinLevel; break;
            case "Key": currentLevel = data.startKeyLevel; break;
        }

        int cost = baseCost + (currentLevel * baseCost);

        if (PlayerDataManager.Instance.SpendCoins(cost))
        {
            if (upgradeType == "HP") data.hpLevel++;
            else if (upgradeType == "DMG") data.dmgLevel++;
            else if (upgradeType == "SPD") data.spdLevel++;
            else if (upgradeType == "Item") data.startItemLevel++;
            else if (upgradeType == "Coin") data.startCoinLevel += 5;
            else if (upgradeType == "Key") data.startKeyLevel += 1;

            PlayerDataManager.Instance.SaveData();
            UpdateShopUI();

            if (buySuccessLines.Length > 0)
            {
                string line = buySuccessLines[Random.Range(0, buySuccessLines.Length)];
                ShowMerchantDialogue(line);
            }
        }
        else
        {
            if (buyFailLines.Length > 0)
            {
                string line = buyFailLines[Random.Range(0, buyFailLines.Length)];
                ShowMerchantDialogue(line);
            }
        }
    }
}