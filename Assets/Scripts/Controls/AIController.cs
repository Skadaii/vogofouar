using AIPlanner;
using AIPlanner.GOAP;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.CanvasScaler;

public sealed class AIController : UnitController
{
    #region MonoBehaviour methods

    [SerializeField] private GOAP m_goap;
    [SerializeField] private UnitController m_enemyController;

    [SerializeField] private int m_baseCaptureSquadUnitCount = 3;
    [SerializeField] private int m_baseAttackSquadUnitCount = 3;

    [SerializeField] private InfluenceMap m_influenceMap;

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
        AttackFactory,
        BuildFactory,
    }

    private List<SquadTask> m_squadTasksScheduled = new List<SquadTask>();
    private List<SquadTask> m_squadTaskInProgress = new List<SquadTask>();

    private List<UnitSquad> AvailableSquad
    {
        get
        {
            var unavailableSquads = m_squadTasksScheduled.Select(squadTask => squadTask.m_unitSquad);
            unavailableSquads = unavailableSquads.Concat(m_squadTaskInProgress.Select(squadTask => squadTask.m_unitSquad));

            List<UnitSquad> availableSquad = m_squadList.ToList();

            foreach (UnitSquad unavailableSquad in unavailableSquads)
                availableSquad.Remove(unavailableSquad);

            return availableSquad;
        }
    }

    private List<Entity> GetTaskTargets(ETaskType taskType)
    {
        List<Entity> targets = new List<Entity>();

        foreach (SquadTask squadTask in m_squadTaskInProgress)
            if (squadTask.m_taskType == taskType)
                targets.Add(squadTask.m_target);

        foreach (SquadTask squadTask in m_squadTasksScheduled)
            if (squadTask.m_taskType == taskType)
                targets.Add(squadTask.m_target);

        return targets;
    }

    private int GetTaskCount(ETaskType taskType)
    {
        int taskCount = 0;

        foreach (SquadTask squadTask in m_squadTaskInProgress)
            if (squadTask.m_taskType == taskType)
                taskCount++;

        foreach (SquadTask squadTask in m_squadTasksScheduled)
            if (squadTask.m_taskType == taskType)
                taskCount++;

        return taskCount;
    }

    private void CancelTaskTarget(ETaskType taskType)
    {
        foreach (SquadTask squadTask in m_squadTaskInProgress)
        {
            if (squadTask.m_taskType == taskType)
            {
                m_squadTaskInProgress.Remove(squadTask);
                return;
            }
        }

        foreach (SquadTask squadTask in m_squadTasksScheduled)
        {
            if (squadTask.m_taskType == taskType)
            {
                m_squadTasksScheduled.Remove(squadTask);
                return;
            }
        }
    }

    private int AverageSquadCostEnemy
    {
        get
        {
            int totalCostAverage = 0;
            foreach (UnitSquad unitSquad in m_enemyController.SquadList)
            {
                foreach (Unit unit in unitSquad.Units)
                    totalCostAverage += unit.UnitData.cost;
            }

            if (totalCostAverage != 0)  
                totalCostAverage /= m_enemyController.SquadList.Count;

            Debug.LogWarning($"TotalCostAverage: {totalCostAverage}");
            return totalCostAverage;
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
        if (GetTaskCount(ETaskType.Capture) + CapturedTargets <= m_enemyController.CapturedTargets)
            return 1f;

        if (StaticBuilding.staticBuildings.Count > CapturedTargets)
            return 0.5f;

        return 0f;
    }

    [ConsiderationMethod]
    public float Goal_Attack(WorldState worldState)
    {
        foreach (SquadTask attackSquadTask in m_squadTasksScheduled)
            if (attackSquadTask.m_taskType == ETaskType.Attack && attackSquadTask.IsReady)
                return 1f;

        return 0f;
    }

    [ConsiderationMethod]
    public float Goal_PlanifyAttackTarget(WorldState worldState) => 
        m_enemyController.SquadList.Count - GetTaskCount(ETaskType.Attack) > 0 ? 1f : 0f;

    [ConsiderationMethod]
    public float Goal_PlanifyAttackFactoryTarget(WorldState worldState) =>
        m_enemyController.FactoryList.Count - GetTaskCount(ETaskType.AttackFactory) > 0 ? 1f : 0f;

    [ConsiderationMethod]
    public float Goal_AttackFactory(WorldState worldState)
    {
        foreach (SquadTask attackSquadTask in m_squadTasksScheduled)
            if (attackSquadTask.m_taskType == ETaskType.AttackFactory && attackSquadTask.IsReady)
                return 1f;

        return 0f;
    }

    [ConsiderationMethod]
    public float Goal_PlanifyBuildFactoryTarget(WorldState worldState)
    {
        return m_enemyController.FactoryList.Count >= FactoryList.Count &&
           GetTaskCount(ETaskType.BuildFactory) + FactoryList.Count <= m_enemyController.FactoryList.Count ? 1f : 0f;
    }

    [ConsiderationMethod]
    public float Goal_BuildFactory(WorldState worldState)
    {
        foreach (SquadTask attackSquadTask in m_squadTasksScheduled)
            if (attackSquadTask.m_taskType == ETaskType.BuildFactory && attackSquadTask.IsReady)
                return 1f;

        return 0f;
    }

    [StateMethod]
    public BoolType State_ResetState() => new BoolType(false);

    void RequestUnitProductionForTask(int count, SquadTask squadTask, bool onlyBuilder = false)
    {
        System.Action<Unit> action = (Unit unit) =>
        {
            if (squadTask != null)
            {
                squadTask.m_unitCountProdution--;
                squadTask.m_units.Add(unit);
            }
            else
            {
                CreateDynamicSquad(unit);
            }
        };

        int averageSquadCostEnemy = AverageSquadCostEnemy;

        int currentTotalCost = 0;
        foreach (Unit unit in squadTask.m_units)
            currentTotalCost += unit.UnitData.cost;

        Factory selectedFactory = null;
        float closestDistanceSqr = float.MaxValue;

        //TODO: Set TypeId according to the influence of the target
        int factoryTypeId = 0;

        foreach (Factory factory in FactoryList)
        {
            if (squadTask.m_target)
            {
                if (factory.FactoryData.typeId == factoryTypeId)
                {
                    float distanceSqr = (squadTask.m_target.transform.position - factory.transform.position).sqrMagnitude;
                    if (selectedFactory != null && distanceSqr > closestDistanceSqr)
                        continue;

                    selectedFactory = factory;
                    closestDistanceSqr = distanceSqr;
                }
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
            {
                if (!onlyBuilder)
                {
                    if (count != 1)
                    {
                        count--;
                        currentTotalCost += unit.Cost;
                        squadTask.m_unitCountProdution++;
                        selectedFactory.RequestUnitProduction(unit.gameObject, action);
                    }
                    continue;
                }

                selectedUnit = unit;
                totalCost = unit.Cost;
                break;
            }

            int totalCostUnit = unit.Cost * count;
            if (totalCostUnit <= m_currentResources)
            {
                //if (currentTotalCost + totalCostUnit <= averageSquadCostEnemy + averageSquadCostEnemy * 0.5f || selectedUnit == null)
                {
                    selectedUnit = unit;
                    totalCost = totalCostUnit;
                }
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
            squadTask.m_unitCountProdution++;
            selectedFactory.RequestUnitProduction(selectedUnit.gameObject, action);
        }
    }

    [ActionMethod]
    public Action.EActionState Action_PlanifyCaptureTarget(WorldState worldState)
    {
        //Select capture target
        List<StaticBuilding> targets = StaticBuilding.staticBuildings.Where(target => target.Team != Team).ToList();

        //Already planed capture target
        List<Entity> captureTaskTargets = GetTaskTargets(ETaskType.Capture);

        foreach (Entity entity in captureTaskTargets)
            targets.Remove(entity as StaticBuilding);

        if (!targets.Any() || FactoryList.Count == 0)
            return Action.EActionState.Failed;



        var target = targets.OrderBy(target => (target.transform.position - FactoryList[0].transform.position).sqrMagnitude).First();

        if (target == null)
            return Action.EActionState.Failed;

        //Todo: Check influence and potential enemy puisance

        SquadTask captureSquadTask = new SquadTask() { m_target = target, m_taskType = ETaskType.Capture };

        //Check unit available
        List<UnitSquad> availableSquad = AvailableSquad;

        //TODO: Find better algorithm to choose desired unit count
        int desiredUnitCount = m_baseCaptureSquadUnitCount;

        if (availableSquad.Any())
        {
            var completeSquads = availableSquad.Where(squad => squad.Units.Count >= desiredUnitCount);

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

                    if (units.Count >= desiredUnitCount)
                        break;
                }

                captureSquadTask.m_units = units;

                if (units.Count < desiredUnitCount)
                    RequestUnitProductionForTask(desiredUnitCount - units.Count, captureSquadTask);

                m_squadTasksScheduled.Add(captureSquadTask);
                return Action.EActionState.Finished;
            }
        }

        RequestUnitProductionForTask(desiredUnitCount, captureSquadTask);

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
        {
            if (captureSquadTask.IsReady && captureSquadTask.m_taskType == ETaskType.Capture)
            {
                squadTask = captureSquadTask;
                break;
            }
        }

        if (squadTask == null)
            return Action.EActionState.Failed;

        m_squadTasksScheduled.Remove(squadTask);

        if (squadTask.m_target == null || squadTask.m_target.Team == Team || !squadTask.m_units.Any())
            return Action.EActionState.Failed;

        m_squadTaskInProgress.Add(squadTask);

        squadTask.m_unitSquad = CreateDynamicSquad(squadTask.m_units.ToArray());
        squadTask.m_unitSquad.m_leaderComponent.SetTarget(squadTask.m_target, ETargetType.Capture);

        return Action.EActionState.Finished;
    }

    [ActionMethod]
    public Action.EActionState Action_PlanifyAttackTarget(WorldState worldState)
    {
        //Select capture target
        List<UnitSquad> targets = m_enemyController.SquadList.ToList();

        //Already planed attack target
        List<Entity> attackTaskTargets = GetTaskTargets(ETaskType.Attack);

        foreach (Entity entity in attackTaskTargets)
        {
            foreach (UnitSquad unitSquad in targets)
            {
                if (unitSquad.m_leaderComponent == entity)
                {
                    targets.Remove(unitSquad);
                    break;
                }
            }
        }
        if (!targets.Any() || FactoryList.Count == 0)
            return Action.EActionState.Failed;

        var target = targets.OrderBy(target => (target.m_leaderComponent.transform.position - FactoryList[0].transform.position).sqrMagnitude).First();

        if (target == null)
            return Action.EActionState.Failed;

        //Todo: Check influence and potential enemy puisance

        SquadTask attackSquadTask = new SquadTask() { m_target = target.m_leaderComponent, m_taskType = ETaskType.Attack };

        //Check unit available
        List<UnitSquad> availableSquad = AvailableSquad;

        int desiredUnitCount = target.Units.Count;

        if (availableSquad.Any())
        {
            var completeSquads = availableSquad.Where(squad => squad.Units.Count >= desiredUnitCount);

            //Already complete squad to capture target
            if (completeSquads.Any())
            {
                var closestSquad = completeSquads.OrderBy(squad =>
                (target.m_leaderComponent.transform.position - squad.m_leaderComponent.transform.position).sqrMagnitude).First();

                attackSquadTask.m_units = closestSquad.Units;

                m_squadTasksScheduled.Add(attackSquadTask);
                return Action.EActionState.Finished;
            }
            else
            {
                var orderedSquads = availableSquad.OrderBy(squad =>
                (target.m_leaderComponent.transform.position - squad.m_leaderComponent.transform.position).sqrMagnitude);

                List<Unit> units = new List<Unit>();
                foreach (var squad in orderedSquads)
                {
                    units.AddRange(squad.Units);

                    if (units.Count >= desiredUnitCount)
                        break;
                }

                attackSquadTask.m_units = units;

                if (units.Count < desiredUnitCount)
                {
                    if (GetTaskCount(ETaskType.AttackFactory) != 0)
                    {
                        CancelTaskTarget(ETaskType.AttackFactory);
                        return Action.EActionState.Loading;
                    }

                    RequestUnitProductionForTask(desiredUnitCount - units.Count, attackSquadTask);
                }

                m_squadTasksScheduled.Add(attackSquadTask);
                return Action.EActionState.Finished;
            }
        }

        RequestUnitProductionForTask(m_baseAttackSquadUnitCount, attackSquadTask);

        if (attackSquadTask.m_unitCountProdution + attackSquadTask.m_units.Count == 0)
            return Action.EActionState.Failed;

        m_squadTasksScheduled.Add(attackSquadTask);

        return Action.EActionState.Finished;
    }

    [ActionMethod]
    public Action.EActionState Action_AttackTarget(WorldState worldState)
    {
        SquadTask squadTask = null;

        foreach (SquadTask attackSquadTask in m_squadTasksScheduled)
        {
            if (attackSquadTask.IsReady && attackSquadTask.m_taskType == ETaskType.Attack)
            {
                squadTask = attackSquadTask;
                break;
            }
        }

        if (squadTask == null)
            return Action.EActionState.Failed;

        m_squadTasksScheduled.Remove(squadTask);

        if (squadTask.m_target == null || squadTask.m_target.Team == Team || !squadTask.m_units.Any())
            return Action.EActionState.Failed;

        m_squadTaskInProgress.Add(squadTask);

        squadTask.m_unitSquad = CreateDynamicSquad(squadTask.m_units.ToArray());
        squadTask.m_unitSquad.m_leaderComponent.SetTargetPosition(squadTask.m_target.transform.position);

        return Action.EActionState.Finished;
    }

    [ActionMethod]
    public Action.EActionState Action_PlanifyAttackFactoryTarget(WorldState worldState)
    {
        //Select capture target
        List<Factory> targets = m_enemyController.FactoryList;

        //Already planed attack target
        List<Entity> attackTaskTargets = GetTaskTargets(ETaskType.AttackFactory);

        foreach (Entity entity in attackTaskTargets)
            targets.Remove(entity as Factory);

        if (!targets.Any() || FactoryList.Count == 0)
            return Action.EActionState.Failed;

        var target = targets.OrderBy(target => (target.transform.position - FactoryList[0].transform.position).sqrMagnitude).First();

        if (target == null)
            return Action.EActionState.Failed;

        SquadTask attackSquadTask = new SquadTask() { m_target = target, m_taskType = ETaskType.AttackFactory };

        int desiredSquadCount = m_baseAttackSquadUnitCount;

        //Check unit available
        List<UnitSquad> availableSquad = AvailableSquad;

        if (availableSquad.Any())
        {
            var completeSquads = availableSquad.Where(squad => squad.Units.Count >= desiredSquadCount);

            //Already complete squad to capture target
            if (completeSquads.Any())
            {
                var closestSquad = completeSquads.OrderBy(squad =>
                (target.transform.position - squad.m_leaderComponent.transform.position).sqrMagnitude).First();

                attackSquadTask.m_units = closestSquad.Units;

                m_squadTasksScheduled.Add(attackSquadTask);
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

                    if (units.Count >= desiredSquadCount)
                        break;
                }

                attackSquadTask.m_units = units;

                if (units.Count < desiredSquadCount)
                    RequestUnitProductionForTask(desiredSquadCount - units.Count, attackSquadTask);

                m_squadTasksScheduled.Add(attackSquadTask);
                return Action.EActionState.Finished;
            }
        }

        RequestUnitProductionForTask(desiredSquadCount, attackSquadTask);

        if (attackSquadTask.m_unitCountProdution + attackSquadTask.m_units.Count == 0)
            return Action.EActionState.Failed;

        m_squadTasksScheduled.Add(attackSquadTask);

        return Action.EActionState.Finished;
    }

    [ActionMethod]
    public Action.EActionState Action_AttackFactoryTarget(WorldState worldState)
    {
        SquadTask squadTask = null;

        foreach (SquadTask attackSquadTask in m_squadTasksScheduled)
        {
            if (attackSquadTask.IsReady && attackSquadTask.m_taskType == ETaskType.AttackFactory)
            {
                squadTask = attackSquadTask;
                break;
            }
        }

        if (squadTask == null)
            return Action.EActionState.Failed;

        m_squadTasksScheduled.Remove(squadTask);

        if (squadTask.m_target == null || squadTask.m_target.Team == Team || !squadTask.m_units.Any())
            return Action.EActionState.Failed;

        m_squadTaskInProgress.Add(squadTask);

        squadTask.m_unitSquad = CreateDynamicSquad(squadTask.m_units.ToArray());
        squadTask.m_unitSquad.m_leaderComponent.SetTarget(squadTask.m_target, ETargetType.Attack);

        return Action.EActionState.Finished;
    }

    [ActionMethod]
    public Action.EActionState Action_PlanifyBuildFactoryTarget(WorldState worldState)
    {
        SquadTask buildSquadTask = new SquadTask() { m_target = null, m_taskType = ETaskType.BuildFactory };

        //Check unit available
        List<UnitSquad> availableSquad = AvailableSquad;

        if (availableSquad.Any())
        {
            var closestSquad = availableSquad.OrderBy(squad =>
                (squad.m_leaderComponent.transform.position - squad.m_leaderComponent.transform.position).sqrMagnitude).First();

            foreach (Unit unit in closestSquad.Units)
            {
                if (unit as Builder)
                {
                    buildSquadTask.m_units.Add(unit);

                    m_squadTasksScheduled.Add(buildSquadTask);
                    return Action.EActionState.Finished;
                }
            }
        }

        RequestUnitProductionForTask(1, buildSquadTask, true);

        if (buildSquadTask.m_unitCountProdution + buildSquadTask.m_units.Count == 0)
            return Action.EActionState.Failed;

        m_squadTasksScheduled.Add(buildSquadTask);

        return Action.EActionState.Finished;
    }

    [ActionMethod]
    public Action.EActionState Action_BuildFactoryTarget(WorldState worldState)
    {
        SquadTask buildSquadTask = null;

        foreach (SquadTask squadTask in m_squadTasksScheduled)
        {
            if (squadTask.IsReady && squadTask.m_taskType == ETaskType.BuildFactory)
            {
                buildSquadTask = squadTask;
                break;
            }
        }

        if (buildSquadTask == null || !buildSquadTask.m_units.Any())
            return Action.EActionState.Failed;

        m_squadTasksScheduled.Remove(buildSquadTask);

        buildSquadTask.m_unitSquad = CreateDynamicSquad(buildSquadTask.m_units.ToArray());

        if (buildSquadTask.m_units.Count != 0)
        {
            Builder builder = buildSquadTask.m_units[0] as Builder;

            if (!builder)
                return Action.EActionState.Failed;

            BuilderDataScriptable builderData = builder.UnitData as BuilderDataScriptable;
            int selectedBuildingId = Random.Range(0, builderData.availableBuildings.Length - 1);

            if (builderData.availableBuildings.Length != 0)
            {
                int currIt = 0;
                bool buildingRequested = false;
                while (currIt != 10)
                {
                    Vector3 position;
                    m_influenceMap.GetRandomPositionInInfluenceZone(Team, 0.2f, out position);
                    if (RequestBuildingConstruction(builderData.availableBuildings[selectedBuildingId], position, buildSquadTask.m_unitSquad))
                    {
                        buildingRequested = true;   
                        break;
                    }

                    currIt++;
                }

                if (!buildingRequested)
                    return Action.EActionState.Failed;
            }
        }

        m_squadTaskInProgress.Add(buildSquadTask);


        return Action.EActionState.Finished;
    }

    #endregion
}
