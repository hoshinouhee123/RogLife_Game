using UnityEngine;

public class Coin : MonoBehaviour
{
    public int coinValue = 1; // 이 코인을 먹으면 오르는 개수

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어 몸에 닿으면?
        if (collision.CompareTag("Player"))
        {
            // 플레이어의 코인 증가 함수 실행
            collision.GetComponent<Player>().AddCoin(coinValue);

            // 먹었으니 화면에서 파괴
            Destroy(gameObject);
        }
    }
}