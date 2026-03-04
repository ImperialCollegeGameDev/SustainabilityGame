using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkillNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public List<SkillNode> prerequisites = new List<SkillNode>();
    public int cost = 40000;

    [SerializeField] private SkillNodeTooltip tooltip;

    private SkillTreeUI tree;
    private Button btn;
    private Image img;

    [HideInInspector]
    public string skillId;

    public List<string> unlockBuildingIds = new List<string>();     // name of the gameObject in BuilderSelector (in UI prefab)

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip == null) return;
        tooltip.Show(skillId, cost, this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip == null) return;
        tooltip.Hide(this);
    }

    public void SetTooltip(SkillNodeTooltip t) => tooltip = t;

    public void RefreshVisuals()
    {
        if (tree == null) return;

        bool isUnlocked = tree.IsUnlocked(skillId);
        bool canUnlock = tree.CanUnlock(this);

        btn.interactable = canUnlock;

        Color c = img.color;
        if (isUnlocked) c.a = 1f;
        else if (canUnlock) c.a = 0.6f;
        else c.a = 0.2f;

        img.color = c;
    }
}