using UnityEngine;

public class cred_entry : MonoBehaviour
{
    [Header("Text")]
    public string link1_text;
    public string link2_text;

    [Header("Display")]
    public GameObject icon;
    public GameObject nameObj;
    public GameObject link1;
    public GameObject link2;

    void Start()
    {
        if (string.IsNullOrEmpty(link1_text))
        {
            link1.SetActive(false);
        }
        if (string.IsNullOrEmpty(link2_text))
        {
            link2.SetActive(false);
        }
    }


    public void ClickLink1()
    {
        if (link1_text != null)
        {
            Application.OpenURL(link1_text);
        }
    }

    public void ClickLink2()
    {
        if (link2_text != null)
        {
            Application.OpenURL(link2_text);
        }
    }

}
