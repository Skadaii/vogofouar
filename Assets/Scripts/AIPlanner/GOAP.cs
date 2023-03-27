using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIPlanner.GOAP
{
    public class GOAP : MonoBehaviour
    {
        public WorldState WorldState;

        [SerializeField] private List<Action> m_actionSet = new List<Action>();
        public List<Action> ActionSet => m_actionSet;

        [SerializeField] private List<Goal> m_goals = new List<Goal>();
        public List<Goal> Goals => m_goals;

        private Queue<Node> m_nodes = new Queue<Node>();

        private Node m_currentNode;
        private Goal m_currentGoal;

        private void Awake()
        {
            //for (int i = 0; i < 100; ++i)
            //   m_actionSet.Add(Action.CreateSampleAction());

            foreach (Action action in m_actionSet)
                action.Initialize(gameObject);

            for (int i = 0; i < m_goals.Count; ++i)
                m_goals[i].Initialize(gameObject);

            WorldState.Initialize(gameObject);
        }

        private void Update()
        {
            WorldState.UpdateStates();

            UpdateGoal();
            UpdatePlan();
        }

        private bool GoalAchieved(in WorldState InWorldState, in List<StateId> Goal)
        {
            foreach (StateId stateId in Goal)
            {
                if (InWorldState.States[stateId.Id].StateValue.HashValue != stateId.StateValue.HashValue)
                    return false;
            }

            return true;
        }

        private void BuildGraph(ref Node Parent, ref Node Leave, List<Action> AvailableActions, in List<StateId> InGoal, ref int i)
        {
            foreach (Action action in AvailableActions)
            {
                int preconditionId = action.IsValid(Parent.WorldState);

                if (preconditionId == -1)
                    continue;

                ++i;
                WorldState NewWorldState = action.ApplyEffects(Parent.WorldState);

                Node node = Node.CreateNode(Parent, NewWorldState, action, preconditionId);

                if (Leave.Cost < node.Cost)
                    continue;

                if (GoalAchieved(NewWorldState, InGoal))
                {
                    Leave = node;
                }
                else
                {
                    List<Action> actionsSubSet = AvailableActions.ToList();
                    actionsSubSet.Remove(action);

                    BuildGraph(ref node, ref Leave, actionsSubSet, InGoal, ref i);
                }

            }

        }

        private void BuildNodePlan(Node node)
        {
            if (node.Parent == null)
                return;

            BuildNodePlan(node.Parent);

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
                float newHeuristic = goal.Evaluate(WorldState);

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
            WorldState.ComputeHashValues();

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            Node startNode = Node.CreateEmptyNode(WorldState);
            Node leave = Node.CreateEmptyNode(WorldState, int.MaxValue);

            int i = 0;
            BuildGraph(ref startNode, ref leave, m_actionSet, m_currentGoal.States, ref i);

            if (leave.Parent == null)
                return;

            BuildNodePlan(leave);

            string plan = $"{gameObject.name} | Plan: ";
            foreach (Node node in m_nodes)
                plan += "->" + node.Action.Name;

            Debug.Log(plan + $" | Count : {i} | Generation duration : {stopwatch.ElapsedMilliseconds}");

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

            WorldState.ComputeHashValues();
            if (!m_currentNode.Action.Start(in WorldState, m_currentNode.PreconditionId))
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
            m_currentNode.Action.Update(WorldState, out actionState);

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
            Debug.Log($"Action Finished: {m_currentNode.Action.Name}");

            WorldState = m_currentNode.Action.ApplyEffects(WorldState);
            m_currentNode = null;
        }

        private void ActionFailed()
        {
            Debug.Log($"Action Failed: {m_currentNode.Action.Name}");

            m_nodes.Clear();

            m_currentNode = null;
            m_currentGoal = null;
        }

    }
}
