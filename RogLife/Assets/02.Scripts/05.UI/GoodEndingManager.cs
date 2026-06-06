using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GoodEndingManager : MonoBehaviour
{
    [Header("스토리 대화 설정")]
    public DialogueLine[] endingDialogues; // 병실에서 언니와 나누는 대화

    [Header("엔딩 크레딧 설정")]
    public GameObject creditsPanel;        // 크레딧 텍스트를 담을 패널
    public RectTransform creditsTransform; // 위로 스크롤될 텍스트 오브젝트
    public float creditsScrollSpeed = 100f;
    public float targetCreditY = 1500f;    // 크레딧이 멈출 Y 좌표
    public float waitAfterCredits = 4.0f;  // 여운 대기 시간

    [Header("사운드 설정")]
    public AudioSource audioSource;
    public AudioClip endingBgm;            // 감동적인 피아노 브금 등

    [Header("씬 이동")]
    public string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        // 씬 진입 시 시간 정상화 보장 및 크레딧 숨기기
        Time.timeScale = 1f;
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // 엔딩 브금 재생
        if (audioSource != null && endingBgm != null)
        {
            audioSource.clip = endingBgm;
            audioSource.loop = true;
            audioSource.Play();
        }

        // 1초 대기 후 대화 시작 (자연스러운 시작을 위해)
        Invoke("StartEndingDialogue", 1.0f);
    }

    private void StartEndingDialogue()
    {
        // 대화가 끝나면 크레딧 코루틴을 실행하라고 예약!
        DialogueManager.instance.onDialogueEndCallback = () =>
        {
            StartCoroutine(CreditsRoutine());
        };

        // 대화 시작
        DialogueManager.instance.StartDialogue(endingDialogues);
    }

    private IEnumerator CreditsRoutine()
    {
        // ==========================================
        // ★ 1. 진엔딩 업적 해금! (스팀 팝업 등장)
        // ==========================================
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.UnlockAchievement("TrueEnding");
        }

        // 2. 크레딧 패널 켜기
        if (creditsPanel != null) creditsPanel.SetActive(true);

        // 3. 크레딧 위로 스크롤
        float timer = 0f;
        while (creditsTransform.anchoredPosition.y < targetCreditY)
        {
            // 엔터키 누르면 스킵
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (AchievementManager.Instance != null)
                {
                    AchievementManager.Instance.UnlockAchievement("Skip_TrueEnding_Credit");
                }
                break;
            }

                creditsTransform.anchoredPosition += new Vector2(0, creditsScrollSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        // 4. 여운을 즐기며 대기
        timer = 0f;
        while (timer < waitAfterCredits)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) break;
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 5. 대망의 메인 메뉴로 복귀!
        SceneManager.LoadScene(mainMenuSceneName);
    }
}