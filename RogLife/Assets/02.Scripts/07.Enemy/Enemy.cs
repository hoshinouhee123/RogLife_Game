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
    public float wakeUpDelay = 1.0f; // 깨어난 후 가만히 쳐다보는 시간 (초)
    private float currentWakeUpDelay = 0f;

    // 기존 BossState에 새로운 상태 3개 추가!
    public enum BossState { Idle, PrepDash, Dashing, Stunned, HiddenPattern, Reappearing, Invincible }
    public BossState bossState = BossState.Idle;

    // 패턴을 위한 타이머
    private float finalBossTimer = 0f;
    private SpriteRenderer mySpriteRenderer;
    private Collider2D myCollider;

    public int splitLevel = 0;
    public float myMaxHealth;       // 복제될 때 전달받기 위해 public으로 변경
    private float stateTimer = 0f;
    private Vector2 dashDirection;

    // ★ [새로 추가된 자물쇠 변수!] 한 번 분열했는지 기억합니다.
    private bool hasSplit = false;

    [Header("드랍 설정")]
    public GameObject coinPrefab;       // 떨어뜨릴 코인 프리팹
    [Range(0, 100)]
    public float coinDropChance = 50f;  // 코인 드랍 확률 (기본 50%)

    // [변수 선언부 쪽에 추가]
    [Header("레이저 패턴 스폰 위치 (자식 오브젝트 연결)")]
    public Transform[] pattern1_Lasers;
    public Transform[] pattern2_Lasers;
    public Transform[] pattern3_Lasers;
    public Transform[] pattern4_Lasers;

    // [변수 선언부에 추가]
    private Vector2 lastPos;
    private Sprite[] currentAnim;
    private int animFrame = 0;
    private float animTimer = 0f;

    // ★ [추가됨] 열쇠 드랍 설정
    public GameObject keyPrefab;
    [Range(0, 100)] public float keyDropChance = 10f; // 일반 몹은 10% 정도로 낮게!

    // [기존 변수 아래에 추가]
    private float fireTimer = 0f; // 원거리 몹 사격 쿨타임 타이머

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

        // ==========================================
        // ★ [수정됨] 내 몸에 맞게 '원형' 콜라이더 크기 조절하기!
        // ==========================================
        CircleCollider2D circleCol = GetComponent<CircleCollider2D>();
        if (circleCol != null)
        {
            if (enemyData.useCustomHitbox)
            {
                // 데이터에 적힌 반지름대로 수동 조절
                circleCol.radius = enemyData.hitboxRadius;
                circleCol.offset = enemyData.hitboxOffset;
            }
            else if (sr.sprite != null)
            {
                // 수동 설정을 안 켰다면, 이미지의 가로/세로 중 더 긴 쪽을 기준으로 넉넉하게 원을 씌워줌!
                float extentsX = sr.sprite.bounds.extents.x;
                float extentsY = sr.sprite.bounds.extents.y;
                circleCol.radius = Mathf.Max(extentsX, extentsY);
                circleCol.offset = Vector2.zero;
            }
        }
        // ==========================================

        isAwake = false;

        mySpriteRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        finalBossTimer = data.patternCooldown; // 패턴 쿨타임 장전
    }

    public void WakeUp()
    {
        isAwake = true;
        // 깨어나면 타이머 1초 장전!
        currentWakeUpDelay = wakeUpDelay;
    }

    // [Update() 함수 전체를 아래 코드로 덮어쓰기]
    void Update()
    {
        if (!isAwake || enemyData == null) return;

        // 1. 대쉬 보스 회전 연출 (기존 코드)
        if (enemyData.isDashSplittingBoss)
        {
            if (bossState == BossState.Dashing) transform.Rotate(0, 0, 1500f * Time.deltaTime);
            else transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, 15f * Time.deltaTime);
        }

        // ==========================================
        // ★ 2. [새로 추가] 상하좌우 걷는 애니메이션 재생!
        // ==========================================
        // 애니메이션 이미지가 1개라도 등록되어 있고, 숨어있지 않을 때만 작동
        if (enemyData.animDown != null && enemyData.animDown.Length > 0 && bossState != BossState.HiddenPattern)
        {
            Vector2 moveVelocity = (Vector2)transform.position - lastPos;
            lastPos = transform.position;

            if (moveVelocity.sqrMagnitude > 0.0001f) // 찌끔이라도 움직이고 있다면?
            {
                // 가로 이동 vs 세로 이동 비교
                if (Mathf.Abs(moveVelocity.x) > Mathf.Abs(moveVelocity.y))
                {
                    currentAnim = moveVelocity.x > 0 ? enemyData.animRight : enemyData.animLeft;
                }
                else
                {
                    currentAnim = moveVelocity.y > 0 ? enemyData.animUp : enemyData.animDown;
                }

                // 타이머에 맞춰 이미지(프레임) 갈아 끼우기
                animTimer += Time.deltaTime;
                if (animTimer >= enemyData.animFrameTime)
                {
                    animTimer = 0f;
                    animFrame++;
                    if (animFrame >= currentAnim.Length) animFrame = 0;
                    GetComponent<SpriteRenderer>().sprite = currentAnim[animFrame];
                }
            }
            else // 가만히 멈춰있을 때
            {
                animFrame = 0;
                if (currentAnim != null && currentAnim.Length > 0)
                    GetComponent<SpriteRenderer>().sprite = currentAnim[0]; // 기본 서 있는 포즈
            }
        }
    }

    void FixedUpdate()
    {
        if (!isAwake || enemyData == null || playerTransform == null) return;

        // ★ [수정됨] 최종 보스 로직 추가
        if (enemyData.isFinalBoss) HandleFinalBoss();
        else if (enemyData.isDashSplittingBoss) HandleDashBoss();
        else if (enemyData.isStealthBoss) HandleStealthBoss();
        else HandleNormalEnemy();
    }

    // ★ [수정됨] 일반 몹 / 원거리 몹 이동 및 사격 로직 분리
    // ★ [수정됨] 뭉침 방지(Separation) 기술이 적용된 이동 AI
    private void HandleNormalEnemy()
    {
        // 1. 플레이어를 향하는 기본 방향
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;

        // ==========================================
        // ★ 2. 서로 겹치지 않게 밀어내는 힘(Separation) 계산
        // ==========================================
        Vector2 separationForce = Vector2.zero;

        // 내 주변 반경 1.5f 안에 있는 모든 콜라이더를 찾습니다.
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, 1.5f);
        foreach (Collider2D col in nearbyEnemies)
        {
            // 찾은 것 중에 나 자신이 아니고, 'Enemy' 태그를 가진 다른 적이라면?
            if (col.gameObject != this.gameObject && col.CompareTag("Enemy"))
            {
                // 나와 상대방의 거리 차이를 계산해서 반대 방향으로 밀어냄! (가까울수록 강하게)
                Vector2 pushDirection = transform.position - col.transform.position;
                separationForce += pushDirection.normalized / Mathf.Max(pushDirection.magnitude, 0.1f);
            }
        }

        // 3. 최종 이동 방향 = (플레이어 방향) + (서로 밀어내는 힘 * 0.5배)
        Vector2 moveDir = (directionToPlayer + separationForce * 0.5f).normalized;
        // ==========================================

        // 4. 계산된 최종 방향(moveDir)으로 이동!
        if (enemyData.isShooter)
        {
            float dist = Vector2.Distance(rb.position, playerTransform.position);
            if (dist > enemyData.attackRange)
            {
                rb.MovePosition(rb.position + moveDir * enemyData.moveSpeed * Time.fixedDeltaTime);
            }
            else
            {
                // 무빙샷
                rb.MovePosition(rb.position + moveDir * (enemyData.moveSpeed * 0.3f) * Time.fixedDeltaTime);

                fireTimer -= Time.fixedDeltaTime;
                if (fireTimer <= 0)
                {
                    ShootAtPlayer();
                    fireTimer = enemyData.fireRate;
                }
            }
        }
        else
        {
            // 일반 몹
            rb.MovePosition(rb.position + moveDir * enemyData.moveSpeed * Time.fixedDeltaTime);
        }
    }

    // ★ [수정됨] 대쉬 보스의 AI 패턴 (자석 기능 + 무한 팽이 방지 추가)
    private void HandleDashBoss()
    {
        switch (bossState)
        {
            case BossState.Idle:
                // 1. 보스도 평상시엔 자석처럼 서로 밀어내며 플레이어를 쫓아갑니다.
                Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
                Vector2 separationForce = Vector2.zero;

                Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, 1.5f);
                foreach (Collider2D col in nearbyEnemies)
                {
                    if (col.gameObject != this.gameObject && col.CompareTag("Enemy"))
                    {
                        Vector2 pushDirection = transform.position - col.transform.position;
                        separationForce += pushDirection.normalized / Mathf.Max(pushDirection.magnitude, 0.1f);
                    }
                }

                Vector2 moveDir = (directionToPlayer + separationForce * 0.5f).normalized;
                rb.MovePosition(rb.position + moveDir * enemyData.moveSpeed * Time.fixedDeltaTime);

                // 2. 쿨타임이 차면 대쉬 준비 상태로 넘어감
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    bossState = BossState.PrepDash;
                    stateTimer = enemyData.dashPrepTime;
                }
                break;

            case BossState.PrepDash:
                // 대쉬 준비 (기를 모음)
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    dashDirection = (playerTransform.position - transform.position).normalized;
                    bossState = BossState.Dashing;

                    // ★ [핵심 안전장치] 대쉬 최대 지속 시간을 2초로 강제 설정합니다!
                    stateTimer = 2.0f;
                }
                break;

            case BossState.Dashing:
                // 미친 속도로 돌진!
                Vector2 dashVelocity = dashDirection * (enemyData.moveSpeed * enemyData.dashSpeedMultiplier);
                rb.MovePosition(rb.position + dashVelocity * Time.fixedDeltaTime);

                // ★ [핵심 안전장치] 만약 벽에 못 박고 2초가 지났다면? 강제로 대쉬를 끝내버립니다!
                // (어딘가에 껴서 영원히 빙글빙글 도는 버그 원천 차단)
                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0)
                {
                    bossState = BossState.Idle;
                    stateTimer = 1f; // 1초 쉬었다가 다시 패턴 시작
                }
                break;

            case BossState.Stunned:
                // 기절 상태
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
        // 사라졌을 때나 무적 등장 중일 때는 데미지 0!
        if (currentHealth <= 0 || hasSplit || bossState == BossState.HiddenPattern || bossState == BossState.Reappearing) return;

        float actualDamage = Mathf.Min(damageAmount, currentHealth);
        currentHealth -= actualDamage;
        PlaySoundWithMixer(enemyData.hitSound);

        if (currentRoom != null && currentRoom.isBossRoom)
        {
            if (BossUIManager.Instance != null) BossUIManager.Instance.ApplyBossDamage(actualDamage);
        }

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // ★ [핵심 방어 2] hasSplit이 false일 때만 분열 허용
        if (enemyData.isDashSplittingBoss && splitLevel < 2 && !hasSplit)
        {
            if (currentHealth <= myMaxHealth / 2f)
            {
                hasSplit = true; // 자물쇠 쾅! (이후 들어오는 총알은 무시됨)
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

            // 스탯 및 상태 초기화
            splitScript.splitLevel = this.splitLevel + 1;
            splitScript.myMaxHealth = this.myMaxHealth / 2f;
            splitScript.currentHealth = splitScript.myMaxHealth;
            splitScript.currentRoom = this.currentRoom;
            splitScript.playerTransform = this.playerTransform;
            splitScript.isAwake = true;

            // ★ [핵심 방어 3] 새로 태어난 꼬마 보스는 다시 분열할 수 있게 자물쇠를 풀어줌!
            splitScript.hasSplit = false;

            splitScript.bossState = BossState.Idle;
            splitScript.stateTimer = 1f;

            splitBoss.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            splitBoss.transform.localScale = this.transform.localScale * 0.6f;

            if (currentRoom != null)
            {
                currentRoom.enemiesInRoom.Add(splitScript);
            }
        }

        Destroy(gameObject); // 원본은 파괴
    }

    // ★ [수정됨] 죽을 때 분열하는 로직 추가
    void Die()
    {
        PlaySoundWithMixer(enemyData.deathSound);

        if (enemyData.deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(enemyData.deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1.5f);
        }

        // 코인 & 열쇠 드랍 (기존 코드)
        if (coinPrefab != null && Random.Range(0, 100f) <= coinDropChance) Instantiate(coinPrefab, transform.position, Quaternion.identity);
        if (keyPrefab != null && Random.Range(0, 100f) <= keyDropChance) Instantiate(keyPrefab, transform.position, Quaternion.identity);

        // ==========================================
        // ★ [새로 추가됨] 죽을 때 2마리로 쪼개짐!
        // (원본(0)일 때만 분열하고, 분열된 애들은 다시 분열하지 않음)
        // ==========================================
        if (enemyData != null && enemyData.isNormalSplitter && splitLevel == 0)
        {
            for (int i = 0; i < 2; i++)
            {
                // 옆으로 살짝 비켜서 스폰
                Vector3 randomOffset = (Vector3)Random.insideUnitCircle * 0.8f;
                GameObject splitMob = Instantiate(gameObject, transform.position + randomOffset, Quaternion.identity);

                Enemy splitScript = splitMob.GetComponent<Enemy>();

                // 스탯 절반 깎고 복제 세팅
                splitScript.splitLevel = this.splitLevel + 1;
                splitScript.myMaxHealth = this.myMaxHealth / 2f;
                if (splitScript.myMaxHealth < 1) splitScript.myMaxHealth = 1; // 체력 0 방지
                splitScript.currentHealth = splitScript.myMaxHealth;

                splitScript.currentRoom = this.currentRoom;
                splitScript.playerTransform = this.playerTransform;
                splitScript.isAwake = true; // 태어나자마자 바로 추격 시작

                splitMob.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                splitMob.transform.localScale = this.transform.localScale * 0.7f; // 크기도 70%로 축소

                if (currentRoom != null) currentRoom.enemiesInRoom.Add(splitScript);
            }
        }

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

    // ★ [새로 추가] 총알 생성 및 발사
    private void ShootAtPlayer()
    {
        if (enemyData.enemyBulletPrefab != null)
        {
            // ★ [추가됨] 총알 쏠 때 효과음 재생!
            if (enemyData.shootSound != null)
            {
                PlaySoundWithMixer(enemyData.shootSound);
            }

            GameObject bullet = Instantiate(enemyData.enemyBulletPrefab, transform.position, Quaternion.identity);
            Vector2 dir = (playerTransform.position - transform.position).normalized;

            bullet.GetComponent<EnemyBullet>().Setup(dir, enemyData.damage);
        }
    }

    // ==========================================
    // ★ [새로 추가됨] 2층 시야 기믹 보스 AI
    // ==========================================
    private void HandleStealthBoss()
    {
        Player playerScript = playerTransform.GetComponent<Player>();
        if (playerScript == null) return;

        // 1. 플레이어 중심에서 나(보스)를 향하는 방향 벡터
        Vector2 dirToBoss = (transform.position - playerTransform.position).normalized;

        // 2. 플레이어가 현재 바라보고 있는 방향 벡터
        Vector2 playerFacing = playerScript.lastFacingDir;

        // 3. 두 방향 사이의 '각도'를 수학적으로 계산!
        float angle = Vector2.Angle(playerFacing, dirToBoss);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float currentSpeed = enemyData.moveSpeed;

        // 4. 내가 플레이어의 시야각(예: 좌우 45도 = 총 90도 부채꼴) 안에 들어왔는가?
        if (angle < enemyData.sightAngle)
        {
            // [시야 안 (빛)]
            // 괴로워하며 속도가 절반으로 뚝 떨어지고, 모습이 완전히 드러납니다.
            currentSpeed *= 0.5f;
            sr.color = Color.white;
        }
        else
        {
            // [시야 밖 (어둠)]
            // 모습이 거의 안 보이는 까만 그림자 상태로, 미친 속도(대쉬 속도)로 덮쳐옵니다!
            currentSpeed *= enemyData.dashSpeedMultiplier;

            // 검은색에 가까우면서 반투명하게 만듦 (유령처럼)
            sr.color = new Color(0.1f, 0f, 0f, 0.3f);
        }

        // 5. 계산된 속도로 플레이어를 향해 이동
        Vector2 targetPos = playerTransform.position;
        Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, currentSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);
    }

    // ==========================================
    // ★ 5층 최종 보스 (도플갱어) AI
    // ==========================================
    private void HandleFinalBoss()
    {
        if (bossState == BossState.Idle)
        {
            // 1. 평상시엔 플레이어를 쫓아다님
            Vector2 targetPos = playerTransform.position;
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, enemyData.moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);

            // 2. 쿨타임이 다 되면 레이저 패턴 시작!
            finalBossTimer -= Time.fixedDeltaTime;
            if (finalBossTimer <= 0)
            {
                StartCoroutine(LaserPatternRoutine());
            }
        }
    }

    // ★ [완전히 깔끔해진 레이저 패턴 코루틴!]
    private System.Collections.IEnumerator LaserPatternRoutine()
    {
        bossState = BossState.HiddenPattern;

        mySpriteRenderer.enabled = false;
        myCollider.enabled = false;

        // 보스를 방 중앙으로 옮깁니다.
        transform.position = currentRoom.transform.position;

        // ==========================================
        // ★ [수정됨] t.position 대신, 부모 크기 변화를 무시하는 공식을 사용합니다!
        // (보스 위치 + 원래 설정해둔 순수한 거리값)
        // ==========================================

        // ⚔️ 1차 패턴
        foreach (Transform t in pattern1_Lasers)
        {
            Vector3 realPos = transform.position + t.localPosition;
            SpawnLaser(realPos, t.rotation);
        }
        yield return new WaitForSeconds(1.5f);

        // ⚔️ 2차 패턴
        foreach (Transform t in pattern2_Lasers)
        {
            Vector3 realPos = transform.position + t.localPosition;
            SpawnLaser(realPos, t.rotation);
        }
        yield return new WaitForSeconds(1.5f);

        // ⚔️ 3차 패턴
        foreach (Transform t in pattern3_Lasers)
        {
            Vector3 realPos = transform.position + t.localPosition;
            SpawnLaser(realPos, t.rotation);
        }
        yield return new WaitForSeconds(1.5f);

        // ⚔️ 4차 패턴
        foreach (Transform t in pattern4_Lasers)
        {
            Vector3 realPos = transform.position + t.localPosition;
            SpawnLaser(realPos, t.rotation);
        }
        yield return new WaitForSeconds(1.5f);

        // 🌟 패턴 종료
        bossState = BossState.Reappearing;
        mySpriteRenderer.enabled = true;

        for (int i = 0; i < 5; i++)
        {
            mySpriteRenderer.color = new Color(1, 1, 1, 0.3f);
            yield return new WaitForSeconds(0.15f);
            mySpriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.15f);
        }

        myCollider.enabled = true;
        bossState = BossState.Idle;
        finalBossTimer = enemyData.patternCooldown;
    }

    private void SpawnLaser(Vector3 pos, Quaternion rot)
    {
        if (enemyData.laserBlasterPrefab != null)
        {
            GameObject laser = Instantiate(enemyData.laserBlasterPrefab, pos, rot);
            laser.GetComponent<LaserBlaster>().Setup(enemyData.damage);
        }
    }
}