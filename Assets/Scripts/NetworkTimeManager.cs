using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkTimeManager : MonoBehaviour
{
    public static NetworkTimeManager Instance { get; private set; }

    // Sử dụng API cung cấp giờ chuẩn UTC (Không cần API key)
    private const string TIME_API_URL = "https://timeapi.io/api/Time/current/zone?timeZone=UTC";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Lấy thời gian UTC thực tế từ Internet.
    /// Nếu lấy thành công, gọi hàm onSuccess trả về DateTime.
    /// Nếu thất bại (Mất mạng hoặc API lỗi), gọi hàm onError trả về chuỗi thông báo lỗi.
    /// </summary>
    public void GetUTCTime(Action<DateTime> onSuccess, Action<string> onError)
    {
        StartCoroutine(FetchTimeRoutine(onSuccess, onError));
    }

    private IEnumerator FetchTimeRoutine(Action<DateTime> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(TIME_API_URL))
        {
            // Thiết lập timeout khoảng 5 giây để tránh chờ quá lâu khi rớt mạng
            webRequest.timeout = 5;

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || 
                webRequest.result == UnityWebRequest.Result.DataProcessingError || 
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogWarning("[NetworkTimeManager] Lỗi lấy giờ mạng: " + webRequest.error);
                onError?.Invoke("Không có kết nối mạng. Vui lòng kết nối Internet để nhận thưởng!");
            }
            else
            {
                try
                {
                    string jsonResult = webRequest.downloadHandler.text;
                    // Phân tích JSON trả về từ timeapi.io
                    TimeApiResponse response = JsonUtility.FromJson<TimeApiResponse>(jsonResult);
                    
                    // Parse chuỗi sang DateTime (UTC)
                    // API timeapi.io trả về chuỗi "2026-06-02T14:26:00" không có chữ Z ở đuôi
                    // Nên ta phải tự thêm "Z" vào để Unity hiểu đây là giờ Quốc Tế, tránh bị trừ lùi 7 tiếng
                    DateTime utcNow = DateTime.Parse(response.dateTime + "Z");
                    
                    Debug.Log("[NetworkTimeManager] Lấy giờ chuẩn thành công: " + utcNow.ToString());
                    onSuccess?.Invoke(utcNow);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[NetworkTimeManager] Lỗi xử lý dữ liệu giờ: " + e.Message);
                    onError?.Invoke("Lỗi đồng bộ máy chủ thời gian.");
                }
            }
        }
    }

    [Serializable]
    private class TimeApiResponse
    {
        public string dateTime; // timeapi.io trả về trường 'dateTime' (chữ T viết hoa)
    }
}
