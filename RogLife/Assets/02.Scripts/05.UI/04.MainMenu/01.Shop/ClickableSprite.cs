using UnityEngine;
using UnityEngine.EventSystems; // UI 클릭 감지를 위해 필수!

// ★ IPointerClickHandler를 달아주면 버튼 컴포넌트 없이도 UI 클릭을 감지합니다.
public class MainMenuMerchant : MonoBehaviour, IPointerClickHandler
{
    [Header("메인 메뉴 상점 UI 연결")]
    public MainMenuShopUI shopUI;

    // ==========================================
    // 1. 상인이 캔버스 안의 UI(Image)일 경우 여기서 클릭을 감지합니다!
    // ==========================================
    public void OnPointerClick(PointerEventData eventData)
    {
        if (shopUI != null)
        {
            shopUI.OnClickMerchant();
        }
    }

    // ==========================================
    // 2. 상인이 조명을 받기 위해 캔버스 밖의 도트(Sprite+Collider)일 경우 여기서 감지합니다!
    // ==========================================
    private void OnMouseDown()
    {
        // 마우스가 UI 패널이나 버튼에 가려져 있을 때는 클릭되지 않도록 방지
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (shopUI != null)
        {
            shopUI.OnClickMerchant();
        }
    }
}