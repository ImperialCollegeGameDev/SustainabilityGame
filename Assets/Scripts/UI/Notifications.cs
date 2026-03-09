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
            Debugger.LogWarning("Multiple instances of Notifications detected. Destroying duplicate.");
            return;
        }
        Instance = this;
    }

    public void PostNotification(string message)
    {
        if (notificationPrefab == null)
        {
            Debugger.LogError("Notification prefab is not assigned in the inspector.");
            return;
        }
        if (transform == null)
        {
            Debugger.LogError("Notifications script is not attached to a GameObject.");
            return;
        }
        GameObject notification = Instantiate(notificationPrefab, transform);
        notification.GetComponentsInChildren<TextMeshProUGUI>()[1].SetText(message);

        notification.transform.localScale = Vector3.zero;
        LeanTween.scale(notification, new Vector3(0.5f, 0.5f, 0.5f), 0.25f).setEaseOutBack();
        LeanTween.scale(notification, Vector3.zero, 0.2f)
            .setEaseInBack()
            .setDelay(notificationLifetime)
            .setOnComplete(() => Destroy(notification));
    }
}
