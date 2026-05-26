using UnityEngine;

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
        // TODO: Hiển thị UI Game Over (Bảng điểm, Nút chơi lại)
    }

    public void Execute()
    {
        // Nhấn phím R để giả lập chơi lại
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[FSM] Người chơi chọn Chơi lại (Restart).");
            // Thực tế sẽ phải Reset bàn chơi trước khi về PlayingState
            manager.ChangeState(manager.PlayingState);
        }
    }

    public void Exit()
    {
        Debug.Log("[FSM] Thoát GameOverState. Ẩn UI.");
    }
}
