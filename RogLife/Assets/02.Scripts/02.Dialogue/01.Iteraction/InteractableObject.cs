using UnityEngine;
using UnityEngine.SceneManagement;

public class InteractableObject : MonoBehaviour
{
    // 인스펙터에서 여러 줄의 대화와 각각의 일러스트를 설정.
    public DialogueLine[] dialogueLines;

    [Header("씬 이동 설정 (태그가 Door일 때만 작동)")]
    public string nextSceneName; // 이동할 다음 씬의 이름을 적는 곳

    // 플레이어가 상호작용 키를 누르면 실행될 함수
    public void Interact()
    {
        // 1. 매니저에게 대화가 다 끝나면 이 코드를 실행하라고 예약.
        DialogueManager.instance.onDialogueEndCallback = () =>
        {
            // 이 오브젝트의 태그가 Door인지 확인
            if (gameObject.CompareTag("Door"))
            {
                // 인스펙터에 씬 이름이 적혀있다면 그 씬으로 이동
                if (!string.IsNullOrEmpty(nextSceneName))
                {
                    SceneManager.LoadScene(nextSceneName);
                }
            }
        };

        // 2. 예약이 끝났으니 텍스트 띄우기 시작
        DialogueManager.instance.StartDialogue(dialogueLines);
    }
}