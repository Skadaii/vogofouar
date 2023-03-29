using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIPlanner.GOAP
{
    public class GOAP : MonoBehaviour
    {
        [SerializeField] private WorldState m_worldState;
        [SerializeField] private List<Action> m_actionSet = new List<Action>();
        [SerializeField] private List<Goal> m_goals = new List<Goal>();

        public ref WorldState WorldState => ref m_worldState;
        public List<Goal> Goals => m_goals;
        public List<Action> ActionSet => m_actionSet;

        private Queue<Node> m_nodes = new Queue<Node>();

        private Node m_currentNode;
        private Goal m_currentGoal;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            foreach (Action action in m_actionSet)
                action.Initialize(gameObject);

            for (int i = 0; i < m_goals.Count; ++i)
                m_goals[i].Initialize(gameObject);

            m_worldState.Initialize(gameObject);
        }

        private void Update()
        {
            m_worldState.UpdateStates();

            UpdateGoal();
            UpdatePlan();
        }

        private static bool GoalAchieved(in WorldState InWorldState, in List<StateId> Goal)
        {
            foreach (StateId stateId in Goal)
            {
                if (InWorldState.states[stateId.id].stateValue.HashValue != stateId.stateValue.HashValue)
                    return false;
            }

            return true;
        }

        public static void BuildGraph(ref Node Parent, ref Node Leave, List<Action> AvailableActions, in List<StateId> InGoal)
        {
            foreach (Action action in AvailableActions)
            {
                int preconditionId = action.IsValid(Parent.worldState);

                if (preconditionId == -1)
                    continue;

                WorldState NewWorldState = action.ApplyEffects(Parent.worldState);

                Node node = Node.CreateNode(Parent, NewWorldState, action, preconditionId);

                if (Leave.cost < node.cost)
                    continue;

                if (GoalAchieved(NewWorldState, InGoal))
                {
                    Leave = node;
                }
                else
                {
                    List<Action> actionsSubSet = AvailableActions.ToList();
                    actionsSubSet.Remove(action);

                    BuildGraph(ref node, ref Leave, actionsSubSet, InGoal);
                }

            }

        }

        private void BuildNodePlan(Node node)
        {
            if (node.parent == null)
                return;

            BuildNodePlan(node.parent);

            m_nodes.Enqueue(node);
        }

        private void UpdateGoal()
        {
            float heuristic = 0f;
            Goal bestGoal = null;
            int bestGoalId = -1;
            for (int i = 0; i < m_goals.Count; ++i)
            {
                Goal goal = m_goals[i];
                float newHeuristic = goal.Evaluate(m_worldState);

                if (newHeuristic > heuristic)
                {
                    heuristic = newHeuristic;
                    bestGoal = goal;
                    bestGoalId = i;
                }
            }

            if (bestGoal == null)
                return;

            if (m_currentGoal != bestGoal)
            {
                m_currentGoal = bestGoal;
                Debug.Log($"New Goal ({bestGoalId})");
                GeneratePlan();
            }
        }

        private void GeneratePlan()
        {
            m_worldState.ComputeHashValues();

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Node startNode = Node.CreateEmptyNode(m_worldState);
            Node leave = Node.CreateEmptyNode(m_worldState, int.MaxValue);

            BuildGraph(ref startNode, ref leave, m_actionSet, m_currentGoal.States);

            if (leave.parent == null)
                return;

            BuildNodePlan(leave);

            string plan = $"{gameObject.name} | Plan: ";
            foreach (Node node in m_nodes)
                plan += "->" + node.action.name;

            Debug.Log(plan + $"Generation duration : {stopwatch.ElapsedMilliseconds}");

            NextNode();

            stopwatch.Stop();
        }

        private void NextNode()
        {
            if (m_nodes.Count == 0)
            {
                m_currentGoal = null;
                m_currentNode = null;
                return;
            }

            m_currentNode = m_nodes.Dequeue();

            m_worldState.ComputeHashValues();
            if (!m_currentNode.action.Start(in m_worldState, m_currentNode.preconditionId))
            {
                ActionFailed();
                return;
            }
        }

        private void UpdatePlan()
        {
            if (m_currentNode == null)
            {
                NextNode();
                return;
            }

            Action.EActionState actionState;
            m_currentNode.action.Update(m_worldState, out actionState);

            switch (actionState)
            {
                case Action.EActionState.Finished:
                    ActionFinished();
                    break;
                case Action.EActionState.Failed:
                    ActionFailed();
                    return;
            }
        }

        private void ActionFinished()
        {
            Debug.Log($"Action Finished: {m_currentNode.action.name}");

            m_worldState = m_currentNode.action.ApplyEffects(m_worldState);
            m_currentNode = null;
        }

        private void ActionFailed()
        {
            Debug.Log($"Action Failed: {m_currentNode.action.name}");

            m_nodes.Clear();

            m_currentNode = null;
            m_currentGoal = null;
        }

    }
}
