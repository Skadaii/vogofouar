using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AIPlanner.GOAP
{
    public class Node
    {
        public Node parent;

        public int cost;
        public Action action;
        public int preconditionId;
        public WorldState worldState;

        private static Node CreateNode(Node Parent, WorldState WorldState, Action Action, int PreconditionId, int Cost)
        {
            Node node = new Node();

            node.parent = Parent;
            node.action = Action;
            node.preconditionId = PreconditionId;
            node.cost = Cost;
            node.worldState = WorldState;

            return node;
        }

        public static Node CreateNode(Node Parent, WorldState WorldState, Action Action, int PreconditionId)
        {
            return CreateNode(Parent, WorldState, Action, PreconditionId, Action.GetCost(PreconditionId) + Parent.cost);
        }

        public static Node CreateEmptyNode(WorldState WorldState, int Cost = 0)
        {
            return CreateNode(null, WorldState, null, -1, Cost);
        }
    }
}
