using UnityEngine;

public class FogOfWarSystem : MonoBehaviour
{
    //  Variables
    //  ---------

    public int gridWidth;
    public int gridHeight;

    [SerializeField]
    protected FogOfWarTexture m_fogTexture;
    [SerializeField]
    protected Camera m_fogCamera;
    [SerializeField]
    protected Transform m_fogQuadParent;

    private Grid m_visibilityGrid;
    private Grid m_previousVisibilityGrid;
    private Vector2 m_textureScale;

    //  Functions
    //  ---------

    public void Init()
    {
        m_textureScale = new Vector2(m_fogQuadParent.localScale.x / gridWidth,
                                   m_fogQuadParent.localScale.y / gridHeight);
        m_fogTexture.CreateTexture(gridWidth, gridHeight, m_textureScale);

        m_visibilityGrid = new Grid(gridWidth, gridHeight, 0);
        m_previousVisibilityGrid = new Grid(gridWidth, gridHeight, 0);

        m_fogQuadParent.gameObject.SetActive(true);
    }

    private Vector2Int GetPositionInGrid(Vector2 p)
    {
        return new Vector2Int
        {
            x = Mathf.RoundToInt(p.x * gridWidth / m_fogQuadParent.localScale.x),
            y = Mathf.RoundToInt(p.y * gridHeight / m_fogQuadParent.localScale.y)
        };
    }

    private void SetCell(int team, int x, int y)
    {  
        if (!m_visibilityGrid.Contains(x, y))
            return;
        if ((m_visibilityGrid.values[x + y * m_visibilityGrid.height] & team) > 0)
            return;

        m_visibilityGrid.values[x + y * m_visibilityGrid.width] |= team;
        m_previousVisibilityGrid.values[x + y * m_previousVisibilityGrid.width] |= team;
    }

    public void ClearVisibility()
    {
        m_visibilityGrid.Clear();
    }

    public void UpdateVisions(EntityVisibility[] visibilities)
    {
        foreach (EntityVisibility v in visibilities)
        {
            Vector2Int gridPos = GetPositionInGrid(v.Position);
            
            int radius = Mathf.FloorToInt(v.range / m_textureScale.x) - 1;
            if (radius <= 0)
                return;

            int x = radius;
            int y = 0;
            int xTemp = 1 - (radius * 2);
            int yTemp = 0;
            int radiusTemp = 0;

            while (x >= y)
            {
                for (int j = gridPos.x - x; j <= gridPos.x + x; ++j)
                {
                    SetCell(1 << (int)v.Team, j, gridPos.y + y);
                    SetCell(1 << (int)v.Team, j, gridPos.y - y);
                }
                for (int j = gridPos.x - y; j <= gridPos.x + y; ++j)
                {
                    SetCell(1 << (int)v.Team, j, gridPos.y + x);
                    SetCell(1 << (int)v.Team, j, gridPos.y - x);
                }

                ++y;
                radiusTemp += yTemp;
                yTemp += 2;

                if (((radiusTemp * 2) + xTemp) > 0)
                {
                    x--;
                    radiusTemp += xTemp;
                    xTemp += 2;
                }
            }
        }
    }

    public void UpdateTextures(int team)
    {
        m_fogTexture.SetTexture(m_visibilityGrid, m_previousVisibilityGrid, team);
    }

    public bool IsVisible(int team, Vector2 position)
    {
        Vector2Int posGrid = GetPositionInGrid(position);
        return m_visibilityGrid.IsValue(team, posGrid.x, posGrid.y);
    }

    public bool WasVisible(int team, Vector2 position)
    {
        Vector2Int posGrid = GetPositionInGrid(position);
        return m_previousVisibilityGrid.IsValue(team, posGrid.x, posGrid.y);
    }
}
