using UnityEngine;

public class NextStagePortal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 콘솔창에 닿은 물체 이름 띄우기 (작동 테스트용)
        Debug.Log(collision.gameObject.name + "가 포탈에 닿았습니다!");

        if (collision.CompareTag("Player"))
        {
            Debug.Log("다음 층으로 이동합니다!");
            MapGenerator.Instance.GoToNextStage();
        }
    }
}