using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DailyRewardButton : MonoBehaviour
{
    [Tooltip("Kéo Bảng DailyRewardPanel của bạn vào đây")]
    public DailyRewardPanelUI rewardPanel;
    
    [Tooltip("Kéo icon dấu chấm đỏ (Image) vào đây để báo hiệu có quà")]
    public GameObject redDotNotification;

    void Start()
    {
        // Tự động gắn sự kiện Click
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnClick);
        }

        // Lắng nghe sự kiện check mạng để bật/tắt chấm đỏ
        if (DailyRewardManager.Instance != null)
        {
            DailyRewardManager.Instance.OnRewardDataLoaded += CheckNotification;
        }
    }

    void OnDestroy()
    {
        if (DailyRewardManager.Instance != null)
        {
            DailyRewardManager.Instance.OnRewardDataLoaded -= CheckNotification;
        }
    }

    private void CheckNotification()
    {
        if (DailyRewardManager.Instance == null) return;
        
        int currentDay = DataManager.Instance.playerData.currentDailyRewardDay;
        bool hasReward = false;

        // Quà hôm nay đã có và chưa nhận
        if (currentDay < 7)
        {
            hasReward = (DailyRewardManager.Instance.GetStateForDay(currentDay) == DailyRewardManager.RewardState.Available);
        }

        // Bật/tắt chấm đỏ
        if (redDotNotification != null)
        {
            redDotNotification.SetActive(hasReward);
        }
    }

    private void OnClick()
    {
        if (rewardPanel != null)
        {
            rewardPanel.gameObject.SetActive(true);
            rewardPanel.OpenPanel();
            
            // Tạm thời tắt dấu chấm đỏ khi vừa mở bảng ra
            if (redDotNotification != null) redDotNotification.SetActive(false);
        }
    }
}
