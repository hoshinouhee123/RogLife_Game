using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "ScriptableObjects/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("아이템 정보")]
    public string itemName;            // 아이템 이름
    [TextArea]
    public string itemDescription;     // 아이템 설명
    public Sprite itemIcon;            // 아이템 이미지

    [Header("올려줄 스탯")]
    public float addDamage;            // 올라갈 공격력 (예: 1)
    public int addMaxHealth;           // 늘어날 최대 체력 (예: 1)
}