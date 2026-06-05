using UnityEngine;

public class AbsorbStar : MonoBehaviour
{
    private Transform targetBoss;
    private float speed;
    private float damage;

    public void Setup(Transform boss, float moveSpeed, float dmg)
    {
        targetBoss = boss;
        speed = moveSpeed;
        damage = dmg;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        if (targetBoss == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetBoss.position, speed * Time.unscaledDeltaTime);

        // 보스 몸에 닿으면 즉시 파괴
        if (Vector2.Distance(transform.position, targetBoss.position) < 0.6f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player p = collision.GetComponent<Player>();
            if (p != null)
            {
                p.TakeDamage(Mathf.RoundToInt(damage));
            }

            // ★ [완벽 해결 1] 플레이어에게 닿는 즉시 별을 파괴합니다! 
            // 이 한 줄이 없어서 별들이 지워지지 않고 쌓여 유니티가 터졌던 것입니다!
            Destroy(gameObject);
        }
    }
}