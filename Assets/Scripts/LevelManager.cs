using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level State")]
    public int CurrentLevel { get; private set; } = 1;
    public int CurrentExp { get; private set; } = 0;

    [Header("UI References")]
    [Tooltip("Thanh Slider hiển thị % Exp")]
    public Slider expSlider;
    [Tooltip("Text hiển thị Level hiện tại")]
    public TextMeshProUGUI currentLevelText;
    [Tooltip("Text hiển thị Level tiếp theo")]
    public TextMeshProUGUI nextLevelText;

    [Header("Difficulty Settings")]
    [Tooltip("Số điểm (vàng) nhận được mặc định khi nổ 1 đĩa")]
    public int baseScorePerPlate = 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LoadLevelData();
        UpdateUI(false);
    }

    /// <summary>
    /// Tính toán số Exp cần thiết để thăng cấp lên Level tiếp theo.
    /// Level càng cao, Exp cần càng nhiều.
    /// Công thức: 100 + (Level - 1) * 50
    /// </summary>
    public int GetExpToNextLevel()
    {
        return 100 + (CurrentLevel - 1) * 50;
    }

    /// <summary>
    /// Số điểm được cộng khi ăn Pizza, tăng dần theo Level.
    /// Cứ mỗi 5 Level sẽ được +1 điểm thưởng.
    /// </summary>
    public int GetScoreMultiplier()
    {
        int bonus = CurrentLevel / 5;
        return baseScorePerPlate + bonus;
    }

    public void AddExp(int amount)
    {
        CurrentExp += amount;
        
        int expNeeded = GetExpToNextLevel();
        if (CurrentExp >= expNeeded)
        {
            CurrentExp -= expNeeded;
            LevelUp();
        }
        else
        {
            UpdateUI(true);
            SaveLevelData();
        }
    }

    private void LevelUp()
    {
        CurrentLevel++;
        Debug.Log($"[LevelManager] THĂNG CẤP! Đạt Level {CurrentLevel}");
        
        // Hiệu ứng phình to UI Level để ăn mừng
        if (currentLevelText != null)
        {
            currentLevelText.transform.DOPunchScale(new Vector3(0.5f, 0.5f, 0.5f), 0.5f, 10, 1);
        }

        SaveLevelData();
        
        // Kiểm tra xem Exp dư thừa có đủ để lên cấp tiếp không (hiếm khi xảy ra nhưng an toàn)
        int expNeeded = GetExpToNextLevel();
        if (CurrentExp >= expNeeded)
        {
            CurrentExp -= expNeeded;
            LevelUp();
        }
        else
        {
            UpdateUI(true);
        }
    }

    public void LevelDownOnGameOver()
    {
        // Trừ 3-4 level ngẫu nhiên
        int levelsLost = Random.Range(3, 5);
        CurrentLevel -= levelsLost;

        // Xóa sạch Exp hiện tại (phạt thua)
        CurrentExp = 0;

        // Đảm bảo Level không bao giờ tụt xuống dưới 1
        if (CurrentLevel < 1)
        {
            CurrentLevel = 1;
        }

        Debug.Log($"[LevelManager] GAME OVER! Bị tụt {levelsLost} cấp. Hiện tại đang ở Level {CurrentLevel}");
        
        SaveLevelData();
        UpdateUI(false);
    }

    private void UpdateUI(bool animate)
    {
        if (currentLevelText != null) currentLevelText.text = CurrentLevel.ToString();
        if (nextLevelText != null) nextLevelText.text = (CurrentLevel + 1).ToString();

        if (expSlider != null)
        {
            float targetValue = (float)CurrentExp / GetExpToNextLevel();
            
            if (animate)
            {
                expSlider.DOValue(targetValue, 0.3f).SetEase(Ease.OutQuad);
            }
            else
            {
                expSlider.value = targetValue;
            }
        }
    }

    private void SaveLevelData()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.playerData.Level = CurrentLevel;
            DataManager.Instance.playerData.Exp = CurrentExp;
            DataManager.Instance.SaveData();
        }
    }

    private void LoadLevelData()
    {
        if (DataManager.Instance != null)
        {
            CurrentLevel = DataManager.Instance.playerData.Level;
            CurrentExp = DataManager.Instance.playerData.Exp;
        }
        else
        {
            CurrentLevel = 1;
            CurrentExp = 0;
        }
    }
}
