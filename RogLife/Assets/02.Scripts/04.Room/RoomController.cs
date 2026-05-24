using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    [Header("문 및 벽 오브젝트")]
    public GameObject doorTop; public GameObject doorBottom;
    public GameObject doorLeft; public GameObject doorRight;
    public GameObject blockTop; public GameObject blockBottom;
    public GameObject blockLeft; public GameObject blockRight;

    [Header("미니맵 안개 시스템")]
    public GameObject minimapGroup;
    public SpriteRenderer mapIconCenter;

    [Header("미니맵 통로 아이콘")]
    public GameObject mapIconTop; public GameObject mapIconBottom;
    public GameObject mapIconLeft; public GameObject mapIconRight;

    private RoomController tRoom, bRoom, lRoom, rRoom;
    private bool hasT, hasB, hasL, hasR;

    private bool isCleared = false;
    private bool isPlayerInRoom = false;

    // ★ [추가됨] 이 방을 플레이어가 직접 밟았는지(가봤는지) 기억하는 변수
    public bool isVisited = false;

    public List<Enemy> enemiesInRoom = new List<Enemy>();

    public void SetupDoors(RoomController t, RoomController b, RoomController l, RoomController r)
    {
        tRoom = t; bRoom = b; lRoom = l; rRoom = r;
        hasT = t != null; hasB = b != null; hasL = l != null; hasR = r != null;

        // 처음 방이 만들어질 때는 모든 통로를 무조건 다 꺼둡니다. (나중에 상황에 맞게 켭니다)
        if (mapIconTop != null) mapIconTop.SetActive(false);
        if (mapIconBottom != null) mapIconBottom.SetActive(false);
        if (mapIconLeft != null) mapIconLeft.SetActive(false);
        if (mapIconRight != null) mapIconRight.SetActive(false);

        if (minimapGroup != null) minimapGroup.SetActive(false);
        if (mapIconCenter != null) mapIconCenter.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        UnlockDoors();
    }

    // ★ [핵심 추가 기능] 상황에 맞게 통로를 켜주는 마법의 함수
    private void UpdateMinimapCorridors()
    {
        if (isVisited)
        {
            // 내가 직접 가본 방이면: 원래 길이 있는 곳의 통로를 100% 다 보여줍니다.
            if (mapIconTop != null) mapIconTop.SetActive(hasT);
            if (mapIconBottom != null) mapIconBottom.SetActive(hasB);
            if (mapIconLeft != null) mapIconLeft.SetActive(hasL);
            if (mapIconRight != null) mapIconRight.SetActive(hasR);
        }
        else
        {
            // 아직 안 가보고 발견만 된(회색) 방이면: '이미 방문한 방'과 연결된 통로 딱 1개만 보여줍니다!
            if (mapIconTop != null) mapIconTop.SetActive(hasT && tRoom != null && tRoom.isVisited);
            if (mapIconBottom != null) mapIconBottom.SetActive(hasB && bRoom != null && bRoom.isVisited);
            if (mapIconLeft != null) mapIconLeft.SetActive(hasL && lRoom != null && lRoom.isVisited);
            if (mapIconRight != null) mapIconRight.SetActive(hasR && rRoom != null && rRoom.isVisited);
        }
    }

    public void DiscoverRoom()
    {
        if (minimapGroup != null) minimapGroup.SetActive(true);
        // 발견되었을 때 통로 업데이트 실행! (내가 왔던 길만 켜짐)
        UpdateMinimapCorridors();
    }

    public void VisitRoom()
    {
        isVisited = true; // 방에 방문했다고 도장 쾅!

        if (minimapGroup != null) minimapGroup.SetActive(true);
        if (mapIconCenter != null) mapIconCenter.color = Color.white;

        // 내 방의 모든 통로를 켭니다.
        UpdateMinimapCorridors();

        // 이웃 방들을 발견시킵니다. (이웃 방들도 자기 통로를 알아서 업데이트하게 됩니다)
        if (tRoom != null) tRoom.DiscoverRoom();
        if (bRoom != null) bRoom.DiscoverRoom();
        if (lRoom != null) lRoom.DiscoverRoom();
        if (rRoom != null) rRoom.DiscoverRoom();
    }

    public void LockDoors()
    {
        if (hasT) { doorTop.SetActive(false); blockTop.SetActive(true); }
        if (hasB) { doorBottom.SetActive(false); blockBottom.SetActive(true); }
        if (hasL) { doorLeft.SetActive(false); blockLeft.SetActive(true); }
        if (hasR) { doorRight.SetActive(false); blockRight.SetActive(true); }
    }

    public void UnlockDoors()
    {
        doorTop.SetActive(hasT); blockTop.SetActive(!hasT);
        doorBottom.SetActive(hasB); blockBottom.SetActive(!hasB);
        doorLeft.SetActive(hasL); blockLeft.SetActive(!hasL);
        doorRight.SetActive(hasR); blockRight.SetActive(!hasR);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            VisitRoom();

            if (!isCleared)
            {
                isPlayerInRoom = true;
                if (enemiesInRoom.Count > 0)
                {
                    LockDoors();
                    foreach (Enemy enemy in enemiesInRoom)
                    {
                        if (enemy != null) enemy.WakeUp();
                    }
                }
                else { isCleared = true; }
            }
        }
    }

    private void Update()
    {
        if (isPlayerInRoom && !isCleared)
        {
            enemiesInRoom.RemoveAll(enemy => enemy == null);
            if (enemiesInRoom.Count == 0)
            {
                isCleared = true;
                UnlockDoors();
            }
        }
    }
}