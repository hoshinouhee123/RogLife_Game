using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 5f;
    public float lifeTime = 3f; // 총알이 살아있는 시간
    private float damage;
    private Vector2 direction;

    public void Setup(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        Destroy(gameObject, lifeTime); // 일정 시간 후 파괴
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