using System;
using UnityEngine;

public partial class  Entity
{
    public class Command
    {
        //  Variables

        protected string m_name = "Action";
        //protected string m_method = "";
        //protected string m_reverseMethod = "";
        protected Sprite m_icon;

        //  Properties

        public string Name => m_name;
        public Sprite Icon => m_icon;

        //public Command(string newActionName = "Action", string newMethod = "", string newReverseMethod = "", Sprite icon = null)
        public Command(string newActionName = "Action", Sprite icon = null)
        {
            m_name = newActionName;
            //m_method = newMethod;
            //m_reverseMethod = newReverseMethod;
            m_icon = icon;
        }


        public virtual void ExecuteCommand(Entity entity, object param = null)
        {
            //if (m_method == "") return;
            //if (param == null) entity.SendMessage(m_method, SendMessageOptions.DontRequireReceiver);
            //else entity.SendMessage(m_method, param, SendMessageOptions.DontRequireReceiver);
        }

        public virtual void ReverseCommand(Entity entity, object param = null)
        {
            //if (m_reverseMethod == "") return;
            //if (param == null) entity.SendMessage(m_reverseMethod, SendMessageOptions.DontRequireReceiver);
            //else entity.SendMessage(m_reverseMethod, param, SendMessageOptions.DontRequireReceiver);
        }
    }


    public class TargetCommand : Command
    {
        //public TargetCommand(string newActionName = "Action", string newMethod = "", string newReverseMethod = "", Sprite icon = null)
        //    : base(newActionName, newMethod, newReverseMethod, icon)
        //{ }

        Action<Entity, Entity> m_method;

        public TargetCommand(string newActionName = "Action", Sprite icon = null, Action<Entity, Entity> newMethod = null)
            : base(newActionName, icon)
        {
            m_method = newMethod;
        }

        public override void ExecuteCommand(Entity entity, object param = null)
        {
            if (param is not Entity) return;

            m_method?.Invoke(entity, (Entity)param);
        }
    }

    public class LocationCommand : Command
    {
        //public LocationCommand(string newActionName = "Action", string newMethod = "", string newReverseMethod = "", Sprite icon = null)
        //    : base(newActionName, newMethod, newReverseMethod, icon)
        //{ }

        Action<Entity, Vector3> m_method;

        public LocationCommand(string newActionName = "Action", Sprite icon = null, Action<Entity, Vector3> newMethod = null)
            : base(newActionName, icon)
        {
            m_method = newMethod;
        }

        public override void ExecuteCommand(Entity entity, object param = null)
        {
            if (param is not Vector3) return;

            m_method?.Invoke(entity, (Vector3)param);
        }
    }

    public class VoidCommand : Command
    {
        //public VoidCommand(string newActionName = "Action", string newMethod = "", string newReverseMethod = "", Sprite icon = null)
        //    : base(newActionName, newMethod, newReverseMethod, icon)
        //{ }

        Action<Entity> m_method;

        public VoidCommand(string newActionName = "Action", Sprite icon = null, Action<Entity> newMethod = null)
            : base(newActionName, icon)
        {
            m_method = newMethod;
        }

        public override void ExecuteCommand(Entity entity, object param = null)
        {
            m_method?.Invoke(entity);
        }
    }

    public class BuildCommand : Command
    {
        public delegate int CountDelegate(Entity entity, GameObject toBuild);

        //  Variables

        protected GameObject m_toBuild;

        public int count = 0;

        //public BuildCommand(string newActionName = "Action", string newMethod = "", string newReverseMethod = "", Sprite icon = null, GameObject toBuild = null)
        //    : base(newActionName, newMethod, newReverseMethod, icon)
        //{
        //    m_toBuild = toBuild;
        //}

        Action<Entity, GameObject> m_method;
        Action<Entity, GameObject> m_reverseMethod;
        CountDelegate m_countMethod;


        public BuildCommand(string newActionName = "Action", Sprite icon = null, Action<Entity, GameObject> newMethod = null, Action<Entity, GameObject> reverseMethod = null, CountDelegate countMethod = null, GameObject toBuild = null)
            : base(newActionName, icon)
        {
            m_toBuild = toBuild;
            m_method  = newMethod;
            m_reverseMethod = reverseMethod;
            m_countMethod   = countMethod;
        }


        public int GetCount(Entity entity)
        {
            if (m_countMethod == null) return 0;

            return m_countMethod(entity, m_toBuild);
        }


        public override void ExecuteCommand(Entity entity, object param = null)
        {
            m_method?.Invoke(entity, m_toBuild);
        }
        public override void ReverseCommand(Entity entity, object param = null)
        {
            m_reverseMethod?.Invoke(entity, m_toBuild);
        }

        //public override void ExecuteCommand(Entity entity, object param = null)
        //{
        //    if (m_method == "") return;
        //    entity.SendMessage(m_method, m_toBuild, SendMessageOptions.DontRequireReceiver);
        //}

        //public override void ReverseCommand(Entity entity, object param = null)
        //{
        //    if (m_reverseMethod == "") return;
        //    entity.SendMessage(m_reverseMethod, m_toBuild, SendMessageOptions.DontRequireReceiver);
        //}
    }
}
