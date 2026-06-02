using System;
using UnityEngine;

public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================================
    // 1. SỰ KIỆN TÀI NGUYÊN (VÀNG)
    // =========================================
    public event Action<int> OnGoldChanged;
    public void TriggerGoldChanged(int currentGold)
    {
        OnGoldChanged?.Invoke(currentGold);
    }

    // =========================================
    // 2. SỰ KIỆN TRANG BỊ / CỬA HÀNG
    // =========================================
    public event Action<string> OnSkinEquipped;
    public void TriggerSkinEquipped(string skinId)
    {
        OnSkinEquipped?.Invoke(skinId);
    }
    
    public event Action<string> OnSkinPurchased;
    public void TriggerSkinPurchased(string skinId)
    {
        OnSkinPurchased?.Invoke(skinId);
    }

    // =========================================
    // 3. SỰ KIỆN NHIỆM VỤ (ACHIEVEMENT)
    // =========================================
    public event Action<string, int, int, bool> OnQuestProgressUpdated;
    public void TriggerQuestProgressUpdated(string questId, int currentProgress, int targetProgress, bool isCompleted)
    {
        OnQuestProgressUpdated?.Invoke(questId, currentProgress, targetProgress, isCompleted);
    }
}
