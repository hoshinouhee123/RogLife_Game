using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class StageTransitionUI : MonoBehaviour
{
    public static StageTransitionUI Instance;

    public CanvasGroup canvasGroup;
    public TextMeshProUGUI stageText;

    private bool skipRequested = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        canvasGroup.gameObject.SetActive(false);
    }

    private void Update()
    {
        // 화면이 떠 있을 때 클릭하면 스킵!
        if (canvasGroup.gameObject.activeInHierarchy && Input.GetMouseButtonDown(0))
        {
            skipRequested = true;
        }
    }

    // MapGenerator가 맵을 지우기 '전'에 부름
    public IEnumerator ShowTransition(int floorNumber)
    {
        skipRequested = false;
        stageText.text = floorNumber + "층";

        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(true);

        // 1. 검은 화면이 스르륵 나타남 (0.5초)
        float timer = 0f;
        while (timer < 0.5f)
        {
            if (skipRequested) break;
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / 0.5f);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 2. 검은 화면 대기 (2.5초간 대기, 클릭 시 즉시 맵 넘기기)
        timer = 0f;
        while (timer < 2.5f)
        {
            if (skipRequested) break;
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // MapGenerator가 맵을 다 만든 '후'에 부름
    public IEnumerator HideTransition()
    {
        // 3. 검은 화면이 스르륵 걷힘 (0.5초)
        float timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / 0.5f);
            yield return null;
        }

        canvasGroup.gameObject.SetActive(false);
    }
}