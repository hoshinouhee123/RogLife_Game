using UnityEngine;
using UnityEngine.Audio;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("오디오 설정")]
    public AudioSource audioSource;
    public AudioMixerGroup bgmMixerGroup; // BGM용 믹서 그룹 넣을 곳

    [Header("BGM 파일")]
    public AudioClip stageBgm;      // 평상시 던전 BGM
    public AudioClip bossClearBgm;  // 보스 잡았을 때 나오는 승리 BGM

    [Header("엔딩 BGM")]
    public AudioClip badEndingBgm;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // 내 몸에 오디오 소스가 없으면 자동으로 달아주기
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // 믹서 그룹 연결
        if (bgmMixerGroup != null) audioSource.outputAudioMixerGroup = bgmMixerGroup;

        // 게임 시작 시 일반 던전 브금 틀기
        PlayStageBGM();
    }

    // 1. 일반 브금 재생
    public void PlayStageBGM()
    {
        if (audioSource == null || stageBgm == null) return;
        audioSource.clip = stageBgm;
        audioSource.loop = true; // 무한 반복
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
}