using UnityEditor.EditorTools;
using UnityEngine;

// 문의 방향을 정하는 열거형
public enum DoorType { Top, Bottom, Left, Right }

public class Door : MonoBehaviour
{
    [Header("이 문은 어느 방향에 있나요")]
    public DoorType doorType;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 문에 닿은 것이 플레이어라면
        if (collision.CompareTag("Player"))
        {
            // RoomManager에게 방을 넘어가라고 명령
            RoomManager.Instance.ChangeRoom(doorType);
        }
    }
}