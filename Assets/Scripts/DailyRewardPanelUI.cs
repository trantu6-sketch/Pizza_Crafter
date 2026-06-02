using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class DailyRewardPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelContainer; // Cái bảng bự chứa toàn bộ UI
    public List<DailyRewardItemUI> dayItems; // Danh sách 7 ô phần thưởng đã được cấu hình sẵn trong Editor
    public Button claimButton;
    public TextMeshProUGUI statusText; // Hiện chữ Đang tải... hoặc Lỗi Mạng
    
    [Header("Animation")]
    public float popupDuration = 0.4f;

    void Start()
    {
        if (DailyRewardManager.Instance != null)
        {
            DailyRewardManager.Instance.OnRewardDataLoaded += RefreshUI;
        }

        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClaimButtonClicked);
        }
    }

    void OnDestroy()
    {
        if (DailyRewardManager.Instance != null)
        {
            DailyRewardManager.Instance.OnRewardDataLoaded -= RefreshUI;
        }
    }

    public void OpenPanel()
    {
        if (panelContainer != null)
        {
            panelContainer.SetActive(true);
            panelContainer.transform.localScale = Vector3.zero;
            panelContainer.transform.DOScale(Vector3.one, popupDuration).SetEase(Ease.OutBack);
        }

        // Báo Manager kiểm tra mạng lại (nếu cần) và refresh
        if (DailyRewardManager.Instance != null)
        {
            DailyRewardManager.Instance.CheckDailyStatus();
        }
        
        RefreshUI();
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

    private void RefreshUI()
    {
        if (DailyRewardManager.Instance == null) return;

        // Cập nhật trạng thái từng ô vuông
        foreach (var item in dayItems)
        {
            item.UpdateUI();
        }

        // Cập nhật nút Claim và thông báo trạng thái
        if (DailyRewardManager.Instance.IsCheckingNetwork())
        {
            if (claimButton != null) claimButton.interactable = false;
            if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "Đang đồng bộ máy chủ..."; }
        }
        else if (DailyRewardManager.Instance.HasNetworkError())
        {
            if (claimButton != null) claimButton.interactable = false;
            if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = DailyRewardManager.Instance.GetNetworkError(); }
        }
        else
        {
            // Không lỗi mạng, xem thử có quà không
            int currentDay = DataManager.Instance.playerData.currentDailyRewardDay;
            if (currentDay < 7)
            {
                DailyRewardManager.RewardState currentState = DailyRewardManager.Instance.GetStateForDay(currentDay);
                if (currentState == DailyRewardManager.RewardState.Available)
                {
                    if (claimButton != null) claimButton.interactable = true;
                    if (statusText != null) statusText.gameObject.SetActive(false);
                }
                else
                {
                    if (claimButton != null) claimButton.interactable = false;
                    if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "Bạn đã nhận quà hôm nay rồi. Hãy quay lại vào ngày mai!"; }
                }
            }
            else
            {
                if (claimButton != null) claimButton.interactable = false;
                if (statusText != null) { statusText.gameObject.SetActive(true); statusText.text = "Đang chờ chu kỳ mới..."; }
            }
        }
    }

    private void OnClaimButtonClicked()
    {
        if (DailyRewardManager.Instance != null)
        {
            DailyRewardManager.Instance.TryClaimReward();
            
            // Tự động tắt UI sau 1.5 giây để người chơi kịp nhìn thấy nút Claim bị khóa và quà bay ra
            Invoke("ClosePanel", 1.5f);
        }
    }
}
