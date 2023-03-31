using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIPlanner.GOAP
{
    [CustomEditor(typeof(GOAP))]
    public class GOAPEditor : Editor
    {
        private Button editPlannerButton;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            SerializedProperty serializeProperty = serializedObject.FindProperty("m_useDebugLog");
            PropertyField debugLogField = new PropertyField();
            debugLogField.BindProperty(serializeProperty);

            root.Add(debugLogField);
            root.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));

            editPlannerButton = new Button(delegate
            {
                GOAPWindowEditor window = EditorWindow.GetWindow<GOAPWindowEditor>();
                window.titleContent = new GUIContent("GOAP Editor");
                window.goap = target as GOAP;
                window.Show();
            });

            editPlannerButton.text = "Edit Planner";
            editPlannerButton.style.width = new Length(65f, LengthUnit.Percent);
            editPlannerButton.style.alignSelf = Align.Center;
            editPlannerButton.style.height = 30f;
            root.Add(editPlannerButton);


            return root;
        }
    }
}
