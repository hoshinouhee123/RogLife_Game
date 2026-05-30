using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition instance;

    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임 시작 시 페이드 캔버스가 화면을 가리지 않도록 강제로 투명하게 초기화!
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    public void LoadNextScene(string sceneName)
    {
        StartCoroutine(Transition(sceneName));
    }

    private IEnumerator Transition(string sceneName)
    {
        // 페이드 아웃 (화면 까매짐)
        fadeCanvasGroup.blocksRaycasts = true;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            // ★ Time.deltaTime 대신 unscaledDeltaTime 사용 (시간 정지 무시!)
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f; // 오차 방지용 1 강제 고정

        // ★ WaitForSeconds 대신 Realtime 사용
        yield return new WaitForSecondsRealtime(0.2f);

        // 씬 로드
        SceneManager.LoadScene(sceneName);

        yield return new WaitForSecondsRealtime(0.2f);

        // 씬 로드 직후 시간을 정상으로 강제 복구!
        Time.timeScale = 1f;

        // 페이드 인 (화면 밝아짐)
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f; // 오차 방지용 0 강제 고정

        fadeCanvasGroup.blocksRaycasts = false;
    }
}