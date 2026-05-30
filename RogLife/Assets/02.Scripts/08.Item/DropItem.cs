using UnityEngine;

public class DropItem : MonoBehaviour
{

    // ★ [수정됨] Heart 타입 추가
    public enum DropType { Coin, Key, Heart }
    public DropType dropType;
    public int amount = 1;

    private void Start()
    {
        // 태어나자마자 Z축 고정 및 무작위 방향으로 통통 튕김
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            rb.AddForce(randomDir * 3f, ForceMode2D.Impulse);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();

            // ★ [추가됨] 하트일 경우
            if (dropType == DropType.Heart)
            {
                // 플레이어가 풀피라면 먹지 않고 그대로 바닥에 남겨둠!
                if (player.currentHealth >= player.maxHealth) return;

                player.Heal(amount);
                // (선택) 하트 먹는 소리가 필요하면 player.audioSource.PlayOneShot(...) 추가
            }
            else if (dropType == DropType.Coin)
            {
                player.AddCoin(amount);
            }
            else if (dropType == DropType.Key)
            {
                player.AddKey(amount);
            }

            Destroy(gameObject); // 먹었으면 파괴
        }
    }
}