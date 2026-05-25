using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class BossUIManager : MonoBehaviour
{
    public static BossUIManager Instance;

    [Header("배경")]
    public CanvasGroup bgCanvasGroup;

    [Header("플레이어 UI 부품 (왼쪽)")]
    public RectTransform playerImageRect;
    public RectTransform playerNameRect;
    public Image playerImage;
    public TextMeshProUGUI playerNameText;
    public string playerName = "Player";
    public Sprite playerSprite;

    [Header("보스 UI 부품 (오른쪽)")]
    public RectTransform bossImageRect;
    public RectTransform bossNameRect;
    public Image bossImage;
    public TextMeshProUGUI bossNameText;

    [Header("컷신 속도 조절 (초)")]
    public float appearTime = 0.4f;
    public float stayTime = 3.0f;
    public float disappearTime = 0.3f;

    [Header("애니메이션 설정")]
    public float slideDistance = 800f;

    private Vector2 originPlayerImg;
    private Vector2 originPlayerName;
    private Vector2 originBossImg;
    private Vector2 originBossName;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        bgCanvasGroup.gameObject.SetActive(false);

        if (playerImage != null) playerImage.sprite = playerSprite;
        if (playerNameText != null) playerNameText.text = playerName;

        originPlayerImg = playerImageRect.anchoredPosition;
        originPlayerName = playerNameRect.anchoredPosition;
        originBossImg = bossImageRect.anchoredPosition;
        originBossName = bossNameRect.anchoredPosition;
    }

    public IEnumerator ShowCutsceneRoutine(EnemyData bossData)
    {
        // [방어 1] 인스펙터에서 실수로 시간이 0이 되어 스킵되는 버그 원천 차단
        if (appearTime <= 0f) appearTime = 0.4f;
        if (stayTime <= 0f) stayTime = 3.0f;
        if (disappearTime <= 0f) disappearTime = 0.3f;
        if (slideDistance <= 0f) slideDistance = 800f;

        if (bossData != null)
        {
            bossImage.sprite = bossData.enemySprite;
            bossNameText.text = bossData.enemyName;
        }

        bgCanvasGroup.gameObject.SetActive(true);

        Vector2 startPlayerImg = originPlayerImg + new Vector2(-slideDistance, 0);
        Vector2 startPlayerName = originPlayerName + new Vector2(-slideDistance, 0);
        Vector2 startBossImg = originBossImg + new Vector2(slideDistance, 0);
        Vector2 startBossName = originBossName + new Vector2(slideDistance, 0);

        Vector2 exitPlayerImg = originPlayerImg + new Vector2(slideDistance, 0);
        Vector2 exitPlayerName = originPlayerName + new Vector2(slideDistance, 0);
        Vector2 exitBossImg = originBossImg + new Vector2(-slideDistance, 0);
        Vector2 exitBossName = originBossName + new Vector2(-slideDistance, 0);

        playerImageRect.localScale = Vector3.one;
        playerNameRect.localScale = Vector3.one;
        bossImageRect.localScale = Vector3.one;
        bossNameRect.localScale = Vector3.one;
        bgCanvasGroup.alpha = 0f;

        // [방어 2] 보스 소환 렉을 넘기기 위해 한 프레임 대기! (이게 없으면 시작하자마자 훅 지나갑니다)
        yield return null;

        // 연출 1: 등장
        float time = 0f;
        while (time < appearTime)
        {
            // [방어 3] 컴퓨터에 렉이 걸려도, 한 번에 최대 0.1초까지만 애니메이션이 움직이게 강제 고정 (스킵 불가)
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            time += dt;

            float t = time / appearTime;
            float easeOut = 1f - Mathf.Pow(1f - t, 4f);

            bgCanvasGroup.alpha = Mathf.Lerp(0f, 0.8f, easeOut);
            playerImageRect.anchoredPosition = Vector2.Lerp(startPlayerImg, originPlayerImg, easeOut);
            playerNameRect.anchoredPosition = Vector2.Lerp(startPlayerName, originPlayerName, easeOut);
            bossImageRect.anchoredPosition = Vector2.Lerp(startBossImg, originBossImg, easeOut);
            bossNameRect.anchoredPosition = Vector2.Lerp(startBossName, originBossName, easeOut);

            yield return null;
        }

        //  연출 2: 대치 상태
        time = 0f;
        while (time < stayTime)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            time += dt;

            float scale = Mathf.Lerp(1f, 1.1f, time / stayTime);
            playerImageRect.localScale = new Vector3(scale, scale, 1f);
            playerNameRect.localScale = new Vector3(scale, scale, 1f);
            bossImageRect.localScale = new Vector3(scale, scale, 1f);
            bossNameRect.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        // 연출 3: 퇴장
        time = 0f;
        while (time < disappearTime)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            time += dt;

            float t = time / disappearTime;
            float easeIn = t * t * t;

            bgCanvasGroup.alpha = Mathf.Lerp(0.8f, 0f, easeIn);
            playerImageRect.anchoredPosition = Vector2.Lerp(originPlayerImg, exitPlayerImg, easeIn);
            playerNameRect.anchoredPosition = Vector2.Lerp(originPlayerName, exitPlayerName, easeIn);
            bossImageRect.anchoredPosition = Vector2.Lerp(originBossImg, exitBossImg, easeIn);
            bossNameRect.anchoredPosition = Vector2.Lerp(originBossName, exitBossName, easeIn);

            yield return null;
        }

        bgCanvasGroup.gameObject.SetActive(false);
    }
}