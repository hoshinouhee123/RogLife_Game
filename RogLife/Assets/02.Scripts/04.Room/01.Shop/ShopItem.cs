using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    // ★ [수정됨] Key 타입 추가
    public enum ShopItemType { Item, Health, Key }
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

    // ★ [새로 추가된 세팅 함수]
    public void SetupKey(int cost, Sprite keySprite)
    {
        itemType = ShopItemType.Key;
        price = cost;
        sr = GetComponent<SpriteRenderer>();
        if (keySprite != null) sr.sprite = keySprite;
        if (priceText != null) priceText.text = price.ToString();
    }

    // 플레이어가 물건에 부딪혔을 때 (구매 시도)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();

            // 체력 꽉 찼으면 거절
            if (itemType == ShopItemType.Health && player.currentHealth >= player.maxHealth) return;
            // ★ [추가됨] 열쇠 99개면 거절
            if (itemType == ShopItemType.Key && player.keyCount >= 99) return;

            if (player.SpendCoin(price))
            {
                if (itemType == ShopItemType.Item)
                {
                    player.AcquireItem(itemData);
                    ItemUIManager.Instance.ShowItemGet(itemData);
                }
                else if (itemType == ShopItemType.Health) player.Heal(1);
                // ★ [추가됨] 열쇠 구매 시
                else if (itemType == ShopItemType.Key) player.AddKey(1);

                Destroy(gameObject);
            }
        }
    }
}