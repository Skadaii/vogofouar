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


        public Command(string newActionName = "Action", string newMethod = "", Sprite icon = null)
        {
            m_name = newActionName;
            m_method = newMethod;
            m_icon = icon;
        }

        public virtual void ExecuteCommand(Entity entity, object param = null)
        {
            if (param == null) entity.SendMessage(m_method, SendMessageOptions.DontRequireReceiver);
            else entity.SendMessage(m_method, param, SendMessageOptions.DontRequireReceiver);
        }
    }


    public class TargetCommand : Command
    {
        public TargetCommand(string newActionName = "Action", string newMethod = "", Sprite icon = null)
            : base(newActionName, newMethod, icon)
        { }
    }

    public class LocationCommand : Command
    {
        public LocationCommand(string newActionName = "Action", string newMethod = "", Sprite icon = null)
            : base(newActionName, newMethod, icon)
        { }
    }

    public class VoidCommand : Command
    {
        public VoidCommand(string newActionName = "Action", string newMethod = "", Sprite icon = null)
            : base(newActionName, newMethod, icon)
        { }
    }

    public class BuildCommand : Command
    {
        //  Variables

        protected GameObject m_toBuild;

        public BuildCommand(string newActionName = "Action", string newMethod = "", Sprite icon = null, GameObject toBuild = null)
            : base(newActionName, newMethod, icon)
        {
            m_toBuild = toBuild;
        }

        public override void ExecuteCommand(Entity entity, object param = null)
        {
            entity.SendMessage(m_method, m_toBuild, SendMessageOptions.DontRequireReceiver);
        }
    }
}
