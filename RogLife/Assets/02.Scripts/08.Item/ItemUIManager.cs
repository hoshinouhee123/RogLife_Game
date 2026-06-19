using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro; // ★ [필수] DOTween을 사용하기 위한 주문!

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

    private RectTransform popupRect;
    private Sequence popupSequence; // ★ 현재 재생 중인 DOTween 애니메이션 기억용

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (popupPanel != null)
        {
            popupRect = popupPanel.GetComponent<RectTransform>();
        }
    }

    private void Start()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    public void ShowItemGet(ItemData item, int count = 1)
    {
        // 1. 우측 인벤토리에 아이템 아이콘 추가 (기존 동일)
        for (int i = 0; i < count; i++)
        {
            GameObject newSlot = Instantiate(inventorySlotPrefab, inventoryParent);
            newSlot.GetComponent<Image>().sprite = item.itemIcon;
        }

        // 2. 팝업 데이터 세팅
        string countText = count > 1 ? " x" + count : "";
        popupNameText.text = item.itemName + countText;
        popupDescText.text = item.itemDescription;
        popupIcon.sprite = item.itemIcon;

        // 3. DOTween 팝업 연출 실행!
        PlayPopupAnimation();
    }

    private void PlayPopupAnimation()
    {
        // ==========================================
        // ★ [버그 완벽 차단] 이미 팝업이 떠있는 도중에 또 아이템을 먹었다면?
        // 기존 애니메이션을 즉시 강제 취소해서 UI가 굳어버리는 버그를 막습니다!
        // ==========================================
        if (popupSequence != null && popupSequence.IsActive())
        {
            popupSequence.Kill();
        }

        // 초기화 (크기를 0으로 만들고 켬)
        popupPanel.SetActive(true);
        popupRect.localScale = Vector3.zero;

        // ==========================================
        // ★ [DOTween 연출] Sequence를 사용해 차례대로 예약해 둡니다.
        // ==========================================
        popupSequence = DOTween.Sequence();

        // 중요! 아이템을 먹으면 게임 시간이 0(정지)이 되므로, 이걸 켜야 애니메이션이 움직입니다.
        popupSequence.SetUpdate(true);

        // ① [등장] 0.4초 동안 크기가 0에서 1로 '띠용!(OutBack)' 하고 커집니다.
        popupSequence.Append(popupRect.DOScale(1f, 0.4f).SetEase(Ease.OutBack));

        // ② [대기] 설정한 시간(2.5초) 동안 가만히 머무릅니다.
        popupSequence.AppendInterval(popupDuration);

        // ③ [퇴장] 0.3초 동안 다시 크기가 0으로 스르륵(InBack) 줄어듭니다.
        popupSequence.Append(popupRect.DOScale(0f, 0.3f).SetEase(Ease.InBack));

        // ④ [종료] 애니메이션이 완전히 끝나면 패널을 깔끔하게 끕니다.
        popupSequence.OnComplete(() =>
        {
            popupPanel.SetActive(false);
        });
    }
}