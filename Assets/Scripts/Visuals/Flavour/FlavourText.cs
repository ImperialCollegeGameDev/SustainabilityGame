using UnityEngine;
using TMPro;

public class FlavourText : MonoBehaviour
{
    public float lifetime = 1f;
    public float floatSpeed = 50f;

    private TextMeshPro text;
    private RectTransform rectTransform;
    private Color startColor;
    private float timer;

    void Awake()
    {
        text = GetComponent<TextMeshPro>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(string value, float fontSize, Color color)
    {
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        startColor = color;
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }

    void Update()
    {
        timer += Time.deltaTime;

        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        float alpha = Mathf.Lerp(startColor.a, 0f, timer / lifetime);
        text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        if (timer >= lifetime)
            Destroy(gameObject);
    }

    void LateUpdate()
    {
        transform.LookAt(
            transform.position + Camera.main.transform.rotation * Vector3.forward,
            Camera.main.transform.rotation * Vector3.up
        );
    }


}