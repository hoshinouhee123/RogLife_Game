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

    // ★ [추가됨] 보스 체력바 UI 부품
    [Header("보스 체력바 UI")]
    public GameObject hpBarGroup;    // 전체 체력바 그룹
    public Image hpBarFill;          // 깎이는 이미지 (HP_Fill)

    [Header("컷신 속도 조절 (초)")]
    public float appearTime = 0.4f;
    public float stayTime = 3.0f;
    public float disappearTime = 0.3f;
    public float slideDistance = 800f;

    private Vector2 originPlayerImg, originPlayerName, originBossImg, originBossName;

    // 통합 보스 체력 계산용 변수
    private float totalBossMaxHP;
    private float totalBossCurrentHP;

    private bool skipRequested = false;

    private void Awake() { if (Instance == null) Instance = this; }

    private void Start()
    {
        bgCanvasGroup.gameObject.SetActive(false);
        hpBarGroup.SetActive(false); // 시작할 땐 체력바 숨김

        if (playerImage != null) playerImage.sprite = playerSprite;
        if (playerNameText != null) playerNameText.text = playerName;

        originPlayerImg = playerImageRect.anchoredPosition;
        originPlayerName = playerNameRect.anchoredPosition;
        originBossImg = bossImageRect.anchoredPosition;
        originBossName = bossNameRect.anchoredPosition;
    }

    // ★ [새로 추가됨] 마우스 클릭 감지
    private void Update()
    {
        // 컷신이 켜져 있을 때 마우스 왼쪽 버튼(클릭)을 누르면 스킵 요청!
        if (bgCanvasGroup.gameObject.activeInHierarchy && Input.GetMouseButtonDown(0))
        {
            skipRequested = true;
        }
    }

    // ★ [추가됨] 보스전 시작 시 체력바 세팅
    public void InitBossHealth(EnemyData bossData)
    {
        // 수학의 마법: 분열 보스(최대 4마리까지 쪼개짐)를 모두 죽이려면
        // 원본 체력의 딱 '2배'의 데미지를 넣어야 방이 클리어 됩니다!
        float multiplier = bossData.isDashSplittingBoss ? 2f : 1f;

        totalBossMaxHP = bossData.maxHealth * multiplier;
        totalBossCurrentHP = totalBossMaxHP;

        hpBarFill.fillAmount = 1f; // 꽉 채우기
    }

    // ★ [추가됨] 보스 체력바 켜기 / 끄기
    public void ShowHPBar() { hpBarGroup.SetActive(true); }
    public void HideHPBar() { hpBarGroup.SetActive(false); }

    // ★ [추가됨] 보스가 맞을 때마다 체력바 깎기
    public void ApplyBossDamage(float damage)
    {
        totalBossCurrentHP -= damage;
        if (totalBossCurrentHP < 0) totalBossCurrentHP = 0;

        hpBarFill.fillAmount = totalBossCurrentHP / totalBossMaxHP;
    }

    // 컷신 코루틴 (방어 코드 포함)
    public IEnumerator ShowCutsceneRoutine(EnemyData bossData)
    {
        // (초기화 및 변수 방어 코드는 기존과 동일)
        skipRequested = false; // 시작할 때 스킵 초기화

        if (appearTime <= 0f) appearTime = 0.4f;
        if (stayTime <= 0f) stayTime = 3.0f;
        if (disappearTime <= 0f) disappearTime = 0.3f;
        if (slideDistance <= 0f) slideDistance = 800f;

        if (bossData != null)
        {
            bossImage.sprite = bossData.enemySprite;
            bossNameText.text = bossData.enemyName;

            // ★ 컷신이 시작될 때 보스 전체 체력을 계산해둠
            InitBossHealth(bossData);
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

        playerImageRect.localScale = Vector3.one; playerNameRect.localScale = Vector3.one;
        bossImageRect.localScale = Vector3.one; bossNameRect.localScale = Vector3.one;
        bgCanvasGroup.alpha = 0f;

        yield return null;

        // 연출 1: 등장
        float time = 0f;
        while (time < appearTime)
        {
            if (skipRequested) break; // ★ 스킵 버튼 눌리면 즉시 탈출!

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f); time += dt;
            float t = time / appearTime; float easeOut = 1f - Mathf.Pow(1f - t, 4f);

            bgCanvasGroup.alpha = Mathf.Lerp(0f, 0.8f, easeOut);
            playerImageRect.anchoredPosition = Vector2.Lerp(startPlayerImg, originPlayerImg, easeOut);
            playerNameRect.anchoredPosition = Vector2.Lerp(startPlayerName, originPlayerName, easeOut);
            bossImageRect.anchoredPosition = Vector2.Lerp(startBossImg, originBossImg, easeOut);
            bossNameRect.anchoredPosition = Vector2.Lerp(startBossName, originBossName, easeOut);
            yield return null;
        }

        // ★ 스킵 당했다면 더 볼 것 없이 컷신 아예 종료!
        if (skipRequested) { bgCanvasGroup.gameObject.SetActive(false); yield break; }

        time = 0f;
        while (time < stayTime)
        {
            if (skipRequested) break; // ★ 스킵 탈출

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f); time += dt;
            float scale = Mathf.Lerp(1f, 1.1f, time / stayTime);
            playerImageRect.localScale = new Vector3(scale, scale, 1f); playerNameRect.localScale = new Vector3(scale, scale, 1f);
            bossImageRect.localScale = new Vector3(scale, scale, 1f); bossNameRect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        if (skipRequested) { bgCanvasGroup.gameObject.SetActive(false); yield break; }

        time = 0f;
        while (time < disappearTime)
        {
            if (skipRequested) break; // ★ 스킵 탈출

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f); time += dt;
            float t = time / disappearTime; float easeIn = t * t * t;

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