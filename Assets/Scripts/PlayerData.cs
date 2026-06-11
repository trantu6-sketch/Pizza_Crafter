using System.Collections.Generic;

[System.Serializable]
public class QuestData
{
    public string questId;
    public int currentProgress;
    public int targetProgress;
    public bool isCompleted;
    public bool isClaimed;
    public int level;

    public QuestData(string id, int target)
    {
        this.questId = id;
        this.currentProgress = 0;
        this.targetProgress = target;
        this.isCompleted = false;
        this.isClaimed = false;
        this.level = 1;
    }
}

[System.Serializable]
public class SliceData
{
    public PizzaColor color;

    public SliceData(PizzaColor color)
    {
        this.color = color;
    }
}

[System.Serializable]
public class PlateData
{
    public List<SliceData> slices = new List<SliceData>();
}

[System.Serializable]
public class GridCellData
{
    public int row;
    public int col;
    public PlateData plate;
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

    // Các thông số hiện tại
    public int Level = 1;
    public int Exp = 0;
    public int BestScore = 0;
    
    // --- DAILY REWARD SYSTEM ---
    public string lastLoginDate = ""; // (Cũ) Local time
    public int consecutiveLoginDays = 0;
    public string lastDailyRewardClaimDateUTC = ""; // Lưu ngày điểm danh cuối cùng theo giờ UTC
    public int currentDailyRewardDay = 0; // Số ngày đã điểm danh liên tiếp (0 đến 6)
    
    // --- TÚI ĐỒ BOOSTER ---
    public int hammerCount = 0;
    public int swapCount = 0;
    public int trashCount = 0;
    public int rerollCount = 0;
    
    // Lưu trữ Skin đang được trang bị
    public string EquippedPlateSkin = "Plate_Default";

    // --- GAME STATE (BOARD SAVE/LOAD) ---
    public int currentSessionScore = 0;
    public List<GridCellData> savedGrid = new List<GridCellData>();
    public List<PlateData> savedLobby = new List<PlateData>();
    public bool hasSavedGame = false;

    public PlayerData()
    {
        // Khởi tạo một số skin cơ bản hoặc nhiệm vụ tân thủ nếu cần
        PurchasedSkins.Add("Skin_Default");
        
        QuestsProgress.Add(new QuestData("Quest_Play_1_Game", 1));
        QuestsProgress.Add(new QuestData("Quest_Earn_100_Gold", 100));
    }
}
