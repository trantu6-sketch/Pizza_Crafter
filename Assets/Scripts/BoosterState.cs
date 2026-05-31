using UnityEngine;

public class BoosterState : IGameState
{
    private GameStateManager manager;

    public BoosterState(GameStateManager manager)
    {
        this.manager = manager;
    }

    public void Enter()
    {
        Debug.Log("[FSM] Đang ở BoosterState: Chờ người chơi nhắm mục tiêu kỹ năng.");
    }

    public void Execute()
    {
        // BoosterManager sẽ tự Update để bắt sự kiện chuột.
        // Trạng thái này chỉ để khóa DragDropManager.
        
        // Bấm chuột phải hoặc phím Escape để hủy kỹ năng
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (BoosterManager.Instance != null)
            {
                BoosterManager.Instance.CancelBoosterDrop();
            }
        }
    }

    public void Exit()
    {
        Debug.Log("[FSM] Thoát BoosterState.");
    }
}
