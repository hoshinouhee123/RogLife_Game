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
    public DialogueLine[] deathDialogues; // 1~4층 일반 죽음 대화

    // ==========================================
    // ★ [새로 추가됨] 5층 보스전에서 죽었을 때 나올 전용 대화(언니 독백) 칸입니다!
    // ==========================================
    [Header("5층 전용 게임오버")]
    public DialogueLine[] floor5DeathDialogues;

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

        // ==========================================
        // ★ [핵심 추가] 여기서 5층인지 검사해서 대화를 바꿔치기 합니다!
        // ==========================================
        DialogueLine[] dialoguesToPlay = deathDialogues; // 기본값은 1~4층 일반 대화

        // 맵 생성기를 확인해서 현재 층이 5층(숫자 5)이라면?
        if (MapGenerator.Instance != null && MapGenerator.Instance.currentFloor >= 5)
        {
            dialoguesToPlay = floor5DeathDialogues; // 5층 전용 대화(언니 독백)로 교체!
        }
        // ==========================================

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
        // ==========================================
        // ★ [완벽 수정] 5층에서 죽었는지 확인해서 업적을 다르게 지급합니다!
        // ==========================================
        bool isFloor5 = (MapGenerator.Instance != null && MapGenerator.Instance.currentFloor >= 5);

        if (AchievementManager.Instance != null)
        {
            if (isFloor5)
            {
                // 5층에서 사망 시: 5층 전용 배드엔딩 업적 해금!
                AchievementManager.Instance.UnlockAchievement("BadEnding2");
            }
            else
            {
                // 1~4층에서 사망 시: 일반 배드엔딩 업적 해금!
                AchievementManager.Instance.UnlockAchievement("BadEnding1");
            }
        }
        // ==========================================

        if (BGMManager.Instance != null) BGMManager.Instance.PlayBadEndingBGM();

        creditsPanel.SetActive(true);

        float timer = 0f;
        // 시간 대신 Y좌표를 검사합니다!
        while (creditsTransform.anchoredPosition.y < targetCreditY)
        {
            // 엔터 누르면 스킵
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) break;

            creditsTransform.anchoredPosition += new Vector2(0, creditsScrollSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        timer = 0f;
        while (timer < waitAfterCredits)
        {
            // 여운을 즐기는 중에도 엔터키 누르면 즉시 메인화면으로 이동
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) break;

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}