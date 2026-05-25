using UnityEngine;

public class AutoDialogueTrigger : MonoBehaviour
{
    [Header("자동 재생할 대화 내용")]
    public DialogueLine[] dialogueLines;

    [Header("옵션")]
    public bool triggerOnlyOnce = true; // true면 한 번만 실행됨
    private bool hasTriggered = false;  // 이미 실행되었는지 체크하는 변수

    // Start()에서 자동으로 대화 시작 (씬이 로드될 때 바로 대화가 시작됨)
    private void Start()
    {
        DialogueManager.instance.StartDialogue(dialogueLines);
    }
    
}