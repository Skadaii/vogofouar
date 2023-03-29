using System.IO;
using UnityEngine;

public enum ETeam
{
    Blue = 0,
    Red = 1,
    //Green,

    Neutral
}

[RequireComponent(typeof(GameState))]
public class GameServices : MonoBehaviour
{
    //  Variables
    //  ---------

    //[SerializeField, Tooltip("Generic material used for 3D models, in the following order : blue, red and green")]
    //private Material[] m_teamMaterials = new Material[3];

    [SerializeField, Tooltip("Unplayable terrain border size")]
    private float m_nonPlayableBorder = 100f;

    [SerializeField, Tooltip("Playable bounds size if no terrain is found")]
    private float m_defaultPlayableBoundsSize = 100f;

    private static GameServices m_instance = null;

    private UnitController[] m_controllersArray;
    private StaticBuilding[] m_targetBuildingArray;
    private GameState m_currentGameState = null;
 
    private Terrain m_currentTerrain = null;
    private Bounds m_playableBounds;

    //  Properties
    //  ----------

    public static float NonPlayableBorder => m_instance.m_nonPlayableBorder;
    public static Terrain Terrain => m_instance.m_currentTerrain;
    public static Bounds PlayableBounds => m_instance.m_playableBounds;

    public static Vector3 TerrainSize
    {
        get
        {
            if (m_instance.m_currentTerrain)
                return m_instance.m_currentTerrain.terrainData.bounds.size;

            return new Vector3(m_instance.m_defaultPlayableBoundsSize, 10.0f, m_instance.m_defaultPlayableBoundsSize);
        }
    }
    public static GameState GameState => m_instance.m_currentGameState;

    //  Functions
    //  ---------

    #region Static methods
    //public static GameServices GetGameServices() => m_instance;
    public static UnitController GetControllerByTeam(ETeam team)
    {
        if (m_instance.m_controllersArray.Length < (int)team)
            return null;
        return m_instance.m_controllersArray[(int)team];
    }
    //public static Material GetTeamMaterial(ETeam team) => m_instance.m_teamMaterials[(int)team];


    public static Material GetDefaultTeamMaterial(ETeam team)
    {
        switch(team)
        {
            case ETeam.Blue:
                return Resources.Load<Material>("Materials/M_Blue");
            case ETeam.Red:
                return Resources.Load<Material>("Materials/M_Red");
            default:
                return Resources.Load<Material>("Materials/M_Green"); 

        }
    }
    public static ETeam GetOpponent(ETeam team) => m_instance.m_currentGameState.GetOpponent(team);

    public static StaticBuilding[] GetTargetBuildings() => m_instance.m_targetBuildingArray;

    // return RGB color struct for each team
    public static Color GetTeamColor(ETeam team)
    {
        switch (team)
        {
            case ETeam.Blue:
                return Color.blue;
            case ETeam.Red:
                return Color.red;
            //case Team.Green:
            //    return Color.green;
            default:
                return Color.grey;
        }
    }
    
    public static bool IsPosInPlayableBounds(Vector3 pos) => PlayableBounds.Contains(pos);

    #endregion

    #region MonoBehaviour methods

    private void Awake()
    {
        if(m_instance != null)
        {
            Destroy(this);
            return;
        }

        m_instance = this;

        // Retrieve controllers from scene for each team
        m_controllersArray = new UnitController[2];
        foreach (UnitController controller in FindObjectsOfType<UnitController>())
        {
            m_controllersArray[(int)controller.Team] = controller;
        }

        // Store TargetBuildings
        m_targetBuildingArray = FindObjectsOfType<StaticBuilding>();

        // Store GameState ref
        if (m_currentGameState == null)
            m_currentGameState = GetComponent<GameState>();

        // Assign first found terrain
        foreach (Terrain terrain in FindObjectsOfType<Terrain>())
        {
            m_currentTerrain = terrain;
            //Debug.Log("terrainData " + CurrentTerrain.terrainData.bounds.ToString());
            break;
        }

        if (m_currentTerrain)
        {
            m_playableBounds = m_currentTerrain.terrainData.bounds;
            Vector3 clampedOne = new Vector3(1f, 0f, 1f);
            Vector3 heightReduction = Vector3.up * 0.1f; // $$ hack : this is to prevent selectioning / building in high areas
            m_playableBounds.SetMinMax(m_playableBounds.min + clampedOne * m_nonPlayableBorder / 2f, m_playableBounds.max - clampedOne * m_nonPlayableBorder / 2f - heightReduction);
        }
        else
        {
            Debug.LogWarning("could not find terrain asset in scene, setting default PlayableBounds");
            Vector3 clampedOne = new Vector3(1f, 0f, 1f);
            m_playableBounds.SetMinMax(   new Vector3(-m_defaultPlayableBoundsSize, -10.0f, -m_defaultPlayableBoundsSize) + clampedOne * m_nonPlayableBorder / 2f,
                                        new Vector3(m_defaultPlayableBoundsSize, 10.0f, m_defaultPlayableBoundsSize) - clampedOne * m_nonPlayableBorder / 2f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(m_playableBounds.center, m_playableBounds.size);
    }
    #endregion
}
