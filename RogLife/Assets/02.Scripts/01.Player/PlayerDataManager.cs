using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class PlayerSaveData
{
    public int totalCoins = 0;

    public int hpLevel = 0;
    public int dmgLevel = 0;
    public int spdLevel = 0;

    public int startItemLevel = 0;
    public int startCoinLevel = 0;
    public int startKeyLevel = 0;

    public List<int> unlockedBgmList = new List<int>();

    public int selectedBgmIndex = 0;
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