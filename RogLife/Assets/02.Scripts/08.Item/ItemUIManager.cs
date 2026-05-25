using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ItemUIManager : MonoBehaviour
{
    public static ItemUIManager Instance;

    [Header("상단 팝업 UI")]
    public GameObject popupPanel;
    public TextMeshProUGUI popupNameText;
    public TextMeshProUGUI popupDescText;
    public Image popupIcon;
    public float popupDuration = 2.5f;

    [Header("우측 인벤토리 UI")]
    public Transform inventoryParent;
    public GameObject inventorySlotPrefab;

    // [추가됨] 팝업창이 원래 있어야 할 '정상 위치'를 기억할 변수
    private Vector2 originalPopupPos;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // 시작할 때 원래 위치를 저장해둡니다.
        RectTransform popupRect = popupPanel.GetComponent<RectTransform>();
        originalPopupPos = popupRect.anchoredPosition;

        popupPanel.SetActive(false);
    }

    public void ShowItemGet(ItemData item)
    {
        GameObject newSlot = Instantiate(inventorySlotPrefab, inventoryParent);
        newSlot.GetComponent<Image>().sprite = item.itemIcon;

        StartCoroutine(PopupRoutine(item));
    }

    private IEnumerator PopupRoutine(ItemData item)
    {
        // 1. UI 텍스트 및 이미지 세팅
        popupNameText.text = item.itemName;
        popupDescText.text = item.itemDescription;
        popupIcon.sprite = item.itemIcon;

        RectTransform popupRect = popupPanel.GetComponent<RectTransform>();
        popupRect.localScale = Vector3.one; // 크기는 무조건 1로 고정

        // 2. 화면 우측 바깥쪽 좌표 계산 (화면 너비만큼 오른쪽으로 밀어둠)
        float slideOffset = Screen.width;
        Vector2 startPos = originalPopupPos + new Vector2(slideOffset, 0); // 시작 위치 (우측 바깥)
        Vector2 endPos = originalPopupPos;                                 // 목표 위치 (정중앙)

        // 팝업을 화면 오른쪽 밖으로 치운 뒤 켭니다.
        popupRect.anchoredPosition = startPos;
        popupPanel.SetActive(true);

        // ===============================================
        // 페르소나 스타일 등장: 우측 바깥에서 '슉!' 하고 미끄러져 옴
        // ===============================================
        float appearTime = 0.3f; // 엄청 빠르게 등장 (0.3초)
        float timer = 0f;

        while (timer < appearTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / appearTime;

            // 마법의 수식 (Ease-Out Cubic): 처음에 미친듯이 빠르고, 끝에서 슥~ 멈추는 페르소나식 브레이크
            float easeT = 1f - Mathf.Pow(1f - t, 3f);

            popupRect.anchoredPosition = Vector2.Lerp(startPos, endPos, easeT);
            yield return null;
        }
        popupRect.anchoredPosition = endPos; // 정확한 중앙 위치 강제 고정

        // ===============================================
        // 화면에 떠있는 시간 대기
        // ===============================================
        yield return new WaitForSecondsRealtime(popupDuration);

        // ===============================================
        // 페르소나 스타일 퇴장: 반대편(좌측)으로 '휙!' 하고 베어내듯 날아감
        // ===============================================
        Vector2 exitPos = originalPopupPos + new Vector2(-slideOffset, 0); // 퇴장 위치 (좌측 바깥)
        timer = 0f;
        float disappearTime = 0.2f; // 나갈 땐 더 빠르게 퇴장 (0.2초)

        while (timer < disappearTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / disappearTime;

            // 마법의 수식 (Ease-In Cubic): 서서히 출발해서 미친듯이 빠르게 사라짐
            float easeT = t * t * t;

            popupRect.anchoredPosition = Vector2.Lerp(endPos, exitPos, easeT);
            yield return null;
        }
        popupRect.anchoredPosition = exitPos;

        // 완전히 화면 밖으로 나가면 비활성화
        popupPanel.SetActive(false);
    }
}