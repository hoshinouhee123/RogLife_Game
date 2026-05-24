using UnityEngine;

public class Enemy : MonoBehaviour
{
    private EnemyData enemyData; // 이제 인스펙터에서 직접 안 넣고 코드로 넣어줌.
    private float currentHealth;
    private Transform playerTransform;
    private Rigidbody2D rb;

    // 몬스터가 깨어있는지 확인하는 변수
    private bool isAwake = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // MapGenerator가 적을 소환할 때 데이터를 주입해주는 함수
    public void Setup(EnemyData data)
    {
        enemyData = data;
        currentHealth = enemyData.maxHealth;
        gameObject.name = enemyData.enemyName;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (enemyData.enemySprite != null) sr.sprite = enemyData.enemySprite;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        // 태어날 때는 무조건 잠들어 있음(안 움직임)
        isAwake = false;
    }

    // 방 컨트롤러가 몬스터를 깨울 때 부르는 함수
    public void WakeUp()
    {
        isAwake = true;
    }

    void FixedUpdate()
    {
        // 잠들어 있거나 데이터가 없으면 절대 안 움직임!
        if (!isAwake || enemyData == null || playerTransform == null) return;

        Vector2 targetPos = playerTransform.position;
        Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, enemyData.moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        PlaySoundWithMixer(enemyData.hitSound);

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        PlaySoundWithMixer(enemyData.deathSound);

        Destroy(gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어에게 데미지 1 줌 (EnemyData의 damage를 써도 됩니다)
            collision.gameObject.GetComponent<Player>().TakeDamage(1);
        }
    }

    // ==========================================
    // 믹서가 적용되는 임시 효과음 스피커 생성기
    // ==========================================
    private void PlaySoundWithMixer(AudioClip clip)
    {
        if (clip == null) return;

        // 1. 임시 빈 게임오브젝트 만들기
        GameObject audioObj = new GameObject("TempAudio");
        audioObj.transform.position = transform.position;

        // 2. 오디오 소스 부품 달아주기
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 0f; // 2D 게임이므로 0(전체 볼륨 동일)으로 설정

        // 3. 만들어둔 데이터에서 믹서 그룹을 가져와서 연결
        if (enemyData.sfxMixerGroup != null)
        {
            source.outputAudioMixerGroup = enemyData.sfxMixerGroup;
        }

        // 4. 소리 재생 후, 클립 길이만큼 기다렸다가 오브젝트 깔끔하게 파괴
        source.Play();
        Destroy(audioObj, clip.length);
    }
}