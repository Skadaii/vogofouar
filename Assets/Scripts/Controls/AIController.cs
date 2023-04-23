using AIPlanner;
using AIPlanner.GOAP;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.UI.CanvasScaler;

public sealed class AIController : UnitController
{
    #region MonoBehaviour methods

    [SerializeField] private GOAP m_goap;
    [SerializeField] private UnitController m_enemyController;

    [SerializeField] private int m_baseCaptureSquadUnitCount = 3;

    private class SquadTask
    {
        public ETaskType m_taskType;
        public Entity m_target;

        public UnitSquad m_unitSquad;
        public List<Unit> m_units = new List<Unit>();

        public int m_unitCountProdution;
        public bool IsReady => m_unitCountProdution == 0;
    }

    private enum ETaskType
    {
        Capture,
        Attack,
    }

    private List<SquadTask> m_squadTasksScheduled = new List<SquadTask>();
    private List<SquadTask> m_squadTaskInProgress = new List<SquadTask>();

    private List<UnitSquad> AvailableSquad
    {
        get
        {
            var unavailableSquads = m_squadTasksScheduled.Select(squadTask => squadTask.m_unitSquad);
            unavailableSquads.Concat(m_squadTaskInProgress.Select(squadTask => squadTask.m_unitSquad));

            List<UnitSquad> availableSquad = new List<UnitSquad>();
            availableSquad.AddRange(m_squadList);

            foreach (UnitSquad unavailableSquad in unavailableSquads)
                availableSquad.Remove(unavailableSquad);

            return availableSquad;
        }
    }

    private int CaptureTaskCount
    {
        get
        {
            int captureTaskCount = 0;

            foreach (SquadTask captureSquadTask in m_squadTaskInProgress)
                if (captureSquadTask.m_taskType == ETaskType.Capture)
                    captureTaskCount++;

            foreach (SquadTask captureSquadTask in m_squadTasksScheduled)
                if (captureSquadTask.m_taskType == ETaskType.Capture)
                    captureTaskCount++;

            return captureTaskCount;
        }
    }

