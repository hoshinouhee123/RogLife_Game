using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData itemData;
    private SpriteRenderer sr;

    public void Setup(ItemData data)
    {
        itemData = data;
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = itemData.itemIcon; // 받은 데이터의 이미지로 내 모습 변경
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 플레이어에게 아이템 스탯 적용시키기
            collision.GetComponent<Player>().AcquireItem(itemData);

            // 화면에 UI 띄우기 명령
            ItemUIManager.Instance.ShowItemGet(itemData);

            // 먹었으니 바닥에서 파괴
            Destroy(gameObject);
        }
    }
}