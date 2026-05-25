using UnityEngine;
using UnityEngine.SceneManagement;

public class InteractableObject : MonoBehaviour
{
    // 인스펙터에서 여러 줄의 대화와 각각의 일러스트를 설정.
    public DialogueLine[] dialogueLines;

    [Header("씬 이동 설정 (태그가 Door일 때만 작동)")]
    public string nextSceneName;

    // ★ [추가됨] 연타 방지용 쿨타임 스위치
    private bool isCooldown = false;

    public void Interact()
    {
        // ★ [핵심 방어 코드] 쿨타임 중이거나, 이미 대화창이 켜져있으면 무시하고 튕겨냄!
        if (isCooldown || DialogueManager.instance.isDialogueActive) return;

        isCooldown = true; // 쿨타임 스위치 켜기

        // 1. 매니저에게 대화가 다 끝나면 이 코드를 실행하라고 예약.
        DialogueManager.instance.onDialogueEndCallback = () =>
        {
            // 이 오브젝트의 태그가 Door인지 확인
            if (gameObject.CompareTag("Door"))
            {
                if (!string.IsNullOrEmpty(nextSceneName))
                {
                    SceneManager.LoadScene(nextSceneName);
                }
            }

            // ★ [추가됨] 대화가 완전히 끝난 뒤, 0.2초 뒤에 다시 말을 걸 수 있게 쿨타임 해제
            Invoke("ResetCooldown", 0.2f);
        };

        // 2. 예약이 끝났으니 텍스트 띄우기 시작
        DialogueManager.instance.StartDialogue(dialogueLines);
    }

    // ★ [추가됨] 쿨타임을 풀어주는 함수
    private void ResetCooldown()
    {
        isCooldown = false;
    }
}