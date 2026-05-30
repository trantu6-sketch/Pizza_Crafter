using System.Collections.Generic;

[System.Serializable]
public class QuestData
{
    public string questId;
    public int currentProgress;
    public int targetProgress;
    public bool isCompleted;

    public QuestData(string id, int target)
    {
        this.questId = id;
        this.currentProgress = 0;
        this.targetProgress = target;
        this.isCompleted = false;
    }
}

[System.Serializable]
public class PlayerData
{
    // Tài nguyên người chơi
    public int Gold = 0;
    
    // Cửa hàng / Trang bị
    public List<string> PurchasedSkins = new List<string>();
    
    // Hệ thống Nhiệm vụ
    public List<QuestData> QuestsProgress = new List<QuestData>();

    // Các thông số hiện tại (di dời từ PlayerPrefs sang)
    public int Level = 1;
    public int Exp = 0;
    public int BestScore = 0;
    
    // Lưu trữ Skin đang được trang bị
    public string EquippedPlateSkin = "Plate_Default";

    public PlayerData()
    {
        // Khởi tạo một số skin cơ bản hoặc nhiệm vụ tân thủ nếu cần
        PurchasedSkins.Add("Skin_Default");
        
        QuestsProgress.Add(new QuestData("Quest_Play_1_Game", 1));
        QuestsProgress.Add(new QuestData("Quest_Earn_100_Gold", 100));
    }
}
