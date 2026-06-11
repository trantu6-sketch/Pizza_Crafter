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
#if UNITY_WEBGL && !UNITY_EDITOR
        PlayerPrefs.SetString("PlayerData", json);
        PlayerPrefs.Save();
        Debug.Log("[DataManager] Đã lưu dữ liệu người chơi thành công vào PlayerPrefs (WebGL).");
#else
        File.WriteAllText(saveFilePath, json);
        Debug.Log("[DataManager] Đã lưu dữ liệu người chơi thành công tại: " + saveFilePath);
#endif
    }

    public void LoadData()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (PlayerPrefs.HasKey("PlayerData"))
        {
            string json = PlayerPrefs.GetString("PlayerData");
            playerData = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("[DataManager] Đã tải dữ liệu người chơi thành công từ PlayerPrefs (WebGL).");
        }
        else
        {
            playerData = new PlayerData();
            SaveData();
        }
#else
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
#endif
        
        CheckLoginStreak();
    }

    private void CheckLoginStreak()
    {
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        
        if (string.IsNullOrEmpty(playerData.lastLoginDate))
        {
            // Lần đăng nhập đầu tiên
            playerData.lastLoginDate = today;
            playerData.consecutiveLoginDays = 1;
            SetQuestProgress("Quest_Login_7_Days", 1);
            SaveData();
        }
        else if (playerData.lastLoginDate != today)
        {
            System.DateTime lastDate;
            if (System.DateTime.TryParse(playerData.lastLoginDate, out lastDate))
            {
                System.TimeSpan diff = System.DateTime.Now.Date - lastDate.Date;
                if (diff.Days == 1)
                {
                    // Ngày tiếp theo
                    playerData.consecutiveLoginDays++;
                    UpdateQuestProgress("Quest_Login_7_Days", 1);
                }
                else if (diff.Days > 1)
                {
                    // Đã bỏ lỡ 1 ngày, reset chuỗi
                    playerData.consecutiveLoginDays = 1;
                    SetQuestProgress("Quest_Login_7_Days", 1); // Reset lại nhiệm vụ về 1
                }
            }
            playerData.lastLoginDate = today;
            SaveData();
        }
    }

    // ================= CÁC HÀM TIỆN ÍCH =================

    public void AddGold(int amount)
    {
        playerData.Gold += amount;
        UpdateQuestProgress("Quest_Collect_2000_Gold", amount);
        SaveData();
        
        GameEventManager.Instance?.TriggerGoldChanged(playerData.Gold);
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
            UpdateQuestProgress("Quest_Unlock_5_Skins", 1);
            SaveData();
            
            GameEventManager.Instance?.TriggerGoldChanged(playerData.Gold);
            GameEventManager.Instance?.TriggerSkinPurchased(skinId);
            
            Debug.Log($"[DataManager] Mua thành công {skinId}. Còn lại {playerData.Gold} Vàng.");
            return true;
        }
        else
        {
            Debug.Log("[DataManager] Không đủ Vàng để mua skin!");
            return false;
        }
    }

    public void EquipSkin(string skinId)
    {
        bool isFree = (skinId == "Plate_Default" || skinId == "SinglePlate2");
        if (ShopManager.Instance != null)
        {
            SkinData skin = ShopManager.Instance.GetSkinData(skinId);
            if (skin != null && skin.price == 0) isFree = true;
        }

        if (playerData.PurchasedSkins.Contains(skinId) || isFree)
        {
            playerData.EquippedPlateSkin = skinId;
            SaveData();
            GameEventManager.Instance?.TriggerSkinEquipped(skinId);
            Debug.Log($"[DataManager] Đã trang bị skin: {skinId}");
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
            
            GameEventManager.Instance?.TriggerQuestProgressUpdated(quest.questId, quest.currentProgress, quest.targetProgress, quest.isCompleted);
        }
    }

    public void SetQuestProgress(string questId, int absoluteProgress)
    {
        QuestData quest = playerData.QuestsProgress.Find(q => q.questId == questId);
        if (quest != null && !quest.isCompleted)
        {
            quest.currentProgress = absoluteProgress;
            if (quest.currentProgress >= quest.targetProgress)
            {
                quest.currentProgress = quest.targetProgress;
                quest.isCompleted = true;
                Debug.Log($"[DataManager] HOÀN THÀNH NHIỆM VỤ: {questId}!");
            }
            SaveData();
            
            GameEventManager.Instance?.TriggerQuestProgressUpdated(quest.questId, quest.currentProgress, quest.targetProgress, quest.isCompleted);
        }
    }
}
