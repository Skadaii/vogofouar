using UnityEngine;
using UnityEngine.UI;

public class StaticBuilding : Building
{
    //  Variables
    //  ---------

    [SerializeField]
    private BuildingDataScriptable m_buildingDatas;

    [SerializeField]
    private float m_captureGaugeStart = 100f;
    [SerializeField]
    private float m_captureGaugeSpeed = 1f;
    [SerializeField]
    private int m_buildPoints = 5;
    [SerializeField]
    private Material m_blueTeamMaterial = null;
    [SerializeField]
    private Material m_redTeamMaterial = null;

    private Material m_neutralMaterial = null;
    private MeshRenderer m_buildingMeshRenderer = null;
    private Image m_gaugeImage;
    //private Image m_minimapImage;

    private int[] m_teamScore;
    private float m_captureGaugeValue;
    private ETeam m_owningTeam = ETeam.Neutral;
    private ETeam m_capturingTeam = ETeam.Neutral;
    //  Properties
    //  ----------

    public override BuildingDataScriptable BuildingData => m_buildingDatas;


    //  Functions
    //  ---------

    #region MonoBehaviour methods


    protected virtual new void Awake()
    {
        base.Awake();
        ConstructionCompleted();
    }

    protected virtual new void Start()
    {
        base.Start();

        m_isCompleted = true;
        m_isActive = true;

        m_buildingMeshRenderer = GetComponentInChildren<MeshRenderer>();
        m_neutralMaterial = m_buildingMeshRenderer.material;

        m_gaugeImage = GetComponentInChildren<Image>();
        if (m_gaugeImage)
            m_gaugeImage.fillAmount = 0f;
        m_captureGaugeValue = m_captureGaugeStart;
        m_teamScore = new int[2];
        m_teamScore[0] = 0;
        m_teamScore[1] = 0;

        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null)
            m_icon = iconTransform.GetComponentInChildren<SpriteRenderer>();
    }

    protected virtual new void Update()
    {
        base.Update();

        if (m_capturingTeam == m_owningTeam || m_capturingTeam == ETeam.Neutral)
            return;

        m_captureGaugeValue -= m_teamScore[(int)m_capturingTeam] * m_captureGaugeSpeed * Time.deltaTime;

        m_gaugeImage.fillAmount = 1f - m_captureGaugeValue / m_captureGaugeStart;

        if (m_captureGaugeValue <= 0f)
        {
            m_captureGaugeValue = 0f;
            OnCaptured(m_capturingTeam);
        }
    }

    #endregion


    #region Capture methods

    public void StartCapture(Unit unit)
    {
        if (unit == null)
            return;

        m_teamScore[(int)unit.Team] += unit.Cost;

        if (m_capturingTeam == ETeam.Neutral)
        {
            if (m_teamScore[(int)GameServices.GetOpponent(unit.Team)] == 0)
            {
                m_capturingTeam = unit.Team;
                m_gaugeImage.color = GameServices.GetTeamColor(m_capturingTeam);
            }
        }
        else
        {
            if (m_teamScore[(int)GameServices.GetOpponent(unit.Team)] > 0)
                ResetCapture();
        }
    }

    public void StopCapture(Unit unit)
    {
        if (unit == null)
            return;

        m_teamScore[(int)unit.Team] -= unit.Cost;
        if (m_teamScore[(int)unit.Team] == 0)
        {
            ETeam opponentTeam = GameServices.GetOpponent(unit.Team);
            if (m_teamScore[(int)opponentTeam] == 0)
            {
                ResetCapture();
            }
            else
            {
                m_capturingTeam = opponentTeam;
                m_gaugeImage.color = GameServices.GetTeamColor(m_capturingTeam);
            }
        }
    }

    void ResetCapture()
    {
        m_captureGaugeValue = m_captureGaugeStart;
        m_capturingTeam = ETeam.Neutral;
        m_gaugeImage.fillAmount = 0f;
    }

    void OnCaptured(ETeam newTeam)
    {
        Debug.Log("target captured by " + newTeam.ToString());
        if (m_owningTeam != newTeam)
        {
            UnitController teamController = GameServices.GetControllerByTeam(newTeam);
            if (teamController != null)
                teamController.CaptureTarget(m_buildPoints);

            if (m_owningTeam != ETeam.Neutral)
            {
                // remove points to previously owning team
                teamController = GameServices.GetControllerByTeam(m_owningTeam);
                if (teamController != null)
                    teamController.LoseTarget(m_buildPoints);
            }
        }

        ResetCapture();
        m_owningTeam = newTeam;
        if (Visibility) { Visibility.Team = m_owningTeam; }
        if (m_icon) { m_icon.color = GameServices.GetTeamColor(m_owningTeam); }
        m_buildingMeshRenderer.material = newTeam == ETeam.Blue ? m_blueTeamMaterial : m_redTeamMaterial;
    }

    #endregion
}
