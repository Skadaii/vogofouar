using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AIPlanner.GOAP
{
    [System.Serializable]
    public class Method
    {
        [SerializeField] private string m_methodName = string.Empty;
        [SerializeField] private string m_componentName = string.Empty;

        private Component m_component;
        protected MethodInfo m_methodInfo;

        public static void GetConsiderationMethods(GameObject GameObject, out Dictionary<string, string[]> OutActionMethods)
        {
            GetMethodWithAttribute<ConsiderationMethodAttribute>(GameObject, out OutActionMethods, CheckConsiderationMethodInfo);
        }

        public static void GetActionMethods(GameObject GameObject, out Dictionary<string, string[]> OutActionMethods)
        {
            GetMethodWithAttribute<ActionMethodAttribute>(GameObject, out OutActionMethods, CheckActionMethodInfo);
        }

        public static void GetStateMethods(GameObject GameObject, out Dictionary<string, string[]> OutStateMethods)
        {
            GetMethodWithAttribute<StateMethodAttribute>(GameObject, out OutStateMethods, CheckStateMethodInfo);
        }

        private static void GetMethodWithAttribute<T>(GameObject GameObject, out Dictionary<string, string[]> OutMethods, Func<MethodInfo, bool> CheckMethodAction) where T : Attribute
        {
            OutMethods = new Dictionary<string, string[]>();

            Component[] components = GameObject.GetComponents<Component>();

            foreach (Component component in components)
            {
                string componentName = component.GetType().Name;

                MethodInfo[] methodsInfo = component.GetType().GetMethods(BindingFlags.Public |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly);

                List<string> methodsName = new List<string>();
                foreach (MethodInfo methodInfo in methodsInfo)
                {
                    if (methodInfo.GetCustomAttribute<T>() != null)
                    {
                        if (CheckMethodAction != null && !CheckMethodAction.Invoke(methodInfo))
                            continue;

                        methodsName.Add(methodInfo.Name);
                    }
                }

                if (methodsName.Count > 0)
                    OutMethods.Add(componentName, methodsName.ToArray());
            }
        }

        private static bool CheckActionMethodInfo(MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType != typeof(Action.EActionState))
            {
                Debug.LogError($"The action method ({methodInfo.Name}) must return '{typeof(Action.EActionState).FullName}' ");
                return false;
            }

            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length != 1 || parameterInfos[0].ParameterType != typeof(WorldState))
            {
                Debug.LogError($"The action method ({methodInfo.Name}) parameter must be '{typeof(WorldState).FullName}' ");
                return false;
            }

            return true;
        }

        private static bool CheckStateMethodInfo(MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType == typeof(void))
            {
                Debug.LogError($"The state method ({methodInfo.Name}) must return a value");
                return false;
            }

            return true;
        }

        private static bool CheckConsiderationMethodInfo(MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType != typeof(float))
            {
                Debug.LogError($"The state method ({methodInfo.Name}) must return a float");
                return false;
            }

            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            if (parameterInfos.Length != 1 || parameterInfos[0].ParameterType != typeof(WorldState))
            {
                Debug.LogError($"The consideration method ({methodInfo.Name}) parameter must be '{typeof(WorldState).FullName}' ");
                return false;
            }

            return true;
        }

        public static Method CreateMethod(string ComponentName, string MethodName)
        {
            Method method = new Method();
            method.m_methodName = MethodName;
            method.m_componentName = ComponentName;

            return method;
        }

        public virtual void Initialize(GameObject GameObject)
        {
            if (string.IsNullOrEmpty(m_componentName) || string.IsNullOrEmpty(m_methodName))
                return;

            m_component = GameObject.GetComponent(m_componentName);
            m_methodInfo = m_component.GetType().GetMethod(m_methodName);
        }

        public bool IsValid()
        {
            return m_methodInfo != null && m_component != null;
        }

        public object Invoke(params object[] Parameters)
        {
            return m_methodInfo.Invoke(m_component, Parameters);
        }
    }
}