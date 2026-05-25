using UnityEngine;
using UnityEngine.Audio;

public class Enemy : MonoBehaviour
{
    public EnemyData enemyData;
    public RoomController currentRoom;

    private float currentHealth;
    public Transform playerTransform; // 복제될 때 전달받기 위해 public으로 변경
    private Rigidbody2D rb;

    public bool isAwake = false;

    public enum BossState { Idle, PrepDash, Dashing, Stunned }
    public BossState bossState = BossState.Idle;

    public int splitLevel = 0;
    public float myMaxHealth;       // 복제될 때 전달받기 위해 public으로 변경
    private float stateTimer = 0f;
    private Vector2 dashDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Setup(EnemyData data)
    {
        enemyData = data;
        myMaxHealth = enemyData.maxHealth;
        currentHealth = myMaxHealth;
        gameObject.name = enemyData.enemyName;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (enemyData.enemySprite != null) sr.sprite = enemyData.enemySprite;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        isAwake = false;
    }

    public void WakeUp()
    {
        isAwake = true;
    }

    // ★ [새로 추가됨] 대쉬할 때 팽이처럼 빙글빙글 도는 시각적 연출!
    void Update()
    {
        if (!isAwake || enemyData == null) return;

        if (enemyData.isDashSplittingBoss)
        {
            if (bossState == BossState.Dashing)
            {
                // Z축을 기준으로 초당 1500도 속도로 미친듯이 회전
                transform.Rotate(0, 0, 1500f * Time.deltaTime);
            }
            else
            {
                // 대쉬가 끝나면 다시 똑바로 서도록 부드럽게 복구
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, 15f * Time.deltaTime);
            }
        }
    }

    void FixedUpdate()
    {
        if (!isAwake || enemyData == null || playerTransform == null) return;

        if (enemyData.isDashSplittingBoss) HandleDashBoss();
        else HandleNormalEnemy();
    }

    private void HandleNormalEnemy()
    {
        Vector2 targetPos = playerTransform.position;
        Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, enemyData.moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);
    }

    private void HandleDashBoss()
    {
        switch (bossState)
        {
            case BossState.Idle:
                // ★ [수정됨] 쉬는 동안 가만히 있는게 아니라, 플레이어를 쫓아갑니다!
                Vector2 targetPos = playerTransform.position;
                Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, enemyData.moveSpeed * Time.fixedDeltaTime);
                rb.MovePosition(newPos);

                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    bossState = BossState.PrepDash;
                    stateTimer = enemyData.dashPrepTime;
                }
                break;

            case BossState.PrepDash:
                // 기를 모을 때는 제자리에 딱! 멈춰서 노려봄
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    dashDirection = (playerTransform.position - transform.position).normalized;
                    bossState = BossState.Dashing;
                }
                break;

            case BossState.Dashing:
                Vector2 dashVelocity = dashDirection * (enemyData.moveSpeed * enemyData.dashSpeedMultiplier);
                rb.MovePosition(rb.position + dashVelocity * Time.fixedDeltaTime);
                break;

            case BossState.Stunned:
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    bossState = BossState.Idle;
                    stateTimer = 1f;
                }
                break;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<Player>().TakeDamage(1);
        }
        else if (enemyData != null && enemyData.isDashSplittingBoss && bossState == BossState.Dashing)
        {
            if (collision.gameObject.CompareTag("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("EnemyBlocker"))
            {
                if (CameraShake.Instance != null) CameraShake.Instance.ShakeCamera(0.2f, 0.4f);

                TakeDamage(enemyData.wallCrashDamage);
                if (currentHealth > 0)
                {
                    bossState = BossState.Stunned;
                    stateTimer = enemyData.stunTime;
                }
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) collision.gameObject.GetComponent<Player>().TakeDamage(1);
    }

    public void TakeDamage(float damageAmount)
    {
        // ★ [핵심] 오버킬(남은 체력보다 더 큰 데미지) 방지를 위해 진짜 들어간 데미지만 계산
        float actualDamage = Mathf.Min(damageAmount, currentHealth);
        currentHealth -= actualDamage;
        PlaySoundWithMixer(enemyData.hitSound);

        // ★ [추가됨] 보스방에 있는 적(보스와 분신들)이 맞으면 통합 보스 체력바 깎기!
        if (currentRoom != null && currentRoom.isBossRoom)
        {
            if (BossUIManager.Instance != null) BossUIManager.Instance.ApplyBossDamage(actualDamage);
        }

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (enemyData.isDashSplittingBoss && splitLevel < 2)
        {
            if (currentHealth <= myMaxHealth / 2f)
            {
                Split();
            }
        }
    }

    private void Split()
    {
        for (int i = 0; i < 2; i++)
        {
            Vector3 randomOffset = (Vector3)Random.insideUnitCircle * 1.5f;
            GameObject splitBoss = Instantiate(gameObject, transform.position + randomOffset, Quaternion.identity);

            Enemy splitScript = splitBoss.GetComponent<Enemy>();

            // ★ [수정됨] 분신이 복제될 때 뇌(AI)를 완벽하게 초기화해줍니다!
            splitScript.splitLevel = this.splitLevel + 1;
            splitScript.myMaxHealth = this.myMaxHealth / 2f;
            splitScript.currentHealth = splitScript.myMaxHealth;
            splitScript.currentRoom = this.currentRoom;
            splitScript.playerTransform = this.playerTransform;
            splitScript.isAwake = true;

            // 전 상태(죽기 직전 기절 등)를 무시하고 무조건 추격 상태로 시작
            splitScript.bossState = BossState.Idle;
            splitScript.stateTimer = 1f;

            // 물리 속도도 초기화 (튕겨나가는 버그 방지)
            splitBoss.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

            splitBoss.transform.localScale = this.transform.localScale * 0.6f;

            if (currentRoom != null)
            {
                currentRoom.enemiesInRoom.Add(splitScript);
            }
        }

        Destroy(gameObject);
    }

    void Die()
    {
        PlaySoundWithMixer(enemyData.deathSound);
        Destroy(gameObject);
    }

    private void PlaySoundWithMixer(AudioClip clip)
    {
        if (clip == null) return;
        GameObject audioObj = new GameObject("TempAudio");
        audioObj.transform.position = transform.position;
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 0f;
        if (enemyData.sfxMixerGroup != null) source.outputAudioMixerGroup = enemyData.sfxMixerGroup;
        source.Play();
        Destroy(audioObj, clip.length);
    }
}