    private List<Entity> CaptureTaskTargets
    {
        get
        {
            List<Entity> captureTarget = new List<Entity>();

            foreach (SquadTask captureSquadTask in m_squadTaskInProgress)
                if (captureSquadTask.m_taskType == ETaskType.Capture)
                    captureTarget.Add(captureSquadTask.m_target);

            foreach (SquadTask captureSquadTask in m_squadTasksScheduled)
                if (captureSquadTask.m_taskType == ETaskType.Capture)
                    captureTarget.Add(captureSquadTask.m_target);

            return captureTarget;
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();

        m_squadTaskInProgress = m_squadTaskInProgress.Where(squadTask =>
        {
            if (squadTask.m_unitSquad == null)
                return false;

            GOAP goap = squadTask.m_unitSquad.m_leaderComponent.GOAP;
            return (goap.WorldState.GetState("TargetType") as TargetType).Value != ETargetType.None && squadTask.m_unitSquad.Units.Count != 0;

        }).ToList();

        m_squadTasksScheduled = m_squadTasksScheduled.Where(squadTask =>
        {
            foreach (Unit unit in squadTask.m_units)
                if (unit == null)
                    return false;

            return true;

        }).ToList();
    }

    [ConsiderationMethod]
    public float Goal_CaptureTarget(WorldState worldState)
    {
        foreach (SquadTask captureSquadTask in m_squadTasksScheduled)
            if (captureSquadTask.m_taskType == ETaskType.Capture && captureSquadTask.IsReady)
                return 1f;

        return 0f;
    }

    [ConsiderationMethod]
    public float Goal_PlanifyCaptureTarget(WorldState worldState)
    {
        return m_enemyController.CapturedTargets >= CapturedTargets && 
            CaptureTaskCount + CapturedTargets <= m_enemyController.CapturedTargets ? 1f : 0f;
    }

    [ConsiderationMethod]
    public float Goal_Attack(WorldState worldState) =>
        m_enemyController.FactoryList.Count >= FactoryList.Count ? 1f : 0f;

    [ConsiderationMethod]
    public float Goal_Defend(WorldState worldState) =>
        m_enemyController.FactoryList.Count >= FactoryList.Count ? 1f : 0f;

    [StateMethod]
    public BoolType State_ResetState() => new BoolType(false);

    [ActionMethod]
    public Action.EActionState Action_PlanifyCaptureTarget(WorldState worldState)
    {
        //Select capture target
        List<StaticBuilding> targets = StaticBuilding.staticBuildings.Where(target => target.Team != Team).ToList();

        //Already planed capture target
        List<Entity> captureTaskTargets = CaptureTaskTargets;

        foreach (Entity entity in captureTaskTargets)
            targets.Remove(entity as StaticBuilding);

        if (!targets.Any())
            return Action.EActionState.Failed;

        var target = targets.OrderBy(target => (target.transform.position - FactoryList[0].transform.position).sqrMagnitude).First();

        if (target == null)
            return Action.EActionState.Failed;

        //Todo: Check influence and potential enemy puisance

        SquadTask captureSquadTask = new SquadTask() { m_target = target, m_taskType = ETaskType.Capture };

        //Check unit available
        List<UnitSquad> availableSquad = AvailableSquad;

        void RequestUnitProductionForTask(int count)
        {
            System.Action<Unit> action = (Unit unit) =>
            {
                if (captureSquadTask != null)
                {
                    captureSquadTask.m_unitCountProdution--;
                    captureSquadTask.m_units.Add(unit);
                }
            };

            Factory selectedFactory = null;
            float closestDistanceSqr = float.MaxValue;

            //TODO: Set TypeId according to the influence of the target
            int factoryTypeId = 0;

            foreach (Factory factory in FactoryList)
            {
                if (factory.FactoryData.typeId == factoryTypeId)
                {
                    float distanceSqr = (target.transform.position - factory.transform.position).sqrMagnitude;
                    if (selectedFactory != null && distanceSqr > closestDistanceSqr)
                        continue;

                    selectedFactory = factory;
                    closestDistanceSqr = distanceSqr;
                }

                if (selectedFactory == null)
                    selectedFactory = factory;
            }

            Unit selectedUnit = null;
            int totalCost = 0;

            if (selectedFactory == null)
                return;

            foreach (GameObject go in selectedFactory.FactoryData.availableUnits)
            {
                Unit unit = go.GetComponent<Unit>();

                if (unit as Builder)
                    continue;

                int totalCostUnit = unit.UnitData.cost * count;
                if (totalCostUnit <= m_currentResources)
                {
                    selectedUnit = unit;
                    totalCost = totalCostUnit;
                }

                if (selectedUnit == null)
                {
                    selectedUnit = unit;
                    totalCost = totalCostUnit;
                }

            }

            if (selectedFactory == null)
                return;

            if (totalCost > m_currentResources)
            {
                while (count != 0)
                {
                    count--;
                    Unit unit = selectedUnit;

                    if (unit.UnitData.cost * count <= m_currentResources)
                        break;

                }
            }

            for (int i = 0; i < count; ++i)
            {
                captureSquadTask.m_unitCountProdution++;
                selectedFactory.RequestUnitProduction(selectedUnit.gameObject, action);
            }
        }

        if (availableSquad.Any())
        {
            var completeSquads = availableSquad.Where(squad => squad.Units.Count >= m_baseCaptureSquadUnitCount);

            //Already complete squad to capture target
            if (completeSquads.Any())
            {
                var closestSquad = completeSquads.OrderBy(squad => 
                (target.transform.position - squad.m_leaderComponent.transform.position).sqrMagnitude).First();

                captureSquadTask.m_units = closestSquad.Units;

                m_squadTasksScheduled.Add(captureSquadTask);
                return Action.EActionState.Finished;
            }
            else
            {
                var orderedSquads = availableSquad.OrderBy(squad => 
                (target.transform.position - squad.m_leaderComponent.transform.position).sqrMagnitude);

                List<Unit> units = new List<Unit>();
                foreach (var squad in orderedSquads)
                {
                    units.AddRange(squad.Units);

                    if (units.Count >= m_baseCaptureSquadUnitCount)
                        break;
                }

                captureSquadTask.m_units = units;

                if (units.Count < m_baseCaptureSquadUnitCount)
                    RequestUnitProductionForTask(m_baseCaptureSquadUnitCount - units.Count);

                m_squadTasksScheduled.Add(captureSquadTask);
                return Action.EActionState.Finished;
            }
        }

        RequestUnitProductionForTask(m_baseCaptureSquadUnitCount);

        if (captureSquadTask.m_unitCountProdution + captureSquadTask.m_units.Count == 0)
            return Action.EActionState.Failed;

        m_squadTasksScheduled.Add(captureSquadTask);

        return Action.EActionState.Finished;
    }

    [ActionMethod]
    public Action.EActionState Action_CaptureTarget(WorldState worldState)
    {
        SquadTask squadTask = null;

        foreach (SquadTask captureSquadTask in m_squadTasksScheduled)
            if (captureSquadTask.IsReady)
                squadTask = captureSquadTask;

        if (squadTask == null)
            return Action.EActionState.Failed;

        m_squadTasksScheduled.Remove(squadTask);

        if (squadTask.m_target == null || squadTask.m_target.Team == Team)
            return Action.EActionState.Failed;

        m_squadTaskInProgress.Add(squadTask);

        squadTask.m_unitSquad = CreateDynamicSquad(squadTask.m_units);
        squadTask.m_unitSquad.m_leaderComponent.SetTarget(squadTask.m_target, ETargetType.Capture);

        return Action.EActionState.Finished;
    }

    #endregion
}
