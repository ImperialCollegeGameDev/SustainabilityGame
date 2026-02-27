using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.VisualScripting.Member;

public class BuildingSelectorTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    public BuildMenuManager menuManager;

    private Coroutine hideRoutine;
    private object currentSource;

    void Update()
    {
        if (gameObject.activeSelf)
            transform.position = Mouse.current.position.ReadValue() + new Vector2(1.0f, 1.0f);
    }

    public void Show(string buildingName, int price, object source)
    {
        if (menuManager.isAnimating)
            return;
        currentSource = source;
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        nameText.text = buildingName;
        priceText.text = NumberFormatter.FormatMoney(price);
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