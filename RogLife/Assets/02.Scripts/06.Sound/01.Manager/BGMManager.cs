using UnityEngine;
using UnityEngine.Audio;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("오디오 설정")]
    public AudioSource audioSource;
    public AudioMixerGroup bgmMixerGroup; // BGM용 믹서 그룹 넣을 곳

    [Header("BGM 파일")]
    
    public AudioClip bossClearBgm;  // 보스 잡았을 때 나오는 승리 BGM

    [Header("엔딩 BGM")]
    public AudioClip badEndingBgm;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // ==========================================
        // ★ [Start에 있던 걸 Awake로 이사 옴!]
        // 맵 생성기(Start)가 명령을 내리기 전에, 스피커를 미리 완벽하게 세팅해 둡니다.
        // ==========================================
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (bgmMixerGroup != null)
            audioSource.outputAudioMixerGroup = bgmMixerGroup;
    }

    // 1. 일반 브금 재생
    // ★ [수정됨] 맵 생성기에서 넘어온 브금을 틀어줍니다.
    public void PlayStageBGM(AudioClip newBgm)
    {
        if (audioSource == null || newBgm == null) return;

        // 이미 그 노래가 재생 중이면 다시 처음부터 틀지 않음 (최적화)
        if (audioSource.clip == newBgm && audioSource.isPlaying) return;

        audioSource.clip = newBgm;
        audioSource.loop = true;
        audioSource.Play();
    }

    // 2. 보스 브금 재생 (보스 데이터에서 곡을 받아옴)
    public void PlayBossBGM(AudioClip bossBgm)
    {
        if (audioSource == null || bossBgm == null) return;
        audioSource.clip = bossBgm;
        audioSource.loop = true; // 무한 반복
        audioSource.Play();
    }

    // 3. 승리 브금 재생
    public void PlayClearBGM()
    {
        if (audioSource == null || bossClearBgm == null) return;
        audioSource.clip = bossClearBgm;
        audioSource.loop = false; // 클리어 브금은 보통 1번만 재생됨
        audioSource.Play();
    }

    // [BGMManager.cs 맨 아래에 함수 추가]
    public void PlayBadEndingBGM()
    {
        if (audioSource == null || badEndingBgm == null) return;
        audioSource.clip = badEndingBgm;
        audioSource.loop = false; // 크레딧 길이에 맞추거나 true로 반복
        audioSource.Play();
    }

    // [BGMManager.cs 기존 함수들 아래에 추가]
    public void PlayTransitionBGM(AudioClip transitionBgm)
    {
        if (audioSource == null || transitionBgm == null) return;
        audioSource.clip = transitionBgm;
        audioSource.loop = true;
        audioSource.Play();
    }

    // ==========================================
    // ★ [새로 추가됨] 원하는 브금과 반복 여부를 마음대로 틀어주는 만능 함수!
    // ==========================================
    public void PlayBGM(AudioClip clip, bool loop)
    {
        if (audioSource == null || clip == null) return;
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.Play();
    }
}