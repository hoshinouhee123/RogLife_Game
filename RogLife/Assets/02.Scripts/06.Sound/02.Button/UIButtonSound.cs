using UnityEngine;
using UnityEngine.EventSystems; // 마우스 이벤트를 위해 필요

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    // 마우스가 버튼 위에 올라갔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayHoverSound();
        }
    }

    // 마우스로 버튼을 클릭했을 때
    public void OnPointerClick(PointerEventData eventData)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayClickSound();
        }
    }
}