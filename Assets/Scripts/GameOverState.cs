using UnityEngine;
using DG.Tweening;

public class GameOverState : IGameState
{
    private GameStateManager manager;

    public GameOverState(GameStateManager manager)
    {
        this.manager = manager;
    }

    public void Enter()
    {
        Debug.Log("[FSM] Đang ở GameOverState: Trò chơi kết thúc!");
        
        // Cập nhật và Lưu Best Score thông qua DataManager
        int currentScore = manager.currentScore;
        int bestScore = 0;
        
        if (DataManager.Instance != null)
        {
            bestScore = DataManager.Instance.playerData.BestScore;
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                DataManager.Instance.playerData.BestScore = bestScore;
            }
            
            // XÓA DỮ LIỆU SAVE BÀN CỜ ĐỂ BẮT ĐẦU VÁN MỚI
            DataManager.Instance.playerData.hasSavedGame = false;
            DataManager.Instance.playerData.savedGrid.Clear();
            DataManager.Instance.playerData.savedLobby.Clear();
            DataManager.Instance.playerData.currentSessionScore = 0;
            
            DataManager.Instance.SaveData();
        }
        else
        {
            // Dự phòng nếu chưa có DataManager
            bestScore = PlayerPrefs.GetInt("BestScore", 0);
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                PlayerPrefs.SetInt("BestScore", bestScore);
                PlayerPrefs.Save();
            }
        }

        // Kích hoạt hình phạt giảm Level khi thua
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LevelDownOnGameOver();
        }

        if (manager.gameOverPanel != null)
        {
            // Hiển thị text điểm số
            if (manager.gameOverScoreText != null)
            {
                manager.gameOverScoreText.text = "Score: " + currentScore.ToString();
            }
            if (manager.gameOverBestScoreText != null)
            {
                manager.gameOverBestScoreText.text = "Best: " + bestScore.ToString();
            }

            manager.gameOverPanel.SetActive(true);
            
            // Nếu muốn dùng DOTween làm UI hiện lên mượt mà
            manager.gameOverPanel.transform.localScale = Vector3.zero;
            manager.gameOverPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(DG.Tweening.Ease.OutBack);
        }
    }

    public void Execute()
    {
        // Có thể nhấn R để chơi lại nhanh
        if (Input.GetKeyDown(KeyCode.R))
        {
            manager.RestartGameFromUI();
        }
    }

    public void Exit()
    {
        Debug.Log("[FSM] Thoát GameOverState. Ẩn UI.");
        if (manager.gameOverPanel != null)
        {
            // Hiệu ứng thu nhỏ dần rồi mới tắt hẳn
            manager.gameOverPanel.transform.DOScale(Vector3.zero, 0.3f)
                .SetEase(DG.Tweening.Ease.InBack)
                .OnComplete(() => {
                    manager.gameOverPanel.SetActive(false);
                });
        }
    }
}
