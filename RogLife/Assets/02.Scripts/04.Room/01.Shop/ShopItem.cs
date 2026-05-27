using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    public enum ShopItemType { Item, Health } // 파는 물건 종류 (아이템 or 체력)
    public ShopItemType itemType;

    public int price = 15;        // 가격
    public ItemData itemData;     // 아이템일 경우의 데이터

    public TextMeshProUGUI priceText;        // 밑에 띄워줄 가격 텍스트 UI
    private SpriteRenderer sr;

    // 맵 생성기가 '체력'을 팔라고 설정할 때 부름
    public void SetupHealth(int cost, Sprite healthSprite)
    {
        itemType = ShopItemType.Health;
        price = cost;
        sr = GetComponent<SpriteRenderer>();
        if (healthSprite != null) sr.sprite = healthSprite; // 하트 이미지로 변경
        if (priceText != null) priceText.text = price.ToString();
    }

    // 맵 생성기가 '아이템'을 팔라고 설정할 때 부름
    public void SetupItem(ItemData data, int cost)
    {
        itemType = ShopItemType.Item;
        itemData = data;
        price = cost;
        sr = GetComponent<SpriteRenderer>();
        if (data != null) sr.sprite = data.itemIcon; // 아이템 이미지로 변경
        if (priceText != null) priceText.text = price.ToString();
    }

    // 플레이어가 물건에 부딪혔을 때 (구매 시도)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();

            // 1. 체력 회복을 사려는데 이미 풀피라면? 돈 안 낭비하게 구매 거부!
            if (itemType == ShopItemType.Health && player.currentHealth >= player.maxHealth) return;

            // 2. 플레이어 지갑에서 돈이 성공적으로 빠져나갔다면? (돈이 부족하면 안 빠짐)
            if (player.SpendCoin(price))
            {
                if (itemType == ShopItemType.Item)
                {
                    player.AcquireItem(itemData);
                    ItemUIManager.Instance.ShowItemGet(itemData);
                }
                else if (itemType == ShopItemType.Health)
                {
                    player.Heal(1); // 하트 1칸 회복
                    // 힐 효과음이 있다면 여기서 플레이어 AudioSource로 틀어도 됩니다.
                }

                Destroy(gameObject); // 구매 완료 후 판매대에서 물건 파괴!
            }
        }
    }
}