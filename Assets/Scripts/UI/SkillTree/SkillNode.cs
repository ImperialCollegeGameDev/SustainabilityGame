// SkillNodeUI.cs
// Attach to: each hexagon Button GameObject (with Image + Button + child label)
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillNode : MonoBehaviour  //, IPointerEnterHandler, IPointerExitHandler
{
    public List<SkillNode> prerequisites = new List<SkillNode>();
    //[SerializeField] private BuildingSelectorTooltip tooltip;
    public int cost = 40000;

    private SkillTreeUI tree;
    private Button btn;
    private Image img;

    [HideInInspector]
    public string skillId;

    // which skills will this building unlock
    public List<string> unlockBuildingIds = new List<string>();

    private void Awake()
    {
        skillId = gameObject.name;
    }


    public void Init(SkillTreeUI treeRef)
    {
        tree = treeRef;

        btn = GetComponent<Button>();
        img = GetComponent<Image>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        MusicManager.Instance?.PlayUISound(MusicManager.UISoundType.Click);
        if (tree == null) return;
        tree.TryUnlock(this);
    }

    public void RefreshVisuals()
    {
        if (tree == null) return;

        bool isUnlocked = tree.IsUnlocked(skillId);
        bool canUnlock = tree.CanUnlock(this);

        // Button interactable only if unlockable (simple)
        btn.interactable = canUnlock;

        Color c = img.color;

        if (isUnlocked)
            c.a = 1f;      // fully unlocked
        else if (canUnlock)
            c.a = 0.6f;
        else
            c.a = 0.2f;    // locked

        img.color = c;
    }

    //private Coroutine hideRoutine;

    //public void OnPointerEnter(PointerEventData eventData)
    //{
    //    if (hideRoutine != null)
    //        StopCoroutine(hideRoutine);

    //    tooltip.Show(skillId, cost, this);
    //}

    //public void OnPointerExit(PointerEventData eventData)
    //{
    //    hideRoutine = StartCoroutine(HideDelayed());
    //}

    //private IEnumerator HideDelayed()
    //{
    //    yield return new WaitForSeconds(0.05f);
    //    tooltip.Hide(this);
    //}
}
