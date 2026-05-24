using UnityEngine;
using UnityEngine.UI; // UI를 다루기 위해 필수
using System.Collections;

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

    private SpriteRenderer sr;

    void Start()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
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
        // 총알을 생성하고 방향과 데미지를 전달
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<Bullet>().Setup(dir, attackDamage);
    }

    // 적에게 맞았을 때 호출되는 함수
    public void TakeDamage(int damage)
    {
        if (isInvincible) return; // 무적 상태면 무시

        currentHealth -= damage;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
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
    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        // 깜빡거림 연출
        for (int i = 0; i < 5; i++)
        {
            sr.color = new Color(1, 1, 1, 0.3f); // 반투명
            yield return new WaitForSeconds(invincibilityDuration / 10f);
            sr.color = Color.white; // 원상복구
            yield return new WaitForSeconds(invincibilityDuration / 10f);
        }

        isInvincible = false;
    }

    void Die()
    {
        Debug.Log("게임 오버!");
        // 여기서 게임 오버 UI를 띄우거나 씬을 재시작합니다.
        gameObject.SetActive(false);
    }
}