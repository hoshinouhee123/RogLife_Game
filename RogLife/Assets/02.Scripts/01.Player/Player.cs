using UnityEngine;
using UnityEngine.UI; // UI를 다루기 위해 필수
using System.Collections;
using UnityEngine.Audio; // 오디오 믹서를 사용하기 위해 필수

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

    // 인스펙터에서 SFX 믹서 그룹을 넣을 빈칸
    public AudioMixerGroup sfxMixerGroup;

    // [여기에 아이템 획득 효과음 변수 추가!]
    public AudioClip itemGetSound;

    private AudioSource audioSource;
    private PlayerController playerController;
    private SpriteRenderer sr;

    void Start()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();

        // 내 몸에 오디오 소스가 없으면 자동으로 하나 달아줌
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (sfxMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
        }

        UpdateHealthUI();
    }

    void Update()
    {
        HandleShooting();
    }

    // 공격 (화살표 키)
    void HandleShooting()
    {
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
    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        UpdateHealthUI();

        // 맞았을 때 플레이어 비명 소리 재생
        if (getHitSound != null) audioSource.PlayOneShot(getHitSound);

        if (currentHealth <= 0) Die();
        else StartCoroutine(InvincibilityRoutine());
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
        Debug.Log("게임 오버!");
        // 여기서 게임 오버 UI를 띄우거나 씬을 재시작합니다.
        gameObject.SetActive(false);
    }

    public void AcquireItem(ItemData item)
    {
        // 1. 공격력 증가
        attackDamage += item.addDamage;

        // 2. 체력이 늘어나는 아이템이라면?
        if (item.addMaxHealth > 0)
        {
            maxHealth += item.addMaxHealth;
            currentHealth += item.addMaxHealth; // 늘어난 만큼 피도 채워줌
            UpdateHealthUI(); // 늘어난 하트 UI 새로고침
        }

        // [2. 여기에 효과음 재생 코드 한 줄 추가!]
        if (itemGetSound != null)
        {
            audioSource.PlayOneShot(itemGetSound);
        }

    }
}