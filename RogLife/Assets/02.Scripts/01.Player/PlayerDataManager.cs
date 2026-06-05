using UnityEngine;
using System.IO;

// 영구적으로 보존될 플레이어의 데이터
[System.Serializable]
public class PlayerSaveData
{
    public int totalCoins = 0;         // 누적 코인

    // 스탯 업그레이드 레벨
    public int hpLevel = 0;            // 체력 강화 레벨
    public int dmgLevel = 0;           // 공격력 강화 레벨
    public int spdLevel = 0;           // 스피드 강화 레벨

    // 시작 특전 업그레이드 레벨
    public int startItemLevel = 0;     // 시작 시 주어지는 랜덤 아이템 개수
    public int startCoinLevel = 0;     // 시작 코인 개수
    public int startKeyLevel = 0;      // 시작 열쇠 개수
}

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public PlayerSaveData saveData;
    private string savePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); return; }

        savePath = Application.persistentDataPath + "/playerData.json";
        LoadData();
    }

    public void AddCoins(int amount)
    {
        saveData.totalCoins += amount;
        SaveData();
    }

    public bool SpendCoins(int amount)
    {
        if (saveData.totalCoins >= amount)
        {
            saveData.totalCoins -= amount;
            SaveData();
            return true;
        }
        return false;
    }

    public void SaveData()
    {
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
    }

    public void LoadData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            saveData = JsonUtility.FromJson<PlayerSaveData>(json);
        }
        else
        {
            saveData = new PlayerSaveData();
        }
    }
}