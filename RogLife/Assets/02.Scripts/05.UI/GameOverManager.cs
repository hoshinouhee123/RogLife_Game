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
    public DialogueLine[] deathDialogues;
    public float creditsScrollSpeed = 100f;
    public float creditsDuration = 10f;

    // ★ [추가됨] 크레딧이 다 올라간 뒤 메인화면으로 가기 전 대기할 시간 (초)
    public float waitAfterCredits = 4.0f;

    public string mainMenuSceneName = "MainMenu";

    // ★ [추가됨] 게임 오버가 이미 시작되었는지 체크하는 자물쇠
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
        // ★ 이미 게임 오버가 실행 중이라면 두 번 실행 안 되게 막음!
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

        if (deathDialogues != null && deathDialogues.Length > 0)
        {
            DialogueManager.instance.onDialogueEndCallback = () =>
            {
                StartCoroutine(CreditsRoutine());
            };
            DialogueManager.instance.StartDialogue(deathDialogues);
        }
        else
        {
            StartCoroutine(CreditsRoutine());
        }
    }

    private IEnumerator CreditsRoutine()
    {
        // [새로 추가된 코드] JSON 매니저에게 업적 달성을 알림!
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.UnlockAchievement("BadEnding1");
        }
        // ==========================================

        if (BGMManager.Instance != null) BGMManager.Instance.PlayBadEndingBGM();

        creditsPanel.SetActive(true);

        float timer = 0f;
        while (timer < creditsDuration)
        {

            // ★ [추가됨] 엔터키를 누르면 크레딧 스크롤 즉시 종료
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) break;

            timer += Time.unscaledDeltaTime;
            creditsTransform.anchoredPosition += new Vector2(0, creditsScrollSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        timer = 0f;
        while (timer < waitAfterCredits)
        {
            // ★ [추가됨] 여운을 즐기는 중에도 엔터키 누르면 즉시 메인화면으로 이동
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) break;

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}