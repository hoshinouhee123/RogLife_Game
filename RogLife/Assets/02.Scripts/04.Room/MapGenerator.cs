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

    private Dictionary<Vector2Int, GameObject> spawnedRooms = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        List<Vector2Int> roomPositions = new List<Vector2Int>();
        roomPositions.Add(Vector2Int.zero);

        // 랜덤 방 생성
        while (roomPositions.Count < maxRooms)
        {
            Vector2Int currentPos = roomPositions[Random.Range(0, roomPositions.Count)];
            Vector2Int newPos = currentPos;

            int randomDir = Random.Range(0, 4);
            if (randomDir == 0) newPos += Vector2Int.up;
            else if (randomDir == 1) newPos += Vector2Int.down;
            else if (randomDir == 2) newPos += Vector2Int.left;
            else if (randomDir == 3) newPos += Vector2Int.right;

            if (!roomPositions.Contains(newPos))
            {
                roomPositions.Add(newPos);
            }
        }

        // 방 오브젝트 생성
        foreach (Vector2Int pos in roomPositions)
        {
            Vector3 worldPos = new Vector3(pos.x * roomWidth, pos.y * roomHeight, 0);
            GameObject newRoom = Instantiate(roomPrefab, worldPos, Quaternion.identity);
            newRoom.name = "Room_" + pos.x + "_" + pos.y;
            spawnedRooms.Add(pos, newRoom);
        }

        // 각 방의 문(Door) 활성화/비활성화 처리
        foreach (var kvp in spawnedRooms)
        {
            Vector2Int pos = kvp.Key;
            GameObject roomObj = kvp.Value;

            bool hasTop = spawnedRooms.ContainsKey(pos + Vector2Int.up);
            bool hasBottom = spawnedRooms.ContainsKey(pos + Vector2Int.down);
            bool hasLeft = spawnedRooms.ContainsKey(pos + Vector2Int.left);
            bool hasRight = spawnedRooms.ContainsKey(pos + Vector2Int.right);

            RoomController controller = roomObj.GetComponent<RoomController>();
            if (controller != null)
            {
                controller.SetupDoors(hasTop, hasBottom, hasLeft, hasRight);
            }
        }

        // 맵 생성이 끝나면 배경 패턴 타일 깔기
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