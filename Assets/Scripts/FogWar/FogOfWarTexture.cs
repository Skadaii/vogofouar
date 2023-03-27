using System;
using UnityEngine;

public class FogOfWarTexture : MonoBehaviour
{
    //  Variables
    //  ---------

    public Texture2D texture;

    [SerializeField]
    private Color m_greyColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    [SerializeField]
    protected Color m_whiteColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    [SerializeField]
    private Color m_startColor = new Color(0, 0, 0, 1.0f);

    [SerializeField]
    private SpriteRenderer m_spriteRenderer;

    [SerializeField]
    private float m_interpolateSpeed = 50f;

    [NonSerialized]
    private Color[] m_colors;

    //  Functions
    //  ---------

    private void Start()
    {
        m_spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void CreateTexture(int width, int height, Vector2 scale)
    {
        texture = new Texture2D(width, height);
        m_spriteRenderer.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1);
        m_spriteRenderer.transform.localScale = scale;

        int size = width * height;
        m_colors = new Color[size];
        for (int i = 0; i < size; ++i)
            m_colors[i] = m_startColor;

        texture.SetPixels(m_colors);
        texture.Apply();
    }

    public void SetTexture(Grid visibleGrid, Grid previousVisibleGrid, int team)
    {
        for (int i = 0; i < visibleGrid.Size; ++i)
        {
            bool isVisible = (visibleGrid.Get(i) & team) == team;
            bool wasVisible = (previousVisibleGrid.Get(i) & team) == team;

            Color newColor = m_startColor;
            if (isVisible)
                newColor = m_whiteColor;
            else if (wasVisible)
                newColor = m_greyColor;

            newColor.r = Mathf.Lerp(m_colors[i].r, newColor.r, Time.deltaTime * m_interpolateSpeed);
            m_colors[i] = newColor;
        }

        texture.SetPixels(m_colors);
        texture.Apply();
    }
}
