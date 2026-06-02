using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class StageTransitionUI : MonoBehaviour
{
    public static StageTransitionUI Instance;

    public CanvasGroup blackBgCanvasGroup; // 검은 화면
    public CanvasGroup cgCanvasGroup;      // 일러스트(CG) 캔버스 그룹
    public Image cgImage;                  // 일러스트 이미지
    public TextMeshProUGUI stageText;                 // 2층, 3층 글씨

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        blackBgCanvasGroup.alpha = 0f;
        blackBgCanvasGroup.gameObject.SetActive(false);

        cgCanvasGroup.alpha = 0f;
        cgCanvasGroup.gameObject.SetActive(false);

        stageText.gameObject.SetActive(false);
    }

    // 1. 검은 화면으로 덮기
    public IEnumerator FadeToBlack()
    {
        blackBgCanvasGroup.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            blackBgCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / 0.5f);
            yield return null;
        }
    }

    // 2. 일러스트(CG) 스르륵 띄우기 (DialogueManager가 부를 함수)
    public void ShowCG(Sprite cgSprite)
    {
        if (cgSprite == null) return;
        cgImage.sprite = cgSprite;
        cgCanvasGroup.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeCGRoutine(1f)); // 1(보임)로 페이드
    }

    // 3. 일러스트 스르륵 끄기
    public void HideCG()
    {
        StopAllCoroutines();
        StartCoroutine(FadeCGRoutine(0f)); // 0(투명)으로 페이드
    }

    private IEnumerator FadeCGRoutine(float targetAlpha)
    {
        float startAlpha = cgCanvasGroup.alpha;
        float timer = 0f;
        while (timer < 1.0f) // 1초 동안 부드럽게
        {
            timer += Time.unscaledDeltaTime;
            cgCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / 1.0f);
            yield return null;
        }
        if (targetAlpha == 0f) cgCanvasGroup.gameObject.SetActive(false);
    }

    // 4. "X층" 글씨 띄우고 검은 화면 걷어내기
    public IEnumerator ShowFloorTextAndFadeOut(int floorNumber)
    {
        stageText.text = floorNumber + "층";
        stageText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(2.0f); // 2초간 층 이름 보여주기
        stageText.gameObject.SetActive(false);

        // 검은 화면 걷히기
        float timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            blackBgCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / 0.5f);
            yield return null;
        }
        blackBgCanvasGroup.gameObject.SetActive(false);
    }
}