using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 사용을 위해 필요

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance; // 싱글톤 패턴 (어디서든 접근 가능하게)

    public GameObject dialoguePanel;   // 대화창 패널
    public Image portraitImage;        // 캐릭터 일러스트 UI
    public TextMeshProUGUI dialogueText; // 대화 텍스트 UI

    private Queue<DialogueLine> sentences; // 대화 목록을 담을 큐(Queue)
    public bool isDialogueActive = false;  // 현재 대화 중인지 체크

    void Awake()
    {
        if (instance == null) instance = this;
        sentences = new Queue<DialogueLine>();
    }

    // 대화 시작
    public void StartDialogue(DialogueLine[] dialogueLines)
    {
        isDialogueActive = true;
        dialoguePanel.SetActive(true);
        sentences.Clear();

        // 큐에 모든 대화 줄을 넣음
        foreach (DialogueLine line in dialogueLines)
        {
            sentences.Enqueue(line);
        }

        DisplayNextSentence();
    }

    // 다음 대화 출력
    public void DisplayNextSentence()
    {
        // 큐가 비었다면 대화 종료
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine currentLine = sentences.Dequeue();

        // UI 업데이트
        if (currentLine.characterPortrait != null)
        {
            portraitImage.sprite = currentLine.characterPortrait;
            portraitImage.gameObject.SetActive(true);
        }
        else
        {
            portraitImage.gameObject.SetActive(false); // 일러스트가 없으면 숨김
        }

        dialogueText.text = currentLine.sentence;
    }

    // 대화 종료
    public void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
    }
}