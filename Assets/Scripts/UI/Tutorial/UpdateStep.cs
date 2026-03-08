using UnityEngine;
using UnityEngine.UI;

public class UpdateStep: MonoBehaviour
{
    public bool isForward = true;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (isForward)
        {
            TutorialPanel.Instance.Next();
        }
        else
        {
            TutorialPanel.Instance.Back();
        }
    }
}