using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI 연결")]
    public GameObject gameOverCanvas;
    public Image fadeBlackImage;
    public GameObject creditsPanel;
    public RectTransform creditsTransform;

    [Header("연출 설정")]
    public float fadeDuration = 2.0f;
    public DialogueLine[] deathDialogues;        // 1~4층 일반 독백
    public DialogueLine[] floor5DeathDialogues;  // 5층 전용 언니 독백

    // ==========================================
    // ★ [새로 추가됨] 층별 게임오버 BGM 설정 칸!
    // ==========================================
    [Header("사운드 설정 (BGM)")]
    public AudioClip normalDialogueBgm; // 1~4층 대화(독백) 브금
    public AudioClip floor5DialogueBgm; // 5층 대화(언니) 브금
    public AudioClip normalCreditBgm;   // 1~4층 크레딧 브금
    public AudioClip floor5CreditBgm;   // 5층 크레딧 브금

    [Header("크레딧 설정")]
    public float creditsScrollSpeed = 100f;
    public float targetCreditY = 1500f;
    public float waitAfterCredits = 4.0f;

    public string mainMenuSceneName = "MainMenu";
    private bool isGameOverStarted = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        gameOverCanvas.SetActive(false);
        Color c = fadeBlackImage.color;
        c.a = 0f;
        fadeBlackImage.color = c;
        creditsPanel.SetActive(false);
    }

    public void StartGameOverSequence()
    {
        if (isGameOverStarted) return;
        isGameOverStarted = true;

        // ==========================================
        // ★ [추가됨] 죽을 때 먹은 코인들을 영구 지갑으로 송금!
        // ==========================================
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && PlayerDataManager.Instance != null)
        {
            int earnedCoins = player.GetComponent<Player>().coinCount;
            PlayerDataManager.Instance.AddCoins(earnedCoins);
            Debug.Log($"죽었지만 {earnedCoins}코인을 영구 지갑에 저장했습니다!");
        }

        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        Time.timeScale = 0f;
        gameOverCanvas.SetActive(true);

        float timer = 0f;
        Color color = fadeBlackImage.color;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeBlackImage.color = color;
            yield return null;
        }
        color.a = 1f;
        fadeBlackImage.color = color;

        yield return new WaitForSecondsRealtime(1.0f);

        // 층수 검사
        bool isFloor5 = (MapGenerator.Instance != null && MapGenerator.Instance.currentFloor >= 5);
        DialogueLine[] dialoguesToPlay = isFloor5 ? floor5DeathDialogues : deathDialogues;

        // ==========================================
        // ★ [대화 BGM 재생] 5층이면 5층 브금을, 아니면 일반 브금을 틉니다!
        // ==========================================
        AudioClip dialogueBgmToPlay = isFloor5 ? floor5DialogueBgm : normalDialogueBgm;
        if (BGMManager.Instance != null && dialogueBgmToPlay != null)
        {
            BGMManager.Instance.PlayBGM(dialogueBgmToPlay, true); // 대화 중엔 무한 반복
        }

        if (dialoguesToPlay != null && dialoguesToPlay.Length > 0)
        {
            DialogueManager.instance.onDialogueEndCallback = () =>
            {
                StartCoroutine(CreditsRoutine());
            };
            DialogueManager.instance.StartDialogue(dialoguesToPlay);
        }
        else
        {
            StartCoroutine(CreditsRoutine());
        }
    }

    private IEnumerator CreditsRoutine()
    {
        bool isFloor5 = (MapGenerator.Instance != null && MapGenerator.Instance.currentFloor >= 5);

        if (AchievementManager.Instance != null)
        {
            if (isFloor5) AchievementManager.Instance.UnlockAchievement("BadEnding2");
            else AchievementManager.Instance.UnlockAchievement("BadEnding1");
        }

        // ==========================================
        // ★ [크레딧 BGM 재생] 5층이면 5층 크레딧 브금을, 아니면 일반 크레딧 브금을 틉니다!
        // ==========================================
        AudioClip creditBgmToPlay = isFloor5 ? floor5CreditBgm : normalCreditBgm;
        if (BGMManager.Instance != null && creditBgmToPlay != null)
        {
            BGMManager.Instance.PlayBGM(creditBgmToPlay, false); // 크레딧은 1번만 재생
        }

        creditsPanel.SetActive(true);

        while (creditsTransform.anchoredPosition.y < targetCreditY)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                
                if (AchievementManager.Instance != null)
                {
                    AchievementManager.Instance.UnlockAchievement("Skip_Credit");
                }

                break;
            }
            creditsTransform.anchoredPosition += new Vector2(0, creditsScrollSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        float timer = 0f;
        while (timer < waitAfterCredits)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) break;
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}