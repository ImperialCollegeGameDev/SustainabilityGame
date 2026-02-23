using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.VisualScripting.Member;

public class UpgradeTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    public static UpgradeTooltip Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        gameObject.SetActive(false);
    }

    private Coroutine hideRoutine;
    private object currentSource;

    void Update()
    {
        if (gameObject.activeSelf)
            transform.position = Mouse.current.position.ReadValue() + new Vector2(1.0f, 1.0f);
    }

    public void Show(string name, int points, long price, string description, object source)
    {
        currentSource = source;
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        nameText.text = name;
        pointsText.text = $"{points}";
        priceText.text = NumberFormatter.FormatMoney(price);
        descriptionText.text = description;
        gameObject.SetActive(true);
    }

    public void Hide(object source)
    {
        if (source != currentSource)
            return;

        hideRoutine = StartCoroutine(HideDelayed());
    }

    private IEnumerator HideDelayed()
    {
        yield return new WaitForSeconds(0.05f);

        gameObject.SetActive(false);
        currentSource = null;
    }
}