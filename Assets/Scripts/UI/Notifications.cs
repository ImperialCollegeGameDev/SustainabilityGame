using TMPro;
using UnityEngine;

public class Notifications : MonoBehaviour
{
    public static Notifications Instance { get; private set; }

    //public GameObject notificationsTray;
    public GameObject notificationPrefab; // Assign in inspector also if you move.
    public int notificationLifetime = 4;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //notificationPrefab.SetActive(false);
    }

    public void PostNotification(string message)
    {
        //Debug.Log($"Notification: {message}");
        GameObject notification = Instantiate(notificationPrefab, transform);
        notification.GetComponent<TextMeshProUGUI>().SetText(message);
        Destroy(notification, notificationLifetime);
    }
}
