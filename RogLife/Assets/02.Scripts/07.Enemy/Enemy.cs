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

    // ★ [새로 추가된 변수] 방금 쓴 패턴 번호를 기억합니다. (-1은 아직 안 썼다는 뜻)
    private int lastPattern = -1;

    // ==========================================
    // ★ [새로 추가된 변수들] 패턴 제비뽑기 주머니!
    // ==========================================
    private System.Collections.Generic.List<int> patternPool = new System.Collections.Generic.List<int>();
    private bool isFirstCycle = true; // 첫 번째 사이클(처음)인지 확인하는 스위치

    // 패턴을 위한 타이머
    private float finalBossTimer = 0f;
    private SpriteRenderer mySpriteRenderer;
    private Collider2D myCollider;

    // [변수 선언부 쪽에 추가]
    private GameObject currentBlackFrame; // 방을 덮어줄 검은색 테두리

    // ★ [새로 추가됨] 숨겨둔 옆 방들을 기억할 리스트
    private System.Collections.Generic.List<GameObject> hiddenRooms = new System.Collections.Generic.List<GameObject>();


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

    private bool isPhase2 = false; // 체력 50% 이하 확인용 스위치

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

        // ==========================================
        // ★ [새로 추가됨] 5층 보스 체력 50% 이하 진입 체크!
        // ==========================================
        if (enemyData.isFinalBoss && !isPhase2 && currentHealth <= myMaxHealth * 0.5f)
        {
            isPhase2 = true;
            // 즉시 새로운 패턴 주머니를 섞도록 유도합니다.
            patternPool.Clear();
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

    // [Enemy.cs 맨 아래쪽, OnDestroy 함수 덮어쓰기]
    private void OnDestroy()
    {
        if (Camera.main != null) Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);
        if (currentBlackFrame != null) Destroy(currentBlackFrame);

        // ★ 보스가 죽어서 스크립트가 파괴될 때, 꺼져있던 맵이 있다면 전부 강제로 켜줍니다!
        if (hiddenRooms != null)
        {
            foreach (var obj in hiddenRooms)
            {
                if (obj != null) obj.SetActive(true);
            }
        }
        if (MapGenerator.Instance != null && MapGenerator.Instance.backgroundTilemap != null)
        {
            MapGenerator.Instance.backgroundTilemap.gameObject.SetActive(true);
        }
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
    // ★ 5층 최종 보스 AI (테트리스식 셔플 백 시스템)
    // ==========================================
    private void HandleFinalBoss()
    {
        if (bossState == BossState.Idle)
        {
            Vector2 targetPos = playerTransform.position;
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, enemyData.moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);

            finalBossTimer -= Time.fixedDeltaTime;
            if (finalBossTimer <= 0)
            {
                // ==========================================
                // ★ [핵심] 패턴 주머니(Pool)가 비어있으면 새로 채웁니다!
                // ==========================================
                if (patternPool.Count == 0)
                {
                    if (isFirstCycle)
                    {
                        patternPool = new System.Collections.Generic.List<int>() { 1, 2, 3, 4, 0 };
                        isFirstCycle = false;
                    }
                    else
                    {
                        // ★ [수정됨] 2페이즈(50% 이하)라면 5번 패턴(기억력 레이저) 추가!
                        if (isPhase2) patternPool = new System.Collections.Generic.List<int>() { 0, 1, 2, 3, 4, 5 };
                        else patternPool = new System.Collections.Generic.List<int>() { 0, 1, 2, 3, 4 };

                        for (int i = 0; i < patternPool.Count; i++)
                        {
                            int temp = patternPool[i];
                            int randomIndex = Random.Range(i, patternPool.Count);
                            patternPool[i] = patternPool[randomIndex];
                            patternPool[randomIndex] = temp;
                        }

                        if (patternPool[0] == lastPattern)
                        {
                            int temp = patternPool[0]; patternPool[0] = patternPool[1]; patternPool[1] = temp;
                        }
                    }
                }

                // ==========================================
                // ★ 주머니에서 맨 앞(0번) 패턴을 꺼냅니다.
                // ==========================================
                int currentPattern = patternPool[0];
                patternPool.RemoveAt(0); // 꺼낸 건 주머니에서 지움
                lastPattern = currentPattern; // 방금 쓴 패턴 기억

                // ★ [추가됨] 5번 패턴 분기 추가
                if (currentPattern == 0) StartCoroutine(ScreenFlipPatternRoutine());
                else if (currentPattern == 1) StartCoroutine(LaserPatternRoutine());
                else if (currentPattern == 2) StartCoroutine(StarPatternRoutine());
                else if (currentPattern == 3) StartCoroutine(SpinPatternRoutine());
                else if (currentPattern == 4) StartCoroutine(MeteorShowerRoutine());
                else if (currentPattern == 5) StartCoroutine(MemoryLaserRoutine()); // 6번째 궁극기!
            }
        }
    }



    private void SpawnLaser(Vector3 pos, Quaternion rot)
    {
        if (enemyData.laserBlasterPrefab != null)
        {
            GameObject laser = Instantiate(enemyData.laserBlasterPrefab, pos, rot);
            laser.GetComponent<LaserBlaster>().Setup(enemyData.damage);
        }
    }

    // ⚔️ 1번 패턴: 지정된 위치에서 상하좌우 레이저
    private System.Collections.IEnumerator LaserPatternRoutine()
    {
        bossState = BossState.HiddenPattern;

        if (mySpriteRenderer != null) mySpriteRenderer.enabled = false;
        if (myCollider != null) myCollider.enabled = false;

        transform.position = currentRoom.transform.position;

        // ★ [방어 코드] 배열에 빈칸이 있어도 에러가 나지 않도록 막아줍니다.
        if (pattern1_Lasers != null) { foreach (Transform t in pattern1_Lasers) { if (t != null) SpawnLaser(transform.position + t.localPosition, t.rotation); } }
        yield return new WaitForSeconds(1.5f);

        if (pattern2_Lasers != null) { foreach (Transform t in pattern2_Lasers) { if (t != null) SpawnLaser(transform.position + t.localPosition, t.rotation); } }
        yield return new WaitForSeconds(1.5f);

        if (pattern3_Lasers != null) { foreach (Transform t in pattern3_Lasers) { if (t != null) SpawnLaser(transform.position + t.localPosition, t.rotation); } }
        yield return new WaitForSeconds(1.5f);

        if (pattern4_Lasers != null) { foreach (Transform t in pattern4_Lasers) { if (t != null) SpawnLaser(transform.position + t.localPosition, t.rotation); } }
        yield return new WaitForSeconds(1.5f);

        // 🌟 패턴 종료: 방 정중앙에 등장
        yield return StartCoroutine(ReappearRoutine());
    }

    // ⚔️ 2번 패턴: 별똥별 십자 레이저
    private System.Collections.IEnumerator StarPatternRoutine()
    {
        bossState = BossState.HiddenPattern;

        if (mySpriteRenderer != null) mySpriteRenderer.enabled = false;
        if (myCollider != null) myCollider.enabled = false;

        Vector3 roomCenter = currentRoom.transform.position;

        for (int i = 0; i < 4; i++)
        {
            float randomX = Random.Range(-5.5f, 5.5f);
            float randomY = Random.Range(-2.5f, 3.5f);
            Vector3 targetPos = roomCenter + new Vector3(randomX, randomY, 0);

            if (enemyData.starPrefab != null)
            {
                GameObject star = Instantiate(enemyData.starPrefab, targetPos, Quaternion.identity);
                star.GetComponent<StarFalling>().Setup(targetPos, enemyData.damage, enemyData.laserBlasterPrefab);
            }
            yield return new WaitForSeconds(1.0f);
        }

        yield return new WaitForSeconds(2.0f);

        // 🌟 패턴 종료
        yield return StartCoroutine(ReappearRoutine());
    }

    // ==========================================
    // ★ 3번 패턴: 회전 십자 레이저 (환영 없이 딱 1바퀴씩만 회전!)
    // ==========================================
    private System.Collections.IEnumerator SpinPatternRoutine()
    {
        bossState = BossState.HiddenPattern;

        if (mySpriteRenderer != null) mySpriteRenderer.enabled = false;
        if (myCollider != null) myCollider.enabled = false;

        Vector3 roomCenter = currentRoom.transform.position;

        GameObject laserHub = new GameObject("LaserHub");
        laserHub.transform.position = roomCenter;

        // 속도가 0이 되어 멈추는 에러 방지
        float spinSpeed = enemyData.laserSpinSpeed > 0 ? enemyData.laserSpinSpeed : 100f;

        // ★ [핵심] 360f(1바퀴) 도는 시간 계산
        float spinDuration = 360f / spinSpeed;
        float totalLaserDuration = (spinDuration * 2f) + 2.0f;

        // 레이저 4개 십자 모양으로 소환
        for (int i = 0; i < 4; i++)
        {
            if (enemyData.laserBlasterPrefab != null)
            {
                GameObject laser = Instantiate(enemyData.laserBlasterPrefab, roomCenter, Quaternion.Euler(0, 0, i * 90f));
                laser.transform.SetParent(laserHub.transform);
                laser.GetComponent<LaserBlaster>().Setup(enemyData.damage, 1.0f, totalLaserDuration);
            }
        }

        // 레이저 스케일 조절 후 1초 대기
        laserHub.transform.localScale = new Vector3(enemyData.spinLaserScale, enemyData.spinLaserScale, 1f);
        yield return new WaitForSeconds(1.0f);

        // 🟢 1. 시계방향 회전 (점점 느려지며 정확히 1바퀴-360도)
        float timer = 0f;
        float previousAngle = 0f;
        while (timer < spinDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / spinDuration);
            float easeT = Mathf.Sin(t * Mathf.PI / 2f);

            float currentAngle = 360f * easeT;

            float step = currentAngle - previousAngle;
            if (laserHub != null) laserHub.transform.Rotate(0, 0, -step); // 마이너스가 시계방향
            previousAngle = currentAngle;
            yield return null;
        }

        // 🔴 2. 반시계방향 회전 (점점 빨라지며 정확히 1바퀴-360도)
        timer = 0f;
        previousAngle = 0f;
        while (timer < spinDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / spinDuration);
            float easeT = 1f - Mathf.Cos(t * Mathf.PI / 2f);

            float currentAngle = 360f * easeT;

            float step = currentAngle - previousAngle;
            if (laserHub != null) laserHub.transform.Rotate(0, 0, step); // 플러스가 반시계방향
            previousAngle = currentAngle;
            yield return null;
        }

        if (laserHub != null) Destroy(laserHub);
        yield return new WaitForSeconds(0.5f);

        // 🌟 패턴 종료 및 재등장
        yield return StartCoroutine(ReappearRoutine());
    }

    // ==========================================
    // ★ [새로 추가] 보스가 무적 상태로 나타나는 기능을 하나로 통일!
    // (어떤 에러가 나도 무조건 상태를 정상 복구시킵니다)
    // ==========================================
    private System.Collections.IEnumerator ReappearRoutine()
    {
        bossState = BossState.Reappearing;
        transform.position = currentRoom.transform.position;

        if (mySpriteRenderer != null) mySpriteRenderer.enabled = true;

        for (int i = 0; i < 5; i++)
        {
            if (mySpriteRenderer != null) mySpriteRenderer.color = new Color(1, 1, 1, 0.3f);
            yield return new WaitForSeconds(0.15f);
            if (mySpriteRenderer != null) mySpriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.15f);
        }

        // 완벽하게 상태 복구!
        if (myCollider != null) myCollider.enabled = true;
        bossState = BossState.Idle;
        finalBossTimer = enemyData.patternCooldown;
    }

    // ==========================================
    // ★ 4번 패턴: 화면 반전 (옆방 완벽 차단 업그레이드!)
    // ==========================================
    private System.Collections.IEnumerator ScreenFlipPatternRoutine()
    {
        bossState = BossState.HiddenPattern;
        rb.linearVelocity = Vector2.zero;

        // ==========================================
        // ★ [강력한 해결책 1] 내 방 빼고 다른 모든 방과 배경을 아예 꺼버림!
        // ==========================================
        hiddenRooms.Clear();
        RoomController[] allRooms = FindObjectsOfType<RoomController>();
        foreach (var room in allRooms)
        {
            if (room != currentRoom) // 내가 있는 방이 아니면?
            {
                hiddenRooms.Add(room.gameObject); // 리스트에 기억해두고
                room.gameObject.SetActive(false); // 꺼버림!
            }
        }

        // 배경 타일맵(우주)도 꺼버림
        if (MapGenerator.Instance != null && MapGenerator.Instance.backgroundTilemap != null)
            MapGenerator.Instance.backgroundTilemap.gameObject.SetActive(false);

        // ==========================================
        // ★ [강력한 해결책 2] 검은 테두리를 안쪽으로 0.2칸 당겨서 문 틈새마저 완벽 차단!
        // ==========================================
        currentBlackFrame = new GameObject("BlackFrame");
        currentBlackFrame.transform.position = currentRoom.transform.position;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);        //화이트로 수정
        tex.Apply();
        Sprite blackSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        Vector3[] framePos = { new Vector3(0, 29.8f, 0), new Vector3(0, -29.8f, 0), new Vector3(-33.8f, 0, 0), new Vector3(33.8f, 0, 0) };
        foreach (Vector3 pos in framePos)
        {
            GameObject wall = new GameObject("BlackWall");
            wall.transform.SetParent(currentBlackFrame.transform);
            wall.transform.localPosition = pos;
            wall.transform.localScale = new Vector3(50f, 50f, 1f);

            SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = blackSprite;
            sr.color = Color.black;
            sr.sortingOrder = 32000; // ★ 그 어떤 이미지보다 무조건 덮어버림
        }

        // --- 화면 회전 시작 ---
        Camera cam = Camera.main;
        float flipTime = 1.5f;
        float timer = 0f;

        while (timer < flipTime)
        {
            timer += Time.deltaTime;
            float t = timer / flipTime;
            float ease = Mathf.SmoothStep(0f, 1f, t);
            cam.transform.rotation = Quaternion.Euler(0, 0, 180f * ease);
            yield return null;
        }
        cam.transform.rotation = Quaternion.Euler(0, 0, 180f);

        // 5초간 추격!
        float chaseTime = 5.0f;
        timer = 0f;
        while (timer < chaseTime)
        {
            timer += Time.fixedDeltaTime;
            Vector2 targetPos = playerTransform.position;
            Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, enemyData.moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPos);
            yield return new WaitForFixedUpdate();
        }

        // 추격 정지 및 화면 원상 복구
        rb.linearVelocity = Vector2.zero;
        timer = 0f;
        while (timer < flipTime)
        {
            timer += Time.deltaTime;
            float t = timer / flipTime;
            float ease = Mathf.SmoothStep(0f, 1f, t);
            cam.transform.rotation = Quaternion.Euler(0, 0, 180f + (180f * ease));
            yield return null;
        }
        cam.transform.rotation = Quaternion.Euler(0, 0, 0);

        if (currentBlackFrame != null) Destroy(currentBlackFrame);

        // ==========================================
        // ★ 연출 종료: 아까 꺼뒀던 방과 배경을 다시 원상복구!
        // ==========================================
        foreach (var obj in hiddenRooms)
        {
            if (obj != null) obj.SetActive(true);
        }
        hiddenRooms.Clear();

        if (MapGenerator.Instance != null && MapGenerator.Instance.backgroundTilemap != null)
            MapGenerator.Instance.backgroundTilemap.gameObject.SetActive(true);

        bossState = BossState.Idle;
        finalBossTimer = enemyData.patternCooldown;
    }

    // ==========================================
    // ★ 5번 패턴: 유성우 폭격 (갈수록 빠르고 거세짐)
    // ==========================================
    private System.Collections.IEnumerator MeteorShowerRoutine()
    {
        bossState = BossState.HiddenPattern;
        if (mySpriteRenderer != null) mySpriteRenderer.enabled = false;
        if (myCollider != null) myCollider.enabled = false;

        Vector3 roomCenter = currentRoom.transform.position;

        // ==========================================
        // ★ [수정됨] 별이 떨어질 범위를 벽보다 안쪽(안전 구역)으로 좁혔습니다!
        // ==========================================
        // 알려주신 맵 크기보다 여유 있게 상하좌우 1~1.5칸씩 안쪽으로 당겼습니다.
        float mapLeft = roomCenter.x - 4.5f;
        float mapRight = roomCenter.x + 4.5f;
        float mapBottom = roomCenter.y - 1.5f;
        float mapTop = roomCenter.y + 2.5f;

        float spawnDelay = 0.6f;
        float currentFallSpeed = 10f;

        for (int i = 0; i < 20; i++)
        {
            // 이제 완벽하게 방 안쪽 바닥에만 별이 떨어집니다.
            float randomX = Random.Range(mapLeft, mapRight);
            float targetY = Random.Range(mapBottom, mapTop);
            Vector3 targetPos = new Vector3(randomX, targetY, 0);

            // 별이 출발할 위치 (화면 완전 위쪽 바깥)
            Vector3 spawnPos = new Vector3(randomX, roomCenter.y + 12f, 0);

            if (enemyData.meteorPrefab != null)
            {
                GameObject meteor = Instantiate(enemyData.meteorPrefab, spawnPos, Quaternion.identity);

                // ★ [수정됨] targetY 대신 targetPos(Vector3) 전체와, 경고 마커 프리팹을 넘겨줍니다!
                meteor.GetComponent<MeteorStar>().Setup(
                    currentFallSpeed,
                    targetPos,
                    enemyData.damage,
                    enemyData.meteorFragmentPrefab,
                    enemyData.meteorWarningPrefab
                );
            }

            yield return new WaitForSeconds(spawnDelay);

            spawnDelay = Mathf.Max(0.1f, spawnDelay - 0.03f);
            currentFallSpeed += 1.5f;
        }

        // 마지막 별이 파편으로 흩어지고 사라질 때까지 넉넉하게 2초 대기
        yield return new WaitForSeconds(2.0f);

        // 🌟 패턴 종료
        yield return StartCoroutine(ReappearRoutine());
    }

    // ==========================================
    // ★ 6번 패턴: 기억력 테스트 (진짜 바닥 색상 변경 + 직접 만든 프리팹 정확한 위치 소환)
    // ==========================================
    private System.Collections.IEnumerator MemoryLaserRoutine()
    {
        bossState = BossState.HiddenPattern;
        mySpriteRenderer.enabled = false;
        myCollider.enabled = false;

        Vector3 roomCenter = currentRoom.transform.position;

        int[] colorSequence = new int[5];
        for (int i = 0; i < 5; i++) colorSequence[i] = Random.Range(0, 2);

        transform.position = roomCenter;
        mySpriteRenderer.enabled = true;
        mySpriteRenderer.color = new Color(1, 1, 1, 0.5f);

        yield return new WaitForSeconds(0.5f);

        // ==========================================
        // ★ [여기 수정됨] roomBgRenderer 대신 floorRenderer를 사용합니다!
        // ==========================================
        SpriteRenderer floorBg = currentRoom.floorRenderer;
        Color originalBgColor = Color.white;
        if (floorBg != null) originalBgColor = floorBg.color; // 바닥 원래 색 기억

        Color blueColor = new Color(0f, 0.8f, 1f, 1f);
        Color orangeColor = new Color(1f, 0.5f, 0f, 1f);

        for (int i = 0; i < 5; i++)
        {
            // 바닥 색깔만 파랑/주황으로 직접 바꿈
            if (floorBg != null) floorBg.color = colorSequence[i] == 0 ? blueColor : orangeColor;
            PlaySoundWithMixer(enemyData.hitSound);

            yield return new WaitForSeconds(0.3f);

            // 원래 색으로 복구
            if (floorBg != null) floorBg.color = originalBgColor;
            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.5f);
        mySpriteRenderer.enabled = false;

        PlayerController pc = playerTransform.GetComponent<PlayerController>();
        Player playerScript = playerTransform.GetComponent<Player>();

        // ==========================================
        // ★ [수정 2] 만든 레이저 프리팹을 오른쪽 끝에서 왼쪽으로 쏘기
        // ==========================================
        // 방 중앙에서 오른쪽으로 9칸 간 위치 = 오른쪽 벽 끄트머리 안쪽! (Z축은 0)
        Vector3 spawnPos = roomCenter + new Vector3(9f, 0f, 0f);

        for (int i = 0; i < 5; i++)
        {
            bool isOrange = colorSequence[i] == 1;
            Color laserColor = isOrange ? new Color(1f, 0.5f, 0f, 0.8f) : new Color(0f, 0.8f, 1f, 0.8f);

            GameObject laserObj = null;

            if (enemyData.memoryLaserPrefab != null)
            {
                laserObj = Instantiate(enemyData.memoryLaserPrefab, spawnPos, Quaternion.Euler(0, 0, 180f));

                // ==========================================
                // ★ [완벽 해결 1] 프리팹에 원래 달려있던 콜라이더를 싹 다 꺼버립니다!
                // (이제 프리팹 혼자 멋대로 데미지를 주는 버그가 원천 차단됩니다)
                // ==========================================
                Collider2D[] cols = laserObj.GetComponentsInChildren<Collider2D>();
                foreach (var col in cols) col.enabled = false;

                SpriteRenderer[] srs = laserObj.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in srs)
                {
                    sr.color = laserColor;
                    sr.sortingOrder = 30000;
                }

                if (CameraShake.Instance != null) CameraShake.Instance.ShakeCamera(0.1f, 0.3f);
                PlaySoundWithMixer(enemyData.deathSound);

                // 레이저가 뻗어나가는 시간 (0.1초) + 유지되는 시간 (0.15초) = 총 0.25초
                float blastTime = 0.25f;
                float timer = 0f;
                bool hitPlayer = false; // 이번 레이저에 맞았는지 기억하는 스위치

                while (timer < blastTime)
                {
                    timer += Time.deltaTime;

                    // 0.1초 만에 크기가 쭉 커짐
                    float scaleX = Mathf.Lerp(0f, 25f, timer / 0.1f);
                    if (scaleX > 25f) scaleX = 25f; // 최대 크기 고정
                    laserObj.transform.localScale = new Vector3(scaleX, 15f, 1f);

                    // ==========================================
                    // ★ [완벽 해결 2] 레이저가 켜져 있는 0.25초 내내 움직임을 계속 감시합니다!
                    // ==========================================
                    if (!hitPlayer)
                    {
                        bool isMoving = pc != null && pc.input.sqrMagnitude > 0.01f;

                        if (isOrange && !isMoving) // 주황색인데 멈춰있으면 사망!
                        {
                            playerScript.TakeDamage(Mathf.RoundToInt(enemyData.damage));
                            hitPlayer = true;
                        }
                        else if (!isOrange && isMoving) // 파란색인데 움직이면 사망!
                        {
                            playerScript.TakeDamage(Mathf.RoundToInt(enemyData.damage));
                            hitPlayer = true;
                        }
                    }

                    yield return null;
                }

                // 레이저 서서히 사라짐
                float fadeTime = 0.2f;
                timer = 0f;
                while (timer < fadeTime)
                {
                    timer += Time.deltaTime;
                    foreach (var sr in srs)
                    {
                        sr.color = new Color(laserColor.r, laserColor.g, laserColor.b, Mathf.Lerp(0.8f, 0f, timer / fadeTime));
                    }
                    yield return null;
                }

                Destroy(laserObj);
            }
            yield return new WaitForSeconds(0.1f);  // 다음 레이저 발사 간격
        }

        // 🌟 패턴 종료 및 재등장
        yield return StartCoroutine(ReappearRoutine());
    }
}