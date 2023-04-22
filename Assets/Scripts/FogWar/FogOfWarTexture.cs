using System;
using UnityEngine;

public class FogOfWarTexture : MonoBehaviour
{
    //  Variables
    //  ---------

    [HideInInspector] public Texture2D texture;

    private SpriteRenderer m_spriteRenderer;

    [SerializeField]
    private float m_interpolateSpeed = 50f;

    [NonSerialized]
    private Color[] m_colors;

    //  Functions
    //  ---------

    private void Awake()
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
            m_colors[i] = Color.black;

        texture.SetPixels(m_colors);
        texture.Apply();
    }

    public void SetTexture(Grid visibleGrid, Grid previousVisibleGrid, int team)
    {
        for (int i = 0; i < visibleGrid.Size; ++i)
        {
            bool isVisible = (visibleGrid.Get(i) & team) == team;
            bool wasVisible = (previousVisibleGrid.Get(i) & team) == team;

            Color newColor = Color.black;
            if (isVisible)
                newColor = Color.white;
            else if (wasVisible)
                newColor = Color.gray;

            newColor.r = Mathf.Lerp(m_colors[i].r, newColor.r, Time.deltaTime * m_interpolateSpeed);
            m_colors[i] = newColor;
        }

        texture.SetPixels(m_colors);
        texture.Apply();
    }
}
