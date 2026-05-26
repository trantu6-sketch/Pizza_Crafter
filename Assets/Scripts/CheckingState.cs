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
        // TODO: Viết hàm kiểm tra Game Over (duyệt toàn bộ GridCell và khay chứa).
        // Nếu Game Over -> manager.ChangeState(manager.GameOverState);
        // Nếu chưa -> manager.ChangeState(manager.PlayingState);

        manager.ChangeState(manager.PlayingState);
    }
}
