using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

public class InfluenceMap : Graph
{
    static InfluenceMap _Instance = null;
    static public InfluenceMap Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = FindObjectOfType<InfluenceMap>();
            return _Instance;
        }
    }

    public float UpdateFrequency = 0.5f;
    private float LastUpdateTime = float.MinValue;


    private bool IsGraphCreated = false;


    protected void OnValidate()
    {
        if (!IsGraphCreated)
            CreateTiledGrid();
    }

    private new void Awake()
    {
        base.Awake();
        OnGraphCreated += () => { IsGraphCreated = true; };
    }

    private void Update()
    {
        if (!IsGraphCreated)
            return;

        // TODO : don't update influence map if no Unit has moved
        if (Time.time - LastUpdateTime > UpdateFrequency)
        {
            ComputeInfluence();
        }
    }

    protected override Node CreateNode()
    {
        return new InfluenceNode();
    }

    #region Influence Map

    public void ComputeInfluence()
    {
        // Reset all influence nodes
        foreach (InfluenceNode node in NodeList)
            node.SetValue(ETeam.Neutral, 0f);

        List<InfluenceNode> pending = new List<InfluenceNode>();
        List<InfluenceNode> visited = new List<InfluenceNode>();
        List<InfluenceNode> frontier;

        List<Entity> blueEntities = GameServices.GetControllerByTeam(ETeam.Blue).EntityList;
        List<Entity> redEntities =  GameServices.GetControllerByTeam(ETeam.Red).EntityList;
        List<Entity> allEntities = blueEntities.Concat(redEntities).ToList();


        foreach (Entity entity in allEntities)
        {
            pending.Clear();
            visited.Clear();
            Vector3 uPos = entity.transform.position;
            InfluenceNode node = GetNode(uPos) as InfluenceNode;
            pending.Add(node);

            // BFS for assigning influence
            for (int i = 1; i <= entity.EntityData.influenceRadius; i++)
            {
                frontier = new List<InfluenceNode>();
                foreach (InfluenceNode n in pending)
                {
                    if (visited.Contains(n))
                        continue;
                    visited.Add(n);
                    n.SetValue(entity.Team, entity.EntityData.GetInfluenceDropOff(i));

                    foreach (Node neighbour in n.Neighbours)
                        frontier.Add(neighbour as InfluenceNode);
                }
                pending = new List<InfluenceNode>(frontier);
            }
        }
    }

    #endregion

    #region Gizmos

    protected override void DrawNodesGizmo()
    {
        for (int i = 0; i < NodeList.Count; i++)
        {
            InfluenceNode node = NodeList[i] as InfluenceNode;
            if (node != null)
            {
                Color nodeColor = node.team switch
                {
                    ETeam.Blue => Color.blue,
                    ETeam.Red => Color.red,
                    ETeam.Neutral => Color.black,
                    _ => throw new System.NotImplementedException()
                };

                nodeColor.a = Mathf.Max(node.value, 0.1f);
                Gizmos.color = nodeColor;
                Gizmos.DrawCube(node.Position, Vector3.one * SquareSize * 0.95f);
            }
        }
    }
    #endregion
}
