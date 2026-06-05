using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 5f;
    public float lifeTime = 3f; // 총알이 살아있는 시간
    private float damage;
    private Vector2 direction;

    private void Start()
    {
        // ★ 어떤 에러가 터져도, 총알은 태어난 지 3초(lifeTime)가 지나면 무조건 자동 삭제됩니다!
        Destroy(gameObject, lifeTime);
    }

    // ★ [수정됨] customSpeed를 추가로 받습니다. (기본값은 -1)
    // 숫자를 안 넣으면 프리팹 원래 속도를 쓰고, 숫자를 넣으면 그 속도로 날아갑니다!
    public void Setup(Vector2 dir, float dmg, float customSpeed = -1f)
    {
        direction = dir.normalized;
        damage = dmg;

        // ★ [추가됨] 커스텀 속도가 들어왔다면 덮어쓰기!
        if (customSpeed > 0f)
        {
            speed = customSpeed;
        }

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 플레이어에게 맞으면 데미지를 주고 파괴
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<Player>().TakeDamage(Mathf.RoundToInt(damage));
            Destroy(gameObject);
        }
        // 2. 벽에 닿으면 파괴
        else if (collision.CompareTag("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("EnemyBlocker"))
        {
            Destroy(gameObject);
        }
    }
}