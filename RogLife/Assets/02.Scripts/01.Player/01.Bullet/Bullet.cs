using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 2f; // 총알이 살아있는 시간
    private float damage;
    private Vector2 direction;

    public void Setup(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        Destroy(gameObject, lifeTime); // lifeTime 초 뒤에 자동 파괴
    }

    void Update()
    {
        // 설정된 방향으로 날아가기
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 적을 맞추면 데미지를 주고 총알 파괴
        if (collision.CompareTag("Enemy"))
        {
            collision.GetComponent<Enemy>().TakeDamage(damage);
            Destroy(gameObject);
        }
        // 벽이나 닫힌 문(Blocker)에 닿아도 파괴
        else if (collision.CompareTag("Wall") || collision.gameObject.layer == LayerMask.NameToLayer("EnemyBlocker"))
        {
            Destroy(gameObject);
        }
    }
}