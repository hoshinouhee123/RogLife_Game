using UnityEngine;

[System.Serializable]
public class FloorDialogue
{
    public string floorName; // 보기 편하게 (예: 1층 대화)
    public DialogueLine[] dialogues;
}

public class MerchantNPC : MonoBehaviour
{
    [Header("층별 상점 주인 대화 세팅")]
    public FloorDialogue[] floorDialogues;

    private InteractableObject interactable;

    private void Start()
    {
        interactable = GetComponent<InteractableObject>();

        // 1. 현재 맵의 층수를 가져옵니다.
        int currentFloor = 1;
        if (MapGenerator.Instance != null)
        {
            currentFloor = MapGenerator.Instance.currentFloor;
        }

        // 2. 층수에 맞춰서 대화문을 배열에서 뽑아옵니다.
        // (배열은 0부터 시작하므로 1층 = 인덱스 0)
        int index = Mathf.Clamp(currentFloor - 1, 0, floorDialogues.Length - 1);

        // 3. 상호작용(말걸기) 스크립트 안에 데이터를 강제로 덮어씌워 줍니다!
        if (interactable != null && floorDialogues[index].dialogues != null)
        {
            interactable.dialogueLines = floorDialogues[index].dialogues;
        }
    }
}