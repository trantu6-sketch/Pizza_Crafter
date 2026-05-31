using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    private IGameState currentState;
    
    // Các trạng thái được lưu trữ sẵn để tránh khởi tạo lại
    public MenuState MenuState { get; private set; }
    public PlayingState PlayingState { get; private set; }
    public AnimatingState AnimatingState { get; private set; }
    public CheckingState CheckingState { get; private set; }
    public GameOverState GameOverState { get; private set; }
    public BoosterState BoosterState { get; private set; }

    [Header("UI References")]
    [Tooltip("Kéo Panel Menu (có chứa Button Bắt Đầu) vào đây")]
    public GameObject menuPanel;
    
    [Header("Game Over UI References")]
    [Tooltip("Kéo Panel Game Over vào đây")]
    public GameObject gameOverPanel;
    [Tooltip("Kéo Text Điểm Số (TextMeshPro) trong bảng Game Over vào đây")]
    public TMPro.TextMeshProUGUI gameOverScoreText;
    [Tooltip("Kéo Text Best Score (TextMeshPro) trong bảng Game Over vào đây")]
    public TMPro.TextMeshProUGUI gameOverBestScoreText;

    [Header("In-Game UI References")]
    [Tooltip("Kéo Text Điểm Số (TextMeshPro) ở màn hình chơi vào đây")]
    public TMPro.TextMeshProUGUI scoreText;

    public int currentScore { get; private set; } = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Khởi tạo các trạng thái
        MenuState = new MenuState(this);
        PlayingState = new PlayingState(this);
        AnimatingState = new AnimatingState(this);
        CheckingState = new CheckingState(this);
        GameOverState = new GameOverState(this);
        BoosterState = new BoosterState(this);
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
            
            // Reset scale trước khi Punch để tránh lỗi nếu gọi liên tục
            scoreText.transform.localScale = Vector3.one;
            // Dùng DOTween để tạo hiệu ứng nảy (Punch) khi được cộng điểm
            scoreText.transform.DOPunchScale(new Vector3(0.5f, 0.5f, 0.5f), 0.3f, 10, 1);
        }
    }

    /// <summary>
    /// Hàm này để Nút (Button) Start Game gọi thông qua Event OnClick()
    /// </summary>
    public void StartGameFromUI()
    {
        if (IsState<MenuState>())
        {
            Debug.Log("[FSM] Người chơi đã click nút Start Game trên UI!");
            ChangeState(PlayingState);
        }
    }

    /// <summary>
    /// Hàm này để Nút (Button) Restart gọi thông qua Event OnClick()
    /// </summary>
    public void RestartGameFromUI()
    {
        if (IsState<GameOverState>())
        {
            Debug.Log("[FSM] Người chơi đã click nút Restart Game trên UI!");
            
            // Xóa tất cả các đĩa trên lưới
            GridManager.Instance.ClearAllPlates();
            
            // Reset điểm
            currentScore = 0;
            if (scoreText != null) scoreText.text = "0";

            ChangeState(PlayingState);
        }
    }

    void Start()
    {
        // Khởi đầu game ở trạng thái Menu (hoặc có thể vào thẳng Playing nếu muốn test)
        ChangeState(MenuState);

        // Load điểm cũ nếu có Save
        if (DataManager.Instance != null && DataManager.Instance.playerData.hasSavedGame)
        {
            currentScore = DataManager.Instance.playerData.currentSessionScore;
            if (scoreText != null) scoreText.text = currentScore.ToString();
        }
    }

    void Update()
    {
        if (currentState != null)
        {
            currentState.Execute();
        }
    }

    public void ChangeState(IGameState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;

        if (currentState != null)
        {
            currentState.Enter();
        }
    }

    public bool IsState<T>() where T : IGameState
    {
        return currentState is T;
    }

    // --- Quản lý số lượng Animation đang chạy ---
    private int activeAnimations = 0;

    public void AddActiveAnimation()
    {
        activeAnimations++;
    }

    public void RemoveActiveAnimation()
    {
        activeAnimations--;
        // Nếu đang ở trạng thái Animating mà tất cả animation đã xong, thì chuyển sang Checking
        if (activeAnimations <= 0 && IsState<AnimatingState>())
        {
            activeAnimations = 0;
            ChangeState(CheckingState);
        }
    }

    // --- AUTO SAVE ---
    public void TriggerAutoSave()
    {
        StartCoroutine(AutoSaveRoutine());
    }

    private System.Collections.IEnumerator AutoSaveRoutine()
    {
        // Đợi 1 chút xíu (hết frame) để đảm bảo mọi logic CheckBloom, xóa đĩa cũ... đã được hoàn tất
        yield return new WaitForEndOfFrame();
        
        if (DataManager.Instance != null && LobbyManager.Instance != null && GridManager.Instance != null)
        {
            DataManager.Instance.playerData.hasSavedGame = true;
            DataManager.Instance.playerData.currentSessionScore = currentScore;
            GridManager.Instance.SaveGridState();
            LobbyManager.Instance.SaveLobbyState();
            DataManager.Instance.SaveData();
            
            Debug.Log("[FSM] Đã Auto-save toàn bộ trạng thái bàn cờ!");
        }
    }
}
