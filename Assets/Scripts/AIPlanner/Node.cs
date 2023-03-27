using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIPlanner.GOAP
{
    public class Node
    {
        public Node Parent;

        public int Cost;
        public Action Action;
        public int PreconditionId;
        public WorldState WorldState;

        private static Node CreateNode(Node Parent, WorldState WorldState, Action Action, int PreconditionId, int Cost)
        {
            Node node = new Node();

            node.Parent = Parent;
            node.Action = Action;
            node.PreconditionId = PreconditionId;
            node.Cost = Cost;
            node.WorldState = WorldState;

            return node;
        }

        public static Node CreateNode(Node Parent, WorldState WorldState, Action Action, int PreconditionId)
        {
            return CreateNode(Parent, WorldState, Action, PreconditionId, Action.GetCost(PreconditionId) + Parent.Cost);
        }

        public static Node CreateEmptyNode(WorldState WorldState, int Cost = 0)
        {
            return CreateNode(null, WorldState, null, -1, Cost);
        }
    }
}
