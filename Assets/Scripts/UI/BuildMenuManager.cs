using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildMenuManager : MonoBehaviour
{
    [Header("Root Buttons")]
    public Transform categoriesRoot;

    [Header("Submenus")]
    public List<Transform> subMenus;

    public float animDuration = 0.15f;

    bool isOpen = false;
    bool isAnimating = false;

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

    // Main "Build" button
    public void ToggleMainMenu()
    {
        if (isAnimating) return;

        if (isOpen)
            CloseAll();     // closes categories + all submenus
        else
            OpenCategories();

        isOpen = !isOpen;
    }

    // Category button -> open its submenu, close others
    public void OpenSubMenu(Transform submenu)
    {
        if (isAnimating) return;

        for (int i = 0; i < subMenus.Count; i++)
        {
            if (subMenus[i] != submenu)
                CloseInstant(subMenus[i]);
        }

        Open(submenu);
    }

    // Any building button in any submenu should call this
    public void OnBuildingSelected()
    {
        if (isAnimating) return;
        CloseAll();
        isOpen = false;
        GameState.Instance.SetModePlace(true);
    }

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
    }

    void Open(Transform t)
    {
        StartCoroutine(Scale(t, Vector3.zero, Vector3.one, true));
    }

    void Close(Transform t)
    {
        StartCoroutine(Scale(t, t.localScale, Vector3.zero, false));
        GameState.Instance.SetModeNone();
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
