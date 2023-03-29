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
