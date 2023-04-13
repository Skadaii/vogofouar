using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WheelMenu : MonoBehaviour
{
    // Variables

    [Header("Display settings")]
    [SerializeField] private float m_radius = 50f;
    [SerializeField] private float m_size = 1f;
    [SerializeField] private GameObject m_commandButton;
    [SerializeField] private Transform m_canvas;
    [SerializeField] private Transform m_disk;

    private List<Button> m_buttons = new List<Button>();
    private List<Entity.Command> m_commands = new List<Entity.Command>();
    private List<Entity> m_entities = new List<Entity>();

    private Vector2 clockDirection = Vector2.up;

    private int m_selectedIndex = 0;
    private object m_clickedObject;

    //Functions

    #region MonoBehaviour methods

    private void OnValidate()
    {
        if(m_disk) m_disk.localScale = Vector3.one * (m_size + m_radius * 2f);
    }

    private void Awake()
    {
        if (m_disk) m_disk.localScale = Vector3.one * (m_size + m_radius * 2f);
    }

    private void Update()
    {
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
        clockDirection = (Vector2)Input.mousePosition - screenPosition;

        if (m_buttons.Count > 0)
        {
            float angle = -Vector2.SignedAngle(Vector2.down, clockDirection) + 180f;
            m_selectedIndex = Mathf.RoundToInt(angle / 360f * m_buttons.Count)%m_buttons.Count;

            m_buttons[m_selectedIndex].Select();
        }
    }

    #endregion


    //  Show allowed actions 
    public void SetUnitWheel(List<Unit> selectedUnits, Entity entity)
    {
        Appear();

        //  Clear current wheel
        Clear();

        foreach (Unit unit in selectedUnits) 
        {
            //centerPosition += unit.transform.position;
            m_entities.Add(unit);
            foreach (Entity.Command command in unit.TypeCommands)
            {
                if(command as Entity.VoidCommand != null) TryAddCommand(command);
                else if(command as Entity.BuildCommand != null) TryAddCommand(command);
                else if(command as Entity.TargetCommand != null) TryAddCommand(command);
            }
        }

        //centerPosition /= (float)selectedUnits.Count;
        transform.position = entity.transform.position;
        m_clickedObject = entity;

        ConstructWheel();
    }

    public void SetUnitWheel(List<Unit> selectedUnits, Vector3 position)
    {
        Appear();

        //  Clear current wheel
        Clear();

        foreach (Unit unit in selectedUnits)
        {
            m_entities.Add(unit);
            //centerPosition += unit.transform.position;

            foreach (Entity.Command command in unit.TypeCommands)
            {
                if (command as Entity.VoidCommand != null) TryAddCommand(command);
                else if (command as Entity.BuildCommand != null) TryAddCommand(command);
                else if (command as Entity.LocationCommand != null) TryAddCommand(command);
            }
        }

        //centerPosition /= (float)selectedUnits.Count;
        transform.position = position;
        m_clickedObject = position;

        ConstructWheel();

    }

    public void ExecuteCommand()
    {
        foreach (Entity entity in m_entities)
        {
            m_commands[m_selectedIndex].ExecuteCommand(entity, m_clickedObject);
        }
    }

    public void SetBuildingWheel(Building building)
    {
        transform.position = building.transform.position;


        Appear();

        //  Clear current wheel
        Clear();

        m_entities.Add(building);

        foreach (Entity.Command command in building.TypeCommands)
        {
            if (command as Entity.VoidCommand != null) TryAddCommand(command);
            else if (command as Entity.BuildCommand != null) TryAddCommand(command);
            else if (command as Entity.LocationCommand != null) TryAddCommand(command);
        }

        m_clickedObject = null;
        ConstructWheel();
    }

    public void Clear()
    {
        m_entities.Clear();
        m_commands.Clear();

        for (int i = m_buttons.Count - 1; i >= 0; i--)
        {
            Destroy(m_buttons[i].gameObject);
            m_buttons.RemoveAt(i);
        }
    }

    private void ConstructWheel()
    {
        for (int i = 0; i < m_commands.Count; i++)
        {
            Entity.Command command = m_commands[i];

            float rad = i / (float)m_commands.Count * Mathf.PI * 2f;
            Vector2 pos = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * m_radius;
            Button button = Instantiate(m_commandButton, m_canvas).GetComponent<Button>();
            button.image.sprite = command.Icon;

            button.transform.localPosition = pos;
            RectTransform rt = button.GetComponent(typeof(RectTransform)) as RectTransform;
            if(rt != null) rt.sizeDelta = new Vector2(m_size, m_size);

            m_buttons.Add(button);
        }
    }

    private void TryAddCommand(Entity.Command command)
    {
        if (m_commands.Contains(command)) return;
        m_commands.Add(command);
    }

    private void TryRemoveCommand(Entity.Command command)
    {
        if (!m_commands.Contains(command)) return;
        m_commands.Remove(command);
    }

    public void Appear()
    {
        gameObject.SetActive(true);
        StartCoroutine(ScaleCoroutine(Vector3.zero, Vector3.one, 0.15f)); ;
    }

    public void Disappear()
    {
        StartCoroutine(ScaleCoroutine(Vector3.one, Vector3.zero, 0.15f, () => { Clear(); gameObject.SetActive(false); }));
    }

    #region Coroutine

    System.Collections.IEnumerator ScaleCoroutine(Vector3 from, Vector3 to, float duration, Action finishAction = null)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime+= Time.deltaTime;
            transform.localScale = Vector3.Lerp(from, to, elapsedTime / duration);
            yield return null;
        }

        if(finishAction != null) finishAction();
    }

    #endregion
}
