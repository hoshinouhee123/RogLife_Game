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
    public EnemyData[] possibleEnemies;

    [Header("아이템 방 설정")]
    public GameObject itemPickupPrefab;
    public ItemData[] possibleItems;

    // 보스방 설정
    [Header("보스방 설정")]
    public EnemyData[] possibleBosses;       // 보스용 몬스터 데이터 
    public GameObject nextStagePortalPrefab; // 아까 만든 NextStagePortal 프리팹

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

        // (혹시 아이템방이랑 겹치면 아이템방 위치를 변경)
        if (bossRoomPos == itemRoomPos) itemRoomPos = roomPositions[1];

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
            // 2. [추가됨] 보스방 소환
            else if (pos == bossRoomPos)
            {
                EnemyData randomBoss = null;
                if (possibleBosses.Length > 0)
                    randomBoss = possibleBosses[Random.Range(0, possibleBosses.Length)];

                // 321[수정됨] 방 스크립트에 '어떤 보스 데이터'가 나오는지 함께 넘겨줍니다!
                controller.SetAsBossRoom(itemPickupPrefab, possibleItems, nextStagePortalPrefab, randomBoss);

                if (enemyPrefab != null && randomBoss != null)
                {
                    GameObject spawnedBoss = Instantiate(enemyPrefab, worldPos, Quaternion.identity);
                    Enemy bossScript = spawnedBoss.GetComponent<Enemy>();
                    bossScript.Setup(randomBoss);
                    spawnedBoss.transform.localScale = new Vector3(2f, 2f, 1f); // 2배 크기

                    // ★ [이거 추가!] 보스에게도 알려줌
                    bossScript.currentRoom = controller;

                    controller.enemiesInRoom.Add(bossScript);
                }
            }
            // 3. 일반 몬스터 소환
            else if (pos != Vector2Int.zero && enemyPrefab != null && possibleEnemies.Length > 0)
            {
                int enemyCount = Random.Range(1, 4);
                for (int i = 0; i < enemyCount; i++)
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-4f, 4f), Random.Range(-0.5f, 0.5f), 0);
                    GameObject spawnedEnemy = Instantiate(enemyPrefab, worldPos + randomOffset, Quaternion.identity);
                    Enemy enemyScript = spawnedEnemy.GetComponent<Enemy>();
                    EnemyData randomData = possibleEnemies[Random.Range(0, possibleEnemies.Length)];
                    enemyScript.Setup(randomData);

                    // ★ [이거 추가!] 내가 태어난 방을 적에게 알려줌
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
    public void GoToNextStage()
    {
        // 1. 기존 방들 삭제
        foreach (var room in spawnedRooms.Values)
        {
            if (room != null) Destroy(room);
        }
        spawnedRooms.Clear();

        // 2. 맵에 남아있는 모든 아이템 싹쓸이 (태그 대신 스크립트로 찾음!)
        ItemPickup[] items = FindObjectsOfType<ItemPickup>();
        foreach (var item in items) Destroy(item.gameObject);

        // 3. 맵에 남아있는 포탈 싹쓸이
        NextStagePortal[] portals = FindObjectsOfType<NextStagePortal>();
        foreach (var portal in portals) Destroy(portal.gameObject);

        // 4. 혹시나 남아있을 몬스터, 총알도 싹쓸이
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (var enemy in enemies) Destroy(enemy.gameObject);

        Bullet[] bullets = FindObjectsOfType<Bullet>();
        foreach (var bullet in bullets) Destroy(bullet.gameObject);

        // 5. 배경 타일맵 지우기
        if (backgroundTilemap != null) backgroundTilemap.ClearAllTiles();

        // 6. 플레이어 위치를 시작방(0,0)으로 즉시 되돌리기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) player.transform.position = Vector3.zero;

        // 7. RoomManager 카메라 및 좌표 초기화
        if (RoomManager.Instance != null) RoomManager.Instance.ResetRoomCoordinates();

        // 8. 드디어 새 맵 생성!
        GenerateMap();
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
}