using UnityEngine;

public class EntityVisibility : MonoBehaviour
{
    //  Variables
    //  ---------

    public ETeam team;
    public float range;

    private bool m_isVisibleDefault = true;
    private bool m_isVisibleUI = true;

    [SerializeField]
    private GameObject[] m_gameObjectDefaultLayer;
    [SerializeField]
    private GameObject[] m_gameObjectUILayer;
    [SerializeField]
    private GameObject[] m_gameObjectMinimapLayer;

    //  Properties
    //  ----------

    public Vector2 Position => new Vector2(transform.position.x, transform.position.z);

    //  Functions
    //  ---------

    public void SetVisible(bool visible)
	{
        SetVisibleDefault(visible);
        SetVisibleUI(visible);
    }

    public void SetVisibleUI(bool visible)
	{
        if (m_isVisibleUI == visible)
            return;

        if (visible)
		{
            m_isVisibleUI = true;
            if (m_gameObjectUILayer.Length > 0)
            {
                SetLayer(m_gameObjectUILayer[0], LayerMask.NameToLayer("UI"));
            }
        }
        else
        {
            m_isVisibleUI = false;
            if (m_gameObjectUILayer.Length > 0)
            {
                SetLayer(m_gameObjectUILayer[0], LayerMask.NameToLayer("Hidden"));
            }
        }
    }

    public void SetVisibleDefault(bool visible)
    {
        if (m_isVisibleDefault == visible)
            return;

        if (visible)
        {
            m_isVisibleDefault = true;
            if (m_gameObjectDefaultLayer.Length > 0)
            {
                SetLayer(m_gameObjectDefaultLayer[0], LayerMask.NameToLayer("Default"));
            }
            if (m_gameObjectMinimapLayer.Length > 0)
            {
                SetLayer(m_gameObjectMinimapLayer[0], LayerMask.NameToLayer("Minimap"));
            }
        }
        else
        {
            m_isVisibleDefault = false;
            if (m_gameObjectDefaultLayer.Length > 0)
            {
                SetLayer(m_gameObjectDefaultLayer[0], LayerMask.NameToLayer("Hidden"));
            }
            if (m_gameObjectMinimapLayer.Length > 0)
            {
                SetLayer(m_gameObjectMinimapLayer[0], LayerMask.NameToLayer("Hidden"));
            }
        }
    }

    void SetLayer(GameObject root, int newLayer)
    {
        root.layer = newLayer;

        foreach (Transform t in root.transform)
        {
            SetLayer(t.gameObject, newLayer);
        }
    }
}
