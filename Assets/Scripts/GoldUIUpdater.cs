using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class GoldUIUpdater : MonoBehaviour
{
    private TextMeshProUGUI textComponent;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        // Khởi tạo text lần đầu
        if (DataManager.Instance != null)
        {
            UpdateGoldText(DataManager.Instance.playerData.Gold);
        }
    }

    void OnEnable()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnGoldChanged += UpdateGoldText;
        }
    }

    void OnDisable()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnGoldChanged -= UpdateGoldText;
        }
    }

    private void UpdateGoldText(int newGold)
    {
        if (textComponent != null)
        {
            textComponent.text = newGold.ToString();
        }
    }
}
