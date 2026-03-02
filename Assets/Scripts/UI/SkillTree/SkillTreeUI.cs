// SkillTreeUI.cs
// Attach to: Canvas/SkillTreePanel
using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    public GameObject panelRoot; // set to the gameobject which contains the skill hexagons
    CanvasGroup cg;

    private List<SkillNode> nodes = new List<SkillNode>();

    private readonly HashSet<string> unlocked = new HashSet<string>();

    void Awake()
    {
        cg = panelRoot.transform.GetComponent<CanvasGroup>();

        CollectNodes();
        for (int i = 0; i < nodes.Count; i++)
            nodes[i].Init(this);

        RefreshAll();
    }

    void Start()
    {
        Close();
    }

    public void Open()
    {
        panelRoot.SetActive(true);
        cg.interactable = true;
        cg.blocksRaycasts = true;
        RefreshAll();
    }

    public void Close()
    {
        panelRoot.SetActive(false);
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    public void Toggle()
    {
        bool active = panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;
        if (active) Close();
        else Open();
    }



    // ---- Unlock logic ----
    public bool IsUnlocked(string skillId) => unlocked.Contains(skillId);

    public bool CanUnlock(SkillNode node)
    {
        if (unlocked.Contains(node.skillId)) return false;

        for (int i = 0; i < node.prerequisites.Count; i++)
        {
            if (!unlocked.Contains(node.prerequisites[i].skillId))
                return false;
        }

        return true;
    }

    public void TryUnlock(SkillNode node)
    {
        if (!CanUnlock(node))
        {
            Notifications.Instance.PostNotification($"Cannot unlock {node.skillId} yet.");
            return;
        }

        if (GameState.Instance.money - node.cost < 0)
        {
            Notifications.Instance.PostNotification($"Not enough money to buy that skill.");
            return;
        }

        MusicManager.Instance?.PlayUISound(MusicManager.UISoundType.Buy);
        unlocked.Add(node.skillId);
        GameState.Instance.ChangeMoney(-node.cost);
        Notifications.Instance.PostNotification($"Unlocked {node.skillId}");
        RefreshAll();

        for (int i = 0; i < node.unlockBuildingIds.Count; i++)
            GameState.Instance.UnlockBuilding(node.unlockBuildingIds[i]);

    }



    // ---- UI refresh ----
    void RefreshAll()
    {
        for (int i = 0; i < nodes.Count; i++)
            nodes[i].RefreshVisuals();
    }

    void CollectNodes()
    {
        nodes.Clear();

        if (panelRoot == null)
        {
            Debug.LogWarning("Content root not assigned.");
            return;
        }

        // Find all SkillNodeUI components under Content
        SkillNode[] found = panelRoot.GetComponentsInChildren<SkillNode>(true);

        for (int i = 0; i < found.Length; i++)
        {
            nodes.Add(found[i]);
        }

        //Debug.Log($"Collected {nodes.Count} skill nodes.");
    }

}
