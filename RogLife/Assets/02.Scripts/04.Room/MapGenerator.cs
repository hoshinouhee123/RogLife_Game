using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{

    public static MapGenerator Instance;

    [Header("맵 생성 설정")]
    public GameObject roomPrefab;

    [Header("방 크기 (RoomManager와 동일하게)")]
    public float roomWidth = 18f;
    public float roomHeight = 10f;

    [Header("배경 64x64 패턴 설정 (16x16 타일 16개)")]
    public Tilemap backgroundTilemap;
    

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
    public Sprite shopKeySprite; // ★ [추가됨] 열쇠 판매용 이미지


    [System.Serializable]
    public class FloorData
    {
        // ★ [새로 추가된 부분] 이 층에서 생성할 방의 개수!
        public int maxRooms = 15;

        public string floorName;             // 인스펙터 보기 편하게 (예: 1층, 2층)
        public EnemyData[] enemies;          // 이 층에서 나오는 일반 몬스터들
        public EnemyData[] bosses;           // 이 층에서 나오는 보스들
        // ★ [새로 추가된 부분] 층별 아트와 사운드!

        [Header("층별 아트 & 사운드")]
        public AudioClip floorBgm;              // 이 층의 BGM
        public Sprite roomFloorSprite;          // 방 안쪽 바닥/벽 이미지
        public TileBase[] backgroundPattern = new TileBase[16]; // 맵 바깥쪽 배경 타일 (16개)

        // ==========================================
        // ★ [새로 추가된 연출 데이터들!]
        // ==========================================
        [Header("1. 암전 컷신 연출")]
        public AudioClip transitionBgm;            // 어둠 속에서 나올 컷신 전용 브금
        public DialogueLine[] transitionDialogues; // 어둠 속 일러스트와 함께할 대화

        [Header("2. 맵 진입 후 독백")]
        public DialogueLine[] floorStartDialogues; // 화면 밝아진 후 맵 위에 서서 하는 독백
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

        // ★ [1층 시작 연출] 1층은 포탈을 안 타므로 Start에서 세팅합니다.
        int floorIndex = Mathf.Clamp(currentFloor - 1, 0, floorSettings.Length - 1);
        FloorData currentData = floorSettings[floorIndex];

        if (BGMManager.Instance != null && currentData.floorBgm != null)
            BGMManager.Instance.PlayStageBGM(currentData.floorBgm);

        // 1층 시작 독백이 있다면?
        if (currentData.floorStartDialogues != null && currentData.floorStartDialogues.Length > 0)
        {
            Time.timeScale = 0f;
            DialogueManager.instance.onDialogueEndCallback = () => { Time.timeScale = 1f; };
            DialogueManager.instance.StartDialogue(currentData.floorStartDialogues);
        }
        else { Time.timeScale = 1f; }
    }

    void GenerateMap()
    {
        // ==========================================
        // ★ [핵심 수정] 맵을 짜기 '전'에 현재 층의 데이터를 제일 먼저 불러옵니다!
        // ==========================================
        int floorIndex = Mathf.Clamp(currentFloor - 1, 0, floorSettings.Length - 1);
        FloorData currentFloorData = floorSettings[floorIndex];

       

        // 현재 층에 설정된 방 개수를 가져옴
        int currentMaxRooms = currentFloorData.maxRooms;

        List<Vector2Int> roomPositions = new List<Vector2Int>();
        roomPositions.Add(Vector2Int.zero);

        // 1. 기본 방 좌표 랜덤 생성 
        // ★ [수정됨] 이제 maxRooms 대신 currentMaxRooms 개수만큼 반복합니다!
        while (roomPositions.Count < currentMaxRooms)
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
        // ★ [업그레이드!] 맵 바깥쪽 빈 공간 중, 딱 1개의 방과 맞닿은 '진짜 막다른 길 후보지' 싹 다 찾기
        // ==========================================
        List<Vector2Int> potentialDeadEnds = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int pos in roomPositions)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int emptySpace = pos + dir;

                // 그 자리가 비어있고, 리스트에 아직 안 들어갔다면
                if (!roomPositions.Contains(emptySpace) && !potentialDeadEnds.Contains(emptySpace))
                {
                    // 이 빈 공간이 기존 방들과 몇 개나 맞닿아 있는지 검사
                    int touchCount = 0;
                    foreach (Vector2Int checkDir in directions)
                    {
                        if (roomPositions.Contains(emptySpace + checkDir)) touchCount++;
                    }

                    // 딱 1개의 방(입구)하고만 맞닿아 있다면 완벽한 막다른 길 후보!
                    if (touchCount == 1)
                    {
                        potentialDeadEnds.Add(emptySpace);
                    }
                }
            }
        }

        // ==========================================
        // ★ 특수 방들을 바깥쪽 막다른 길에 하나씩 이어 붙이기
        // ==========================================
        Vector2Int bossRoomPos = Vector2Int.zero;
        Vector2Int itemRoomPos = new Vector2Int(9999, 9999);
        Vector2Int shopRoomPos = new Vector2Int(9999, 9999);

        // 1) 보스방: 시작점에서 가장 먼 막다른 길에 추가
        int maxDist = -1;
        foreach (Vector2Int pos in potentialDeadEnds)
        {
            int dist = Mathf.Abs(pos.x) + Mathf.Abs(pos.y);
            if (dist > maxDist) { maxDist = dist; bossRoomPos = pos; }
        }
        roomPositions.Add(bossRoomPos);
        potentialDeadEnds.Remove(bossRoomPos);
        // (보스방 옆에 다른 특수방이 붙어서 문이 2개가 되는 걸 방지)
        potentialDeadEnds.RemoveAll(p => Vector2Int.Distance(p, bossRoomPos) == 1);

        // 2) 아이템방: 남은 막다른 길 중 하나에 추가 (100% 확률)
        if (Random.Range(0, 100) < 100 && potentialDeadEnds.Count > 0)
        {
            itemRoomPos = potentialDeadEnds[Random.Range(0, potentialDeadEnds.Count)];
            roomPositions.Add(itemRoomPos);
            potentialDeadEnds.Remove(itemRoomPos);
            potentialDeadEnds.RemoveAll(p => Vector2Int.Distance(p, itemRoomPos) == 1);
        }

        // 3) 상점방: 남은 막다른 길 중 하나에 추가 (100% 확률)
        if (Random.Range(0, 100) < 100 && potentialDeadEnds.Count > 0)
        {
            shopRoomPos = potentialDeadEnds[Random.Range(0, potentialDeadEnds.Count)];
            roomPositions.Add(shopRoomPos);
            potentialDeadEnds.Remove(shopRoomPos);
        }
        // ==========================================


        foreach (Vector2Int pos in roomPositions)
        {
            Vector3 worldPos = new Vector3(pos.x * roomWidth, pos.y * roomHeight, 0);
            GameObject newRoom = Instantiate(roomPrefab, worldPos, Quaternion.identity);
            newRoom.name = "Room_" + pos.x + "_" + pos.y;
            spawnedRooms.Add(pos, newRoom);

            RoomController controller = newRoom.GetComponent<RoomController>();

            // ★ [추가됨] 방을 생성하자마자, 이 층의 바닥 이미지로 갈아 끼워줍니다!
            if (currentFloorData.roomFloorSprite != null)
            {
                controller.ChangeRoomBackground(currentFloorData.roomFloorSprite);
            }

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
            // 2. 상점방 소환
            else if (pos == shopRoomPos)
            {
                controller.SetAsShopRoom();
                if (merchantPrefab != null) Instantiate(merchantPrefab, worldPos + new Vector3(0, 2f, 0), Quaternion.identity);
                if (shopItemPrefab != null)
                {
                    GameObject healthStand = Instantiate(shopItemPrefab, worldPos + new Vector3(-3f, -0.5f, 0), Quaternion.identity);
                    healthStand.GetComponent<ShopItem>().SetupHealth(15, shopHealthSprite);

                    // ★ [새로 추가됨] 중앙: 열쇠 판매대 (10원)
                    GameObject keyStand = Instantiate(shopItemPrefab, worldPos + new Vector3(0f, -0.5f, 0), Quaternion.identity);
                    keyStand.GetComponent<ShopItem>().SetupKey(10, shopKeySprite);

                    // 오른쪽: 랜덤 아이템 판매대 (15원)
                    if (possibleItems.Length > 0)
                    {
                        GameObject itemStand = Instantiate(shopItemPrefab, worldPos + new Vector3(3f, -0.5f, 0), Quaternion.identity);
                        ItemData randomItemData = possibleItems[Random.Range(0, possibleItems.Length)];
                        itemStand.GetComponent<ShopItem>().SetupItem(randomItemData, 15);
                    }
                }
            }
            // 3. 보스방 소환
            else if (pos == bossRoomPos)
            {
                EnemyData randomBoss = null;
                if (currentFloorData.bosses.Length > 0)
                    randomBoss = currentFloorData.bosses[Random.Range(0, currentFloorData.bosses.Length)];

                controller.SetAsBossRoom(itemPickupPrefab, possibleItems, nextStagePortalPrefab, randomBoss);

                if (enemyPrefab != null && randomBoss != null)
                {
                    GameObject spawnedBoss = Instantiate(enemyPrefab, worldPos, Quaternion.identity);
                    Enemy bossScript = spawnedBoss.GetComponent<Enemy>();
                    bossScript.Setup(randomBoss);

                    float scale = randomBoss.bossScale;
                    spawnedBoss.transform.localScale = new Vector3(scale, scale, 1f);

                    bossScript.currentRoom = controller;
                    controller.enemiesInRoom.Add(bossScript);
                }
            }
            // 4. 일반 몬스터 소환
            else if (pos != Vector2Int.zero && enemyPrefab != null && currentFloorData.enemies.Length > 0)
            {
                int enemyCount = Random.Range(1, 4);
                for (int i = 0; i < enemyCount; i++)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-4f, 4f), Random.Range(-0.5f, 0.5f), 0);
                    GameObject spawnedEnemy = Instantiate(enemyPrefab, worldPos + randomOffset, Quaternion.identity);
                    Enemy enemyScript = spawnedEnemy.GetComponent<Enemy>();
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

        // ★ [수정됨] 배경을 칠할 때, 현재 층의 타일 패턴을 넘겨줍니다!
        GenerateBackground(roomPositions, currentFloorData.backgroundPattern);
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
    // ==========================================
    // ★ [완벽한 순서로 제어되는 층 이동 코루틴!]
    // ==========================================
    private System.Collections.IEnumerator NextStageRoutine()
    {
        Time.timeScale = 0f;
        int floorIndex = Mathf.Clamp(currentFloor - 1, 0, floorSettings.Length - 1);
        FloorData currentData = floorSettings[floorIndex];

        // 1. 컷신 브금 재생!
        if (BGMManager.Instance != null && currentData.transitionBgm != null)
            BGMManager.Instance.PlayTransitionBGM(currentData.transitionBgm);

        // 2. 화면 까맣게 암전
        if (StageTransitionUI.Instance != null)
            yield return StartCoroutine(StageTransitionUI.Instance.FadeToBlack());

        // 3. 화면 가려진 동안 기존 맵 지우고 새 맵 생성
        foreach (var room in spawnedRooms.Values) { if (room != null) Destroy(room); }
        spawnedRooms.Clear();
        ItemPickup[] items = FindObjectsOfType<ItemPickup>(); foreach (var item in items) Destroy(item.gameObject);
        NextStagePortal[] portals = FindObjectsOfType<NextStagePortal>(); foreach (var portal in portals) Destroy(portal.gameObject);
        Enemy[] enemies = FindObjectsOfType<Enemy>(); foreach (var enemy in enemies) Destroy(enemy.gameObject);
        Bullet[] bullets = FindObjectsOfType<Bullet>(); foreach (var bullet in bullets) Destroy(bullet.gameObject);

        if (backgroundTilemap != null) backgroundTilemap.ClearAllTiles();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) player.transform.position = Vector3.zero;
        if (RoomManager.Instance != null) RoomManager.Instance.ResetRoomCoordinates();

        GenerateMap(); // 맵 생성 완료! (하지만 화면은 아직 까만 상태)

        // 4. 암전 상태 컷신 대화 (일러스트 띄우기)
        DialogueLine[] transitionDialogues = currentData.transitionDialogues;
        if (transitionDialogues != null && transitionDialogues.Length > 0)
        {
            bool isCutsceneFinished = false;
            DialogueManager.instance.onDialogueEndCallback = () =>
            {
                if (StageTransitionUI.Instance != null) StageTransitionUI.Instance.HideCG();
                isCutsceneFinished = true;
            };
            DialogueManager.instance.StartDialogue(transitionDialogues);

            while (!isCutsceneFinished) yield return null;
        }

        // 5. "X층" 글씨 띄우고 검은 화면 스르륵 걷어내기
        if (StageTransitionUI.Instance != null)
            yield return StartCoroutine(StageTransitionUI.Instance.ShowFloorTextAndFadeOut(currentFloor));

        // 6. 화면 밝아진 직후! 맵 전용 BGM으로 교체!
        if (BGMManager.Instance != null && currentData.floorBgm != null)
            BGMManager.Instance.PlayStageBGM(currentData.floorBgm);

        // 7. 맵 위에 덩그러니 선 플레이어의 독백 대화 시작
        DialogueLine[] startDialogues = currentData.floorStartDialogues;
        if (startDialogues != null && startDialogues.Length > 0)
        {
            bool isStartDialogueFinished = false;
            DialogueManager.instance.onDialogueEndCallback = () =>
            {
                isStartDialogueFinished = true;
            };
            DialogueManager.instance.StartDialogue(startDialogues);

            while (!isStartDialogueFinished) yield return null; // 독백 끝날 때까지 게임 정지 유지
        }

        // 8. 모든 연출 종료, 시간 원상 복구 및 게임 시작!
        Time.timeScale = 1f;
    }

    void GenerateBackground(List<Vector2Int> roomPositions, TileBase[] pattern)
    {
        if (backgroundTilemap == null || pattern == null || pattern.Length != 16) return;

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

                backgroundTilemap.SetTile(new Vector3Int(x, y, 0), pattern[tileIndex]);
            }
        }
    }

    // ==========================================
    //  [새로 추가] 마지막 층(예: 3층)을 클리어했을 때 실행되는 엔딩 함수
    // ==========================================
    // ==========================================
    //  [기존 코드] private void ShowEnding()
    // [수정된 코드] public으로 변경!
    // ==========================================
    // ==========================================
    // ★ 마지막 층(5층) 클리어 시 실행되는 엔딩 함수
    // ==========================================
    public void ShowEnding()
    {
        Debug.Log("게임 클리어! 진엔딩 연출이 시작됩니다!");

        // 1. 진엔딩 업적 해금
        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.UnlockAchievement("TrueEnding");
        }

        // ==========================================
        // ★ 2. [새로 추가됨] 씬이 넘어가서 플레이어가 사라지기 직전에 영구 지갑으로 코인 송금!
        // ==========================================
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && PlayerDataManager.Instance != null)
        {
            int earnedCoins = player.GetComponent<Player>().coinCount;
            PlayerDataManager.Instance.AddCoins(earnedCoins);
            Debug.Log($"[진엔딩] 살아서 돌아왔습니다! {earnedCoins}코인을 영구 지갑에 안전하게 저장했습니다!");
        }

        // 3. 진엔딩 씬으로 이동! (만들어두신 씬 이름을 적어주세요)
        UnityEngine.SceneManagement.SceneManager.LoadScene("GoodEndingScene"); 
    }
}