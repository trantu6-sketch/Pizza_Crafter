using UnityEngine;

public class CheckingState : IGameState
{
    private GameStateManager manager;

    public CheckingState(GameStateManager manager)
    {
        this.manager = manager;
    }

    public void Enter()
    {
        Debug.Log("[FSM] Đang ở CheckingState: Kiểm tra trạng thái ván cờ (Nổ combo / Game Over).");
        
        // Hiện tại logic kiểm tra nổ đã nằm trong hàm OnSliceArrived của PizzaPlate.
        // Ở đây chúng ta sẽ kiểm tra xem bàn chơi đã kín chưa (Game Over).
        // Tạm thời để đơn giản, ta tự động quay lại PlayingState sau khi xử lý xong.
        
        CheckGameOver();
    }

    public void Execute()
    {
        // Trạng thái này xử lý logic ngay trong Enter() nên không cần Update liên tục.
    }

    public void Exit()
    {
        Debug.Log("[FSM] Thoát CheckingState.");
    }

    private void CheckGameOver()
    {
        if (GridManager.Instance != null && GridManager.Instance.IsGridFull())
        {
            Debug.Log("[FSM] Grid đã kín! Game Over!");
            manager.ChangeState(manager.GameOverState);
        }
        else
        {
            manager.ChangeState(manager.PlayingState);
            
            // Kích hoạt Auto-save vì lượt đi (bao gồm cả combo nổ) đã kết thúc an toàn
            manager.TriggerAutoSave();
        }
    }
}
