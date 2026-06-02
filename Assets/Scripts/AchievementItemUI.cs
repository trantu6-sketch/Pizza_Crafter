using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI progressText;
    
    [Tooltip("Thanh trượt hoặc ảnh dùng thuộc tính Fill Amount")]
    public Image fillBar;
    
    [Tooltip("Nút bấm vào hình ảnh phần thưởng để nhận")]
    public Button rewardButton;
    public Image rewardIcon;

    [Tooltip("Chữ hiển thị trên Nút (Ví dụ: Nhận, Đã Nhận) - Có thể bỏ trống nếu thiết kế không có chữ")]
    public TextMeshProUGUI rewardStatusText;

    private string questId;

    public void Setup(QuestData quest, AchievementConfig config)
    {
        questId = quest.questId;
        
        // 1. Gán tiêu đề
        if (titleText != null) 
            titleText.text = config.title;
        
        // 2. Gán thanh tiến trình
        if (progressText != null) 
            progressText.text = $"{quest.currentProgress}/{quest.targetProgress}";
            
        if (fillBar != null)
        {
            fillBar.fillAmount = (float)quest.currentProgress / quest.targetProgress;
        }

        // 3. Gán hình ảnh phần thưởng
        if (rewardIcon != null && !string.IsNullOrEmpty(config.rewardIconPath))
        {
            Sprite icon = Resources.Load<Sprite>(config.rewardIconPath);
            if (icon != null) rewardIcon.sprite = icon;
        }

        // 4. Trạng thái của Nút Nhận Thưởng
        if (rewardButton != null)
        {
            // Reset sự kiện
            rewardButton.onClick.RemoveAllListeners();

            if (quest.isClaimed)
            {
                // Đã nhận
                rewardButton.interactable = false;
                if (rewardIcon != null) rewardIcon.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Làm mờ cục vàng đi
                if (rewardStatusText != null) rewardStatusText.text = "Đã Nhận";
            }
            else if (quest.isCompleted)
            {
                // Đủ điều kiện nhận
                rewardButton.interactable = true;
                if (rewardIcon != null) rewardIcon.color = Color.white;
                if (rewardStatusText != null) rewardStatusText.text = "Nhận";
                
                // Gắn sự kiện bấm
                rewardButton.onClick.AddListener(() => {
                    AchievementManager.Instance.ClaimReward(questId);
                    
                    // Cập nhật lại UI sau khi nhận
                    Setup(quest, config); 
                });
            }
            else
            {
                // Chưa đủ tiến trình
                rewardButton.interactable = false;
                if (rewardIcon != null) rewardIcon.color = Color.white;
                if (rewardStatusText != null) rewardStatusText.text = "Chưa đạt";
            }
        }
    }
}
