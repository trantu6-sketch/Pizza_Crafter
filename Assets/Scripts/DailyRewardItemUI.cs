using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class DailyRewardItemUI : MonoBehaviour
{
    public int dayIndex; // 0 đến 6

    [Header("UI References")]
    public GameObject claimedOverlay; // Lớp phủ mờ + chữ CLAIMED
    public GameObject lockOverlay;    // Lớp phủ tối khi bị khóa

    public void UpdateUI()
    {
        if (DailyRewardManager.Instance == null) return;

        DailyRewardManager.RewardState state = DailyRewardManager.Instance.GetStateForDay(dayIndex);

        // Reset DOTween effect if any
        transform.DOKill();
        transform.localScale = Vector3.one;

        switch (state)
        {
            case DailyRewardManager.RewardState.Claimed:
                if (claimedOverlay != null) claimedOverlay.SetActive(true);
                if (lockOverlay != null) lockOverlay.SetActive(false);
                break;

            case DailyRewardManager.RewardState.Available:
                if (claimedOverlay != null) claimedOverlay.SetActive(false);
                if (lockOverlay != null) lockOverlay.SetActive(false);

                // Hiệu ứng nảy nhẹ gây chú ý
                transform.DOScale(1.05f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                break;

            case DailyRewardManager.RewardState.Locked:
                if (claimedOverlay != null) claimedOverlay.SetActive(false);
                if (lockOverlay != null) lockOverlay.SetActive(true);
                break;
        }
    }
}
