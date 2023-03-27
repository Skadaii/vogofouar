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
    [CustomEditor(typeof(GOAPEditor))]
    public class GOAPEditor : Editor
    {
        private VisualElement m_root;

        class FoldoutEditor
        {
            public Foldout ShowFoldout;
        }

        class MethodEditor : FoldoutEditor
        {
            public SerializedProperty ComponentNameProperty;
            public PopupField<string> ComponentPopupField;

            public SerializedProperty MethodNameProperty;
            public PopupField<string> MethodPopupField;
        }

        class StateEditor : MethodEditor
        {
            public Button RemoveStateButton;

            public SerializedProperty StateNameProperty;
            public SerializedProperty ValueProperty;
            public PropertyField ValueField;
        }

        class WorldStateEditor
        {
            public SerializedProperty WorldStateProperty;
            public SerializedProperty StatesProperty;

            public List<StateEditor> StateEditors;

            public Button AddStateButton;
            public PopupField<string> StatePopupField;
            public SerializedProperty StateNameProperty;
            public PropertyField StateNameField;
        }

        class StateIdEditor : FoldoutEditor
        {
            public Button RemoveStateIdButton;

            public SerializedProperty StateIdProperty;
            public SerializedProperty IdOfStateProperty;
            public SerializedProperty ValueProperty;
            public PropertyField ValueField;

            public static Dictionary<int, List<StateIdEditor>> AllStateIdEditor;
        }

        class StateIdListEditor : FoldoutEditor
        {
            public Button AddStateIdButton;

            public PopupField<string> StateTypePopup;

            public SerializedProperty StateIdsProperty;
            public List<StateIdEditor> StateIdEditors;
        }

        class PreconditionEditor : FoldoutEditor
        {
            public Button RemovePreconditionButton;

            public StateIdListEditor StateIdListEditor;

            public SerializedProperty CostProperty;
            public PropertyField CostField;
        }

        class PreconditionListEditor : FoldoutEditor
        {
            public Button AddPreconditionButton;

            public SerializedProperty PreconditionsProperty;
            public List<PreconditionEditor> PreconditionEditors;
        }

        class ActionEditor : MethodEditor
        {
            public SerializedProperty ActionProperty;
            public Button RemoveActionButton;

            public StateIdListEditor StateIdListEditor;
            public PreconditionListEditor PreconditionListEditor;
        }

        class ActionSetEditor
        {
            public SerializedProperty ActionSetProperty;

            public List<ActionEditor> ActionEditors;

            public Button AddActionButton;
            public TextField ActionNameField;
        }

        class GoalEditor : MethodEditor
        {
            public StateIdListEditor StateEditor;

            public SerializedProperty AnimationCurveProperty;
            public CurveField CurveField;
            public TextElement TextElement;

            public Button RemoveGoalButton;
        }

        class GoalListEditor
        {
            public Button AddGoalButton;

            public SerializedProperty GoalsProperty;
            public List<GoalEditor> GoalEditors;
        }

        private AIPlanner m_aiPlanner;

        private WorldStateEditor m_worldStateEditor;
        private ActionSetEditor m_actionSetEditor;
        //private StateIdListEditor m_goalEditor;
        private GoalListEditor m_goalListEditor;

        private Dictionary<string, string[]> m_stateMethods;
        private Dictionary<string, string[]> m_actionMethods;
        private Dictionary<string, string[]> m_considerationMethods;
        private List<string> m_supportedTypesString;

        public override VisualElement CreateInspectorGUI()
        {
            m_root = new VisualElement();
            m_aiPlanner = target as AIPlanner;

            Initialize();
            Compose();

            return m_root;
        }

        private void Initialize()
        {
            Method.GetStateMethods(m_aiPlanner.gameObject, out m_stateMethods);
            Method.GetActionMethods(m_aiPlanner.gameObject, out m_actionMethods);
            Method.GetConsiderationMethods(m_aiPlanner.gameObject, out m_considerationMethods);
            m_supportedTypesString = StateType.SupportedTypesString;

            InitializeWorldState();
            InitializeActionSet();
            InitializeGoalListEditor();
        }

        private void Compose()
        {
            m_root.Clear();

            ComposeWorldState();
            ComposeActionSet();
            ComposeGoal();
        }

        private void SetMethodPopupField(MethodEditor MethodEditor, in Dictionary<string, string[]> InMethods)
        {
            if (MethodEditor.ComponentNameProperty.stringValue != string.Empty)
            {
                string[] methods;
                InMethods.TryGetValue(MethodEditor.ComponentNameProperty.stringValue, out methods);

                if (methods != null && methods.Length != 0)
                {
                    string methodName = MethodEditor.MethodNameProperty.stringValue;
                    List<string> methodsName = methods.ToList();

                    MethodEditor.MethodPopupField.choices = methodsName;

                    MethodEditor.MethodPopupField.index = GetListStringIndex(methodsName, methodName);
                }
                else
                {
                    MethodEditor.MethodPopupField.choices = new List<string>();
                    MethodEditor.MethodPopupField.index = 0;
                }
            }
        }

        private int GetListStringIndex(in List<string> ListString, string str)
        {
            int id = ListString.IndexOf(str);

            if (id == -1)
                id = 0;

            return id;
        }

        private void InitializeWorldState()
        {
            m_worldStateEditor = new WorldStateEditor();
            m_worldStateEditor.StateEditors = new List<StateEditor>();

            m_worldStateEditor.WorldStateProperty = serializedObject.FindProperty(nameof(AIPlanner.WorldState));

            m_worldStateEditor.StateNameProperty = m_worldStateEditor.WorldStateProperty.FindPropertyRelative(nameof(WorldState.StateName));
            m_worldStateEditor.StateNameField = new PropertyField(m_worldStateEditor.StateNameProperty);

            m_worldStateEditor.StatePopupField = new PopupField<string>("Type", m_supportedTypesString, 0);
            m_worldStateEditor.AddStateButton = new Button(delegate
            {
                string stateName = m_worldStateEditor.StateNameProperty.stringValue;
                string selectedTypeName = m_worldStateEditor.StatePopupField.choices[m_worldStateEditor.StatePopupField.index];

                if (stateName == string.Empty && selectedTypeName == string.Empty)
                    return;

                foreach (StateEditor stateEditor in m_worldStateEditor.StateEditors)
                    if (stateEditor.StateNameProperty.stringValue == stateName)
                        return;

                Type type = StateType.GetType(selectedTypeName);

                State state = new State();
                state.Name = m_worldStateEditor.StateNameProperty.stringValue;
                object value = Activator.CreateInstance(type);
                state.StateValue = new StateValue();
                state.StateValue.Value = value;

                m_aiPlanner.WorldState.States.Add(state);
                serializedObject.Update();

                InitializeWorldStateList();

                foreach (ActionEditor actionEditor in m_actionSetEditor.ActionEditors)
                {
                    InitializeAddStateIdButton(actionEditor.StateIdListEditor);

                    foreach (PreconditionEditor preconditionEditor in actionEditor.PreconditionListEditor.PreconditionEditors)
                    {
                        InitializeAddStateIdButton(preconditionEditor.StateIdListEditor);
                    }
                }

                foreach (GoalEditor goalEditor in m_goalListEditor.GoalEditors)
                    InitializeAddStateIdButton(goalEditor.StateEditor);

                Compose();
            });
            m_worldStateEditor.AddStateButton.text = "Add";

            InitializeWorldStateList();
        }
        private void InitializeWorldStateList()
        {
            m_worldStateEditor.StateEditors.Clear();

            if (m_aiPlanner.WorldState.States == null)
                m_aiPlanner.WorldState.States = new List<State>();

            m_worldStateEditor.StatesProperty = m_worldStateEditor.WorldStateProperty.FindPropertyRelative(nameof(WorldState.States));

            int stateCount = m_aiPlanner.WorldState.States.Count;

            for (int i = 0; i < stateCount; ++i)
            {
                StateEditor stateEditor = new StateEditor();

                SerializedProperty stateProperty = m_worldStateEditor.StatesProperty.GetArrayElementAtIndex(i);

                stateEditor.StateNameProperty = stateProperty.FindPropertyRelative(nameof(State.Name));
                stateEditor.ShowFoldout = EditorUtils.CreateFoldout(stateEditor.StateNameProperty.stringValue, 15, Color.white, FlexDirection.Column);
                stateEditor.ShowFoldout.value = false;

                SerializedProperty stateValueProperty = stateProperty.FindPropertyRelative(nameof(State.StateValue));
                SerializedProperty valueProperty = stateValueProperty.FindPropertyRelative("m_value");

                stateEditor.ValueProperty = valueProperty;
                stateEditor.ValueField = new PropertyField();
                stateEditor.ValueField.BindProperty(valueProperty);

                SerializedProperty methodProperty = stateProperty.FindPropertyRelative("m_stateMethod");
                SerializedProperty componentNameProperty = methodProperty.FindPropertyRelative("m_componentName");

                stateEditor.ComponentNameProperty = componentNameProperty;
                string componentName = stateEditor.ComponentNameProperty.stringValue;
                List<string> componentNames = m_stateMethods.Keys.ToList();
                stateEditor.ComponentPopupField = new PopupField<string>("Component", componentNames, GetListStringIndex(componentNames, componentName));
                stateEditor.ComponentPopupField.BindProperty(stateEditor.ComponentNameProperty);
                stateEditor.ComponentPopupField.RegisterValueChangedCallback(delegate { SetMethodPopupField(stateEditor, m_stateMethods); });

                SerializedProperty methodNameProperty = methodProperty.FindPropertyRelative("m_methodName");

                stateEditor.MethodNameProperty = methodNameProperty;
                stateEditor.MethodPopupField = new PopupField<string>("Methods");
                stateEditor.MethodPopupField.BindProperty(stateEditor.MethodNameProperty);
                SetMethodPopupField(stateEditor, m_stateMethods);

                stateEditor.RemoveStateButton = new Button(
                    delegate
                    {
                        int id = m_worldStateEditor.StateEditors.IndexOf(stateEditor);

                        List<StateIdEditor> linkedStateIdEditors;
                        if (StateIdEditor.AllStateIdEditor.TryGetValue(id, out linkedStateIdEditors))
                        {
                            foreach (StateIdEditor stateIdEditor in linkedStateIdEditors)
                                stateIdEditor.StateIdProperty.DeleteCommand();
                        }

                        m_worldStateEditor.StatesProperty.serializedObject.ApplyModifiedProperties();
                        InitializeActionSet();
                        InitializeGoalListEditor();

                        m_worldStateEditor.StatesProperty.DeleteArrayElementAtIndex(id);

                        foreach (KeyValuePair<int, List<StateIdEditor>> keyValuePair in StateIdEditor.AllStateIdEditor)
                        {
                            if (keyValuePair.Key <= id)
                                continue;

                            int newStateEditorId = keyValuePair.Key - 1;

                            foreach (StateIdEditor linkedStateIdEditor in keyValuePair.Value)
                                linkedStateIdEditor.IdOfStateProperty.intValue = newStateEditorId;
                        }

                        m_worldStateEditor.StatesProperty.serializedObject.ApplyModifiedProperties();

                        InitializeWorldStateList();
                        InitializeActionSet();
                        InitializeGoalListEditor();

                        Compose();
                    });

                stateEditor.RemoveStateButton.text = "Remove";
                stateEditor.RemoveStateButton.style.width = new Length(25f, LengthUnit.Percent);
                stateEditor.RemoveStateButton.style.alignSelf = Align.FlexEnd;
                stateEditor.RemoveStateButton.style.position = Position.Absolute;

                m_worldStateEditor.StateEditors.Add(stateEditor);
            }
        }

        private void InitializeActionSet()
        {
            m_actionSetEditor = new ActionSetEditor();
            m_actionSetEditor.ActionEditors = new List<ActionEditor>();

            m_actionSetEditor.ActionSetProperty = serializedObject.FindProperty("m_actionSet");

            m_actionSetEditor.ActionNameField = new TextField("Action Name");

            m_actionSetEditor.AddActionButton = new Button(delegate
            {
                string actionName = m_actionSetEditor.ActionNameField.text;

                if (actionName == string.Empty)
                    return;

                int actionCount = m_actionSetEditor.ActionSetProperty.arraySize - 1;

                if (actionCount == -1)
                    actionCount = 0;

                Action newAction = new Action();
                newAction.Name = actionName;

                m_aiPlanner.ActionSet.Add(newAction);
                serializedObject.Update();

                InitializeActionList();
                Compose();
            });

            m_actionSetEditor.AddActionButton.text = "Add Action";
            m_actionSetEditor.AddActionButton.style.width = new Length(25f, LengthUnit.Percent);
            m_actionSetEditor.AddActionButton.style.alignSelf = Align.FlexEnd;
            m_actionSetEditor.AddActionButton.style.position = Position.Absolute;

            InitializeActionList();
        }
        private void InitializeActionList()
        {
            m_actionSetEditor.ActionEditors.Clear();

            if (StateIdEditor.AllStateIdEditor == null)
                StateIdEditor.AllStateIdEditor = new Dictionary<int, List<StateIdEditor>>();

            StateIdEditor.AllStateIdEditor.Clear();

            int actionCount = m_actionSetEditor.ActionSetProperty.arraySize;

            for (int i = 0; i < actionCount; i++)
            {
                ActionEditor actionEditor = new ActionEditor();
                actionEditor.StateIdListEditor = new StateIdListEditor();

                actionEditor.ActionProperty = m_actionSetEditor.ActionSetProperty.GetArrayElementAtIndex(i);

                SerializedProperty actionNameProperty = actionEditor.ActionProperty.FindPropertyRelative(nameof(Action.Name));
                actionEditor.ShowFoldout = EditorUtils.CreateFoldout(actionNameProperty.stringValue, 15, Color.white, FlexDirection.Column);
                actionEditor.ShowFoldout.value = false;


                SerializedProperty methodProperty = actionEditor.ActionProperty.FindPropertyRelative("m_actionMethod");
                SerializedProperty componentNameProperty = methodProperty.FindPropertyRelative("m_componentName");

                actionEditor.ComponentNameProperty = componentNameProperty;
                actionEditor.ComponentPopupField = new PopupField<string>("Component", m_actionMethods.Keys.ToList(), 0);
                actionEditor.ComponentPopupField.BindProperty(actionEditor.ComponentNameProperty);
                actionEditor.ComponentPopupField.RegisterValueChangedCallback(delegate { SetMethodPopupField(actionEditor, m_actionMethods); });

                SerializedProperty methodNameProperty = methodProperty.FindPropertyRelative("m_methodName");

                actionEditor.MethodNameProperty = methodNameProperty;
                actionEditor.MethodPopupField = new PopupField<string>("Methods");
                actionEditor.MethodPopupField.BindProperty(actionEditor.MethodNameProperty);
                SetMethodPopupField(actionEditor, m_actionMethods);

                actionEditor.RemoveActionButton = new Button(
                    delegate
                    {
                        int id = m_actionSetEditor.ActionEditors.IndexOf(actionEditor);
                        m_actionSetEditor.ActionSetProperty.DeleteArrayElementAtIndex(id);
                        m_actionSetEditor.ActionSetProperty.serializedObject.ApplyModifiedProperties();

                        InitializeActionList();
                        Compose();
                    });

                actionEditor.RemoveActionButton.text = "Remove";
                actionEditor.RemoveActionButton.style.width = new Length(25f, LengthUnit.Percent);
                actionEditor.RemoveActionButton.style.alignSelf = Align.FlexEnd;
                actionEditor.RemoveActionButton.style.position = Position.Absolute;

                actionEditor.StateIdListEditor.StateIdsProperty = actionEditor.ActionProperty.FindPropertyRelative("m_stateEffects");
                actionEditor.StateIdListEditor.StateIdEditors = new List<StateIdEditor>();
                InitializeStateIdList(actionEditor.StateIdListEditor, "Effects");

                InitializePreconditionList(actionEditor);

                m_actionSetEditor.ActionEditors.Add(actionEditor);
            }
        }

        private void InitializePreconditionList(ActionEditor ActionEditor)
        {
            ActionEditor.PreconditionListEditor = new PreconditionListEditor();

            PreconditionListEditor preconditionListEditor = ActionEditor.PreconditionListEditor;
            preconditionListEditor.PreconditionEditors = new List<PreconditionEditor>();
            preconditionListEditor.PreconditionsProperty = ActionEditor.ActionProperty.FindPropertyRelative("m_preconditions");

            preconditionListEditor.AddPreconditionButton = new Button(delegate
            {
                int preconditionCount = preconditionListEditor.PreconditionsProperty.arraySize - 1;

                if (preconditionCount < 0)
                    preconditionCount = 0;

                int actionId = m_actionSetEditor.ActionEditors.IndexOf(ActionEditor);
                m_aiPlanner.ActionSet[actionId].Preconditions.Add(new Action.Precondition());
                serializedObject.Update();

                InitializePreconditionList(ActionEditor);
                Compose();
            });
            preconditionListEditor.AddPreconditionButton.text = "Add Precondition";
            preconditionListEditor.AddPreconditionButton.style.width = new Length(40f, LengthUnit.Percent);
            preconditionListEditor.AddPreconditionButton.style.alignSelf = Align.FlexEnd;
            preconditionListEditor.AddPreconditionButton.style.position = Position.Absolute;

            preconditionListEditor.ShowFoldout = EditorUtils.CreateFoldout("Preconditions", 15, Color.white, FlexDirection.Column);
            preconditionListEditor.ShowFoldout.value = false;

            int preconditionCount = preconditionListEditor.PreconditionsProperty.arraySize;

            for (int i = 0; i < preconditionCount; i++)
                InitializePreconditionEditor(ActionEditor, preconditionListEditor, i);
        }
        private void InitializePreconditionEditor(ActionEditor ActionEditor, PreconditionListEditor PreconditionListEditor, int Index)
        {
            PreconditionEditor preconditionEditor = new PreconditionEditor();
            preconditionEditor.StateIdListEditor = new StateIdListEditor();

            preconditionEditor.ShowFoldout = EditorUtils.CreateFoldout($"Preconditions_{Index}", 15, Color.white, FlexDirection.Column);
            preconditionEditor.ShowFoldout.value = false;

            SerializedProperty preconditionProperty = PreconditionListEditor.PreconditionsProperty.GetArrayElementAtIndex(Index);

            preconditionEditor.CostProperty = preconditionProperty.FindPropertyRelative("Cost");
            preconditionEditor.CostField = new PropertyField();
            preconditionEditor.CostField.BindProperty(preconditionEditor.CostProperty);

            preconditionEditor.StateIdListEditor.StateIdsProperty = preconditionProperty.FindPropertyRelative("States");
            preconditionEditor.StateIdListEditor.StateIdEditors = new List<StateIdEditor>();
            InitializeStateIdList(preconditionEditor.StateIdListEditor, "States");

            preconditionEditor.RemovePreconditionButton = new Button(
                delegate
                {
                    int id = PreconditionListEditor.PreconditionEditors.IndexOf(preconditionEditor);
                    PreconditionListEditor.PreconditionsProperty.DeleteArrayElementAtIndex(id);
                    serializedObject.ApplyModifiedProperties();

                    InitializeActionSet();
                    Compose();
                });

            preconditionEditor.RemovePreconditionButton.text = "Remove";
            preconditionEditor.RemovePreconditionButton.style.width = new Length(25f, LengthUnit.Percent);
            preconditionEditor.RemovePreconditionButton.style.alignSelf = Align.FlexEnd;
            preconditionEditor.RemovePreconditionButton.style.position = Position.Absolute;

            PreconditionListEditor.PreconditionEditors.Add(preconditionEditor);
        }

        private void InitializeStateIdList(StateIdListEditor StateIdListEditor, string FoldoutText)
        {
            StateIdListEditor.StateIdEditors.Clear();

            int stateIdCount = StateIdListEditor.StateIdsProperty.arraySize;

            for (int i = 0; i < stateIdCount; ++i)
                InitializeStateId(StateIdListEditor, i);

            StateIdListEditor.ShowFoldout = EditorUtils.CreateFoldout(FoldoutText, 15, Color.white, FlexDirection.Column);
            StateIdListEditor.ShowFoldout.value = false;

            InitializeAddStateIdButton(StateIdListEditor);
        }
        private void InitializeStateId(StateIdListEditor StateIdListEditor, int Index)
        {
            StateIdEditor stateIdEditor = new StateIdEditor();

            stateIdEditor.StateIdProperty = StateIdListEditor.StateIdsProperty.GetArrayElementAtIndex(Index);
            stateIdEditor.IdOfStateProperty = stateIdEditor.StateIdProperty.FindPropertyRelative(nameof(StateId.Id));

            string stateName = m_worldStateEditor.StateEditors[stateIdEditor.IdOfStateProperty.intValue].StateNameProperty.stringValue;
            stateIdEditor.ShowFoldout = EditorUtils.CreateFoldout(stateName, 15, Color.white, FlexDirection.Column);
            stateIdEditor.ShowFoldout.value = false;

            SerializedProperty stateValueProperty = stateIdEditor.StateIdProperty.FindPropertyRelative(nameof(StateId.StateValue));
            stateIdEditor.ValueProperty = stateValueProperty.FindPropertyRelative("m_value");
            stateIdEditor.ValueField = new PropertyField(stateIdEditor.ValueProperty);
            stateIdEditor.ValueField.BindProperty(stateIdEditor.ValueProperty);

            List<StateIdEditor> linkedStateIdEditors;
            if (!StateIdEditor.AllStateIdEditor.TryGetValue(stateIdEditor.IdOfStateProperty.intValue, out linkedStateIdEditors))
            {
                linkedStateIdEditors = new List<StateIdEditor>();
                StateIdEditor.AllStateIdEditor.Add(stateIdEditor.IdOfStateProperty.intValue, linkedStateIdEditors);
            }

            stateIdEditor.RemoveStateIdButton = new Button(
                delegate
                {
                    int id = StateIdListEditor.StateIdEditors.IndexOf(stateIdEditor);
                    StateIdListEditor.StateIdsProperty.DeleteArrayElementAtIndex(id);
                    serializedObject.ApplyModifiedProperties();

                    InitializeStateIdList(StateIdListEditor, StateIdListEditor.ShowFoldout.text);
                    Compose();
                });

            stateIdEditor.RemoveStateIdButton.text = "Remove";
            stateIdEditor.RemoveStateIdButton.style.width = new Length(25f, LengthUnit.Percent);
            stateIdEditor.RemoveStateIdButton.style.alignSelf = Align.FlexEnd;
            stateIdEditor.RemoveStateIdButton.style.position = Position.Absolute;

            linkedStateIdEditors.Add(stateIdEditor);
            StateIdListEditor.StateIdEditors.Add(stateIdEditor);
        }
        private void InitializeAddStateIdButton(StateIdListEditor StateIdListEditor)
        {
            List<int> statesAlreadySetted = new List<int>();
            foreach (StateIdEditor stateIdEditor in StateIdListEditor.StateIdEditors)
                statesAlreadySetted.Add(stateIdEditor.IdOfStateProperty.intValue);

            Dictionary<string, int> stateChoice = new Dictionary<string, int>();
            int stateCount = m_worldStateEditor.StateEditors.Count;
            for (int i = 0; i < stateCount; ++i)
            {
                if (statesAlreadySetted.Contains(i))
                    continue;

                StateEditor stateEditor = m_worldStateEditor.StateEditors[i];
                stateChoice.Add(stateEditor.StateNameProperty.stringValue, i);
            }


            StateIdListEditor.StateTypePopup = new PopupField<string>();
            StateIdListEditor.StateTypePopup.choices = stateChoice.Keys.ToList();
            StateIdListEditor.StateTypePopup.index = 0;
            StateIdListEditor.StateTypePopup.style.width = new Length(40f, LengthUnit.Percent);
            StateIdListEditor.StateTypePopup.style.alignSelf = Align.Center;
            StateIdListEditor.StateTypePopup.style.position = Position.Absolute;

            StateIdListEditor.AddStateIdButton = new Button(
                delegate
                {
                    if (StateIdListEditor.StateTypePopup.choices.Count == 0)
                    {
                        Debug.Log($"All state is added in effect of action");
                        return;
                    }

                    string selectedTypeName = StateIdListEditor.StateTypePopup.choices[StateIdListEditor.StateTypePopup.index];

                    if (selectedTypeName == string.Empty)
                        return;

                    int id = stateChoice[selectedTypeName];
                    Type type = m_aiPlanner.WorldState.States[id].StateValue.Value.GetType();
                    object value = Activator.CreateInstance(type);

                    StateValue stateValue = new StateValue();
                    stateValue.Value = value;

                    int stateIdCount = StateIdListEditor.StateIdsProperty.arraySize - 1;

                    if (stateIdCount < 0)
                        stateIdCount = 0;

                    StateIdListEditor.StateIdsProperty.InsertArrayElementAtIndex(stateIdCount);

                    stateIdCount = StateIdListEditor.StateIdsProperty.arraySize - 1;

                    SerializedProperty stateIdProperty = StateIdListEditor.StateIdsProperty.GetArrayElementAtIndex(stateIdCount);
                    stateIdProperty.FindPropertyRelative("Id").intValue = id;
                    SerializedProperty stateValueProperty = stateIdProperty.FindPropertyRelative("StateValue");
                    stateValueProperty.FindPropertyRelative("m_value").managedReferenceValue = stateValue.Value;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    InitializeStateId(StateIdListEditor, StateIdListEditor.StateIdsProperty.arraySize - 1);
                    InitializeAddStateIdButton(StateIdListEditor);
                    Compose();
                });

            StateIdListEditor.AddStateIdButton.text = "Add States";
            StateIdListEditor.AddStateIdButton.style.width = new Length(25f, LengthUnit.Percent);
            StateIdListEditor.AddStateIdButton.style.alignSelf = Align.FlexEnd;
            StateIdListEditor.AddStateIdButton.style.position = Position.Absolute;
        }

        private void InitializeGoalListEditor()
        {
            m_goalListEditor = new GoalListEditor();
            m_goalListEditor.GoalEditors = new List<GoalEditor>();
            m_goalListEditor.GoalsProperty = serializedObject.FindProperty("m_goals");

            m_goalListEditor.AddGoalButton = new Button(delegate
            {
                m_aiPlanner.Goals.Add(new Goal());
                serializedObject.Update();

                InitializeGoalEditor(m_aiPlanner.Goals.Count - 1);
                Compose();
            });
            m_goalListEditor.AddGoalButton.text = "Add Goal";
            m_goalListEditor.AddGoalButton.style.width = new Length(70f, LengthUnit.Percent);
            m_goalListEditor.AddGoalButton.style.alignSelf = Align.Center;
            //m_goalListEditor.AddGoalButton.style.position = Position.Absolute;

            int goalCount = m_aiPlanner.Goals.Count;

            for (int i = 0; i < goalCount; ++i)
                InitializeGoalEditor(i);
        }
        private void InitializeGoalEditor(int index)
        {
            GoalEditor goalEditor = new GoalEditor();

            SerializedProperty goalProperty = m_goalListEditor.GoalsProperty.GetArrayElementAtIndex(index);

            goalEditor.StateEditor = new StateIdListEditor();
            goalEditor.StateEditor.StateIdsProperty = goalProperty.FindPropertyRelative("m_states");
            goalEditor.StateEditor.StateIdEditors = new List<StateIdEditor>();
            InitializeStateIdList(goalEditor.StateEditor, "States");

            goalEditor.AnimationCurveProperty = goalProperty.FindPropertyRelative("m_animationCurve");
            goalEditor.CurveField = new CurveField();
            goalEditor.CurveField.BindProperty(goalEditor.AnimationCurveProperty);
            goalEditor.CurveField.style.height = 50;

            goalEditor.TextElement = new TextElement();
            goalEditor.TextElement.text = "Consideration Curve";

            goalEditor.RemoveGoalButton = new Button(
                    delegate
                    {
                        int id = m_goalListEditor.GoalEditors.IndexOf(goalEditor);
                        m_goalListEditor.GoalsProperty.DeleteArrayElementAtIndex(id);
                        serializedObject.ApplyModifiedProperties();

                        InitializeGoalListEditor();
                        Compose();
                    });

            goalEditor.RemoveGoalButton.text = "Remove";
            goalEditor.RemoveGoalButton.style.width = new Length(25f, LengthUnit.Percent);
            goalEditor.RemoveGoalButton.style.alignSelf = Align.FlexEnd;
            goalEditor.RemoveGoalButton.style.position = Position.Absolute;

            goalEditor.ShowFoldout = EditorUtils.CreateFoldout($"Goal_{index}", 15, Color.white, FlexDirection.Column);
            goalEditor.ShowFoldout.value = false;

            SerializedProperty methodProperty = goalProperty.FindPropertyRelative("m_considerationMethod");
            SerializedProperty componentNameProperty = methodProperty.FindPropertyRelative("m_componentName");

            goalEditor.ComponentNameProperty = componentNameProperty;
            goalEditor.ComponentPopupField = new PopupField<string>("Component", m_considerationMethods.Keys.ToList(), 0);
            goalEditor.ComponentPopupField.BindProperty(goalEditor.ComponentNameProperty);
            goalEditor.ComponentPopupField.RegisterValueChangedCallback(delegate { SetMethodPopupField(goalEditor, m_considerationMethods); });

            SerializedProperty methodNameProperty = methodProperty.FindPropertyRelative("m_methodName");

            goalEditor.MethodNameProperty = methodNameProperty;
            goalEditor.MethodPopupField = new PopupField<string>("Methods");
            goalEditor.MethodPopupField.BindProperty(goalEditor.MethodNameProperty);
            SetMethodPopupField(goalEditor, m_considerationMethods);

            m_goalListEditor.GoalEditors.Add(goalEditor);
        }

        private void ComposeWorldState()
        {
            VisualElement worldStateLabel = EditorUtils.CreateLabel(1f, 5f, 0f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
            VisualElement worldStateText = EditorUtils.CreateText("World State", 20f, Align.Center);

            worldStateLabel.Add(worldStateText);

            VisualElement addStateLabel = EditorUtils.CreateLabel(1f, 5f, 10f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);

            addStateLabel.Add(m_worldStateEditor.StateNameField);
            addStateLabel.Add(m_worldStateEditor.StatePopupField);
            addStateLabel.Add(m_worldStateEditor.AddStateButton);

            worldStateLabel.Add(addStateLabel);

            int stateCount = m_worldStateEditor.StateEditors.Count;
            for (int i = 0; i < stateCount; ++i)
            {
                StateEditor stateEditor = m_worldStateEditor.StateEditors[i];

                stateEditor.ShowFoldout.Add(stateEditor.ValueField);
                stateEditor.ShowFoldout.Add(stateEditor.ComponentPopupField);
                stateEditor.ShowFoldout.Add(stateEditor.MethodPopupField);

                VisualElement stateFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
                stateFoldoutLabel.Add(stateEditor.ShowFoldout);
                stateFoldoutLabel.Add(stateEditor.RemoveStateButton);
                worldStateLabel.Add(stateFoldoutLabel);
            }

            m_root.Add(worldStateLabel);
        }
        private void ComposeActionSet()
        {
            VisualElement actionSetLabel = EditorUtils.CreateLabel(1f, 5f, 0f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
            VisualElement actionSetText = EditorUtils.CreateText("Action Set", 20f, Align.Center);

            actionSetLabel.Add(actionSetText);

            VisualElement addActionLabel = EditorUtils.CreateLabel(1f, 5f, 10f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);

            addActionLabel.Add(m_actionSetEditor.ActionNameField);
            addActionLabel.Add(m_actionSetEditor.AddActionButton);

            actionSetLabel.Add(addActionLabel);


            int actionCount = m_actionSetEditor.ActionEditors.Count;
            for (int i = 0; i < actionCount; ++i)
            {
                ActionEditor actionEditor = m_actionSetEditor.ActionEditors[i];
                actionEditor.ShowFoldout.Clear();
                actionEditor.StateIdListEditor.ShowFoldout.Clear();
                actionEditor.PreconditionListEditor.ShowFoldout.Clear();

                actionEditor.ShowFoldout.Add(actionEditor.ComponentPopupField);
                actionEditor.ShowFoldout.Add(actionEditor.MethodPopupField);

                actionEditor.ShowFoldout.Add(ComposeStateIdEditor(actionEditor.StateIdListEditor));

                VisualElement preconditionListFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
                preconditionListFoldoutLabel.Add(actionEditor.PreconditionListEditor.ShowFoldout);
                preconditionListFoldoutLabel.Add(actionEditor.PreconditionListEditor.AddPreconditionButton);

                int preconditionCount = actionEditor.PreconditionListEditor.PreconditionEditors.Count;
                for (int j = 0; j < preconditionCount; ++j)
                {
                    PreconditionEditor preconditionEditor = actionEditor.PreconditionListEditor.PreconditionEditors[j];
                    preconditionEditor.ShowFoldout.Clear();
                    preconditionEditor.StateIdListEditor.ShowFoldout.Clear();

                    VisualElement preconditionFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
                    preconditionFoldoutLabel.Add(preconditionEditor.ShowFoldout);
                    preconditionFoldoutLabel.Add(preconditionEditor.RemovePreconditionButton);

                    preconditionEditor.ShowFoldout.Add(preconditionEditor.CostField);
                    preconditionEditor.ShowFoldout.Add(ComposeStateIdEditor(preconditionEditor.StateIdListEditor));

                    actionEditor.PreconditionListEditor.ShowFoldout.Add(preconditionFoldoutLabel);
                }

                actionEditor.ShowFoldout.Add(preconditionListFoldoutLabel);

                VisualElement actionFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
                actionFoldoutLabel.Add(actionEditor.ShowFoldout);
                actionFoldoutLabel.Add(actionEditor.RemoveActionButton);

                actionSetLabel.Add(actionFoldoutLabel);
            }

            m_root.Add(EditorUtils.CreateSpace(new Vector2(0, 20f)));
            m_root.Add(actionSetLabel);
        }
        private void ComposeGoal()
        {
            VisualElement goalListSetLabel = EditorUtils.CreateLabel(1f, 5f, 0f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
            VisualElement goalSetText = EditorUtils.CreateText("Goals", 20f, Align.Center);

            goalListSetLabel.Add(goalSetText);
            goalListSetLabel.Add(m_goalListEditor.AddGoalButton);
            goalListSetLabel.Add(EditorUtils.CreateSpace(new Vector2(0f, 10f)));

            for (int i = 0; i < m_goalListEditor.GoalEditors.Count; i++)
            {
                GoalEditor goalEditor = m_goalListEditor.GoalEditors[i];
                VisualElement goalSetLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);

                goalEditor.ShowFoldout.Clear();

                goalEditor.ShowFoldout.Add(goalEditor.TextElement);
                goalEditor.ShowFoldout.Add(goalEditor.ComponentPopupField);
                goalEditor.ShowFoldout.Add(goalEditor.MethodPopupField);
                goalEditor.ShowFoldout.Add(goalEditor.CurveField);

                goalEditor.StateEditor.ShowFoldout.Clear();
                goalEditor.ShowFoldout.Add(ComposeStateIdEditor(goalEditor.StateEditor));

                goalSetLabel.Add(goalEditor.ShowFoldout);
                goalSetLabel.Add(goalEditor.RemoveGoalButton);

                goalListSetLabel.Add(goalSetLabel);
            }

            m_root.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));
            m_root.Add(goalListSetLabel);
        }

        private VisualElement ComposeStateIdEditor(StateIdListEditor stateIdListEditor)
        {
            VisualElement stateIdListFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
            stateIdListFoldoutLabel.Add(stateIdListEditor.ShowFoldout);
            stateIdListFoldoutLabel.Add(stateIdListEditor.StateTypePopup);
            stateIdListFoldoutLabel.Add(stateIdListEditor.AddStateIdButton);

            int stateIdCount = stateIdListEditor.StateIdEditors.Count;

            for (int j = 0; j < stateIdCount; ++j)
            {
                StateIdEditor stateIdEditor = stateIdListEditor.StateIdEditors[j];

                stateIdEditor.ShowFoldout.Add(stateIdEditor.ValueField);

                VisualElement stateIdFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
                stateIdFoldoutLabel.Add(stateIdEditor.ShowFoldout);
                stateIdFoldoutLabel.Add(stateIdEditor.RemoveStateIdButton);

                stateIdListEditor.ShowFoldout.Add(stateIdFoldoutLabel);
            }

            return stateIdListFoldoutLabel;
        }
    }
}
