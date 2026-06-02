using UnityEngine;

public class DailyRewardAutoPopup : MonoBehaviour
{
    [Tooltip("Kéo Bảng DailyRewardPanel của bạn vào đây")]
    public DailyRewardPanelUI rewardPanel;
    
    private bool hasChecked = false;

    void Start()
    {
        if (DailyRewardManager.Instance != null)
        {
            DailyRewardManager.Instance.OnRewardDataLoaded += CheckAndPopup;
        }
    }

    void OnDestroy()
    {
        if (DailyRewardManager.Instance != null)
        {
            DailyRewardManager.Instance.OnRewardDataLoaded -= CheckAndPopup;
        }
    }

    private void CheckAndPopup()
    {
        if (hasChecked) return;
        if (DailyRewardManager.Instance == null) return;
        
        // Đợi đến khi check giờ mạng xong
        if (!DailyRewardManager.Instance.IsCheckingNetwork() && !DailyRewardManager.Instance.HasNetworkError())
        {
            hasChecked = true; // Đánh dấu là đã kiểm tra (chỉ tự động mở 1 lần khi vô game)
            
            int currentDay = DataManager.Instance.playerData.currentDailyRewardDay;
            if (currentDay < 7)
            {
                if (DailyRewardManager.Instance.GetStateForDay(currentDay) == DailyRewardManager.RewardState.Available)
                {
                    if (rewardPanel != null)
                    {
                        // Kích hoạt GameObject nếu nó đang bị tắt
                        rewardPanel.gameObject.SetActive(true);
                        rewardPanel.OpenPanel();
                        Debug.Log("[AutoPopup] Có quà mới, tự động mở bảng Điểm Danh!");
                    }
                }
            }
        }
    }
}
