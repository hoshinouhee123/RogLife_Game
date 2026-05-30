using UnityEngine;
using System.Collections.Generic;
using System.IO;

// ★ [추가됨] 인스펙터에서 설정할 업적의 상세 정보 (이름, 아이콘 등)
[System.Serializable]
public class AchievementInfo
{
    public string id;           // 예: "BadEnding1"
    public string title;        // 예: "끝나지 않은 악몽"
    public string description;  // 예: "체력을 모두 잃고 쓰러졌습니다."
    public Sprite icon;         // 업적 아이콘
}

[System.Serializable]
public class Achievement
{
    public string id;
    public bool isUnlocked;
}

[System.Serializable]
public class AchievementSaveData
{
    public List<Achievement> achievements = new List<Achievement>();
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    // ★ [추가됨] 게임에 존재하는 모든 업적 리스트 (유니티 에디터에서 설정)
    [Header("게임의 모든 도전과제 목록")]
    public List<AchievementInfo> achievementDatabase = new List<AchievementInfo>();

    private string savePath;
    private AchievementSaveData saveData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        savePath = Application.persistentDataPath + "/achievements.json";
        LoadAchievements();
    }

    public void UnlockAchievement(string achievementId)
    {
        Achievement ach = saveData.achievements.Find(a => a.id == achievementId);

        if (ach != null)
        {
            if (ach.isUnlocked) return; // 이미 깬 업적이면 무시
            ach.isUnlocked = true;
        }
        else
        {
            saveData.achievements.Add(new Achievement { id = achievementId, isUnlocked = true });
        }

        SaveAchievements();
        Debug.Log("도전과제 해금됨: " + achievementId);

        // ★ [추가됨] 데이터베이스에서 해당 업적의 상세 정보를 찾아 팝업 매니저로 전달!
        AchievementInfo info = achievementDatabase.Find(a => a.id == achievementId);
        if (info != null && AchievementPopupUI.Instance != null)
        {
            AchievementPopupUI.Instance.EnqueuePopup(info);
        }
    }

    private void SaveAchievements()
    {
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
    }

    private void LoadAchievements()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            saveData = JsonUtility.FromJson<AchievementSaveData>(json);
        }
        else
        {
            saveData = new AchievementSaveData();
        }
    }

    // [추가됨] 메인 메뉴에서 업적이 깨졌는지 확인할 때 쓰는 함수
    public bool IsUnlocked(string achievementId)
    {
        Achievement ach = saveData.achievements.Find(a => a.id == achievementId);
        return ach != null && ach.isUnlocked;
    }
}