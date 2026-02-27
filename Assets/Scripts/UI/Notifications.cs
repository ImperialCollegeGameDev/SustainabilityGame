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
            Debug.LogWarning("Multiple instances of Notifications detected. Destroying duplicate.");
            return;
        }
        Instance = this;
        //notificationPrefab.SetActive(false);
    }

    public void PostNotification(string message)
    {
        if (notificationPrefab == null)
        {
            Debug.LogError("Notification prefab is not assigned in the inspector.");
            return;
        }
        if (transform == null)
        {
            Debug.LogError("Notifications script is not attached to a GameObject.");
            return;
        }
        GameObject notification = Instantiate(notificationPrefab, transform);
        notification.GetComponentsInChildren<TextMeshProUGUI>()[1].SetText(message);
        Destroy(notification, notificationLifetime);
    }
}
