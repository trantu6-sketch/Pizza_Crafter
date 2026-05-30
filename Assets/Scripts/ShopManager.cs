using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[System.Serializable]
public class SkinData
{
    public string id;
    public string name;
    public int price;
    public string prefabPath;
    public string iconPath; // Đường dẫn ảnh 2D trong Resources
}

[System.Serializable]
public class SkinDatabase
{
    public List<SkinData> skins;
}

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Config")]
    public string databaseFileName = "SkinDatabase";

    [Header("UI Panels")]
    public GameObject shopPanel;
    public TextMeshProUGUI goldText;

    [Header("Carousel Display")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public Image itemIcon;
    
    [Header("Buttons")]
    public Button leftButton;
    public Button rightButton;
    public Button buyButton;
    public TextMeshProUGUI buyButtonText;

    [Header("Pagination Dots")]
    public Transform dotsContainer;
    public GameObject dotPrefab;
    public Color dotActiveColor = Color.yellow;
    public Color dotInactiveColor = Color.gray;

    private SkinDatabase database;
    private int currentIndex = 0;
    private List<Image> paginationDots = new List<Image>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LoadDatabase();
        
        // Gắn sự kiện cho các nút
        if (leftButton != null) leftButton.onClick.AddListener(PreviousItem);
        if (rightButton != null) rightButton.onClick.AddListener(NextItem);
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyOrEquipClicked);

        UpdateGoldUI();
    }

    private void LoadDatabase()
    {
        TextAsset jsonText = Resources.Load<TextAsset>(databaseFileName);
        if (jsonText != null)
        {
            database = JsonUtility.FromJson<SkinDatabase>(jsonText.text);
            Debug.Log($"[ShopManager] Nạp thành công {database.skins.Count} skins từ JSON.");
            GenerateDots();
        }
        else
        {
            Debug.LogError("[ShopManager] Không tìm thấy file JSON cấu hình skin!");
        }
    }

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            
            // [DOTween] Hiệu ứng nảy lên (Pop-up) khi mở Shop
            shopPanel.transform.localScale = Vector3.zero;
            shopPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }
        
        currentIndex = 0; // Luôn hiển thị món đầu tiên khi mở shop
        UpdateDisplay();
        UpdateGoldUI();
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            // [DOTween] Hiệu ứng thu nhỏ dần rồi mới tắt
            shopPanel.transform.DOScale(Vector3.zero, 0.3f)
                .SetEase(Ease.InBack)
                .OnComplete(() => {
                    shopPanel.SetActive(false);
                });
        }
    }

    public void UpdateGoldUI()
    {
        if (goldText != null && DataManager.Instance != null)
        {
            goldText.text = DataManager.Instance.playerData.Gold.ToString();
        }
    }

    // ================== LOGIC CAROUSEL ==================

    public void NextItem()
    {
        if (database == null || database.skins.Count == 0) return;
        currentIndex++;
        if (currentIndex >= database.skins.Count) currentIndex = 0; // Quay vòng lại đầu
        UpdateDisplay();
    }

    public void PreviousItem()
    {
        if (database == null || database.skins.Count == 0) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = database.skins.Count - 1; // Quay vòng về cuối
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (database == null || database.skins.Count == 0) return;

        SkinData currentSkin = database.skins[currentIndex];

        // Cập nhật Tên và Giá
        if (itemNameText != null) itemNameText.text = currentSkin.name;
        
        // Tải Icon 2D
        if (itemIcon != null)
        {
            // [DOTween] Hiệu ứng giật nhẹ (Punch Scale) khi chuyển ảnh đĩa
            itemIcon.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.2f, 5, 1);

            if (!string.IsNullOrEmpty(currentSkin.iconPath))
            {
                Sprite loadedSprite = Resources.Load<Sprite>(currentSkin.iconPath);
                if (loadedSprite != null)
                {
                    itemIcon.sprite = loadedSprite;
                    itemIcon.color = Color.white;
                }
                else
                {
                    itemIcon.color = Color.clear; // Ẩn hình nếu lỗi
                }
            }
            else
            {
                itemIcon.color = Color.clear; // Ẩn hình nếu không có icon
            }
        }

        // Cập nhật trạng thái Nút Mua/Trang Bị
        if (DataManager.Instance != null && buyButtonText != null && buyButton != null)
        {
            bool isOwned = DataManager.Instance.playerData.PurchasedSkins.Contains(currentSkin.id) || currentSkin.price == 0;
            bool isEquipped = DataManager.Instance.playerData.EquippedPlateSkin == currentSkin.id;

            if (itemPriceText != null)
            {
                itemPriceText.text = isOwned ? "SỞ HỮU" : currentSkin.price.ToString();
            }

            if (isEquipped)
            {
                buyButtonText.text = "ĐANG DÙNG";
                buyButton.interactable = false; // Đang dùng thì mờ đi
            }
            else if (isOwned)
            {
                buyButtonText.text = "TRANG BỊ";
                buyButton.interactable = true;
            }
            else
            {
                buyButtonText.text = "MUA";
                buyButton.interactable = DataManager.Instance.playerData.Gold >= currentSkin.price;
            }
        }

        UpdateDots();
    }

    // ================== LOGIC CHẤM TRANG ==================

    private void GenerateDots()
    {
        if (dotPrefab == null || dotsContainer == null || database == null) return;

        // Dọn dẹp chấm cũ
        foreach (Transform child in dotsContainer)
        {
            Destroy(child.gameObject);
        }
        paginationDots.Clear();

        // Sinh chấm mới
        for (int i = 0; i < database.skins.Count; i++)
        {
            GameObject dot = Instantiate(dotPrefab, dotsContainer);
            Image dotImage = dot.GetComponent<Image>();
            if (dotImage != null)
            {
                paginationDots.Add(dotImage);
            }
        }
    }

    private void UpdateDots()
    {
        for (int i = 0; i < paginationDots.Count; i++)
        {
            if (i == currentIndex)
            {
                paginationDots[i].color = dotActiveColor;
            }
            else
            {
                paginationDots[i].color = dotInactiveColor;
            }
        }
    }

    // ================== LOGIC NÚT MUA ==================

    private void OnBuyOrEquipClicked()
    {
        if (DataManager.Instance == null || database == null) return;

        SkinData currentSkin = database.skins[currentIndex];
        bool isOwned = DataManager.Instance.playerData.PurchasedSkins.Contains(currentSkin.id) || currentSkin.price == 0;

        if (isOwned)
        {
            // Trang bị (Equip)
            DataManager.Instance.playerData.EquippedPlateSkin = currentSkin.id;
            DataManager.Instance.SaveData();
            Debug.Log($"[ShopManager] Đã trang bị skin: {currentSkin.name}");
            UpdateAllPlatesInScene();
        }
        else
        {
            // Cố gắng mua
            bool success = DataManager.Instance.BuySkin(currentSkin.id, currentSkin.price);
            if (success)
            {
                // Mua thành công, tự động trang bị
                DataManager.Instance.playerData.EquippedPlateSkin = currentSkin.id;
                DataManager.Instance.SaveData();
                UpdateAllPlatesInScene();
            }
        }

        // Cập nhật lại UI
        UpdateGoldUI();
        UpdateDisplay();
    }

    private void UpdateAllPlatesInScene()
    {
        PizzaPlate[] allPlates = FindObjectsByType<PizzaPlate>(FindObjectsSortMode.None);
        foreach (var plate in allPlates)
        {
            plate.ApplySkin();
        }
    }

    public SkinData GetSkinData(string skinId)
    {
        if (database == null) return null;
        return database.skins.Find(s => s.id == skinId);
    }
}
