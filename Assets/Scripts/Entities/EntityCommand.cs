using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class  Entity
{
    public class Command
    {
        //  Variables

        protected string m_name = "Action";
        protected string m_method = "";
        protected Sprite m_icon;

        //  Properties

        public string Name => m_name;
        public string Method => m_method;
        public Sprite Icon => m_icon;


        public Command(string newActionName = "Action", string newMethod = "", Type type = null, Sprite icon = null)
        {
            m_name = newActionName;
            m_method = newMethod;
            m_icon = icon;
        }

        public bool ExecuteCommand(Entity entity, object param = null)
        {
            try
            {
                if (param == null) entity.SendMessage(m_method);
                else entity.SendMessage(m_method, param);
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"Command could not be executed in entity {entity}");
                return false;
            }
        }
    }


    public class TargetCommand : Command
    {
        public TargetCommand(string newActionName = "Action", string newMethod = "", Type type = null, Sprite icon = null)
            : base(newActionName = "Action", newMethod, type, icon)
        { }
    }

    public class LocationCommand : Command
    {
        public LocationCommand(string newActionName = "Action", string newMethod = "", Type type = null, Sprite icon = null)
            : base(newActionName = "Action", newMethod, type, icon)
        { }
    }
    public class VoidCommand : Command
    {
        public VoidCommand(string newActionName = "Action", string newMethod = "", Type type = null, Sprite icon = null)
            : base(newActionName = "Action", newMethod, type, icon)
        { }
    }
}
