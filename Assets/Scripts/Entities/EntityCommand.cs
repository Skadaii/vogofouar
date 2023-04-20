using System;
using System.Xml.Serialization;
using UnityEngine;

public partial class  Entity
{
    public class Command
    {
        //  Variables

        protected string m_name = "Action";
        protected Sprite m_icon;

        //  Properties

        public string Name => m_name;
        public Sprite Icon => m_icon;

        //Functions

        public Command(string newActionName = "Action", Sprite icon = null)
        {
            m_name = newActionName;
            m_icon = icon;
        }


        public virtual void ExecuteCommand(Entity entity, object param = null)
        {
        }

        public virtual void ReverseCommand(Entity entity, object param = null)
        {
        }

        public virtual bool VerifyCommand(Entity entity, object param = null) => true;
    }


    public class TargetCommand : Command
    {
        public delegate bool VerificationDelegate(Entity entity, Entity target);

        Action<Entity, Entity> m_method;
        VerificationDelegate m_verification;

        public TargetCommand(string newActionName = "Action", Sprite icon = null, Action<Entity, Entity> newMethod = null, VerificationDelegate verificationMethod = null)
            : base(newActionName, icon)
        {
            m_method = newMethod;
            m_verification = verificationMethod;
        }

        public override void ExecuteCommand(Entity entity, object param = null)
        {
            if (param is not Entity) return;

            m_method?.Invoke(entity, (Entity)param);
        }

        public override bool VerifyCommand(Entity entity, object param = null)
        {
            if (!entity || param is not Entity) return false;

            return m_verification != null ? m_verification.Invoke(entity, (Entity)param) : true;
        }
    }

    public class LocationCommand : Command
    {
        public delegate bool VerificationDelegate(Entity entity, Vector3 location);

        Action<Entity, Vector3> m_method;
        VerificationDelegate m_verification;
        public LocationCommand(string newActionName = "Action", Sprite icon = null, Action<Entity, Vector3> newMethod = null, VerificationDelegate verificationMethod = null)
            : base(newActionName, icon)
        {
            m_method = newMethod;
            m_verification = verificationMethod;
        }

        public override void ExecuteCommand(Entity entity, object param = null)
        {
            if (param is not Vector3) return;

            m_method?.Invoke(entity, (Vector3)param);
        }

        public override bool VerifyCommand(Entity entity, object param = null)
        {
            if (!entity || param is not Vector3) return false;

            return m_verification != null ? m_verification.Invoke(entity, (Vector3)param) : true;
        }
    }

    public class VoidCommand : Command
    {
        public delegate bool VerificationDelegate(Entity entity);

        Action<Entity> m_method;
        VerificationDelegate m_verification;

        public VoidCommand(string newActionName = "Action", Sprite icon = null, Action<Entity> newMethod = null, VerificationDelegate verificationMethod = null)
            : base(newActionName, icon)
        {
            m_method = newMethod;
            m_verification = verificationMethod;
        }

        public override void ExecuteCommand(Entity entity, object param = null)
        {
            m_method?.Invoke(entity);
        }

        public override bool VerifyCommand(Entity entity, object param = null)
        {
            if (!entity) return false;

            return m_verification != null ? m_verification.Invoke(entity) : true;
        }
    }

    public class BuildCommand : Command
    {
        public delegate int CountDelegate(Entity entity, GameObject toBuild);
        public delegate bool VerificationDelegate(Entity entity, GameObject toBuild);

        //  Variables

        protected GameObject m_toBuild;

        public int count = 0;

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

        public override bool VerifyCommand(Entity entity, object param = null)
        {
            if (!entity) return false;

            return true;
        }
    }
}
