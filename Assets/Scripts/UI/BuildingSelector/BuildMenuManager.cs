using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenuManager : MonoBehaviour
{
    [Header("Root Buttons")]
    public Transform categoriesRoot;

    [Header("Submenus")]
    public List<Transform> subMenus;

    public float animDuration = 0.15f;

    bool isOpen = false;
    bool isAnimating = false;


    public List<GameObject> buildingButtons;
    public float lockedAlpha = 0.2f;
    public float unlockedAlpha = 1f;



    void Start()
    {
        categoriesRoot.localScale = Vector3.zero;
        categoriesRoot.gameObject.SetActive(false);

        foreach (var m in subMenus)
        {
            m.localScale = Vector3.zero;
            m.gameObject.SetActive(false);
        }
    }



    // ---------- Open/Close ----------



    // Main "Build" button
    public void ToggleMainMenu()
    {
        if (isAnimating) return;

        if (isOpen)
        {
            if (isAnimating) return;
            MusicManager.Instance?.PlayUISound(MusicManager.UISoundType.Close);
            CloseAll();         // closes categories + all submenus
            isOpen = false;
            GameState.Instance.SetModeSelect();
        }
        else
        {
            MusicManager.Instance?.PlayUISound(MusicManager.UISoundType.Open);
            OpenCategories();
            isOpen = true;
            RefreshBuildingButtons();
        }
    }

    // Category button -> open its submenu, close others
    public void OpenSubMenu(Transform submenu)
    {
        if (isAnimating) return;

        MusicManager.Instance?.PlayUISound(MusicManager.UISoundType.Click);

        for (int i = 0; i < subMenus.Count; i++)
        {
            if (subMenus[i] != submenu)
                CloseInstant(subMenus[i]);
        }

        Open(submenu);
    }



    // ---------- Enable/Disable Buttons ----------



    public void RefreshBuildingButtons()
    {
        if (GameState.Instance == null) return;

        for (int i = 0; i < buildingButtons.Count; i++)
        {
            GameObject go = buildingButtons[i];
            if (go == null) continue;

            string buildingId = go.name;

            bool unlocked = GameState.Instance.IsBuildingUnlocked(buildingId);

            Button btn = go.GetComponent<Button>();
            Image img = go.GetComponent<Image>();

            if (btn != null)
                btn.interactable = unlocked;

            if (img != null)
            {
                Color c = img.color;
                c.a = unlocked ? unlockedAlpha : lockedAlpha;
                img.color = c;
            }
        }
    }


    //void OnEnable()
    //{
    //    GameState.Instance.OnBuildingUnlocksChanged += RefreshBuildingButtons;
    //}

    //void OnDisable()
    //{
    //    GameState.Instance.OnBuildingUnlocksChanged -= RefreshBuildingButtons;
    //}




    // ---------- Helpers ----------



    void OpenCategories()
    {
        Open(categoriesRoot);
    }

    void CloseAll()
    {
        for (int i = 0; i < subMenus.Count; i++)
            Close(subMenus[i]);

        Close(categoriesRoot);
        isOpen = false;
    }

    void Open(Transform t)
    {
        StartCoroutine(Scale(t, Vector3.zero, Vector3.one, true));
    }

    void Close(Transform t)
    {
        StartCoroutine(Scale(t, t.localScale, Vector3.zero, false));
    }

    void CloseInstant(Transform t)
    {
        t.localScale = Vector3.zero;
        t.gameObject.SetActive(false);
    }

    IEnumerator Scale(Transform target, Vector3 from, Vector3 to, bool setActive)
    {
        isAnimating = true;

        if (setActive)
            target.gameObject.SetActive(true);

        float t = 0f;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            target.localScale = Vector3.Lerp(from, to, t / animDuration);
            yield return null;
        }

        target.localScale = to;

        if (!setActive) // closing
            target.gameObject.SetActive(false);

        isAnimating = false;
    }
}
