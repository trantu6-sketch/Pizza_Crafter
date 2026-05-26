using UnityEngine;

public class AnimatingState : IGameState
{
    private GameStateManager manager;

    public AnimatingState(GameStateManager manager)
    {
        this.manager = manager;
    }

    public void Enter()
    {
        Debug.Log("[FSM] Đang ở AnimatingState: Chờ các lát Pizza bay xong...");
    }

    public void Execute()
    {
        // Trong trạng thái này, chúng ta chỉ chờ GameStateManager tự động chuyển State 
        // khi số lượng ActiveAnimation về 0 (được xử lý trong GameStateManager).
    }

    public void Exit()
    {
        Debug.Log("[FSM] Thoát AnimatingState: Tất cả animation đã hoàn thành.");
    }
}
