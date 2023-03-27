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
            public Foldout showFoldout;
        }

        class MethodEditor : FoldoutEditor
        {
            public SerializedProperty componentNameProperty;
            public PopupField<string> componentPopupField;

            public SerializedProperty methodNameProperty;
            public PopupField<string> methodPopupField;
        }

        class StateEditor : MethodEditor
        {
            public Button removeStateButton;

            public SerializedProperty stateNameProperty;
            public SerializedProperty valueProperty;
            public PropertyField valueField;
        }

        class WorldStateEditor
        {
            public SerializedProperty worldStateProperty;
            public SerializedProperty statesProperty;

            public List<StateEditor> stateEditors;

            public Button addStateButton;
            public PopupField<string> statePopupField;
            public SerializedProperty stateNameProperty;
            public PropertyField stateNameField;
        }

        class StateIdEditor : FoldoutEditor
        {
            public Button removeStateIdButton;

            public SerializedProperty stateIdProperty;
            public SerializedProperty idOfStateProperty;
            public SerializedProperty valueProperty;
            public PropertyField valueField;

            public static Dictionary<int, List<StateIdEditor>> allStateIdEditor;
        }

        class StateIdListEditor : FoldoutEditor
        {
            public Button addStateIdButton;

            public PopupField<string> stateTypePopup;

            public SerializedProperty stateIdsProperty;
            public List<StateIdEditor> stateIdEditors;
        }

        class PreconditionEditor : FoldoutEditor
        {
            public Button removePreconditionButton;

            public StateIdListEditor stateIdListEditor;

            public SerializedProperty costProperty;
            public PropertyField costField;
        }

        class PreconditionListEditor : FoldoutEditor
        {
            public Button addPreconditionButton;

            public SerializedProperty preconditionsProperty;
            public List<PreconditionEditor> preconditionEditors;
        }

        class ActionEditor : MethodEditor
        {
            public SerializedProperty actionProperty;
            public Button removeActionButton;

            public StateIdListEditor stateIdListEditor;
            public PreconditionListEditor preconditionListEditor;
        }

        class ActionSetEditor
        {
            public SerializedProperty actionSetProperty;

            public List<ActionEditor> actionEditors;

            public Button addActionButton;
            public TextField actionNameField;
        }

        class GoalEditor : MethodEditor
        {
            public StateIdListEditor stateEditor;

            public SerializedProperty animationCurveProperty;
            public CurveField curveField;
            public TextElement textElement;

            public Button removeGoalButton;
        }

        class GoalListEditor
        {
            public Button addGoalButton;

            public SerializedProperty goalsProperty;
            public List<GoalEditor> goalEditors;
        }

        private GOAP m_GOAP;

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
            m_GOAP = target as GOAP;

            Initialize();
            Compose();

            return m_root;
        }

        private void Initialize()
        {
            Method.GetStateMethods(m_GOAP.gameObject, out m_stateMethods);
            Method.GetActionMethods(m_GOAP.gameObject, out m_actionMethods);
            Method.GetConsiderationMethods(m_GOAP.gameObject, out m_considerationMethods);
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
            if (MethodEditor.componentNameProperty.stringValue != string.Empty)
            {
                string[] methods;
                InMethods.TryGetValue(MethodEditor.componentNameProperty.stringValue, out methods);

                if (methods != null && methods.Length != 0)
                {
                    string methodName = MethodEditor.methodNameProperty.stringValue;
                    List<string> methodsName = methods.ToList();

                    MethodEditor.methodPopupField.choices = methodsName;

                    MethodEditor.methodPopupField.index = GetListStringIndex(methodsName, methodName);
                }
                else
                {
                    MethodEditor.methodPopupField.choices = new List<string>();
                    MethodEditor.methodPopupField.index = 0;
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
            m_worldStateEditor.stateEditors = new List<StateEditor>();

            m_worldStateEditor.worldStateProperty = serializedObject.FindProperty(nameof(GOAP.worldState));

            m_worldStateEditor.stateNameProperty = m_worldStateEditor.worldStateProperty.FindPropertyRelative(nameof(WorldState.stateName));
            m_worldStateEditor.stateNameField = new PropertyField(m_worldStateEditor.stateNameProperty);

            m_worldStateEditor.statePopupField = new PopupField<string>("Type", m_supportedTypesString, 0);
            m_worldStateEditor.addStateButton = new Button(delegate
            {
                string stateName = m_worldStateEditor.stateNameProperty.stringValue;
                string selectedTypeName = m_worldStateEditor.statePopupField.choices[m_worldStateEditor.statePopupField.index];

                if (stateName == string.Empty && selectedTypeName == string.Empty)
                    return;

                foreach (StateEditor stateEditor in m_worldStateEditor.stateEditors)
                    if (stateEditor.stateNameProperty.stringValue == stateName)
                        return;

                Type type = StateType.GetType(selectedTypeName);

                State state = new State();
                state.name = m_worldStateEditor.stateNameProperty.stringValue;
                object value = Activator.CreateInstance(type);
                state.stateValue = new StateValue();
                state.stateValue.Value = value;

                m_GOAP.worldState.states.Add(state);
                serializedObject.Update();

                InitializeWorldStateList();

                foreach (ActionEditor actionEditor in m_actionSetEditor.actionEditors)
                {
                    InitializeAddStateIdButton(actionEditor.stateIdListEditor);

                    foreach (PreconditionEditor preconditionEditor in actionEditor.preconditionListEditor.preconditionEditors)
                    {
                        InitializeAddStateIdButton(preconditionEditor.stateIdListEditor);
                    }
                }

                foreach (GoalEditor goalEditor in m_goalListEditor.goalEditors)
                    InitializeAddStateIdButton(goalEditor.stateEditor);

                Compose();
            });
            m_worldStateEditor.addStateButton.text = "Add";

            InitializeWorldStateList();
        }
        private void InitializeWorldStateList()
        {
            m_worldStateEditor.stateEditors.Clear();

            if (m_GOAP.worldState.states == null)
                m_GOAP.worldState.states = new List<State>();

            m_worldStateEditor.statesProperty = m_worldStateEditor.worldStateProperty.FindPropertyRelative(nameof(WorldState.states));

            int stateCount = m_GOAP.worldState.states.Count;

            for (int i = 0; i < stateCount; ++i)
            {
                StateEditor stateEditor = new StateEditor();

                SerializedProperty stateProperty = m_worldStateEditor.statesProperty.GetArrayElementAtIndex(i);

                stateEditor.stateNameProperty = stateProperty.FindPropertyRelative(nameof(State.name));
                stateEditor.showFoldout = EditorUtils.CreateFoldout(stateEditor.stateNameProperty.stringValue, 15, Color.white, FlexDirection.Column);
                stateEditor.showFoldout.value = false;

                SerializedProperty stateValueProperty = stateProperty.FindPropertyRelative(nameof(State.stateValue));
                SerializedProperty valueProperty = stateValueProperty.FindPropertyRelative("m_value");

                stateEditor.valueProperty = valueProperty;
                stateEditor.valueField = new PropertyField();
                stateEditor.valueField.BindProperty(valueProperty);

                SerializedProperty methodProperty = stateProperty.FindPropertyRelative("m_stateMethod");
                SerializedProperty componentNameProperty = methodProperty.FindPropertyRelative("m_componentName");

                stateEditor.componentNameProperty = componentNameProperty;
                string componentName = stateEditor.componentNameProperty.stringValue;
                List<string> componentNames = m_stateMethods.Keys.ToList();
                stateEditor.componentPopupField = new PopupField<string>("Component", componentNames, GetListStringIndex(componentNames, componentName));
                stateEditor.componentPopupField.BindProperty(stateEditor.componentNameProperty);
                stateEditor.componentPopupField.RegisterValueChangedCallback(delegate { SetMethodPopupField(stateEditor, m_stateMethods); });

                SerializedProperty methodNameProperty = methodProperty.FindPropertyRelative("m_methodName");

                stateEditor.methodNameProperty = methodNameProperty;
                stateEditor.methodPopupField = new PopupField<string>("Methods");
                stateEditor.methodPopupField.BindProperty(stateEditor.methodNameProperty);
                SetMethodPopupField(stateEditor, m_stateMethods);

                stateEditor.removeStateButton = new Button(
                    delegate
                    {
                        int id = m_worldStateEditor.stateEditors.IndexOf(stateEditor);

                        List<StateIdEditor> linkedStateIdEditors;
                        if (StateIdEditor.allStateIdEditor.TryGetValue(id, out linkedStateIdEditors))
                        {
                            foreach (StateIdEditor stateIdEditor in linkedStateIdEditors)
                                stateIdEditor.stateIdProperty.DeleteCommand();
                        }

                        m_worldStateEditor.statesProperty.serializedObject.ApplyModifiedProperties();
                        InitializeActionSet();
                        InitializeGoalListEditor();

                        m_worldStateEditor.statesProperty.DeleteArrayElementAtIndex(id);

                        foreach (KeyValuePair<int, List<StateIdEditor>> keyValuePair in StateIdEditor.allStateIdEditor)
                        {
                            if (keyValuePair.Key <= id)
                                continue;

                            int newStateEditorId = keyValuePair.Key - 1;

                            foreach (StateIdEditor linkedStateIdEditor in keyValuePair.Value)
                                linkedStateIdEditor.idOfStateProperty.intValue = newStateEditorId;
                        }

                        m_worldStateEditor.statesProperty.serializedObject.ApplyModifiedProperties();

                        InitializeWorldStateList();
                        InitializeActionSet();
                        InitializeGoalListEditor();

                        Compose();
                    });

                stateEditor.removeStateButton.text = "Remove";
                stateEditor.removeStateButton.style.width = new Length(25f, LengthUnit.Percent);
                stateEditor.removeStateButton.style.alignSelf = Align.FlexEnd;
                stateEditor.removeStateButton.style.position = Position.Absolute;

                m_worldStateEditor.stateEditors.Add(stateEditor);
            }
        }

        private void InitializeActionSet()
        {
            m_actionSetEditor = new ActionSetEditor();
            m_actionSetEditor.actionEditors = new List<ActionEditor>();

            m_actionSetEditor.actionSetProperty = serializedObject.FindProperty("m_actionSet");

            m_actionSetEditor.actionNameField = new TextField("Action Name");

            m_actionSetEditor.addActionButton = new Button(delegate
            {
                string actionName = m_actionSetEditor.actionNameField.text;

                if (actionName == string.Empty)
                    return;

                int actionCount = m_actionSetEditor.actionSetProperty.arraySize - 1;

                if (actionCount == -1)
                    actionCount = 0;

                Action newAction = new Action();
                newAction.name = actionName;

                m_GOAP.ActionSet.Add(newAction);
                serializedObject.Update();

                InitializeActionList();
                Compose();
            });

            m_actionSetEditor.addActionButton.text = "Add Action";
            m_actionSetEditor.addActionButton.style.width = new Length(25f, LengthUnit.Percent);
            m_actionSetEditor.addActionButton.style.alignSelf = Align.FlexEnd;
            m_actionSetEditor.addActionButton.style.position = Position.Absolute;

            InitializeActionList();
        }
        private void InitializeActionList()
        {
            m_actionSetEditor.actionEditors.Clear();

            if (StateIdEditor.allStateIdEditor == null)
                StateIdEditor.allStateIdEditor = new Dictionary<int, List<StateIdEditor>>();

            StateIdEditor.allStateIdEditor.Clear();

            int actionCount = m_actionSetEditor.actionSetProperty.arraySize;

            for (int i = 0; i < actionCount; i++)
            {
                ActionEditor actionEditor = new ActionEditor();
                actionEditor.stateIdListEditor = new StateIdListEditor();

                actionEditor.actionProperty = m_actionSetEditor.actionSetProperty.GetArrayElementAtIndex(i);

                SerializedProperty actionNameProperty = actionEditor.actionProperty.FindPropertyRelative(nameof(Action.name));
                actionEditor.showFoldout = EditorUtils.CreateFoldout(actionNameProperty.stringValue, 15, Color.white, FlexDirection.Column);
                actionEditor.showFoldout.value = false;

                SerializedProperty componentNameProperty = actionEditor.actionProperty.FindPropertyRelative("m_componentName");

                actionEditor.componentNameProperty = componentNameProperty;
                actionEditor.componentPopupField = new PopupField<string>("Component", m_actionMethods.Keys.ToList(), 0);
                actionEditor.componentPopupField.BindProperty(actionEditor.componentNameProperty);
                actionEditor.componentPopupField.RegisterValueChangedCallback(delegate { SetMethodPopupField(actionEditor, m_actionMethods); });

                SerializedProperty methodNameProperty = actionEditor.actionProperty.FindPropertyRelative("m_methodName");

                actionEditor.methodNameProperty = methodNameProperty;
                actionEditor.methodPopupField = new PopupField<string>("Methods");
                actionEditor.methodPopupField.BindProperty(actionEditor.methodNameProperty);
                SetMethodPopupField(actionEditor, m_actionMethods);

                actionEditor.removeActionButton = new Button(
                    delegate
                    {
                        int id = m_actionSetEditor.actionEditors.IndexOf(actionEditor);
                        m_actionSetEditor.actionSetProperty.DeleteArrayElementAtIndex(id);
                        m_actionSetEditor.actionSetProperty.serializedObject.ApplyModifiedProperties();

                        InitializeActionList();
                        Compose();
                    });

                actionEditor.removeActionButton.text = "Remove";
                actionEditor.removeActionButton.style.width = new Length(25f, LengthUnit.Percent);
                actionEditor.removeActionButton.style.alignSelf = Align.FlexEnd;
                actionEditor.removeActionButton.style.position = Position.Absolute;

                actionEditor.stateIdListEditor.stateIdsProperty = actionEditor.actionProperty.FindPropertyRelative("m_stateEffects");
                actionEditor.stateIdListEditor.stateIdEditors = new List<StateIdEditor>();
                InitializeStateIdList(actionEditor.stateIdListEditor, "Effects");

                InitializePreconditionList(actionEditor);

                m_actionSetEditor.actionEditors.Add(actionEditor);
            }
        }

        private void InitializePreconditionList(ActionEditor ActionEditor)
        {
            ActionEditor.preconditionListEditor = new PreconditionListEditor();

            PreconditionListEditor preconditionListEditor = ActionEditor.preconditionListEditor;
            preconditionListEditor.preconditionEditors = new List<PreconditionEditor>();
            preconditionListEditor.preconditionsProperty = ActionEditor.actionProperty.FindPropertyRelative("m_preconditions");

            preconditionListEditor.addPreconditionButton = new Button(delegate
            {
                int preconditionCount = preconditionListEditor.preconditionsProperty.arraySize - 1;

                if (preconditionCount < 0)
                    preconditionCount = 0;

                int actionId = m_actionSetEditor.actionEditors.IndexOf(ActionEditor);
                m_GOAP.ActionSet[actionId].Preconditions.Add(new Action.Precondition());
                serializedObject.Update();

                InitializePreconditionList(ActionEditor);
                Compose();
            });
            preconditionListEditor.addPreconditionButton.text = "Add Precondition";
            preconditionListEditor.addPreconditionButton.style.width = new Length(40f, LengthUnit.Percent);
            preconditionListEditor.addPreconditionButton.style.alignSelf = Align.FlexEnd;
            preconditionListEditor.addPreconditionButton.style.position = Position.Absolute;

            preconditionListEditor.showFoldout = EditorUtils.CreateFoldout("Preconditions", 15, Color.white, FlexDirection.Column);
            preconditionListEditor.showFoldout.value = false;

            int preconditionCount = preconditionListEditor.preconditionsProperty.arraySize;

            for (int i = 0; i < preconditionCount; i++)
                InitializePreconditionEditor(ActionEditor, preconditionListEditor, i);
        }
        private void InitializePreconditionEditor(ActionEditor ActionEditor, PreconditionListEditor PreconditionListEditor, int Index)
        {
            PreconditionEditor preconditionEditor = new PreconditionEditor();
            preconditionEditor.stateIdListEditor = new StateIdListEditor();

            preconditionEditor.showFoldout = EditorUtils.CreateFoldout($"Preconditions_{Index}", 15, Color.white, FlexDirection.Column);
            preconditionEditor.showFoldout.value = false;

            SerializedProperty preconditionProperty = PreconditionListEditor.preconditionsProperty.GetArrayElementAtIndex(Index);

            preconditionEditor.costProperty = preconditionProperty.FindPropertyRelative("Cost");
            preconditionEditor.costField = new PropertyField();
            preconditionEditor.costField.BindProperty(preconditionEditor.costProperty);

            preconditionEditor.stateIdListEditor.stateIdsProperty = preconditionProperty.FindPropertyRelative("States");
            preconditionEditor.stateIdListEditor.stateIdEditors = new List<StateIdEditor>();
            InitializeStateIdList(preconditionEditor.stateIdListEditor, "States");

            preconditionEditor.removePreconditionButton = new Button(
                delegate
                {
                    int id = PreconditionListEditor.preconditionEditors.IndexOf(preconditionEditor);
                    PreconditionListEditor.preconditionsProperty.DeleteArrayElementAtIndex(id);
                    serializedObject.ApplyModifiedProperties();

                    InitializeActionSet();
                    Compose();
                });

            preconditionEditor.removePreconditionButton.text = "Remove";
            preconditionEditor.removePreconditionButton.style.width = new Length(25f, LengthUnit.Percent);
            preconditionEditor.removePreconditionButton.style.alignSelf = Align.FlexEnd;
            preconditionEditor.removePreconditionButton.style.position = Position.Absolute;

            PreconditionListEditor.preconditionEditors.Add(preconditionEditor);
        }

        private void InitializeStateIdList(StateIdListEditor StateIdListEditor, string FoldoutText)
        {
            StateIdListEditor.stateIdEditors.Clear();

            int stateIdCount = StateIdListEditor.stateIdsProperty.arraySize;

            for (int i = 0; i < stateIdCount; ++i)
                InitializeStateId(StateIdListEditor, i);

            StateIdListEditor.showFoldout = EditorUtils.CreateFoldout(FoldoutText, 15, Color.white, FlexDirection.Column);
            StateIdListEditor.showFoldout.value = false;

            InitializeAddStateIdButton(StateIdListEditor);
        }
        private void InitializeStateId(StateIdListEditor StateIdListEditor, int Index)
        {
            StateIdEditor stateIdEditor = new StateIdEditor();

            stateIdEditor.stateIdProperty = StateIdListEditor.stateIdsProperty.GetArrayElementAtIndex(Index);
            stateIdEditor.idOfStateProperty = stateIdEditor.stateIdProperty.FindPropertyRelative(nameof(StateId.id));

            string stateName = m_worldStateEditor.stateEditors[stateIdEditor.idOfStateProperty.intValue].stateNameProperty.stringValue;
            stateIdEditor.showFoldout = EditorUtils.CreateFoldout(stateName, 15, Color.white, FlexDirection.Column);
            stateIdEditor.showFoldout.value = false;

            SerializedProperty stateValueProperty = stateIdEditor.stateIdProperty.FindPropertyRelative(nameof(StateId.stateValue));
            stateIdEditor.valueProperty = stateValueProperty.FindPropertyRelative("m_value");
            stateIdEditor.valueField = new PropertyField(stateIdEditor.valueProperty);
            stateIdEditor.valueField.BindProperty(stateIdEditor.valueProperty);

            List<StateIdEditor> linkedStateIdEditors;
            if (!StateIdEditor.allStateIdEditor.TryGetValue(stateIdEditor.idOfStateProperty.intValue, out linkedStateIdEditors))
            {
                linkedStateIdEditors = new List<StateIdEditor>();
                StateIdEditor.allStateIdEditor.Add(stateIdEditor.idOfStateProperty.intValue, linkedStateIdEditors);
            }

            stateIdEditor.removeStateIdButton = new Button(
                delegate
                {
                    int id = StateIdListEditor.stateIdEditors.IndexOf(stateIdEditor);
                    StateIdListEditor.stateIdsProperty.DeleteArrayElementAtIndex(id);
                    serializedObject.ApplyModifiedProperties();

                    InitializeStateIdList(StateIdListEditor, StateIdListEditor.showFoldout.text);
                    Compose();
                });

            stateIdEditor.removeStateIdButton.text = "Remove";
            stateIdEditor.removeStateIdButton.style.width = new Length(25f, LengthUnit.Percent);
            stateIdEditor.removeStateIdButton.style.alignSelf = Align.FlexEnd;
            stateIdEditor.removeStateIdButton.style.position = Position.Absolute;

            linkedStateIdEditors.Add(stateIdEditor);
            StateIdListEditor.stateIdEditors.Add(stateIdEditor);
        }
        private void InitializeAddStateIdButton(StateIdListEditor StateIdListEditor)
        {
            List<int> statesAlreadySetted = new List<int>();
            foreach (StateIdEditor stateIdEditor in StateIdListEditor.stateIdEditors)
                statesAlreadySetted.Add(stateIdEditor.idOfStateProperty.intValue);

            Dictionary<string, int> stateChoice = new Dictionary<string, int>();
            int stateCount = m_worldStateEditor.stateEditors.Count;
            for (int i = 0; i < stateCount; ++i)
            {
                if (statesAlreadySetted.Contains(i))
                    continue;

                StateEditor stateEditor = m_worldStateEditor.stateEditors[i];
                stateChoice.Add(stateEditor.stateNameProperty.stringValue, i);
            }


            StateIdListEditor.stateTypePopup = new PopupField<string>();
            StateIdListEditor.stateTypePopup.choices = stateChoice.Keys.ToList();
            StateIdListEditor.stateTypePopup.index = 0;
            StateIdListEditor.stateTypePopup.style.width = new Length(40f, LengthUnit.Percent);
            StateIdListEditor.stateTypePopup.style.alignSelf = Align.Center;
            StateIdListEditor.stateTypePopup.style.position = Position.Absolute;

            StateIdListEditor.addStateIdButton = new Button(
                delegate
                {
                    if (StateIdListEditor.stateTypePopup.choices.Count == 0)
                    {
                        Debug.Log($"All state is added in effect of action");
                        return;
                    }

                    string selectedTypeName = StateIdListEditor.stateTypePopup.choices[StateIdListEditor.stateTypePopup.index];

                    if (selectedTypeName == string.Empty)
                        return;

                    int id = stateChoice[selectedTypeName];
                    Type type = m_GOAP.worldState.states[id].stateValue.Value.GetType();
                    object value = Activator.CreateInstance(type);

                    StateValue stateValue = new StateValue();
                    stateValue.Value = value;

                    int stateIdCount = StateIdListEditor.stateIdsProperty.arraySize - 1;

                    if (stateIdCount < 0)
                        stateIdCount = 0;

                    StateIdListEditor.stateIdsProperty.InsertArrayElementAtIndex(stateIdCount);

                    stateIdCount = StateIdListEditor.stateIdsProperty.arraySize - 1;

                    SerializedProperty stateIdProperty = StateIdListEditor.stateIdsProperty.GetArrayElementAtIndex(stateIdCount);
                    stateIdProperty.FindPropertyRelative("Id").intValue = id;
                    SerializedProperty stateValueProperty = stateIdProperty.FindPropertyRelative("StateValue");
                    stateValueProperty.FindPropertyRelative("m_value").managedReferenceValue = stateValue.Value;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    InitializeStateId(StateIdListEditor, StateIdListEditor.stateIdsProperty.arraySize - 1);
                    InitializeAddStateIdButton(StateIdListEditor);
                    Compose();
                });

            StateIdListEditor.addStateIdButton.text = "Add States";
            StateIdListEditor.addStateIdButton.style.width = new Length(25f, LengthUnit.Percent);
            StateIdListEditor.addStateIdButton.style.alignSelf = Align.FlexEnd;
            StateIdListEditor.addStateIdButton.style.position = Position.Absolute;
        }

        private void InitializeGoalListEditor()
        {
            m_goalListEditor = new GoalListEditor();
            m_goalListEditor.goalEditors = new List<GoalEditor>();
            m_goalListEditor.goalsProperty = serializedObject.FindProperty("m_goals");

            m_goalListEditor.addGoalButton = new Button(delegate
            {
                m_GOAP.Goals.Add(new Goal());
                serializedObject.Update();

                InitializeGoalEditor(m_GOAP.Goals.Count - 1);
                Compose();
            });
            m_goalListEditor.addGoalButton.text = "Add Goal";
            m_goalListEditor.addGoalButton.style.width = new Length(70f, LengthUnit.Percent);
            m_goalListEditor.addGoalButton.style.alignSelf = Align.Center;
            //m_goalListEditor.AddGoalButton.style.position = Position.Absolute;

            int goalCount = m_GOAP.Goals.Count;

            for (int i = 0; i < goalCount; ++i)
                InitializeGoalEditor(i);
        }
        private void InitializeGoalEditor(int index)
        {
            GoalEditor goalEditor = new GoalEditor();

            SerializedProperty goalProperty = m_goalListEditor.goalsProperty.GetArrayElementAtIndex(index);

            goalEditor.stateEditor = new StateIdListEditor();
            goalEditor.stateEditor.stateIdsProperty = goalProperty.FindPropertyRelative("m_states");
            goalEditor.stateEditor.stateIdEditors = new List<StateIdEditor>();
            InitializeStateIdList(goalEditor.stateEditor, "States");

            goalEditor.animationCurveProperty = goalProperty.FindPropertyRelative("m_animationCurve");
            goalEditor.curveField = new CurveField();
            goalEditor.curveField.BindProperty(goalEditor.animationCurveProperty);
            goalEditor.curveField.style.height = 50;

            goalEditor.textElement = new TextElement();
            goalEditor.textElement.text = "Consideration Curve";

            goalEditor.removeGoalButton = new Button(
                    delegate
                    {
                        int id = m_goalListEditor.goalEditors.IndexOf(goalEditor);
                        m_goalListEditor.goalsProperty.DeleteArrayElementAtIndex(id);
                        serializedObject.ApplyModifiedProperties();

                        InitializeGoalListEditor();
                        Compose();
                    });

            goalEditor.removeGoalButton.text = "Remove";
            goalEditor.removeGoalButton.style.width = new Length(25f, LengthUnit.Percent);
            goalEditor.removeGoalButton.style.alignSelf = Align.FlexEnd;
            goalEditor.removeGoalButton.style.position = Position.Absolute;

            goalEditor.showFoldout = EditorUtils.CreateFoldout($"Goal_{index}", 15, Color.white, FlexDirection.Column);
            goalEditor.showFoldout.value = false;

            SerializedProperty methodProperty = goalProperty.FindPropertyRelative("m_considerationMethod");
            SerializedProperty componentNameProperty = methodProperty.FindPropertyRelative("m_componentName");

            goalEditor.componentNameProperty = componentNameProperty;
            goalEditor.componentPopupField = new PopupField<string>("Component", m_considerationMethods.Keys.ToList(), 0);
            goalEditor.componentPopupField.BindProperty(goalEditor.componentNameProperty);
            goalEditor.componentPopupField.RegisterValueChangedCallback(delegate { SetMethodPopupField(goalEditor, m_considerationMethods); });

            SerializedProperty methodNameProperty = methodProperty.FindPropertyRelative("m_methodName");

            goalEditor.methodNameProperty = methodNameProperty;
            goalEditor.methodPopupField = new PopupField<string>("Methods");
            goalEditor.methodPopupField.BindProperty(goalEditor.methodNameProperty);
            SetMethodPopupField(goalEditor, m_considerationMethods);

            m_goalListEditor.goalEditors.Add(goalEditor);
        }

        private void ComposeWorldState()
        {
            VisualElement worldStateLabel = EditorUtils.CreateLabel(1f, 5f, 0f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
            VisualElement worldStateText = EditorUtils.CreateText("World State", 20f, Align.Center);

            worldStateLabel.Add(worldStateText);

            VisualElement addStateLabel = EditorUtils.CreateLabel(1f, 5f, 10f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);

            addStateLabel.Add(m_worldStateEditor.stateNameField);
            addStateLabel.Add(m_worldStateEditor.statePopupField);
            addStateLabel.Add(m_worldStateEditor.addStateButton);

            worldStateLabel.Add(addStateLabel);

            int stateCount = m_worldStateEditor.stateEditors.Count;
            for (int i = 0; i < stateCount; ++i)
            {
                StateEditor stateEditor = m_worldStateEditor.stateEditors[i];

                stateEditor.showFoldout.Add(stateEditor.valueField);
                stateEditor.showFoldout.Add(stateEditor.componentPopupField);
                stateEditor.showFoldout.Add(stateEditor.methodPopupField);

                VisualElement stateFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
                stateFoldoutLabel.Add(stateEditor.showFoldout);
                stateFoldoutLabel.Add(stateEditor.removeStateButton);
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

            addActionLabel.Add(m_actionSetEditor.actionNameField);
            addActionLabel.Add(m_actionSetEditor.addActionButton);

            actionSetLabel.Add(addActionLabel);


            int actionCount = m_actionSetEditor.actionEditors.Count;
            for (int i = 0; i < actionCount; ++i)
            {
                ActionEditor actionEditor = m_actionSetEditor.actionEditors[i];
                actionEditor.showFoldout.Clear();
                actionEditor.stateIdListEditor.showFoldout.Clear();
                actionEditor.preconditionListEditor.showFoldout.Clear();

                actionEditor.showFoldout.Add(actionEditor.componentPopupField);
                actionEditor.showFoldout.Add(actionEditor.methodPopupField);

                actionEditor.showFoldout.Add(ComposeStateIdEditor(actionEditor.stateIdListEditor));

                VisualElement preconditionListFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
                preconditionListFoldoutLabel.Add(actionEditor.preconditionListEditor.showFoldout);
                preconditionListFoldoutLabel.Add(actionEditor.preconditionListEditor.addPreconditionButton);

                int preconditionCount = actionEditor.preconditionListEditor.preconditionEditors.Count;
                for (int j = 0; j < preconditionCount; ++j)
                {
                    PreconditionEditor preconditionEditor = actionEditor.preconditionListEditor.preconditionEditors[j];
                    preconditionEditor.showFoldout.Clear();
                    preconditionEditor.stateIdListEditor.showFoldout.Clear();

                    VisualElement preconditionFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
                    preconditionFoldoutLabel.Add(preconditionEditor.showFoldout);
                    preconditionFoldoutLabel.Add(preconditionEditor.removePreconditionButton);

                    preconditionEditor.showFoldout.Add(preconditionEditor.costField);
                    preconditionEditor.showFoldout.Add(ComposeStateIdEditor(preconditionEditor.stateIdListEditor));

                    actionEditor.preconditionListEditor.showFoldout.Add(preconditionFoldoutLabel);
                }

                actionEditor.showFoldout.Add(preconditionListFoldoutLabel);

                VisualElement actionFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
                actionFoldoutLabel.Add(actionEditor.showFoldout);
                actionFoldoutLabel.Add(actionEditor.removeActionButton);

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
            goalListSetLabel.Add(m_goalListEditor.addGoalButton);
            goalListSetLabel.Add(EditorUtils.CreateSpace(new Vector2(0f, 10f)));

            for (int i = 0; i < m_goalListEditor.goalEditors.Count; i++)
            {
                GoalEditor goalEditor = m_goalListEditor.goalEditors[i];
                VisualElement goalSetLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);

                goalEditor.showFoldout.Clear();

                goalEditor.showFoldout.Add(goalEditor.textElement);
                goalEditor.showFoldout.Add(goalEditor.componentPopupField);
                goalEditor.showFoldout.Add(goalEditor.methodPopupField);
                goalEditor.showFoldout.Add(goalEditor.curveField);

                goalEditor.stateEditor.showFoldout.Clear();
                goalEditor.showFoldout.Add(ComposeStateIdEditor(goalEditor.stateEditor));

                goalSetLabel.Add(goalEditor.showFoldout);
                goalSetLabel.Add(goalEditor.removeGoalButton);

                goalListSetLabel.Add(goalSetLabel);
            }

            m_root.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));
            m_root.Add(goalListSetLabel);
        }

        private VisualElement ComposeStateIdEditor(StateIdListEditor stateIdListEditor)
        {
            VisualElement stateIdListFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
            stateIdListFoldoutLabel.Add(stateIdListEditor.showFoldout);
            stateIdListFoldoutLabel.Add(stateIdListEditor.stateTypePopup);
            stateIdListFoldoutLabel.Add(stateIdListEditor.addStateIdButton);

            int stateIdCount = stateIdListEditor.stateIdEditors.Count;

            for (int j = 0; j < stateIdCount; ++j)
            {
                StateIdEditor stateIdEditor = stateIdListEditor.stateIdEditors[j];

                stateIdEditor.showFoldout.Add(stateIdEditor.valueField);

                VisualElement stateIdFoldoutLabel = EditorUtils.CreateLabel(1f, 5f, 5f, Color.gray, new Color(0.3f, 0.3f, 0.3f), FlexDirection.Column);
                stateIdFoldoutLabel.Add(stateIdEditor.showFoldout);
                stateIdFoldoutLabel.Add(stateIdEditor.removeStateIdButton);

                stateIdListEditor.showFoldout.Add(stateIdFoldoutLabel);
            }

            return stateIdListFoldoutLabel;
        }
    }
}
