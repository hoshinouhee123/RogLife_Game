using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class BGMTrack
{
    public string bgmName;
    public AudioClip audioClip;
}

public class SettingsBGMManager : MonoBehaviour
{
    [Header("BGM 트랙 목록 (0번은 기본 브금)")]
    public BGMTrack[] bgmTracks;

    // ==========================================
    // ★ [새로 추가됨] 메인 메뉴에서 브금을 틀고 있는 빈 오브젝트의 스피커!
    // ==========================================
    [Header("메인 메뉴 BGM 재생기")]
    public AudioSource mainMenuAudioSource;

    [Header("UI 연결")]
    public TextMeshProUGUI currentBgmText;
    public GameObject dropdownListPanel;
    public Transform contentParent;
    public GameObject bgmSlotPrefab;

    private void Start()
    {
        dropdownListPanel.SetActive(false);

        if (PlayerDataManager.Instance != null)
        {
            int savedIndex = PlayerDataManager.Instance.saveData.selectedBgmIndex;
            ApplyBGM(savedIndex); // 시작하자마자 저장된 브금 틀어주기
        }
    }

    public void ToggleDropdown()
    {
        bool isOpen = !dropdownListPanel.activeSelf;
        dropdownListPanel.SetActive(isOpen);
        if (isOpen) RefreshList();
    }

    private void RefreshList()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        if (PlayerDataManager.Instance == null) return;
        PlayerSaveData data = PlayerDataManager.Instance.saveData;

        for (int i = 0; i < bgmTracks.Length; i++)
        {
            GameObject slotObj = Instantiate(bgmSlotPrefab, contentParent);
            BGMSlotUI slotUI = slotObj.GetComponent<BGMSlotUI>();
            bool isUnlocked = (i == 0) || data.unlockedBgmList.Contains(i);
            slotUI.Setup(i, bgmTracks[i].bgmName, isUnlocked, this);
        }
    }

    public void SelectBGM(int index)
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.saveData.selectedBgmIndex = index;
            PlayerDataManager.Instance.SaveData();

            ApplyBGM(index);
            dropdownListPanel.SetActive(false);
        }
    }

    // ==========================================
    // ★ [수정됨] 직접 연결한 AudioSource의 음악을 갈아 끼웁니다!
    // ==========================================
    private void ApplyBGM(int index)
    {
        if (index >= 0 && index < bgmTracks.Length)
        {
            currentBgmText.text = bgmTracks[index].bgmName;

            if (mainMenuAudioSource != null)
            {
                // 이미 같은 곡이 재생 중이면 뚝 끊기지 않게 무시함
                if (mainMenuAudioSource.clip == bgmTracks[index].audioClip && mainMenuAudioSource.isPlaying) return;

                // 새로운 곡으로 갈아 끼우고 무한 반복 재생!
                mainMenuAudioSource.clip = bgmTracks[index].audioClip;
                mainMenuAudioSource.loop = true;
                mainMenuAudioSource.Play();
            }
        }
    }
}