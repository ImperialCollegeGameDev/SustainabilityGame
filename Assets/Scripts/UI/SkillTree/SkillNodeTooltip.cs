using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillNodeTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;   // on tooltip
    [SerializeField] private TextMeshProUGUI priceText;  // on tooltip
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private Vector2 offset = new Vector2(12f, 12f);

    private Coroutine hideRoutine;
    private object currentSource;
    private RectTransform rt;

    void Awake()
    {
        rt = (RectTransform)transform;

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;
        if (Mouse.current == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue() + offset;

        RectTransform canvasRect = parentCanvas.transform as RectTransform;
        Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out Vector2 localPoint))
            rt.anchoredPosition = localPoint;
    }

    public void Show(string skillName, int cost, object source)
    {
        currentSource = source;

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        nameText.text = skillName;
        priceText.text = NumberFormatter.FormatMoney(cost);

        gameObject.SetActive(true);
    }

    public void Hide(object source)
    {
        if (source != currentSource) return;

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideDelayed());
    }

    private IEnumerator HideDelayed()
    {
        yield return new WaitForSeconds(0.05f);
        gameObject.SetActive(false);
        currentSource = null;
    }
}