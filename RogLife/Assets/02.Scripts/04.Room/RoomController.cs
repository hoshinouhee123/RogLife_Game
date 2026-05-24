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

    [Header("미니맵 통로 아이콘)")]
    public GameObject mapIconTop;
    public GameObject mapIconBottom;
    public GameObject mapIconLeft;
    public GameObject mapIconRight;

    public void SetupDoors(bool hasTop, bool hasBottom, bool hasLeft, bool hasRight)
    {
        // 1. 실제 게임 화면의 문과 벽 세팅
        doorTop.SetActive(hasTop);
        blockTop.SetActive(!hasTop);

        doorBottom.SetActive(hasBottom);
        blockBottom.SetActive(!hasBottom);

        doorLeft.SetActive(hasLeft);
        blockLeft.SetActive(!hasLeft);

        doorRight.SetActive(hasRight);
        blockRight.SetActive(!hasRight);

        // 2. 미니맵 통로 아이콘 세팅 (길이 있으면 켜고, 없으면 끔)
        if (mapIconTop != null) mapIconTop.SetActive(hasTop);
        if (mapIconBottom != null) mapIconBottom.SetActive(hasBottom);
        if (mapIconLeft != null) mapIconLeft.SetActive(hasLeft);
        if (mapIconRight != null) mapIconRight.SetActive(hasRight);
    }
}