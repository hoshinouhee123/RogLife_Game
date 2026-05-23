using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PettingVolumeController2D : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("캐릭터 스프라이트 설정")]
    public SpriteRenderer characterSprite;
    public Sprite normalFace;
    public Sprite happyFace;

    [Header("UI 텍스트 연결")]
    public TextMeshProUGUI bgmText;
    public TextMeshProUGUI sfxText;

    [Header("선택 글로우 효과 연결")]
    public GameObject bgmGlowObject; // BGM 버튼의 노란 불빛 오브젝트
    public GameObject sfxGlowObject; // 효과음 버튼의 노란 불빛 오브젝트

    [Header("볼륨 설정")]
    public float maxVolume = 100f;
    public float sensitivity = 10f;

    private float currentBGM;
    private float currentSFX;

    public enum AudioType { None, BGM, SFX }
    private AudioType selectedAudio = AudioType.None;

    void Start()
    {
        currentBGM = PlayerPrefs.GetFloat("BGM_Volume", 50f);
        currentSFX = PlayerPrefs.GetFloat("SFX_Volume", 50f);
        UpdateUIText();

        // 시작할 때는 두 불빛 모두 꺼두기
        if (bgmGlowObject != null) bgmGlowObject.SetActive(false);
        if (sfxGlowObject != null) sfxGlowObject.SetActive(false);
    }

    public void SelectBGM()
    {
        selectedAudio = AudioType.BGM;
        Debug.Log("BGM 모드 선택됨");

        // BGM 불빛은 켜고, SFX 불빛은 끄기
        if (bgmGlowObject != null) bgmGlowObject.SetActive(true);
        if (sfxGlowObject != null) sfxGlowObject.SetActive(false);
    }

    public void SelectSFX()
    {
        selectedAudio = AudioType.SFX;
        Debug.Log("효과음 모드 선택됨");

        // SFX 불빛은 켜고, BGM 불빛은 끄기
        if (sfxGlowObject != null) sfxGlowObject.SetActive(true);
        if (bgmGlowObject != null) bgmGlowObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (selectedAudio == AudioType.None) return;

        if (selectedAudio == AudioType.BGM && currentBGM >= maxVolume)
        {
            currentBGM = 0f;
        }
        else if (selectedAudio == AudioType.SFX && currentSFX >= maxVolume)
        {
            currentSFX = 0f;
        }

        characterSprite.sprite = happyFace;
        UpdateUIText();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (selectedAudio == AudioType.None) return;

        float increaseAmount = eventData.delta.magnitude * sensitivity * Time.deltaTime;

        if (selectedAudio == AudioType.BGM)
        {
            currentBGM += increaseAmount;
            if (currentBGM > maxVolume) currentBGM = maxVolume;
        }
        else if (selectedAudio == AudioType.SFX)
        {
            currentSFX += increaseAmount;
            if (currentSFX > maxVolume) currentSFX = maxVolume;
        }

        UpdateUIText();
    }

    // 쓰다듬기가 끝나고 손을 뗄 때
    public void OnPointerUp(PointerEventData eventData)
    {
        if (selectedAudio == AudioType.None) return; // 선택 안 했었으면 무시

        characterSprite.sprite = normalFace; // 표정 복구

        // 데이터 저장
        PlayerPrefs.SetFloat("BGM_Volume", currentBGM);
        PlayerPrefs.SetFloat("SFX_Volume", currentSFX);
        PlayerPrefs.Save();

        // 모든 노란 불빛 끄기 & 선택 모드 초기화
        if (bgmGlowObject != null) bgmGlowObject.SetActive(false);
        if (sfxGlowObject != null) sfxGlowObject.SetActive(false);

        selectedAudio = AudioType.None; // 모드를 None으로 바꿔서 다시 버튼을 누르게 만듦

        Debug.Log("저장 완료 및 모드 초기화됨");
    }

    private void UpdateUIText()
    {
        bgmText.text = $"BGM: {Mathf.RoundToInt(currentBGM)}";
        sfxText.text = $"SFX: {Mathf.RoundToInt(currentSFX)}";
    }
}