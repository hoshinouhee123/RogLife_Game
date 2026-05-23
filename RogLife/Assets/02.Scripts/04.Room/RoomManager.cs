using System.Collections;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    [Header("연결할 오브젝트")]
    public Transform player;
    public Camera mainCamera;

    [Header("방 설정")]
    public float roomWidth = 18f;
    public float roomHeight = 10f;
    public float offset = 2f;

    [Header("카메라 이동 속도")]
    public float transitionSpeed = 8f;

    private bool isTransitioning = false;

    //현재 방의 '좌표'를 기억하는 변수
    private int currentRoomX = 0;
    private int currentRoomY = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ChangeRoom(DoorType doorType)
    {
        if (isTransitioning) return;
        StartCoroutine(RoomTransitionRoutine(doorType));
    }

    private IEnumerator RoomTransitionRoutine(DoorType doorType)
    {
        isTransitioning = true;
        Time.timeScale = 0f; // 게임 일시정지

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol != null) playerCol.enabled = false;

        //문 방향에 따라 방의 X, Y 좌표값을 먼저 바꿉니다.
        switch (doorType)
        {
            case DoorType.Top: currentRoomY += 1; break;
            case DoorType.Bottom: currentRoomY -= 1; break;
            case DoorType.Left: currentRoomX -= 1; break;
            case DoorType.Right: currentRoomX += 1; break;
        }

        // 바뀐 좌표를 바탕으로 방의 중심점 계산
        
        Vector3 exactRoomCenter = new Vector3(
            currentRoomX * roomWidth,
            currentRoomY * roomHeight,
            mainCamera.transform.position.z
        );

        Vector3 newPlayerPos = player.position;

        // 플레이어가 순간이동할 위치 계산 (방 중심점 기준)
        switch (doorType)
        {
            case DoorType.Top:
                newPlayerPos = exactRoomCenter + new Vector3(0, -roomHeight / 2 + offset, 0);
                break;
            case DoorType.Bottom:
                newPlayerPos = exactRoomCenter + new Vector3(0, roomHeight / 2 - offset, 0);
                break;
            case DoorType.Left:
                newPlayerPos = exactRoomCenter + new Vector3(roomWidth / 2 - offset, 0, 0);
                break;
            case DoorType.Right:
                newPlayerPos = exactRoomCenter + new Vector3(-roomWidth / 2 + offset, 0, 0);
                break;
        }

        newPlayerPos.z = 0;
        player.position = newPlayerPos;

        // 카메라 이동 (절대 빗나가지 않는 exactRoomCenter로 이동)
        while (Vector3.Distance(mainCamera.transform.position, exactRoomCenter) > 0.01f)
        {
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                exactRoomCenter,
                Time.unscaledDeltaTime * transitionSpeed
            );
            yield return null;
        }

        // 카메라 위치 오차 없는 정수로 강제 고정
        mainCamera.transform.position = exactRoomCenter;

        if (playerCol != null) playerCol.enabled = true;
        Time.timeScale = 1f; // 게임 재개

        // 실제 시간 기준으로 0.1초 대기 
        yield return new WaitForSecondsRealtime(0.1f);

        isTransitioning = false;
    }
}