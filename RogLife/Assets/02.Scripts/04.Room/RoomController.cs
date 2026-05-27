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

    [Header("특수 방 마커")]
    public GameObject itemRoomMarker; // 미니맵에 띄울 황금방 아이콘

    private RoomController tRoom, bRoom, lRoom, rRoom;
    private bool hasT, hasB, hasL, hasR;

    private bool isCleared = false;
    private bool isPlayerInRoom = false;

    [Header("보스방 설정")]
    public GameObject bossRoomMarker; // 미니맵 보스방 마커 (빨간색)

    public bool isBossRoom = false;
    private GameObject itemPickupPrefab;
    private ItemData[] possibleItems;
    private GameObject portalPrefab;

    [Header("상점방 설정")]
    public GameObject shopRoomMarker; // 상점방 마커

    // ★ [추가됨] 이 방을 플레이어가 직접 밟았는지(가봤는지) 기억하는 변수
    public bool isVisited = false;

    private EnemyData myBossData; // 내 방에 소환된 보스 정보

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

    // 플레이어 입장 시 컷신 코루틴 부르기
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            VisitRoom();

            // [중요] !isPlayerInRoom 을 추가해서 문에 부비적거려도 딱 1번만 실행되게 막음!
            if (!isCleared && !isPlayerInRoom)
            {
                isPlayerInRoom = true;

                if (enemiesInRoom.Count > 0)
                {
                    LockDoors();

                    if (isBossRoom)
                    {
                        StartCoroutine(BossCutsceneRoutine());
                    }
                    else
                    {
                        foreach (Enemy enemy in enemiesInRoom)
                        {
                            if (enemy != null) enemy.WakeUp();
                        }
                    }
                }
                else { isCleared = true; }
            }
        }
    }

    // 보스가 죽었을 때 보상 소환 로직 추가!
    // ★ 보스가 다 죽었을 때
    private void Update()
    {
        if (isPlayerInRoom && !isCleared)
        {
            enemiesInRoom.RemoveAll(enemy => enemy == null);
            if (enemiesInRoom.Count == 0)
            {
                isCleared = true;
                UnlockDoors();

                if (isBossRoom)
                {
                    // 보스가 싹 다 죽으면 HP바 숨기기!
                    if (BossUIManager.Instance != null) BossUIManager.Instance.HideHPBar();

                    SpawnBossRewards();
                }
            }
        }
    }


    // MapGenerator가 "너 황금방 해라!" 라고 명령할 때 부를 함수
    public void SetAsItemRoom()
    {
        // 미니맵 마커 켜기
        if (itemRoomMarker != null)
        {
            itemRoomMarker.SetActive(true);
        }
    }

    // ★ [1. 수정됨] MapGenerator가 보스 정보를 넘겨주도록 파라미터 추가
    public void SetAsBossRoom(GameObject itemPrefab, ItemData[] items, GameObject portal, EnemyData bossData)
    {
        isBossRoom = true;
        itemPickupPrefab = itemPrefab;
        possibleItems = items;
        portalPrefab = portal;
        myBossData = bossData; // 보스 정보 기억하기!

        if (bossRoomMarker != null) bossRoomMarker.SetActive(true);
    }

    //  보상과 포탈 소환
    private void SpawnBossRewards()
    {
        // 보스를 잡았으니 승리 BGM으로 변경!
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.PlayClearBGM();
        }

        // 1. 방 정중앙에 아이템 소환
        if (itemPickupPrefab != null && possibleItems.Length > 0)
        {
            GameObject spawnedItem = Instantiate(itemPickupPrefab, transform.position, Quaternion.identity);
            ItemPickup pickupScript = spawnedItem.GetComponent<ItemPickup>();
            ItemData randomItemData = possibleItems[Random.Range(0, possibleItems.Length)];
            if (pickupScript != null && randomItemData != null) pickupScript.Setup(randomItemData);
        }

        // 2. 방 위쪽(중앙에서 살짝 위)에 다음 스테이지 포탈 소환
        if (portalPrefab != null)
        {
            Instantiate(portalPrefab, transform.position + new Vector3(0, 1.5f, 0), Quaternion.identity);
        }
    }

    // 보스 컷신 연출 코루틴
    private System.Collections.IEnumerator BossCutsceneRoutine()
    {
        // 1. 시간 정지
        Time.timeScale = 0f;

        // 보스 BGM으로 음악 체인지!
        if (BGMManager.Instance != null && myBossData != null)
        {
            BGMManager.Instance.PlayBossBGM(myBossData.bossBgm);
        }

        // 2. ★ [핵심 수정] BossUIManager의 역동적인 컷신 코루틴이 완전히 끝날 때까지 기다림!
        if (BossUIManager.Instance != null)
        {
            yield return StartCoroutine(BossUIManager.Instance.ShowCutsceneRoutine(myBossData));
        }

        // 3. 컷신 직후 대화문 출력 로직
        if (myBossData != null && myBossData.bossDialogues != null && myBossData.bossDialogues.Length > 0)
        {
            DialogueManager.instance.onDialogueEndCallback = () =>
            {
                WakeUpBossAndStartFight();
            };
            DialogueManager.instance.StartDialogue(myBossData.bossDialogues);
        }
        else
        {
            Time.timeScale = 1f;
            WakeUpBossAndStartFight();
        }
    }

    // 멈춰있던 보스 몬스터 깨우기 (전투 시작 함수)
    private void WakeUpBossAndStartFight()
    {
        // 컷신, 대화가 다 끝나고 드디어 보스가 깨어날 때 HP바 등장!
        if (isBossRoom && BossUIManager.Instance != null)
        {
            BossUIManager.Instance.ShowHPBar();
        }

        foreach (Enemy enemy in enemiesInRoom)
        {
            if (enemy != null) enemy.WakeUp();
        }
    }


    // ★ [함수 추가]
    public void SetAsShopRoom()
    {
        if (shopRoomMarker != null) shopRoomMarker.SetActive(true);
    }
}