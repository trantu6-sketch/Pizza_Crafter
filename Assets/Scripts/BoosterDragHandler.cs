using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BoosterDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public BoosterType boosterType;
    
    [Tooltip("UI hiển thị số lượng của Booster này")]
    public TMPro.TextMeshProUGUI countText;
    
    [Tooltip("Hình ảnh icon của Booster để làm mờ khi hết số lượng")]
    public Image iconImage;

    private GameObject ghostIcon;
    private Canvas canvas;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
    }

    void Update()
    {
        if (DataManager.Instance != null && BoosterManager.Instance != null)
        {
            int count = BoosterManager.Instance.GetBoosterCount(boosterType);
            if (countText != null) countText.text = count.ToString();
            
            if (iconImage != null)
            {
                if (count <= 0) 
                    iconImage.color = new Color(0.4f, 0.4f, 0.4f, 1f); // Tối đi báo hiệu hết hàng
                else 
                    iconImage.color = Color.white;
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Chặn kéo thả nếu hết vật phẩm hoặc game không ở trạng thái Playing (hoặc BoosterState)
        if (BoosterManager.Instance == null || BoosterManager.Instance.GetBoosterCount(boosterType) <= 0)
        {
            eventData.pointerDrag = null; 
            return;
        }
        
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsState<PlayingState>())
        {
            eventData.pointerDrag = null; 
            return;
        }

        // Đóng băng DragDropManager để tránh cầm nhầm đĩa Pizza bên dưới
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameStateManager.Instance.BoosterState);
        }

        // Tạo Bóng ma (Ghost Icon) bay theo con trỏ chuột
        ghostIcon = new GameObject("GhostBoosterIcon_" + boosterType.ToString());
        ghostIcon.transform.SetParent(canvas.transform, false);
        ghostIcon.transform.SetAsLastSibling(); // Nổi lên trên cùng
        
        Image img = ghostIcon.AddComponent<Image>();
        img.sprite = iconImage.sprite;
        img.raycastTarget = false; // Xuyên thủng chuột để bắn Raycast 3D sau này
        
        RectTransform rt = ghostIcon.GetComponent<RectTransform>();
        rt.sizeDelta = GetComponent<RectTransform>().sizeDelta;
        rt.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostIcon != null)
        {
            ghostIcon.GetComponent<RectTransform>().position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghostIcon != null)
        {
            Destroy(ghostIcon);
        }

        // Bắn tia Raycast xuống môi trường 3D xem rớt vào đâu
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            BoosterManager.Instance.ProcessBoosterDrop(boosterType, hit);
        }
        else
        {
            // Drop ra ngoài khoảng không
            BoosterManager.Instance.CancelBoosterDrop();
        }
    }
}
