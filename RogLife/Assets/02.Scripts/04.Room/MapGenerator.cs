using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{

    public static MapGenerator Instance;

    [Header("맵 생성 설정")]
    public GameObject roomPrefab;
    public int maxRooms = 15;

    [Header("방 크기 (RoomManager와 동일하게)")]
    public float roomWidth = 18f;
    public float roomHeight = 10f;

    [Header("배경 64x64 패턴 설정 (16x16 타일 16개)")]
    public Tilemap backgroundTilemap;
    // 인스펙터에서 16개의 타일을 0번부터 15번까지 순서대로 넣어주세요
    public TileBase[] backgroundPattern = new TileBase[16];

    [Header("몬스터 소환 설정")]
    public GameObject enemyPrefab;

    [Header("아이템 방 설정")]
    public GameObject itemPickupPrefab;
    public ItemData[] possibleItems;

    // 보스방 설정
    [Header("보스방 설정")] 
    public GameObject nextStagePortalPrefab; // 아까 만든 NextStagePortal 프리팹

    [Header("상점방 설정")]
    public GameObject shopItemPrefab;    // 상점 진열대 프리팹 (ShopItem.cs)
    public GameObject merchantPrefab;    // 상인 NPC 프리팹 (InteractableObject)
    public Sprite shopHealthSprite;      // 체력 판매용 하트 이미지

    [System.Serializable]
    public class FloorData
    {
        public string floorName;             // 인스펙터 보기 편하게 (예: 1층, 2층)
        public EnemyData[] enemies;          // 이 층에서 나오는 일반 몬스터들
        public EnemyData[] bosses;           // 이 층에서 나오는 보스들
    }

    [Header("스테이지(층) 진행 설정")]
    public int currentFloor = 1;
    public int finalFloor = 3;

    // [새로 추가된 층별 데이터 배열]
    [Header("층별 몬스터 세팅")]
    public FloorData[] floorSettings; // 여기에 1층, 2층, 3층 데이터를 각각 넣습니다.


    private Dictionary<Vector2Int, GameObject> spawnedRooms = new Dictionary<Vector2Int, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }


    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        List<Vector2Int> roomPositions = new List<Vector2Int>();
        roomPositions.Add(Vector2Int.zero);

        while (roomPositions.Count < maxRooms)
        {
            Vector2Int currentPos = roomPositions[Random.Range(0, roomPositions.Count)];
            Vector2Int newPos = currentPos;
            int randomDir = Random.Range(0, 4);
            if (randomDir == 0) newPos += Vector2Int.up;
            else if (randomDir == 1) newPos += Vector2Int.down;
            else if (randomDir == 2) newPos += Vector2Int.left;
            else if (randomDir == 3) newPos += Vector2Int.right;
            if (!roomPositions.Contains(newPos)) roomPositions.Add(newPos);
        }



        // ==========================================
        // ★ 1. 보스방 지정 (시작방에서 가장 먼 방 찾기)
        // ==========================================
        Vector2Int bossRoomPos = Vector2Int.zero;
        int maxDist = -1;
        foreach (Vector2Int pos in roomPositions)
        {
            // 시작점(0,0)으로부터 X칸수 + Y칸수 = 실제 이동 거리
            int dist = Mathf.Abs(pos.x) + Mathf.Abs(pos.y);
            if (dist > maxDist && pos != Vector2Int.zero)
            {
                maxDist = dist;
                bossRoomPos = pos;
            }
        }

        // ==========================================
        // ★ 2. 아이템 방 지정 (보스방, 시작방 제외하고 랜덤)
        // ==========================================
        bool hasItemRoom = Random.Range(0, 100) < 100; // 30% 확률
        Vector2Int itemRoomPos = new Vector2Int(9999, 9999);

        if (hasItemRoom && roomPositions.Count > 2)
        {
            List<Vector2Int> possibleItemRooms = new List<Vector2Int>(roomPositions);
            possibleItemRooms.Remove(Vector2Int.zero); // 시작방 제외
            possibleItemRooms.Remove(bossRoomPos);     // 보스방 제외

            if (possibleItemRooms.Count > 0)
            {
                itemRoomPos = possibleItemRooms[Random.Range(0, possibleItemRooms.Count)];
            }
        }

        // ==========================================
        // ★ 3. 상점방 지정 (보스, 시작, 아이템방 피해서 50% 확률로 등장)
        // ==========================================
        bool hasShopRoom = Random.Range(0, 100) < 100; // 50% 확률
        Vector2Int shopRoomPos = new Vector2Int(9999, 9999);

        if (hasShopRoom && roomPositions.Count > 3)
        {
            List<Vector2Int> possibleShopRooms = new List<Vector2Int>(roomPositions);
            possibleShopRooms.Remove(Vector2Int.zero); // 시작방 제외
            possibleShopRooms.Remove(bossRoomPos);     // 보스방 제외
            possibleShopRooms.Remove(itemRoomPos);     // 아이템방 제외

            if (possibleShopRooms.Count > 0)
            {
                shopRoomPos = possibleShopRooms[Random.Range(0, possibleShopRooms.Count)];
            }
        }

        // (혹시 아이템방이랑 겹치면 아이템방 위치를 변경)
        if (bossRoomPos == itemRoomPos) itemRoomPos = roomPositions[1];

        // ★ [새로 추가] 현재 층수에 맞는 데이터 묶음을 가져옵니다.
        // (배열은 0부터 시작하므로 1층은 인덱스 0번입니다. 에러 방지를 위해 Clamp 사용)
        int floorIndex = Mathf.Clamp(currentFloor - 1, 0, floorSettings.Length - 1);
        FloorData currentFloorData = floorSettings[floorIndex];

        foreach (Vector2Int pos in roomPositions)
        {
            Vector3 worldPos = new Vector3(pos.x * roomWidth, pos.y * roomHeight, 0);
            GameObject newRoom = Instantiate(roomPrefab, worldPos, Quaternion.identity);
            newRoom.name = "Room_" + pos.x + "_" + pos.y;
            spawnedRooms.Add(pos, newRoom);

            RoomController controller = newRoom.GetComponent<RoomController>();

            // 1. 아이템 방 소환
            if (pos == itemRoomPos)
            {
                controller.SetAsItemRoom();
                if (itemPickupPrefab != null && possibleItems.Length > 0)
                {
                    GameObject spawnedItem = Instantiate(itemPickupPrefab, worldPos, Quaternion.identity);
                    ItemPickup pickupScript = spawnedItem.GetComponent<ItemPickup>();
                    ItemData randomItemData = possibleItems[Random.Range(0, possibleItems.Length)];
                    if (pickupScript != null && randomItemData != null) pickupScript.Setup(randomItemData);
                }
            }
            // ★ [추가됨] 상점방 소환!
            else if (pos == shopRoomPos)
            {
                controller.SetAsShopRoom();

                // 상인 NPC를 방 위쪽 가운데에 소환
                if (merchantPrefab != null)
                {
                    Instantiate(merchantPrefab, worldPos + new Vector3(0, 2f, 0), Quaternion.identity);
                }

                if (shopItemPrefab != null)
                {
                    // 왼쪽엔 체력 판매대 소환 (15원)
                    GameObject healthStand = Instantiate(shopItemPrefab, worldPos + new Vector3(-3f, -1f, 0), Quaternion.identity);
                    healthStand.GetComponent<ShopItem>().SetupHealth(15, shopHealthSprite);

                    // 오른쪽엔 랜덤 아이템 판매대 소환 (15원)
                    if (possibleItems.Length > 0)
                    {
                        GameObject itemStand = Instantiate(shopItemPrefab, worldPos + new Vector3(3f, -1f, 0), Quaternion.identity);
                        ItemData randomItemData = possibleItems[Random.Range(0, possibleItems.Length)];
                        itemStand.GetComponent<ShopItem>().SetupItem(randomItemData, 15);
                    }
                }
            }
            // 2. 보스방 소환
            else if (pos == bossRoomPos)
            {
                EnemyData randomBoss = null;
                // ★ [수정됨] currentFloorData.bosses 를 사용!
                if (currentFloorData.bosses.Length > 0)
                    randomBoss = currentFloorData.bosses[Random.Range(0, currentFloorData.bosses.Length)];

                controller.SetAsBossRoom(itemPickupPrefab, possibleItems, nextStagePortalPrefab, randomBoss);

                if (enemyPrefab != null && randomBoss != null)
                {
                    GameObject spawnedBoss = Instantiate(enemyPrefab, worldPos, Quaternion.identity);
                    Enemy bossScript = spawnedBoss.GetComponent<Enemy>();
                    bossScript.Setup(randomBoss);
                    spawnedBoss.transform.localScale = new Vector3(2f, 2f, 1f);
                    bossScript.currentRoom = controller;
                    controller.enemiesInRoom.Add(bossScript);
                }
            }
            // 3. 일반 몬스터 소환
            // ★ [수정됨] currentFloorData.enemies 를 사용!
            else if (pos != Vector2Int.zero && enemyPrefab != null && currentFloorData.enemies.Length > 0)
            {
                int enemyCount = Random.Range(1, 4);
                for (int i = 0; i < enemyCount; i++)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-4f, 4f), Random.Range(-0.5f, 0.5f), 0);
                    GameObject spawnedEnemy = Instantiate(enemyPrefab, worldPos + randomOffset, Quaternion.identity);

                    Enemy enemyScript = spawnedEnemy.GetComponent<Enemy>();

                    // ★ [수정됨] currentFloorData.enemies 를 사용!
                    EnemyData randomData = currentFloorData.enemies[Random.Range(0, currentFloorData.enemies.Length)];
                    enemyScript.Setup(randomData);

                    enemyScript.currentRoom = controller;
                    controller.enemiesInRoom.Add(enemyScript);
                }
            }
        }

        // 문 연결 처리
        foreach (var kvp in spawnedRooms)
        {
            Vector2Int pos = kvp.Key;
            RoomController controller = kvp.Value.GetComponent<RoomController>();
            RoomController top = spawnedRooms.ContainsKey(pos + Vector2Int.up) ? spawnedRooms[pos + Vector2Int.up].GetComponent<RoomController>() : null;
            RoomController bottom = spawnedRooms.ContainsKey(pos + Vector2Int.down) ? spawnedRooms[pos + Vector2Int.down].GetComponent<RoomController>() : null;
            RoomController left = spawnedRooms.ContainsKey(pos + Vector2Int.left) ? spawnedRooms[pos + Vector2Int.left].GetComponent<RoomController>() : null;
            RoomController right = spawnedRooms.ContainsKey(pos + Vector2Int.right) ? spawnedRooms[pos + Vector2Int.right].GetComponent<RoomController>() : null;
            if (controller != null) controller.SetupDoors(top, bottom, left, right);
        }

        if (spawnedRooms.ContainsKey(Vector2Int.zero)) spawnedRooms[Vector2Int.zero].GetComponent<RoomController>().VisitRoom();

        GenerateBackground(roomPositions);
    }

    //  포탈을 탔을 때 맵을 싹 지우고 새로 짜는 함수!
    // ★ [수정됨] 이제 코루틴을 부릅니다!
    public void GoToNextStage()
    {
        currentFloor++;

        if (currentFloor > finalFloor)
        {
            ShowEnding();
            return;
        }

        StartCoroutine(NextStageRoutine());
    }

    // ★ [새로 추가됨] 컷신과 맵 재생성을 묶은 완벽한 코루틴
    private System.Collections.IEnumerator NextStageRoutine()
    {
        // 1. 플레이어 조작 차단 및 시간 정지
        Time.timeScale = 0f;

        // 2. 층 이동 컷신 (검은 화면) 켜기! 
        // (UI 매니저가 나타날 때까지 코드가 여기서 기다립니다)
        if (StageTransitionUI.Instance != null)
        {
            yield return StartCoroutine(StageTransitionUI.Instance.ShowTransition(currentFloor));
        }

        // ==========================================
        // 3. 화면이 완전히 까매졌으므로 맵을 싹 지우고 새로 생성 (기존 코드와 동일)
        // ==========================================
        foreach (var room in spawnedRooms.Values) { if (room != null) Destroy(room); }
        spawnedRooms.Clear();

        ItemPickup[] items = FindObjectsOfType<ItemPickup>();
        foreach (var item in items) Destroy(item.gameObject);

        NextStagePortal[] portals = FindObjectsOfType<NextStagePortal>();
        foreach (var portal in portals) Destroy(portal.gameObject);

        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (var enemy in enemies) Destroy(enemy.gameObject);

        Bullet[] bullets = FindObjectsOfType<Bullet>();
        foreach (var bullet in bullets) Destroy(bullet.gameObject);

        if (backgroundTilemap != null) backgroundTilemap.ClearAllTiles();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) player.transform.position = Vector3.zero;

        if (RoomManager.Instance != null) RoomManager.Instance.ResetRoomCoordinates();

        GenerateMap();
        // ==========================================

        // 4. 새 맵 생성이 끝났으니 검은 화면을 걷어냄!
        if (StageTransitionUI.Instance != null)
        {
            yield return StartCoroutine(StageTransitionUI.Instance.HideTransition());
        }

        // 5. 시간 원상 복구 및 조작 재개
        Time.timeScale = 1f;
    }

    void GenerateBackground(List<Vector2Int> roomPositions)
    {
        if (backgroundTilemap == null || backgroundPattern.Length != 16) return;

        int minRoomX = int.MaxValue, maxRoomX = int.MinValue;
        int minRoomY = int.MaxValue, maxRoomY = int.MinValue;

        foreach (Vector2Int pos in roomPositions)
        {
            if (pos.x < minRoomX) minRoomX = pos.x;
            if (pos.x > maxRoomX) maxRoomX = pos.x;
            if (pos.y < minRoomY) minRoomY = pos.y;
            if (pos.y > maxRoomY) maxRoomY = pos.y;
        }

        minRoomX -= 1; maxRoomX += 1;
        minRoomY -= 1; maxRoomY += 1;

        int startTileX = Mathf.RoundToInt(minRoomX * roomWidth - (roomWidth / 2));
        int endTileX = Mathf.RoundToInt(maxRoomX * roomWidth + (roomWidth / 2));
        int startTileY = Mathf.RoundToInt(minRoomY * roomHeight - (roomHeight / 2));
        int endTileY = Mathf.RoundToInt(maxRoomY * roomHeight + (roomHeight / 2));

        for (int x = startTileX; x <= endTileX; x++)
        {
            for (int y = startTileY; y <= endTileY; y++)
            {
                int patternX = (x % 4 + 4) % 4;
                int patternY = (y % 4 + 4) % 4;

                int tileIndex = (3 - patternY) * 4 + patternX;

                backgroundTilemap.SetTile(new Vector3Int(x, y, 0), backgroundPattern[tileIndex]);
            }
        }
    }

    // ==========================================
    // ★ [새로 추가] 마지막 층(예: 3층)을 클리어했을 때 실행되는 엔딩 함수
    // ==========================================
    private void ShowEnding()
    {
        // 일단은 콘솔창에 메시지만 띄워둡니다.
        Debug.Log("게임 클리어! 대망의 엔딩 연출이 시작됩니다!");

        // 나중에 여기에 [해피 엔딩] 업적 달성 코드를 넣거나,
        // 진엔딩 씬으로 넘어가는 코드를 추가하시면 됩니다!

        // 예시: UnityEngine.SceneManagement.SceneManager.LoadScene("HappyEndingScene");
    }
}