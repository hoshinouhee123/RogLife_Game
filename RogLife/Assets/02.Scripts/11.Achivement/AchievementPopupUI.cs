using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class AchievementPopupUI : MonoBehaviour
{
    public static AchievementPopupUI Instance;

    [Header("UI 연결")]
    public RectTransform popupRect;   // 팝업 패널
    public TextMeshProUGUI titleText;            // 업적 이름
    public TextMeshProUGUI descText;             // 업적 설명
    public Image iconImage;           // 업적 아이콘
    public AudioSource audioSource;   // 띠링! 소리 재생용 스피커
    public AudioClip unlockSound;     // 알림 효과음

    [Header("연출 설정")]
    public float slideDistance = 300f; // 화면 아래로 숨겨둘 거리
    public float showDuration = 3f;    // 화면에 떠있는 시간

    // 여러 업적이 동시에 깨질 경우 순서대로 보여주기 위한 대기열(Queue)
    private Queue<AchievementInfo> popupQueue = new Queue<AchievementInfo>();
    private bool isShowing = false;
    private Vector2 originPos;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 모든 씬에서 팝업이 작동하도록 유지!
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        originPos = popupRect.anchoredPosition;
        // 시작할 때 화면 바깥(아래)으로 숨겨둡니다.
        popupRect.anchoredPosition = originPos + new Vector2(0, -slideDistance);
    }

    // AchievementManager가 이 함수를 부릅니다.
    public void EnqueuePopup(AchievementInfo info)
    {
        popupQueue.Enqueue(info);
        if (!isShowing)
        {
            StartCoroutine(ProcessQueueRoutine());
        }
    }

    private IEnumerator ProcessQueueRoutine()
    {
        isShowing = true;

        while (popupQueue.Count > 0)
        {
            AchievementInfo info = popupQueue.Dequeue();

            // UI 내용 갱신
            titleText.text = info.title;
            descText.text = info.description;
            if (info.icon != null) iconImage.sprite = info.icon;

            // 띠링! 소리 재생
            if (audioSource != null && unlockSound != null)
                audioSource.PlayOneShot(unlockSound);

            Vector2 hidePos = originPos + new Vector2(0, -slideDistance);

            // 1. 위로 스르륵 등장 (0.3초)
            float timer = 0f;
            while (timer < 0.3f)
            {
                timer += Time.unscaledDeltaTime;
                float t = 1f - Mathf.Pow(1f - (timer / 0.3f), 3f); // Ease-out
                popupRect.anchoredPosition = Vector2.Lerp(hidePos, originPos, t);
                yield return null;
            }
            popupRect.anchoredPosition = originPos;

            // 2. 대기 (3초)
            yield return new WaitForSecondsRealtime(showDuration);

            // 3. 다시 아래로 스르륵 퇴장 (0.3초)
            timer = 0f;
            while (timer < 0.3f)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / 0.3f;
                float easeIn = t * t * t; // Ease-in
                popupRect.anchoredPosition = Vector2.Lerp(originPos, hidePos, easeIn);
                yield return null;
            }
            popupRect.anchoredPosition = hidePos;

            // 연속 팝업일 경우 약간의 간격(0.2초) 대기
            yield return new WaitForSecondsRealtime(0.2f);
        }

        isShowing = false;
    }
}