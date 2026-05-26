using UnityEngine;
using DG.Tweening;

public class MenuState : IGameState
{
    private GameStateManager manager;

    public MenuState(GameStateManager manager)
    {
        this.manager = manager;
    }

    public void Enter()
    {
        Debug.Log("[FSM] Đang ở MenuState: Chờ người chơi bấm Bắt đầu.");
        // Bật màn hình Menu và reset lại scale để đề phòng bị thu nhỏ từ lần trước
        if (manager.menuPanel != null)
        {
            manager.menuPanel.SetActive(true);
            manager.menuPanel.transform.localScale = Vector3.one;
        }
    }

    public void Execute()
    {
        // (Tùy chọn) Vẫn giữ phím Space để ai thích dùng phím tắt thì dùng
        if (Input.GetKeyDown(KeyCode.Space))
        {
            manager.StartGameFromUI();
        }
    }

    public void Exit()
    {
        Debug.Log("[FSM] Thoát MenuState. Chạy hiệu ứng tắt UI Menu bằng DOTween.");
        if (manager.menuPanel != null)
        {
            // Sử dụng DOTween để thu nhỏ Panel từ từ trong 0.4s với hiệu ứng đàn hồi InBack
            manager.menuPanel.transform.DOScale(Vector3.zero, 0.4f)
                .SetEase(Ease.InBack)
                .OnComplete(() => 
                {
                    // Tắt hẳn Game Object sau khi thu nhỏ xong để tối ưu hiệu năng
                    manager.menuPanel.SetActive(false);
                });
        }
    }
}
