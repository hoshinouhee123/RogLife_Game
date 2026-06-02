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

    // ★ [새로 추가된 부분] 올려줄 이동 속도! (예: 1, 2)
    public float addMoveSpeed;

    // ==========================================
    // ★ [새로 추가된 부분] 공속과 트리플 샷
    // ==========================================
    [Header("특수 능력")]
    [Tooltip("총알 쏘는 딜레이를 줄여줍니다. (예: 0.1 넣으면 0.5초 -> 0.4초로 빨라짐)")]
    public float decreaseFireRate;

    [Tooltip("체크하면 한 번에 3발씩 나갑니다!")]
    public bool isTripleShot;
}