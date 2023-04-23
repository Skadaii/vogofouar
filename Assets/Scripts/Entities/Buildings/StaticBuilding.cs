using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.CanvasScaler;

public class StaticBuilding : Building
{
    //  Variables
    //  ---------

    [SerializeField]
    private BuildingDataScriptable m_buildingDatas;

    [SerializeField]
    private float m_requiredCapturePoint = 100f;

    private Material m_neutralMaterial = null;
    private MeshRenderer m_buildingMeshRenderer = null;

    private float[] m_teamCaptureScore = { 0, 0 };

    //  TODO : Create scriptable object for static buildingss ?
    private float m_resourcePerSecond = 1f;
    private float m_capturePointDecreasePerSecond = 1f;
    private float m_capturePoints = 1f;

    public static List<StaticBuilding> staticBuildings = new List<StaticBuilding>();

    //  Properties
    //  ----------
    public override BuildingDataScriptable BuildingData => m_buildingDatas;

    private ETeam CapturingTeam => m_teamCaptureScore[(int)ETeam.Blue] > m_teamCaptureScore[(int)ETeam.Red] ? ETeam.Blue : 
        (m_teamCaptureScore[(int)ETeam.Blue] < m_teamCaptureScore[(int)ETeam.Red] ? ETeam.Red : ETeam.Neutral);

    //  Functions
    //  ---------

    #region MonoBehaviour methods

    protected virtual new void Awake()
    {
        base.Awake();
        ConstructionCompleted();

        staticBuildings.Add(this);
    }

    protected virtual new void Start()
    {
        base.Start();

        m_isCompleted = true;
        m_isActive = true;

        m_buildingMeshRenderer = GetComponentInChildren<MeshRenderer>();
        m_neutralMaterial = m_buildingMeshRenderer.material;


        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null)
            m_icon = iconTransform.GetComponentInChildren<SpriteRenderer>();
    }

    protected virtual new void Update()
    {
        base.Update();

        if (Team != ETeam.Neutral)
        {
            TeamController.CurrentResources += m_resourcePerSecond * Time.deltaTime;

            if (m_teamCaptureScore[(int)Team] == 0f)
            {
                UnitController teamController = GameServices.GetControllerByTeam(Team);
                teamController.CapturedTargets--;

                m_team = ETeam.Neutral;
                if (Visibility) { Visibility.Team = Team; }
                if (m_icon) { m_icon.color = GameServices.GetTeamColor(Team); }
                m_buildingMeshRenderer.material = GameServices.GetDefaultTeamMaterial(ETeam.Neutral);
            }
        }

        if ((Team == ETeam.Neutral && CapturingTeam == Team)  || (CapturingTeam == Team && m_teamCaptureScore[(int)CapturingTeam] == 100f))
        {
            m_hud.Progression = 0f;
        }
        else
        {
            m_hud.highProgressColor = GameServices.GetTeamColor(CapturingTeam);
            m_capturePoints = m_teamCaptureScore[(int)CapturingTeam];
            float percent = m_capturePoints / m_requiredCapturePoint;
            m_hud.Progression = percent;

            if(percent >= 1f)
                OnCaptured(CapturingTeam);
        }
    }

    #endregion

    #region Capture methods


    public void ComputeCapture(Unit unit)
    {
        if (CapturingTeam == unit.Team || CapturingTeam == ETeam.Neutral)
        {
            m_teamCaptureScore[(int)unit.Team] = Mathf.Min(m_teamCaptureScore[(int)unit.Team] + unit.UnitData.capturePointPerSecond * Time.deltaTime, m_requiredCapturePoint);
        }
        else
        {
            ETeam otherTeam = Team != ETeam.Blue ? ETeam.Red : ETeam.Blue;
            m_teamCaptureScore[(int)otherTeam] = Mathf.Max(m_teamCaptureScore[(int)otherTeam] - unit.UnitData.capturePointPerSecond * Time.deltaTime, 0f);
        }
    }

    void OnCaptured(ETeam newTeam)
    {
        Debug.Log("target captured by " + newTeam.ToString());
        if (Team != newTeam)
        {
            UnitController teamController = GameServices.GetControllerByTeam(newTeam);
            teamController.CapturedTargets++;

            //m_teamCaptureScore = new float[] { 0f, 0f};

            if (Team != ETeam.Neutral)
            {
                // remove points to previously owning team
                teamController = GameServices.GetControllerByTeam(Team);
                teamController.CapturedTargets--;
            }
        }

        m_team = newTeam;
        if (Visibility) { Visibility.Team = Team; }
        if (m_icon) { m_icon.color = GameServices.GetTeamColor(Team); }
        m_buildingMeshRenderer.material = GameServices.GetDefaultTeamMaterial(newTeam);
    }

    #endregion
}
