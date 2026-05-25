using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;

// 1. 대화 데이터 구조체 (이름 추가됨)
[System.Serializable]
public struct DialogueLine
{
    public string speakerName;       // 캐릭터 이름
    public Sprite characterPortrait; // 캐릭터 일러스트
    [TextArea(3, 5)]
    public string sentence;          // 대화 내용
}

// 2. 대화 매니저
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    public Action onDialogueEndCallback; // 대화가 끝나면 실행할 행동을 저장하는 변수

    [Header("UI 연결")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;       // 이름 텍스트 UI
    public TextMeshProUGUI dialogueText;   // 대사 텍스트 UI
    public Image portraitImage;

    [Header("타이핑 효과 설정")]
    public float typingSpeed = 0.05f;      // 글자 출력 속도 (작을수록 빠름)
    public AudioSource audioSource; // 소리를 재생할 스피커 역할
    public AudioClip typingSound;   // 재생할 타이핑 효과음 파일


    private Queue<DialogueLine> sentences;
    public bool isDialogueActive = false;

    private bool isTyping = false;         // 현재 글자가 쳐지고 있는지 확인
    private string currentSentence = "";   // 현재 출력할 전체 문장 임시 저장

    void Awake()
    {
        if (instance == null) instance = this;
        sentences = new Queue<DialogueLine>();
    }

    public void StartDialogue(DialogueLine[] dialogueLines)
    {
        isDialogueActive = true;
        dialoguePanel.SetActive(true);
        sentences.Clear();

        Time.timeScale = 0f;

        foreach (DialogueLine line in dialogueLines)
        {
            sentences.Enqueue(line);
        }

        DisplayNextSentence();

    }

    // 플레이어가 스페이스바(E)를 누를 때마다 실행됨
    public void DisplayNextSentence()
    {
        // 1. 만약 지금 글자가 타타탁 쳐지고 있는 중이라면?
        if (isTyping)
        {
            // 타이핑 연출을 강제로 멈추고, 문장을 한 번에 다 보여줌
            StopAllCoroutines();
            dialogueText.text = currentSentence;
            isTyping = false;
            return;
        }

        // 2. 글자 출력이 다 끝난 상태라면 다음 대화로 넘어감
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine currentLine = sentences.Dequeue();

        // UI에 이름과 일러스트 적용
        nameText.text = currentLine.speakerName;

        if (currentLine.characterPortrait != null)
        {
            portraitImage.sprite = currentLine.characterPortrait;
            portraitImage.gameObject.SetActive(true);
        }
        else
        {
            portraitImage.gameObject.SetActive(false); // 일러스트 없으면 숨김
        }

        // 타이핑 효과 시작
        currentSentence = currentLine.sentence;
        StopAllCoroutines(); // 혹시 모를 꼬임 방지
        StartCoroutine(TypeSentence(currentSentence));
    }

    // 타이핑 효과를 담당하는 코루틴(비동기 함수)
    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = ""; // 텍스트를 일단 싹 비움

        // 문장을 한 글자씩 쪼개서 추가함
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter; // 한 글자 붙이고

            if (letter != ' ' && audioSource != null && typingSound != null)
            {
                // 소리의 높낮이(Pitch)를 살짝 랜덤으로 섞으면 덜 지루하고 자연스러움
                audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(typingSound);
            }


            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false; // 출력이 다 끝나면 상태 변경
    }

    public void EndDialogue()
    {

        isDialogueActive = false;
        dialoguePanel.SetActive(false);

        Time.timeScale = 1f;

        // 대화가 끝났을 때 예약된 행동이 있다면 실행
        if (onDialogueEndCallback != null)
        {
            onDialogueEndCallback.Invoke(); // 예약된 코드 실행
            onDialogueEndCallback = null;   // 실행 후 비워주기 (다음 대화를 위해)
        }
    }

    // 만약 대화 중에 씬이 넘어가거나 파괴될 경우를 대비한 안전장치
    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    // ★ [새로 추가됨] 대화 매니저가 직접 스페이스바를 감지합니다!
    private void Update()
    {
        // 대화창이 켜져 있을 때만 작동
        if (isDialogueActive)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                DisplayNextSentence();
            }
        }
    }
}