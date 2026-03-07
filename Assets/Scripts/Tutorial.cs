using UnityEngine;

public class Tutorial : MonoBehaviour
{
    public static Tutorial Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            Debug.LogWarning("Multiple instances of Tutorial detected. Destroying duplicate.");
            return;
        }
        Instance = this;
    }

    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject tutorialPanelPrefab;

    private TutorialPanel currentTutorialPanel;
    private int currentStep = 0;

    public void UpdateBasedOnStep()
    {
        switch (currentStep)
        {
            case 1:
                ShowTutorial("Welcome to SUS!", "This is a tutorial to help you get started. Use the buttons below to navigate through the steps.");
                break;
            case 2:
                ShowTutorial("Getting Powerful", "Before we place anything else, we need a source of power. Use the build menu in the bottom left to place a Coal Power Plant!");
                break;
            case 3:
                ShowTutorial("A Quiet Town", "Now that we have power, we can place our first residence! Make sure to place it adjacent to your power source so that it can receive the power being generated.");
                break;
            case 4:
                ShowTutorial("A Taxed Town", "People will start moving into a residence, and you'll earn revenue based on your city's population! Don't place too many residences though, since you don't have infinite power to supply!");
                break;
            case 5:
                ShowTutorial("Cleaning Up", "Watch out! It's getting hot in here. As you produce energy, you also produce nasty emissions which can cause illness when left unchecked.");
                break;
            case 6:
                ShowTutorial("Cleaning Up", "The progress bars in the top right of your screen indicate the air quality, power, and happiness in your city. Try placing some Parks to reduce the emission build-up in your city! P.S., you might need 6-7 of them");
                break;
            case 7:
                ShowTutorial("Extras", "You can toggle the build menu by clicking on the build icon in the bottom left again. While it's closed, you can click on your placed buildings to view additional information about each one!");
                break;
            default:
                if (currentTutorialPanel != null)
                {
                    Destroy(currentTutorialPanel.gameObject);
                    currentTutorialPanel = null;
                }
                break;
        }
    }

    private void ShowTutorial(string heading, string body)
    {
        if (currentTutorialPanel == null)
        {
            currentTutorialPanel = Instantiate(tutorialPanelPrefab, canvas.transform).GetComponent<TutorialPanel>();
        }

        currentTutorialPanel.SetHeading(heading);
        currentTutorialPanel.SetBody(body);
    }

    public void Next()
    {
        currentStep++;
        UpdateBasedOnStep();
    }

    public void Back()
    {
        currentStep = Mathf.Max(1, currentStep - 1);
        UpdateBasedOnStep();
    }

    private void Start() // AUTOMATICALLY Starts the tutorial when the game starts
    {
        StartTutorial();
    }

    public void StartTutorial()
    {
        currentStep = 0;
        Next();
    }
}