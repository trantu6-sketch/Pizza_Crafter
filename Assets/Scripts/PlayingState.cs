using UnityEngine;

public class PlayingState : IGameState
{
    private GameStateManager manager;

    public PlayingState(GameStateManager manager)
    {
        this.manager = manager;
    }

    public void Enter()
    {
        Debug.Log("[FSM] Đang ở PlayingState: Mở khóa Drag & Drop cho người chơi.");
        // Bật DragDropManager (nếu cần) hoặc chỉ đơn giản là nó tự đọc IsState<PlayingState>()
    }

    public void Execute()
    {
        // Trong trạng thái này, Update của DragDropManager sẽ được phép chạy.
    }

    public void Exit()
    {
        Debug.Log("[FSM] Thoát PlayingState: Khóa Drag & Drop.");
    }
}
