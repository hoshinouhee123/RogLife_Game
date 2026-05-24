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
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
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
}