using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DailyRewardConfig
{
    public int dayIndex; // 0 đến 6 (tương ứng Day 1 đến Day 7)
    public RewardType rewardType;
    public int rewardAmount;
    
    // Hỗ trợ trường hợp phần thưởng là một combo nhiều món (VD: Day 6, Day 7)
    public bool isComboReward = false;
    public int comboGoldAmount = 0;
    public int comboBoosterAmount = 0;
}

public class DailyRewardManager : MonoBehaviour
{
    public static DailyRewardManager Instance { get; private set; }

    [Header("Cấu hình Quà tặng 7 Ngày")]
    public List<DailyRewardConfig> rewardsDatabase = new List<DailyRewardConfig>();

    public enum RewardState
    {
        Claimed,   // Đã nhận
        Available, // Có thể nhận ngay bây giờ
        Locked     // Chưa đến ngày nhận
    }

    // Sự kiện khi load xong trạng thái điểm danh
    public Action OnRewardDataLoaded;

    // Biến lưu trạng thái thời gian thực
    private bool isCheckingTime = false;
    private DateTime currentUTCTime;
    private bool hasValidNetworkTime = false;
    private string timeErrorMessage = "";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeDatabase();
    }

    void Start()
    {
        // Khi bắt đầu game, tự động kiểm tra giờ (Mở sẵn luồng)
        CheckDailyStatus();
    }

    private void InitializeDatabase()
    {
        if (rewardsDatabase.Count == 0)
        {
            rewardsDatabase.Add(new DailyRewardConfig { dayIndex = 0, rewardType = RewardType.Gold, rewardAmount = 50 });
            rewardsDatabase.Add(new DailyRewardConfig { dayIndex = 1, rewardType = RewardType.Gold, rewardAmount = 100 });
            rewardsDatabase.Add(new DailyRewardConfig { dayIndex = 2, rewardType = RewardType.Gold, rewardAmount = 150 });
            rewardsDatabase.Add(new DailyRewardConfig { dayIndex = 3, rewardType = RewardType.BoosterTrash, rewardAmount = 1 });
            rewardsDatabase.Add(new DailyRewardConfig { dayIndex = 4, rewardType = RewardType.Gold, rewardAmount = 200 });
            
            // Ngày 6: Tặng 1 bộ Booster (mỗi loại 1 cái)
            rewardsDatabase.Add(new DailyRewardConfig { dayIndex = 5, isComboReward = true, comboGoldAmount = 0, comboBoosterAmount = 1 });
            
            // Ngày 7: Tặng phần thưởng lớn (500 vàng + mỗi loại Booster 2 cái)
            rewardsDatabase.Add(new DailyRewardConfig { dayIndex = 6, isComboReward = true, comboGoldAmount = 500, comboBoosterAmount = 2 });
        }
    }

    /// <summary>
    /// Gửi request lên Internet để kiểm tra trạng thái ngày hôm nay.
    /// </summary>
    public void CheckDailyStatus()
    {
        if (isCheckingTime) return;
        isCheckingTime = true;
        hasValidNetworkTime = false;
        
        if (NetworkTimeManager.Instance != null)
        {
            NetworkTimeManager.Instance.GetUTCTime(
                onSuccess: (utcNow) => {
                    currentUTCTime = utcNow;
                    hasValidNetworkTime = true;
                    isCheckingTime = false;
                    
                    // Cập nhật lại chuỗi ngày (Nếu quá hạn 2 ngày thì reset)
                    ValidateLoginStreak();
                    
                    OnRewardDataLoaded?.Invoke(); // Báo cho UI biết để cập nhật
                },
                onError: (errorMsg) => {
                    hasValidNetworkTime = false;
                    timeErrorMessage = errorMsg;
                    isCheckingTime = false;
                    OnRewardDataLoaded?.Invoke();
                }
            );
        }
        else
        {
            Debug.LogError("[DailyRewardManager] Thiếu NetworkTimeManager trong Scene!");
            hasValidNetworkTime = false;
            timeErrorMessage = "Lỗi hệ thống: Thiếu bộ đếm thời gian.";
            isCheckingTime = false;
            OnRewardDataLoaded?.Invoke();
        }
    }

    private void ValidateLoginStreak()
    {
        if (DataManager.Instance == null) return;
        
        string todayStr = currentUTCTime.ToString("yyyy-MM-dd");
        string lastClaimStr = DataManager.Instance.playerData.lastDailyRewardClaimDateUTC;

        if (!string.IsNullOrEmpty(lastClaimStr))
        {
            DateTime lastClaimDate;
            if (DateTime.TryParse(lastClaimStr, out lastClaimDate))
            {
                TimeSpan diff = currentUTCTime.Date - lastClaimDate.Date;
                
                // Nếu khoảng cách >= 2 ngày (tức là đã bỏ lỡ 1 ngày) -> Reset chuỗi về 0
                if (diff.Days >= 2)
                {
                    Debug.Log("[DailyReward] Người chơi đã bỏ lỡ 1 ngày điểm danh. Reset chuỗi về Day 1.");
                    DataManager.Instance.playerData.currentDailyRewardDay = 0;
                    // Xóa ngày lưu để trạng thái của ngày 0 trở thành Available
                    DataManager.Instance.playerData.lastDailyRewardClaimDateUTC = ""; 
                    DataManager.Instance.SaveData();
                }
                // Nếu chuỗi đang ở ngày 7 (index 6) và đã nhận rồi, và giờ là ngày mới -> Reset vòng lặp về Day 1
                else if (diff.Days == 1 && DataManager.Instance.playerData.currentDailyRewardDay > 6)
                {
                     Debug.Log("[DailyReward] Đã hoàn thành chu kỳ 7 ngày. Reset vòng lặp mới.");
                     DataManager.Instance.playerData.currentDailyRewardDay = 0;
                     DataManager.Instance.playerData.lastDailyRewardClaimDateUTC = ""; 
                     DataManager.Instance.SaveData();
                }
            }
        }
    }

    /// <summary>
    /// Trả về trạng thái của một ngày cụ thể. (0 đến 6)
    /// </summary>
    public RewardState GetStateForDay(int dayIndex)
    {
        if (DataManager.Instance == null || !hasValidNetworkTime) return RewardState.Locked;

        int currentDay = DataManager.Instance.playerData.currentDailyRewardDay;
        string lastClaimStr = DataManager.Instance.playerData.lastDailyRewardClaimDateUTC;

        // Các ngày trước ngày hiện tại chắc chắn là đã nhận
        if (dayIndex < currentDay)
        {
            return RewardState.Claimed;
        }
        
        // Nếu là ngày hiện tại của chuỗi
        if (dayIndex == currentDay)
        {
            // Kiểm tra xem hôm nay đã nhận chưa
            string todayStr = currentUTCTime.ToString("yyyy-MM-dd");
            if (lastClaimStr == todayStr)
            {
                return RewardState.Claimed; // Đã nhận hôm nay rồi
            }
            else
            {
                return RewardState.Available; // Hôm nay chưa nhận
            }
        }

        // Các ngày tương lai
        return RewardState.Locked;
    }

    public void TryClaimReward()
    {
        if (DataManager.Instance == null) return;
        
        if (!hasValidNetworkTime)
        {
            Debug.LogWarning("[DailyReward] Không thể nhận thưởng: " + timeErrorMessage);
            return;
        }

        int currentDay = DataManager.Instance.playerData.currentDailyRewardDay;
        if (currentDay > 6) return; // Đã quá 7 ngày mà chưa reset (Phòng hờ lỗi)

        string todayStr = currentUTCTime.ToString("yyyy-MM-dd");
        if (DataManager.Instance.playerData.lastDailyRewardClaimDateUTC == todayStr)
        {
            Debug.LogWarning("[DailyReward] Đã nhận điểm danh hôm nay rồi!");
            return; // Đã nhận hôm nay
        }

        // --- TRAO THƯỞNG ---
        DailyRewardConfig config = rewardsDatabase.Find(c => c.dayIndex == currentDay);
        if (config != null)
        {
            if (config.isComboReward)
            {
                if (config.comboGoldAmount > 0) DataManager.Instance.AddGold(config.comboGoldAmount);
                if (config.comboBoosterAmount > 0)
                {
                    DataManager.Instance.playerData.hammerCount += config.comboBoosterAmount;
                    DataManager.Instance.playerData.swapCount += config.comboBoosterAmount;
                    DataManager.Instance.playerData.trashCount += config.comboBoosterAmount;
                    DataManager.Instance.playerData.rerollCount += config.comboBoosterAmount;
                }
            }
            else
            {
                switch (config.rewardType)
                {
                    case RewardType.Gold: DataManager.Instance.AddGold(config.rewardAmount); break;
                    case RewardType.BoosterHammer: DataManager.Instance.playerData.hammerCount += config.rewardAmount; break;
                    case RewardType.BoosterSwap: DataManager.Instance.playerData.swapCount += config.rewardAmount; break;
                    case RewardType.BoosterTrash: DataManager.Instance.playerData.trashCount += config.rewardAmount; break;
                    case RewardType.BoosterReroll: DataManager.Instance.playerData.rerollCount += config.rewardAmount; break;
                }
            }
        }

        // Cập nhật trạng thái
        DataManager.Instance.playerData.lastDailyRewardClaimDateUTC = todayStr;
        DataManager.Instance.playerData.currentDailyRewardDay++;
        DataManager.Instance.SaveData();

        Debug.Log($"[DailyReward] MẠNG CHUẨN: Đã nhận thành công quà của Day {currentDay + 1}!");

        OnRewardDataLoaded?.Invoke(); // Cập nhật lại UI
    }
    
    public bool IsCheckingNetwork() { return isCheckingTime; }
    public bool HasNetworkError() { return !hasValidNetworkTime && !isCheckingTime; }
    public string GetNetworkError() { return timeErrorMessage; }
}
