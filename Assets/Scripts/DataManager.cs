using UnityEngine;
using System.IO;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public PlayerData playerData;

    private string saveFilePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Đường dẫn an toàn trên mọi thiết bị: Windows, Android, iOS...
            saveFilePath = Path.Combine(Application.persistentDataPath, "player_data.json");
            
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveData()
    {
        string json = JsonUtility.ToJson(playerData, true); // true để format JSON dễ nhìn
        File.WriteAllText(saveFilePath, json);
        Debug.Log("[DataManager] Đã lưu dữ liệu người chơi thành công tại: " + saveFilePath);
    }

    public void LoadData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            playerData = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("[DataManager] Đã tải dữ liệu người chơi thành công.");
        }
        else
        {
            // Nếu file chưa tồn tại (lần đầu chơi), khởi tạo dữ liệu mới
            playerData = new PlayerData();
            SaveData();
        }
    }

    // ================= CÁC HÀM TIỆN ÍCH =================

    public void AddGold(int amount)
    {
        playerData.Gold += amount;
        SaveData();
    }

    public bool BuySkin(string skinId, int price)
    {
        if (playerData.PurchasedSkins.Contains(skinId))
        {
            Debug.Log("[DataManager] Bạn đã sở hữu skin này rồi.");
            return false;
        }

        if (playerData.Gold >= price)
        {
            playerData.Gold -= price;
            playerData.PurchasedSkins.Add(skinId);
            SaveData();
            Debug.Log($"[DataManager] Mua thành công {skinId}. Còn lại {playerData.Gold} Vàng.");
            return true;
        }
        else
        {
            Debug.Log("[DataManager] Không đủ Vàng để mua skin!");
            return false;
        }
    }

    public void UpdateQuestProgress(string questId, int amountAdded)
    {
        QuestData quest = playerData.QuestsProgress.Find(q => q.questId == questId);
        if (quest != null && !quest.isCompleted)
        {
            quest.currentProgress += amountAdded;
            if (quest.currentProgress >= quest.targetProgress)
            {
                quest.currentProgress = quest.targetProgress;
                quest.isCompleted = true;
                Debug.Log($"[DataManager] HOÀN THÀNH NHIỆM VỤ: {questId}!");
            }
            SaveData();
        }
    }
}
