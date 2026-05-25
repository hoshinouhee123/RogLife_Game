using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
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

    private Dictionary<Vector2Int, GameObject> spawnedRooms = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        List<Vector2Int> roomPositions = new List<Vector2Int>();
        roomPositions.Add(Vector2Int.zero);

        // 1. 방 좌표 랜덤 생성
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
        // ★ [추가된 로직] 아이템 방 좌표 지정 (30% 확률)
        // ==========================================
        bool hasItemRoom = Random.Range(0, 100) < 100; // 30% 확률로 아이템 방 등장
        Vector2Int itemRoomPos = new Vector2Int(9999, 9999); // 일단 절대 안 나올 좌표로 초기화

        // 방이 2개 이상이고 당첨되었다면, 시작방(0,0)을 제외한 방 중 하나를 아이템 방으로 지정
        if (hasItemRoom && roomPositions.Count > 1)
        {
            int randomIndex = Random.Range(1, roomPositions.Count);
            itemRoomPos = roomPositions[randomIndex];
        }
        // ==========================================

        // 2. 방 생성 및 몬스터/아이템 소환
        foreach (Vector2Int pos in roomPositions)
        {
            Vector3 worldPos = new Vector3(pos.x * roomWidth, pos.y * roomHeight, 0);
            GameObject newRoom = Instantiate(roomPrefab, worldPos, Quaternion.identity);
            newRoom.name = "Room_" + pos.x + "_" + pos.y;
            spawnedRooms.Add(pos, newRoom);

            RoomController controller = newRoom.GetComponent<RoomController>();

            // ==========================================
            // ★ [수정된 로직] 아이템 방인지 몬스터 방인지 구분해서 소환
            // ==========================================

            // 케이스 A: 이 방이 방금 지정된 '아이템 방'이라면?
            if (pos == itemRoomPos)
            {

                // 이 방의 미니맵에 '황금방 마커'를 띄우라고 명령!
                controller.SetAsItemRoom();

                // 몬스터는 안 낳고 정중앙에 아이템만 딱 1개 소환!
                if (itemPickupPrefab != null && possibleItems.Length > 0)
                {
                    GameObject spawnedItem = Instantiate(itemPickupPrefab, worldPos, Quaternion.identity);

                    ItemData randomItemData = possibleItems[Random.Range(0, possibleItems.Length)];
                    spawnedItem.GetComponent<ItemPickup>().Setup(randomItemData);
                }
            }
            // 케이스 B: 아이템 방도 아니고, 시작 방(0,0)도 아니라면? -> 평소처럼 몬스터 소환
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

                    controller.enemiesInRoom.Add(enemyScript);
                }
            }
        }

        // 3. 문 열고 닫기 및 이웃 방 연결 세팅
        foreach (var kvp in spawnedRooms)
        {
            Vector2Int pos = kvp.Key;
            RoomController controller = kvp.Value.GetComponent<RoomController>();

            RoomController top = spawnedRooms.ContainsKey(pos + Vector2Int.up) ? spawnedRooms[pos + Vector2Int.up].GetComponent<RoomController>() : null;
            RoomController bottom = spawnedRooms.ContainsKey(pos + Vector2Int.down) ? spawnedRooms[pos + Vector2Int.down].GetComponent<RoomController>() : null;
            RoomController left = spawnedRooms.ContainsKey(pos + Vector2Int.left) ? spawnedRooms[pos + Vector2Int.left].GetComponent<RoomController>() : null;
            RoomController right = spawnedRooms.ContainsKey(pos + Vector2Int.right) ? spawnedRooms[pos + Vector2Int.right].GetComponent<RoomController>() : null;

            if (controller != null)
            {
                controller.SetupDoors(top, bottom, left, right);
            }
        }

        // 4. 맵 생성이 다 끝나면, 시작 방(0,0)에 강제로 "방문 완료" 처리를 해서 불을 밝혀줍니다!
        if (spawnedRooms.ContainsKey(Vector2Int.zero))
        {
            spawnedRooms[Vector2Int.zero].GetComponent<RoomController>().VisitRoom();
        }

        GenerateBackground(roomPositions);
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