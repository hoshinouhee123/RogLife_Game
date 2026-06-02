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

    [Header("글리치(공포) 연출 옵션")]
    public bool useGlitch;           // 이 대사에서 글리치 효과를 쓸 것인가?
    public Sprite glitchPortrait;    // 순간적으로 바뀔 기괴한 일러스트
    public string glitchSentence;    // 순간적으로 바뀔 기괴한 텍스트 (예: 살려줘살려줘살려줘)
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

    // ==========================================
    // ★ [새로 추가됨] 화면 전체 공포 연출용 UI
    // ==========================================
    [Header("화면 전체 글리치 연출")]
    public GameObject darkOverlay;     // 화면 어두워짐 (검은 반투명 패널)
    public GameObject glitchOverlay;   // 지지직거리는 TV 노이즈 이미지

    // ★ [새로 추가됨] 글리치 효과음 (치지직! 하는 잡음)
    public AudioClip glitchSound;

    // ★ [수정됨] 현재 출력 중인 대사 정보를 통째로 기억하도록 변경
    private DialogueLine currentActiveLine;


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
        if (isTyping)
        {
            StopAllCoroutines();

            // 스킵 시 혹시 글리치 중이었다면 강제로 정상 복구!
            dialogueText.color = Color.white;
            dialogueText.text = currentActiveLine.sentence;
            if (currentActiveLine.characterPortrait != null)
                portraitImage.sprite = currentActiveLine.characterPortrait;

            // ★ [추가됨] 스킵 시 멈춰있던 BGM도 강제로 다시 틀어줌!
            if (BGMManager.Instance != null && BGMManager.Instance.audioSource != null)
            {
                BGMManager.Instance.audioSource.UnPause();
            }

            // ★ [추가됨] 스킵 시 화면 연출도 강제로 꺼줌!
            if (darkOverlay != null) darkOverlay.SetActive(false);
            if (glitchOverlay != null) glitchOverlay.SetActive(false);

            isTyping = false;
            return;
        }

        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentActiveLine = sentences.Dequeue();
        nameText.text = currentActiveLine.speakerName;
        dialogueText.color = Color.white;

        if (currentActiveLine.characterPortrait != null)
        {
            portraitImage.sprite = currentActiveLine.characterPortrait;
            portraitImage.gameObject.SetActive(true);
        }
        else { portraitImage.gameObject.SetActive(false); }

        StopAllCoroutines();
        StartCoroutine(TypeSentence(currentActiveLine));
    }

    // ★ [수정됨] 타이핑 도중 한 문장당 딱 1번만 글리치를 터뜨리도록 방어막 추가!
    IEnumerator TypeSentence(DialogueLine line)
    {
        isTyping = true;
        dialogueText.text = "";

        char[] chars = line.sentence.ToCharArray();
        bool hasGlitched = false; // ★ 이 문장에서 글리치가 터졌는지 기억하는 변수

        for (int i = 0; i < chars.Length; i++)
        {
            dialogueText.text += chars[i];

            if (chars[i] != ' ' && audioSource != null && typingSound != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(typingSound);
            }

            // ==============================================================
            // [공포 연출] 글리치 옵션이 켜져 있고, 아직 안 터졌으며, 확률에 당첨되었다면?
            // ==============================================================
            // (테스트용으로 100을 넣어도, hasGlitched 덕분에 딱 1번만 터집니다!)
            if (line.useGlitch && !hasGlitched && UnityEngine.Random.Range(0, 100) < 2) // 확률은 2~5 정도로 맞춰주세요!
            {
                hasGlitched = true; // 이제 이 문장에선 더 이상 안 터짐!
                yield return StartCoroutine(GlitchRoutine(line));
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
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
        // ★ [새로 추가됨] 콘솔 켜져있으면 띄어쓰기(스페이스바)를 쳐도 대화가 안 넘어감!
        if (CheatConsole.Instance != null && CheatConsole.Instance.isConsoleActive) return;

        // 대화창이 켜져 있을 때만 작동
        if (isDialogueActive)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                DisplayNextSentence();
            }
        }
    }

    // ★ [수정됨] 글리치 중 BGM 정지 기능 추가
    IEnumerator GlitchRoutine(DialogueLine line)
    {
        string savedText = dialogueText.text;

        if (BGMManager.Instance != null && BGMManager.Instance.audioSource != null)
            BGMManager.Instance.audioSource.Pause();

        // ==============================================================
        // ★ [추가됨] 화면 어두워짐 + 지지직 노이즈 켜기 + 카메라 지진!!
        // ==============================================================
        if (darkOverlay != null) darkOverlay.SetActive(true);
        if (glitchOverlay != null) glitchOverlay.SetActive(true);

        // 화면을 0.15초 동안 덜덜덜덜 떨게 만듭니다!
        if (CameraShake.Instance != null) CameraShake.Instance.ShakeCamera(0.15f, 0.5f);

        dialogueText.color = Color.red;
        if (!string.IsNullOrEmpty(line.glitchSentence)) dialogueText.text = line.glitchSentence;
        if (line.glitchPortrait != null) portraitImage.sprite = line.glitchPortrait;

        if (audioSource != null && glitchSound != null)
            audioSource.PlayOneShot(glitchSound);

        // 0.15초 대기 (이 시간 동안 화면이 미친듯이 떨리고 지지직거립니다)
        yield return new WaitForSecondsRealtime(0.15f);

        // ==============================================================
        // ★ [추가됨] 연출 종료 시 화면 끄기
        // ==============================================================
        if (darkOverlay != null) darkOverlay.SetActive(false);
        if (glitchOverlay != null) glitchOverlay.SetActive(false);

        dialogueText.color = Color.white;
        dialogueText.text = savedText;
        if (line.characterPortrait != null) portraitImage.sprite = line.characterPortrait;

        if (BGMManager.Instance != null && BGMManager.Instance.audioSource != null)
            BGMManager.Instance.audioSource.UnPause();
    }
}