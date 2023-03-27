using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

namespace AIPlanner.GOAP
{
    public class StateType
    {
        private static List<Type> m_baseSupportedType = new List<Type>()
    {
        typeof(Vector2),
        typeof(Vector3),
    };

        public static List<Type> SupportedTypes
        {
            get
            {
                List<Type> supportedTypes = new List<Type>(GetTypesWithHelpAttribute(AppDomain.CurrentDomain.GetAssemblies()));
                supportedTypes.AddRange(m_baseSupportedType);
                return supportedTypes;
            }
        }

        public static List<string> SupportedTypesString
        {
            get
            {
                List<Type> supportedTypes = SupportedTypes;
                List<string> supportedTypesString = new List<string>();

                foreach (Type supportedType in supportedTypes)
                    supportedTypesString.Add(supportedType.FullName);

                return supportedTypesString;
            }

        }

        static IEnumerable<Type> GetTypesWithHelpAttribute(Assembly[] assemblies)
        {
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.GetCustomAttributes(typeof(StateTypeAttribute), true).Length > 0)
                    {
                        yield return type;
                    }
                }
            }
        }

        public static Type GetType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type != null)
                return type;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }
    }

    [Serializable]
    public class StateType<T> : StateType where T : unmanaged
    {
        public T Value;

        public StateType(T Value)
        {
            this.Value = Value;
        }

        public StateType() { }
    }

    [StateType]
    public class BoolType : StateType<bool>
    {
        public BoolType(bool Value) : base(Value) { }
        public BoolType() { }
    }

    [StateType]
    public class IntType : StateType<int>
    {
        public IntType(int Value) : base(Value) { }
        public IntType() { }
    }

    [StateType]
    public class FloatType : StateType<float>
    {
        public FloatType(float Value) : base(Value) { }
        public FloatType() { }
    }
}

