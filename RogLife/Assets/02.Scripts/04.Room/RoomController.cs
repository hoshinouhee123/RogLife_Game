using UnityEngine;

public class RoomController : MonoBehaviour
{
    [Header("문 오브젝트")]
    public GameObject doorTop;
    public GameObject doorBottom;
    public GameObject doorLeft;
    public GameObject doorRight;

    [Header("막힌 벽(Block) 오브젝트")]
    public GameObject blockTop;
    public GameObject blockBottom;
    public GameObject blockLeft;
    public GameObject blockRight;

    // MapGenerator가 맵을 다 만들고 나서 이 함수를 부를 겁니다.
    public void SetupDoors(bool hasTop, bool hasBottom, bool hasLeft, bool hasRight)
    {
        // 연결된 방이 있으면 문(Door)을 켜고, 벽(Block)을 끕니다.
        doorTop.SetActive(hasTop);
        blockTop.SetActive(!hasTop);

        doorBottom.SetActive(hasBottom);
        blockBottom.SetActive(!hasBottom);

        doorLeft.SetActive(hasLeft);
        blockLeft.SetActive(!hasLeft);

        doorRight.SetActive(hasRight);
        blockRight.SetActive(!hasRight);
    }
}