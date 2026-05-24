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
    public GameObject enemyPrefab;    // 아까 만든 빈 껍데기 Enemy_Template 프리팹
    public EnemyData[] possibleEnemies; // 만들어둔 스탯 데이터(박쥐, 슬라임 등) 다 넣기

    private Dictionary<Vector2Int, GameObject> spawnedRooms = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        List<Vector2Int> roomPositions = new List<Vector2Int>();
        roomPositions.Add(Vector2Int.zero);

        // 방 좌표 랜덤 생성 (기존과 동일)
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

        // 방 생성 및 몬스터 소환
        foreach (Vector2Int pos in roomPositions)
        {
            Vector3 worldPos = new Vector3(pos.x * roomWidth, pos.y * roomHeight, 0);
            GameObject newRoom = Instantiate(roomPrefab, worldPos, Quaternion.identity);
            newRoom.name = "Room_" + pos.x + "_" + pos.y;
            spawnedRooms.Add(pos, newRoom);

            RoomController controller = newRoom.GetComponent<RoomController>();

            // ★ [추가된 부분] 시작 방(0,0)이 아니면 몬스터를 랜덤으로 소환!
            if (pos != Vector2Int.zero && enemyPrefab != null && possibleEnemies.Length > 0)
            {
                int enemyCount = Random.Range(1, 4); // 1~3마리 랜덤 소환
                for (int i = 0; i < enemyCount; i++)
                {
                    // 방 안쪽 랜덤한 위치(Offset)에 소환
                    // 방 한가운데 안전 구역(-4 ~ 4, -2 ~ 2)에만 스폰 문 근처에는 절대 안 나옴.
                    Vector3 randomOffset = new Vector3(Random.Range(-4f, 4f), Random.Range(-2f, 2f), 0);
                    GameObject spawnedEnemy = Instantiate(enemyPrefab, worldPos + randomOffset, Quaternion.identity);

                    Enemy enemyScript = spawnedEnemy.GetComponent<Enemy>();

                    // 어떤 몬스터 데이터(슬라임, 박쥐)를 줄지 랜덤으로 뽑기
                    EnemyData randomData = possibleEnemies[Random.Range(0, possibleEnemies.Length)];
                    enemyScript.Setup(randomData); // 몬스터에게 데이터 주입!

                    // 방의 '현재 살아있는 몬스터 리스트'에 등록
                    controller.enemiesInRoom.Add(enemyScript);
                }
            }
        }

        // 3. 문 열고 닫기 및 이웃 방 연결 세팅 (수정됨!)
        foreach (var kvp in spawnedRooms)
        {
            Vector2Int pos = kvp.Key;
            RoomController controller = kvp.Value.GetComponent<RoomController>();

            // 상하좌우에 방이 존재한다면, 그 방의 RoomController 스크립트를 가져옵니다.
            RoomController top = spawnedRooms.ContainsKey(pos + Vector2Int.up) ? spawnedRooms[pos + Vector2Int.up].GetComponent<RoomController>() : null;
            RoomController bottom = spawnedRooms.ContainsKey(pos + Vector2Int.down) ? spawnedRooms[pos + Vector2Int.down].GetComponent<RoomController>() : null;
            RoomController left = spawnedRooms.ContainsKey(pos + Vector2Int.left) ? spawnedRooms[pos + Vector2Int.left].GetComponent<RoomController>() : null;
            RoomController right = spawnedRooms.ContainsKey(pos + Vector2Int.right) ? spawnedRooms[pos + Vector2Int.right].GetComponent<RoomController>() : null;

            if (controller != null)
            {
                // 이웃 방들의 정보를 통째로 넘겨줍니다.
                controller.SetupDoors(top, bottom, left, right);
            }
        }

        // 4. ★ 맵 생성이 다 끝나면, 시작 방(0,0)에 강제로 "방문 완료" 처리를 해서 불을 밝혀줍니다!
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

        // 생성된 전체 맵의 영역(Bounds) 구하기
        foreach (Vector2Int pos in roomPositions)
        {
            if (pos.x < minRoomX) minRoomX = pos.x;
            if (pos.x > maxRoomX) maxRoomX = pos.x;
            if (pos.y < minRoomY) minRoomY = pos.y;
            if (pos.y > maxRoomY) maxRoomY = pos.y;
        }

        // 화면 밖 여백 추가
        minRoomX -= 1; maxRoomX += 1;
        minRoomY -= 1; maxRoomY += 1;

        // 타일 좌표계로 변환
        int startTileX = Mathf.RoundToInt(minRoomX * roomWidth - (roomWidth / 2));
        int endTileX = Mathf.RoundToInt(maxRoomX * roomWidth + (roomWidth / 2));
        int startTileY = Mathf.RoundToInt(minRoomY * roomHeight - (roomHeight / 2));
        int endTileY = Mathf.RoundToInt(maxRoomY * roomHeight + (roomHeight / 2));

        // 16개 타일을 4x4 패턴으로 반복 도배하기
        for (int x = startTileX; x <= endTileX; x++)
        {
            for (int y = startTileY; y <= endTileY; y++)
            {
                int patternX = (x % 4 + 4) % 4; // 0 ~ 3
                int patternY = (y % 4 + 4) % 4; // 0 ~ 3

                // 왼쪽 위부터 오른쪽 아래로 0~15번 인덱스 매칭 공식
                int tileIndex = (3 - patternY) * 4 + patternX;

                backgroundTilemap.SetTile(new Vector3Int(x, y, 0), backgroundPattern[tileIndex]);
            }
        }
    }
}