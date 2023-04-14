using UnityEngine;

public class FogOfWarManager : MonoBehaviour
{
	//	Variables
	//	---------

    private PlayerController m_controller;

	[SerializeField]
    private FogOfWarSystem m_FOWSystem;

    [SerializeField]
    private float m_updateFrequency = 0.05f;

    private float m_lastUpdateDate = 0f;

    //	Properties
    //	----------

    public ETeam Team => m_controller.Team;

    public FogOfWarSystem GetFogOfWarSystem
	{
		get { return m_FOWSystem; }
	}

	//	Functions
	//	---------

    void Start()
    {
        m_controller = FindObjectOfType<PlayerController>();
        m_FOWSystem.Init();
    }

    private void Update()
    {
		if ((Time.time - m_lastUpdateDate) > m_updateFrequency)
		{
			m_lastUpdateDate = Time.time;
			UpdateVisibilityTextures();
			UpdateFactoriesVisibility();
			UpdateUnitVisibility();
			UpdateBuildingVisibility();
		}
    }

	private void UpdateVisibilityTextures()
	{
		m_FOWSystem.ClearVisibility();
		m_FOWSystem.UpdateVisions(FindObjectsOfType<EntityVisibility>());
		m_FOWSystem.UpdateTextures(1 << (int)Team);
	}


	private void UpdateEntityVisibility(Entity entity)
	{
        if (entity.Visibility == null) { return; }

        if (m_FOWSystem.IsVisible(1 << (int)entity.Team, entity.Visibility.Position))
        {
            entity.Visibility.SetVisibleUI(true);
        }
        else
        {
            entity.Visibility.SetVisibleUI(false);
        }

        if (m_FOWSystem.IsVisible(1 << (int)entity.Team, entity.Visibility.Position))
		{
            entity.Visibility.SetVisible(true);
		}
		else
		{
            entity.Visibility.SetVisible(false);
        }
    }

	private void UpdateUnitVisibility()
	{
		foreach (Unit unit in GameServices.GetControllerByTeam(Team).UnitList)
		{
            if (unit.Visibility == null) { continue; }

            unit.Visibility.SetVisible(true);
		}

		foreach (Unit unit in GameServices.GetControllerByTeam(Team.GetOpponent()).UnitList)
		{
			//UpdateEntityVisibility(unit);
            if (unit.Visibility == null) { continue; }

            if (m_FOWSystem.IsVisible(1 << (int)Team, unit.Visibility.Position))
            {
            	unit.Visibility.SetVisible(true);
            }
            else
            {
            	unit.Visibility.SetVisible(false);
            }
        }
	}

	private void UpdateBuildingVisibility()
	{
		foreach (StaticBuilding building in GameServices.GetTargetBuildings())
        {
            //UpdateEntityVisibility(building);
			if (building.Visibility == null) { continue; }

			if (m_FOWSystem.IsVisible(1 << (int)Team, building.Visibility.Position))
			{
				building.Visibility.SetVisibleUI(true);
			}
			else
			{
				building.Visibility.SetVisibleUI(false);
			}

			if (m_FOWSystem.WasVisible(1 << (int)Team, building.Visibility.Position))
			{
	           building.Visibility.SetVisibleDefault(true);
			}
			else
			{
				building.Visibility.SetVisible(false);
			}
        }
	}

	private void UpdateFactoriesVisibility()
	{
		foreach (Factory factory in GameServices.GetControllerByTeam(Team).GetFactoryList)
		{
			factory.Visibility?.SetVisible(true);
		}

		foreach (Factory factory in GameServices.GetControllerByTeam(Team.GetOpponent()).GetFactoryList)
        {
            //UpdateEntityVisibility(factory);
			if (m_FOWSystem.IsVisible(1 << (int)Team, factory.Visibility.Position))
			{
				factory.Visibility.SetVisibleUI(true);
			}
			else
			{
			    factory.Visibility.SetVisibleUI(false);
			}

			if (m_FOWSystem.WasVisible(1 << (int)Team, factory.Visibility.Position))
			{
			    factory.Visibility.SetVisibleDefault(true);
			}
			else
			{
			    factory.Visibility.SetVisible(false);
			}
        }
	}
}
