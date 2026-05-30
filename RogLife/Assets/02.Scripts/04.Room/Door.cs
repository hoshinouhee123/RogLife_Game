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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 잠겨있다면?
            if (isLocked)
            {
                // 플레이어가 열쇠를 지불할 수 있다면 문이 열림!
                if (collision.GetComponent<Player>().SpendKey(1))
                {
                    isLocked = false;
                    if (doorSpriteRenderer != null && unlockedSprite != null)
                        doorSpriteRenderer.sprite = unlockedSprite; // 잠금 해제된 이미지(황금문)로 변경

                    RoomManager.Instance.ChangeRoom(doorType);
                }
                else
                {
                    // 열쇠가 부족하면 안 열림 (여기에 "철컥" 닫힌 소리를 넣어도 좋습니다)
                    Debug.Log("열쇠가 부족합니다!");
                }
            }
            // 안 잠겨있으면 그냥 통과
            else
            {
                RoomManager.Instance.ChangeRoom(doorType);
            }
        }
    }
}