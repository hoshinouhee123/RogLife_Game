using UnityEngine;

[System.Serializable]
public class FloorDialogue
{
    public string floorName; // 예: 1층 대화
    public DialogueLine[] dialogues;

    // ★ [새로 추가된 부분] 이 층에서 상인 NPC가 맵에 서 있을 때의 실제 모습(도트)
    [Header("상인 인게임 외형")]
    public Sprite merchantSprite;
}

public class MerchantNPC : MonoBehaviour
{
    [Header("층별 상점 주인 세팅")]
    public FloorDialogue[] floorDialogues;

    private InteractableObject interactable;

    // ★ [새로 추가된 부분] 상인의 이미지를 바꿔줄 렌더러 부품
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        interactable = GetComponent<InteractableObject>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // 내 몸의 도트 렌더러 찾기

        // 1. 현재 층수 가져오기
        int currentFloor = 1;
        if (MapGenerator.Instance != null)
        {
            currentFloor = MapGenerator.Instance.currentFloor;
        }

        // 2. 층수에 맞춰 인덱스 계산 (1층 = 0번)
        int index = Mathf.Clamp(currentFloor - 1, 0, floorDialogues.Length - 1);

        // 3. 대화문 덮어쓰기 (기존 코드)
        if (interactable != null && floorDialogues[index].dialogues != null)
        {
            interactable.dialogueLines = floorDialogues[index].dialogues;
        }

        // ==========================================
        // ★ 4. [추가됨] 상점 주인의 실제 게임 내 모습(도트) 변경!
        // ==========================================
        if (spriteRenderer != null && floorDialogues[index].merchantSprite != null)
        {
            spriteRenderer.sprite = floorDialogues[index].merchantSprite;
        }
    }
}