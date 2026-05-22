using UnityEngine;

public class AutoDialogueTrigger : MonoBehaviour
{
    [Header("자동 재생할 대화 내용")]
    public DialogueLine[] dialogueLines;

    [Header("옵션")]
    public bool triggerOnlyOnce = true; // true면 한 번만 실행됨
    private bool hasTriggered = false;  // 이미 실행되었는지 체크하는 변수

    // 플레이어가 특정 영역(Trigger)에 들어왔을 때 자동으로 실행되는 코드(필요시 주석 해제
    /*
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 이미 실행된 이벤트라면 무시
        if (triggerOnlyOnce && hasTriggered) return;

        // 2. 부딪힌 대상이 플레이어인지 확인 (태그로 구별)
        if (collision.CompareTag("Player"))
        {
            hasTriggered = true; // 실행 완료로 체크

            // 대화 매니저를 통해 대화 강제 시작
            DialogueManager.instance.StartDialogue(dialogueLines);
        }
    }
    */

    // Start()에서 자동으로 대화 시작 (씬이 로드될 때 바로 대화가 시작됨)
    private void Start()
    {
        DialogueManager.instance.StartDialogue(dialogueLines);
    }
    
}