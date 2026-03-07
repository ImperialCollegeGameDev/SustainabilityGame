using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class TutorialPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI heading;
    [SerializeField] private TextMeshProUGUI body;
    [SerializeField] private Vector4 LeftTopRightBottom;

    private void Start()
    {
        RectTransform rect = GetComponent<RectTransform>();
        rect.offsetMin = new Vector2(LeftTopRightBottom.x, LeftTopRightBottom.w);
        rect.offsetMax = new Vector2(-LeftTopRightBottom.z, -LeftTopRightBottom.y);
    }

    public void SetHeading(string text)
    {
        heading.text = text;
    }

    public void SetBody(string text)
    {
        body.text = text;
    }
}