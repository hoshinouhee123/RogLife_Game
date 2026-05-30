using UnityEngine;
using UnityEngine.UI; // UI를 다루기 위해 필수
using System.Collections;
using UnityEngine.Audio;
using TMPro; // 오디오 믹서를 사용하기 위해 필수

public class Player : MonoBehaviour
{
    [Header("체력 및 스탯")]
    public int maxHealth = 3;      // 최대 하트 갯수 (3 = 하트 3칸)
    public int currentHealth;      // 현재 체력
    public float attackDamage = 1f;// 공격력
    public float fireRate = 0.5f;  // 공격 속도 (0.5초마다 1발)
    private float nextFireTime;

    [Header("무적 시간 (i-frame)")]
    public float invincibilityDuration = 1f;
    private bool isInvincible = false;

    [Header("총알 설정")]
    public GameObject bulletPrefab; // 아까 만든 총알 프리팹
    public Transform firePoint;     // 총알이 발사될 위치

    [Header("UI 설정")]
    public Image[] hearts;          // 화면에 띄울 하트 이미지들
    public Sprite fullHeart;        // 꽉 찬 하트 이미지
    public Sprite emptyHeart;       // 빈 하트 이미지


    [Header("피격 연출 및 효과음")]
    public Sprite hitSprite;          // 으윽! 하고 맞는 피격 이미지
    public float hitSpriteTime = 0.2f;// 피격 이미지가 유지되는 시간
    public AudioClip shootSound;      // 눈물(총알) 발사 효과음
    public AudioClip getHitSound;     // (보너스) 플레이어가 맞았을 때 소리

    // ★ [새로 추가됨] 코인 시스템
    [Header("재화 및 UI")]
    public int coinCount = 0;        // 현재 가진 코인 개수
    public TextMeshProUGUI coinText;            // 화면에 띄울 텍스트 UI
    public AudioClip coinGetSound;   // 짤랑! 하는 코인 획득 소리

    // ★ [열쇠용 변수 추가]
    public int keyCount = 0;
    public TextMeshProUGUI keyText;
    public AudioClip keyGetSound; // 짤그랑! 열쇠 소리

    public AudioClip heartGetSound; // 띠링! 하는 하트 획득 소리

    // 인스펙터에서 SFX 믹서 그룹을 넣을 빈칸
    public AudioMixerGroup sfxMixerGroup;

    // [여기에 아이템 획득 효과음 변수 추가!]
    public AudioClip itemGetSound;

    private bool isDead = false;

    private AudioSource audioSource;
    private PlayerController playerController;
    private SpriteRenderer sr;

    public bool godMode = false; // 치트 무적 모드

    void Start()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();

        //테스트용 시작 코인 추가 코드
        coinCount += 99;

        // 내 몸에 오디오 소스가 없으면 자동으로 하나 달아줌
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (sfxMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
        }

        UpdateHealthUI();

        UpdateCoinUI(); // 시작할 때 코인 숫자 0으로 띄워주기

