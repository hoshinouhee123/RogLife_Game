using UnityEngine;
using UnityEngine.Audio; // 오디오 믹서를 사용하기 위해 필요!

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; // 어디서든 부를 수 있게 싱글톤

    public AudioMixer mainMixer; // 방금 만든 오디오 믹서 연결

    [Header("UI 사운드 설정")]
    public AudioSource uiAudioSource; // UI 전용 오디오 소스 (버튼 소리 등)
    public AudioClip hoverSound;      // 마우스 올릴 때 소리
    public AudioClip clickSound;      // 클릭할 때 소리

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임 시작 시 PlayerPrefs에 저장된 값을 불러와서 믹서에 적용
        float savedBGM = PlayerPrefs.GetFloat("BGM_Volume", 50f);
        float savedSFX = PlayerPrefs.GetFloat("SFX_Volume", 50f);

        SetBGMVolume(savedBGM);
        SetSFXVolume(savedSFX);
    }

    // 0~100의 값을 유니티 데시벨(-80 ~ 0)로 변환해서 적용하는 함수
    public void SetBGMVolume(float value)
    {
        // 값이 0이면 완전한 묵음(-80dB), 아니면 로그 함수를 이용해 자연스러운 볼륨 곡선 생성
        float db = value <= 0.01f ? -80f : Mathf.Log10(value / 100f) * 20f;
        mainMixer.SetFloat("BGM_Vol", db); // 오디오 믹서에 적용!
    }

    public void SetSFXVolume(float value)
    {
        float db = value <= 0.01f ? -80f : Mathf.Log10(value / 100f) * 20f;
        mainMixer.SetFloat("SFX_Vol", db);
    }

    // UI 소리 재생 함수
    // PlayOneShot을 사용하면 소리가 겹쳐도 끊기지 않고 자연스럽게 남.
    public void PlayHoverSound()
    {
        if (hoverSound != null && uiAudioSource != null)
            uiAudioSource.PlayOneShot(hoverSound);
    }

    public void PlayClickSound()
    {
        if (clickSound != null && uiAudioSource != null)
            uiAudioSource.PlayOneShot(clickSound);
    }
}