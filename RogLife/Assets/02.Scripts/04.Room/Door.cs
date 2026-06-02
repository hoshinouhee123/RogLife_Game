using UnityEditor.EditorTools;
using UnityEngine;

// 문의 방향을 정하는 열거형
public enum DoorType { Top, Bottom, Left, Right }

public class Door : MonoBehaviour
{
    [Header("이 문은 어느 방향에 있나요")]
    public DoorType doorType;

    // ★ [새로 추가됨] 잠금 시스템
    public bool isLocked = false;
    public Sprite unlockedSprite;
    public SpriteRenderer doorSpriteRenderer;

    // ★ [1] 안 잠긴 문: 부드럽게 겹쳐질 때(Trigger) 방 이동
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isLocked)
        {
            RoomManager.Instance.ChangeRoom(doorType);
        }
    }

    // ★ [2] 잠긴 문: 단단한 벽에 쿵! 부딪힐 때(Collision) 열쇠 검사
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && isLocked)
        {
            // 플레이어가 열쇠를 냈다면?
            if (collision.gameObject.GetComponent<Player>().SpendKey(1))
            {
                isLocked = false;

                // ★ [핵심] 잠금이 풀렸으니 다시 부드럽게 통과할 수 있게 트리거로 변신!
                GetComponent<Collider2D>().isTrigger = true;

                if (doorSpriteRenderer != null && unlockedSprite != null)
                    doorSpriteRenderer.sprite = unlockedSprite;

                RoomManager.Instance.ChangeRoom(doorType);
            }
            else
            {
                // 열쇠가 부족하면 열리지 않고 그냥 튕겨 나옵니다.
                Debug.Log("열쇠가 부족해서 열 수 없습니다!");
            }
        }
    }
}