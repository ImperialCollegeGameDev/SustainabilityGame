using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPanel : MonoBehaviour
{
    public static TutorialPanel Instance { get; private set; }

    public GameObject Body;
    public GameObject Left;
    public GameObject Right;

    [SerializeField] private TextMeshProUGUI heading;
    [SerializeField] private TextMeshProUGUI body;
    [SerializeField] private Vector4 LeftTopRightBottom;

    private Vector3 initialScale;

    private static readonly (string heading, string body)[] Steps =
    {
        ("Welcome to SUS!",      "This is a tutorial to help you get started. Use the buttons below to navigate through the steps."),
        ("Getting Powerful",     "Before we place anything else, we need a source of power. Use the build menu in the bottom left to place a Coal Power Plant!"),
        ("A Quiet Town",         "Now that we have power, we can place our first residence! Make sure to place it adjacent to your power source so that it can receive the power being generated."),
        ("A Taxed Town",         "People will start moving into a residence, and you'll earn revenue based on your city's population! Don't place too many residences though, since you don't have infinite power to supply!"),
        ("Cleaning Up",          "Watch out! It's getting hot in here. As you produce energy, you also produce nasty emissions which can cause illness when left unchecked."),
        ("Cleaning Up",          "The progress bars in the top right of your screen indicate the air quality, power, and happiness in your city. Try placing some Parks to reduce the emission build-up in your city! P.S., you might need 6-7 of them"),
        ("Extras",               "You can toggle the build menu by clicking on the build icon in the bottom left again. While it's closed, you can click on your placed buildings to view additional information about each one!"),
    };

    private int currentStep = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        initialScale = transform.localScale;
        OnOpen();
    }


    public void OnOpen()
    {
        transform.localScale = Vector3.zero;
        LeanTween.scale(gameObject, initialScale, 0.4f).setEase(LeanTweenType.easeOutBack);

        ShowStep();
    }

    private void ShowStep()
    {
        heading.text = Steps[currentStep].heading;
        body.text = Steps[currentStep].body;

        if (Left != null) Left.SetActive(currentStep > 0);
        if (Right != null) Right.SetActive(currentStep < Steps.Length - 1);
    }

    public void Next()
    {
        if (currentStep < Steps.Length - 1)
        {
            currentStep++;
            ShowStep();
        }
    }

    public void Back()
    {
        if (currentStep > 0)
        {
            currentStep--;
            ShowStep();
        }
    }

    public void OnClose()
    {
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, Vector3.zero, 0.3f)
            .setEase(LeanTweenType.easeInBack);
    }
}