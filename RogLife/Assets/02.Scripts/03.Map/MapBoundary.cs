using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    [Header("출력할 대사")]
    public DialogueLine[] dialogueLines;

    // 인스펙터에서 편하게 고를 수 있도록 방향 목록 만들기
    public enum PushDirection { Up, Down, Left, Right }

    [Header("플레이어를 튕겨낼 방향")]
    public PushDirection pushDir = PushDirection.Down; // 기본값은 아래쪽

    [Header("튕겨낼 거리")]
    public float pushDistance = 0.5f; // 뒤로 물러나는 거리 (타일 반 칸 정도)

    // 플레이어가 경계선(Trigger)에 닿는 순간 실행
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 닿은 오브젝트가 플레이어인지 확인
        if (collision.CompareTag("Player"))
        {
            // 1. 대화창 띄우기
            DialogueManager.instance.StartDialogue(dialogueLines);

            // 2. 튕겨낼 방향 계산하기
            Vector3 moveBack = Vector3.zero;
            switch (pushDir)
            {
                case PushDirection.Up: moveBack = Vector3.up; break;
                case PushDirection.Down: moveBack = Vector3.down; break;
                case PushDirection.Left: moveBack = Vector3.left; break;
                case PushDirection.Right: moveBack = Vector3.right; break;
            }

            // 3. 플레이어의 위치를 강제로 이동 (무한 대화 루프 방지)
            collision.transform.position += moveBack * pushDistance;
        }
    }
}