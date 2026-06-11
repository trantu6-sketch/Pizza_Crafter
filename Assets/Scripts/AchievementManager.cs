using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum RewardType
{
    Gold,
    BoosterHammer,
    BoosterSwap,
    BoosterTrash,
    BoosterReroll
}

[System.Serializable]
public class AchievementConfig
{
    public string id;
    public string title;
    public int targetProgress;
    public RewardType rewardType;
    public int rewardAmount;
    public string rewardIconPath; // Ví dụ: "Icons/Gold" hoặc "Boosters/Icon_Hammer"
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject panelContainer; // Khung Popup bọc ngoài cùng của Achievement
    public GameObject achievementItemPrefab;
    public Transform scrollViewContent;
    public float popupDuration = 0.4f;

    [Header("Database")]
    public List<AchievementConfig> database = new List<AchievementConfig>();

    private List<GameObject> spawnedItems = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Khởi tạo Database cứng nếu chưa có (Đúng như 5 nhiệm vụ trong kế hoạch)
        if (database.Count == 0)
        {
            database.Add(new AchievementConfig { id = "Quest_Match_50_Plates", title = "Successfully match 50 plates", targetProgress = 50, rewardType = RewardType.Gold, rewardAmount = 500, rewardIconPath = "Icons/Gold" });
            database.Add(new AchievementConfig { id = "Quest_Unlock_5_Skins", title = "Unlock 5 new types of Plates", targetProgress = 5, rewardType = RewardType.BoosterHammer, rewardAmount = 1, rewardIconPath = "Boosters/Icon_Hammer" });
            database.Add(new AchievementConfig { id = "Quest_Reach_Level_10", title = "Reach Level 10", targetProgress = 10, rewardType = RewardType.Gold, rewardAmount = 1000, rewardIconPath = "Icons/Gold" });
            database.Add(new AchievementConfig { id = "Quest_Collect_2000_Gold", title = "Collect a total of 2,000 Coins", targetProgress = 2000, rewardType = RewardType.BoosterSwap, rewardAmount = 1, rewardIconPath = "Boosters/Icon_Swap" });
            database.Add(new AchievementConfig { id = "Quest_Login_7_Days", title = "Log in for 7 consecutive days", targetProgress = 7, rewardType = RewardType.Gold, rewardAmount = 500, rewardIconPath = "Icons/Gold" });
        }
    }

    void Start()
    {
        // Trì hoãn 1 frame để đảm bảo DataManager đã load xong
        Invoke(nameof(InitializeQuests), 0.1f);
    }

    private void InitializeQuests()
    {
        if (DataManager.Instance == null) return;

        bool isDataChanged = false;

        // Đảm bảo mọi AchievementConfig đều có 1 QuestData tương ứng trong PlayerData
        foreach (var config in database)
        {
            QuestData existingQuest = DataManager.Instance.playerData.QuestsProgress.Find(q => q.questId == config.id);
            if (existingQuest == null)
            {
                // Thêm mới nếu chưa có
                DataManager.Instance.playerData.QuestsProgress.Add(new QuestData(config.id, config.targetProgress));
                isDataChanged = true;
            }
            else
            {
                // Đồng bộ config (UI) với level hiện tại của QuestData (đảm bảo không mất dữ liệu nâng cấp khi restart game)
                if (existingQuest.level <= 0) existingQuest.level = 1;
                
                // Khôi phục lại chỉ số của config theo cấp độ hiện tại
                // Lấy bản chuẩn trước (vì config hiện tại đang chứa Base Value)
                int baseTarget = config.targetProgress;
                int baseReward = config.rewardAmount;
                
                for (int i = 1; i < existingQuest.level; i++)
                {
                    baseTarget = (int)(baseTarget * 1.5f);
                    if (config.rewardType == RewardType.Gold) baseReward += 200;
                    else baseReward += 1;
                }
                
                config.targetProgress = baseTarget;
                config.rewardAmount = baseReward;

                // Cập nhật lại target lỡ như có thay đổi
                if (existingQuest.targetProgress != config.targetProgress)
                {
                    existingQuest.targetProgress = config.targetProgress;
                    isDataChanged = true;
                }
            }
        }

        if (isDataChanged)
        {
            DataManager.Instance.SaveData();
        }
    }

    /// <summary>
    /// Gọi hàm này khi mở Tab/Popup Achievement để sinh ra danh sách UI
    /// </summary>
    public void OpenPanel()
    {
        if (panelContainer != null)
        {
            panelContainer.SetActive(true);
            panelContainer.transform.localScale = Vector3.zero;
            panelContainer.transform.DOScale(Vector3.one, popupDuration).SetEase(Ease.OutBack);
        }
        
        RenderAchievements();
    }

    public void ClosePanel()
    {
        if (panelContainer != null)
        {
            panelContainer.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                panelContainer.SetActive(false);
            });
        }
    }

    public void RenderAchievements()
    {
        if (DataManager.Instance == null || achievementItemPrefab == null || scrollViewContent == null) return;

        // Dọn dẹp UI cũ
        foreach (var item in spawnedItems)
        {
            Destroy(item);
        }
        spawnedItems.Clear();

        // Ưu tiên hiển thị: Chưa nhận thưởng lên trước, Đã nhận thưởng đẩy xuống dưới
        List<AchievementConfig> sortedList = new List<AchievementConfig>(database);
        sortedList.Sort((a, b) => {
            QuestData qA = DataManager.Instance.playerData.QuestsProgress.Find(q => q.questId == a.id);
            QuestData qB = DataManager.Instance.playerData.QuestsProgress.Find(q => q.questId == b.id);
            
            bool claimA = qA != null ? qA.isClaimed : false;
            bool claimB = qB != null ? qB.isClaimed : false;

            if (claimA && !claimB) return 1; // A đưa xuống dưới
            if (!claimA && claimB) return -1; // B đưa xuống dưới
            return 0; // Giữ nguyên
        });

        // Sinh UI mới với hiệu ứng DOTween xuất hiện tuần tự
        float delay = 0f;
        foreach (var config in sortedList)
        {
            QuestData quest = DataManager.Instance.playerData.QuestsProgress.Find(q => q.questId == config.id);
            if (quest != null)
            {
                GameObject obj = Instantiate(achievementItemPrefab, scrollViewContent);
                AchievementItemUI uiScript = obj.GetComponent<AchievementItemUI>();
                if (uiScript != null)
                {
                    uiScript.Setup(quest, config);
                }
                spawnedItems.Add(obj);

                // Hiệu ứng DOTween: Scale từ 0 lên 1 và trượt nhẹ từ phải sang
                obj.transform.localScale = Vector3.zero;
                
                // Lưu vị trí ban đầu (nếu Content layout tự động xếp thì ta dịch nó ra một chút rồi Tween về 0)
                // Tuy nhiên cách an toàn nhất với Layout Group là dùng PunchScale hoặc Scale
                obj.transform.DOScale(Vector3.one, 0.4f).SetEase(DG.Tweening.Ease.OutBack).SetDelay(delay);
                
                delay += 0.1f; // Độ trễ giữa mỗi thẻ là 0.1 giây
            }
        }
    }

    public void ClaimReward(string questId)
    {
        if (DataManager.Instance == null) return;

        QuestData quest = DataManager.Instance.playerData.QuestsProgress.Find(q => q.questId == questId);
        AchievementConfig config = database.Find(c => c.id == questId);

        if (quest != null && config != null && quest.isCompleted && !quest.isClaimed)
        {
            // 1. Trao thưởng cho người chơi
            switch (config.rewardType)
            {
                case RewardType.Gold:
                    DataManager.Instance.AddGold(config.rewardAmount);
                    break;
                case RewardType.BoosterHammer:
                    DataManager.Instance.playerData.hammerCount += config.rewardAmount;
                    break;
                case RewardType.BoosterSwap:
                    DataManager.Instance.playerData.swapCount += config.rewardAmount;
                    break;
                case RewardType.BoosterTrash:
                    DataManager.Instance.playerData.trashCount += config.rewardAmount;
                    break;
                case RewardType.BoosterReroll:
                    DataManager.Instance.playerData.rerollCount += config.rewardAmount;
                    break;
            }

            Debug.Log($"[Achievement] Đã nhận phần thưởng {config.rewardAmount} {config.rewardType} từ nhiệm vụ {config.title} (Level {quest.level})");

            // 2. Nâng cấp nhiệm vụ lên cấp độ khó hơn (Endless Loop)
            quest.level++;
            config.targetProgress = (int)(config.targetProgress * 1.5f);
            if (config.rewardType == RewardType.Gold) config.rewardAmount += 200;
            else config.rewardAmount += 1;

            // 3. Reset tiến trình về 0 để làm lại
            quest.targetProgress = config.targetProgress;
            quest.currentProgress = 0;
            quest.isCompleted = false;
            quest.isClaimed = false; // Luôn false để có thể tiếp tục claim ở level mới

            // 4. Lưu và vẽ lại UI ngay lập tức
            DataManager.Instance.SaveData();
            RenderAchievements();
        }
    }
}
