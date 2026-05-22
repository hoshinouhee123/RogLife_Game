using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    // 인스펙터에서 여러 줄의 대화와 각각의 일러스트를 설정.
    public DialogueLine[] dialogueLines;

    // 플레이어가 상호작용 키를 누르면 실행될 함수
    public void Interact()
    {
        DialogueManager.instance.StartDialogue(dialogueLines);
    }
}