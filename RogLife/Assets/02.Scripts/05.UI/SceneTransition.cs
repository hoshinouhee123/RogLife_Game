using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필수
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition instance; // 어디서든 쉽게 부를 수 있게 싱글톤 처리

    public CanvasGroup fadeCanvasGroup; // 투명도를 조절할 캔버스 그룹
    public float fadeDuration = 1f;     // 페이드 아웃/인에 걸리는 시간 (1초)

    private void Awake()
    {
        // 씬이 넘어가도 이 오브젝트가 파괴되지 않게 설정
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

    // 버튼에서 이 함수를 실행하게 될 겁니다!
    public void LoadNextScene(string sceneName)
    {
        StartCoroutine(Transition(sceneName));
    }

    private IEnumerator Transition(string sceneName)
    {
        // 1. 페이드 아웃 (화면이 점점 까매짐)
        fadeCanvasGroup.blocksRaycasts = true; // 페이드 중에는 다른 버튼을 못 누르게 막음

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration); // 0에서 1로 서서히 변경
            yield return null;
        }

        // 화면이 완전히 까매진 상태에서 약간 대기 (자연스러움을 위해)
        yield return new WaitForSeconds(0.2f);

        // 2. 씬 로드!
        SceneManager.LoadScene(sceneName);

        // 씬이 로드될 시간을 살짝 벌어줌
        yield return new WaitForSeconds(0.2f);

        // 3. 페이드 인 (새로운 씬에서 화면이 점점 밝아짐)
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration); // 1에서 0으로 서서히 변경
            yield return null;
        }

        fadeCanvasGroup.blocksRaycasts = false; // 이제 다시 버튼 등을 누를 수 있게 해제
    }
}