        UpdateKeyUI(); // ★ 추가
    }

    void Update()
    {
        HandleShooting();
    }

    // 공격 (화살표 키)
    void HandleShooting()
    {
        // ★ [새로 추가됨] 콘솔 켜져있으면 총 쏘기 금지!
        if (CheatConsole.Instance != null && CheatConsole.Instance.isConsoleActive) return;

        if (Time.time < nextFireTime) return;

        Vector2 shootDir = Vector2.zero;

        if (Input.GetKey(KeyCode.UpArrow)) shootDir = Vector2.up;
        else if (Input.GetKey(KeyCode.DownArrow)) shootDir = Vector2.down;
        else if (Input.GetKey(KeyCode.LeftArrow)) shootDir = Vector2.left;
        else if (Input.GetKey(KeyCode.RightArrow)) shootDir = Vector2.right;

        if (shootDir != Vector2.zero)
        {
            Shoot(shootDir);
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot(Vector2 dir)
    {
        // 총알을 쏠 때 효과음 재생
        if (shootSound != null) audioSource.PlayOneShot(shootSound);

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<Bullet>().Setup(dir, attackDamage);
    }

    // 적에게 맞았을 때 호출되는 함수
    // ★ [수정됨] 데미지 받는 함수
    public void TakeDamage(int damage)
    {
        // godMode가 켜져 있으면 데미지를 절대 받지 않음!
        if (isInvincible || isDead || godMode) return;
        

        currentHealth -= damage;
        UpdateHealthUI();

        // 맞았을 때 소리
        if (getHitSound != null) audioSource.PlayOneShot(getHitSound);

        if (currentHealth <= 0)
        {
            isDead = true; // ★ 이제 난 죽었다고 도장 쾅! (이후 데미지 무시)
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityRoutine());
        }
    }

    // 하트 UI 업데이트 로직
    void UpdateHealthUI()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            // 최대 체력까지만 하트를 보여줌
            if (i < maxHealth) hearts[i].enabled = true;
            else hearts[i].enabled = false;

            // 현재 체력 이하면 꽉 찬 하트, 아니면 빈 하트
            if (i < currentHealth) hearts[i].sprite = fullHeart;
            else hearts[i].sprite = emptyHeart;
        }
    }

    // 맞았을 때 깜빡거리는 무적 시간 연출
    // 무적 시간 연출에 피격 스프라이트 기능 추가
    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        // 1. 피격 이미지 띄우고, 걷는 애니메이션 멈추기
        if (playerController != null) playerController.isHit = true;
        sr.sprite = hitSprite;

        // 0.2초 동안 피격 이미지 유지
        yield return new WaitForSeconds(hitSpriteTime);

        // 다시 걷는 애니메이션으로 복귀
        if (playerController != null) playerController.isHit = false;

        // 2. 남은 무적 시간 동안 반투명하게 깜빡거리기
        float blinkTime = invincibilityDuration - hitSpriteTime;
        for (int i = 0; i < 4; i++)
        {
            sr.color = new Color(1, 1, 1, 0.3f);
            yield return new WaitForSeconds(blinkTime / 8f);
            sr.color = Color.white;
            yield return new WaitForSeconds(blinkTime / 8f);
        }

        isInvincible = false;
    }

    void Die()
    {
        Debug.Log("플레이어 사망!");

        // 조작을 불가능하게 막음
        if (playerController != null) playerController.enabled = false;

        // 게임 오버 연출 시작!
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.StartGameOverSequence();
        }
        else
        {
            // 매니저가 없을 경우의 대비책
            gameObject.SetActive(false);
        }
    }

    // ★ [수정됨] 뒤에 개수(count)를 받을 수 있게 추가 (아무것도 안 적으면 기본 1개)
    public void AcquireItem(ItemData item, int count = 1)
    {
        // 1. 받은 개수(count)만큼 곱해서 스탯 증가!
        attackDamage += item.addDamage * count;

        if (item.addMaxHealth > 0)
        {
            maxHealth += item.addMaxHealth * count;
            currentHealth += item.addMaxHealth * count;
            UpdateHealthUI();
        }

        if (item.addMoveSpeed > 0 && playerController != null)
        {
            playerController.moveSpeed += item.addMoveSpeed * count;
        }

        if (itemGetSound != null)
        {
            audioSource.PlayOneShot(itemGetSound);
        }
    }

    // ★ [수정됨] 코인 획득 시 최대 99개 제한
    public void AddCoin(int amount)
    {
        coinCount += amount;
        if (coinCount > 99) coinCount = 99; // 99개 제한!
        UpdateCoinUI();

        if (coinGetSound != null) audioSource.PlayOneShot(coinGetSound);
    }

    // ★ [새로 추가됨] 상점에서 코인을 지불할 때 쓰는 함수
    public bool SpendCoin(int amount)
    {
        if (coinCount >= amount) // 돈이 충분하면?
        {
            coinCount -= amount;
            UpdateCoinUI();
            return true; // 결제 성공!
        }
        return false; // 돈이 부족함!
    }

    // ★ [새로 추가됨] 상점에서 체력을 샀을 때 회복하는 함수
    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateHealthUI();

        // ★ [추가됨] 체력이 회복될 때 효과음 재생! (바닥에서 먹든, 상점에서 사든 다 소리가 납니다)
        if (heartGetSound != null)
        {
            audioSource.PlayOneShot(heartGetSound);
        }
    }

    // ★ [새로 추가됨] 코인 글씨 업데이트
    private void UpdateCoinUI()
    {
        if (coinText != null)
        {
            // 00, 01, 15 처럼 깔끔하게 두 자리 숫자로 띄워줍니다 (원치 않으면 그냥 ToString() 사용)
            coinText.text = coinCount.ToString("D2");
        }
    }

    // ★ [새로 추가된 열쇠 획득/사용 함수]
    public void AddKey(int amount)
    {
        keyCount += amount;
        if (keyCount > 99) keyCount = 99;
        UpdateKeyUI();
        if (keyGetSound != null) audioSource.PlayOneShot(keyGetSound);
    }

    public bool SpendKey(int amount)
    {
        if (keyCount >= amount)
        {
            keyCount -= amount;
            UpdateKeyUI();
            return true;
        }
        return false; // 열쇠 부족!
    }

    private void UpdateKeyUI()
    {
        if (keyText != null) keyText.text = keyCount.ToString("D2");
    }
}