using AIPlanner.GOAP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIPlanner.GOAP
{
    public class GOAPWindowEditor : EditorWindow
    {
        class FoldoutEditor
        {
            public SerializedProperty showProperty;
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
            public Button showButton;

            public SerializedProperty worldStateProperty;
            public SerializedProperty statesProperty;

            public List<StateEditor> stateEditors;

            public Button addStateButton;
            public PopupField<string> statePopupField;
            public TextField stateNameField;

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
            public TextField preconditionNameField;


            public SerializedProperty preconditionsProperty;
            public List<PreconditionEditor> preconditionEditors;
        }

        class ActionEditor : MethodEditor
        {
            public Button showPrecondition;
            public Button showEffect;
            public SerializedProperty currentActionPanelProperty;

            public SerializedProperty actionProperty;
            public Button removeActionButton;

            public StateIdListEditor stateIdListEditor;
            public PreconditionListEditor preconditionListEditor;
        }

        class ActionSetEditor
        {
            public Button showButton;

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
            public Button showButton;

            public Button addGoalButton;
            public TextField goalNameField;

            public SerializedProperty goalsProperty;
            public List<GoalEditor> goalEditors;
        }

        private WorldStateEditor m_worldStateEditor;
        private ActionSetEditor m_actionSetEditor;
        private GoalListEditor m_goalListEditor;

        private Dictionary<string, string[]> m_stateMethods;
        private Dictionary<string, string[]> m_actionMethods;
        private Dictionary<string, string[]> m_considerationMethods;
        private List<string> m_supportedTypesString;

        public GOAP goap;
        private PropertyField m_goapField;

        private SerializedObject m_serializedObject;
        private SerializedObject m_goapObject;

        private Type m_currentGOAPPanel = null;

        private Color m_backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        private Color m_borderColor = Color.gray;

        private ScrollView m_panelScrolView;

        [MenuItem("Planner/GOAP Debug")]
        static void Init()
        {
            GOAPWindowEditor window = GetWindow<GOAPWindowEditor>();
            window.titleContent = new GUIContent("GOAP Editor");
            window.Show();
        }

        private void CreateGUI()
        {
            Initialize();
            Compose();
        }

        private void OnFocus()
        {
            InitalizeGOAP();
            Compose();
        }

        private void Initialize()
        {
            m_panelScrolView = new ScrollView(ScrollViewMode.Vertical);

            m_serializedObject = new SerializedObject(this);
            SerializedProperty goapProperty = m_serializedObject.FindProperty(nameof(goap));

            m_goapField = new PropertyField();
            m_goapField.BindProperty(goapProperty);
            m_goapField.label = "GOAP";
            m_goapField.TrackPropertyValue(goapProperty, delegate
            {
                InitalizeGOAP();
                Compose();
            });

            InitalizeGOAP();

        }
        private void Compose()
        {
            rootVisualElement.Clear();
            m_panelScrolView.Clear();

            m_panelScrolView.Add(m_goapField);

            ComposeGOAP();

            rootVisualElement.Add(m_panelScrolView);
            rootVisualElement.Bind(m_serializedObject);
        }

        #region Initialize

        private void InitalizeGOAP()
        {
            if (goap == null)
            {
                m_goapObject = null;
                return;
            }

            Method.GetStateMethods(goap.gameObject, out m_stateMethods);
            Method.GetActionMethods(goap.gameObject, out m_actionMethods);
            Method.GetConsiderationMethods(goap.gameObject, out m_considerationMethods);
            m_supportedTypesString = StateType.SupportedTypesString;

            m_goapObject = new SerializedObject(goap);

            InitializeWorldState();
            InitializeActionSet();
            InitializeGoalListEditor();
        }

        private void SetMethodPopupField(MethodEditor methodEditor, in Dictionary<string, string[]> inMethods)
        {
            if (methodEditor.componentNameProperty.stringValue != string.Empty)
            {
                string[] methods;
                inMethods.TryGetValue(methodEditor.componentNameProperty.stringValue, out methods);

                if (methods != null && methods.Length != 0)
                {
                    string methodName = methodEditor.methodNameProperty.stringValue;
                    List<string> methodsName = methods.ToList();

                    methodEditor.methodPopupField.choices = methodsName;

                    methodEditor.methodPopupField.index = GetListStringIndex(methodsName, methodName);
                }
                else
                {
                    methodEditor.methodPopupField.choices = new List<string>();
                    methodEditor.methodPopupField.index = 0;
                }
            }
        }

        private void SetComponentPopupField(MethodEditor methodEditor, Dictionary<string, string[]> methods)
        {
            List<string> componentNames = methods.Keys.ToList();

            if (componentNames.Count > 0)
            {
                string componentName = methodEditor.componentNameProperty.stringValue;
                methodEditor.componentPopupField.choices = componentNames;
                methodEditor.componentPopupField.index = GetListStringIndex(componentNames, componentName);
            }
            else
            {
                methodEditor.componentNameProperty.stringValue = null;
                methodEditor.componentPopupField.choices = new List<string>();
                methodEditor.componentPopupField.index = 0;
                m_goapObject.ApplyModifiedPropertiesWithoutUndo();
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

            if (m_currentGOAPPanel == null)
                m_currentGOAPPanel = m_worldStateEditor.GetType();

            InitializeGOAPPanelButton(ref m_worldStateEditor.showButton, m_worldStateEditor.GetType(), "WorldState");


            m_worldStateEditor.worldStateProperty = m_goapObject.FindProperty("m_worldState");

            m_worldStateEditor.stateNameField = new TextField();
            m_worldStateEditor.stateNameField.style.width = new Length(40f, LengthUnit.Percent);

            m_worldStateEditor.statePopupField = new PopupField<string>(m_supportedTypesString, 0);
            m_worldStateEditor.statePopupField.style.width = new Length(30f, LengthUnit.Percent);

            m_worldStateEditor.addStateButton = new Button(delegate
            {
                string stateName = m_worldStateEditor.stateNameField.text;
                string selectedTypeName = m_worldStateEditor.statePopupField.choices[m_worldStateEditor.statePopupField.index];

                if (stateName == string.Empty && selectedTypeName == string.Empty)
                    return;

                foreach (StateEditor stateEditor in m_worldStateEditor.stateEditors)
                    if (stateEditor.stateNameProperty.stringValue == stateName)
                        return;

                Type type = StateType.GetType(selectedTypeName);

                State state = new State();
                state.name = stateName;
                object value = Activator.CreateInstance(type);
                state.stateValue = new StateValue();
                state.stateValue.Value = value;

                goap.WorldState.states.Add(state);
                m_goapObject.Update();

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
            m_worldStateEditor.addStateButton.text = "Add State";
            m_worldStateEditor.addStateButton.style.width = new Length(28f, LengthUnit.Percent);

            InitializeWorldStateList();
        }
        private void InitializeWorldStateList()
        {
            m_worldStateEditor.stateEditors.Clear();

            if (goap.WorldState.states == null)
                goap.WorldState.states = new List<State>();

            m_worldStateEditor.statesProperty = m_worldStateEditor.worldStateProperty.FindPropertyRelative(nameof(WorldState.states));

            int stateCount = goap.WorldState.states.Count;

            for (int i = 0; i < stateCount; ++i)
            {
                StateEditor stateEditor = new StateEditor();

                SerializedProperty stateProperty = m_worldStateEditor.statesProperty.GetArrayElementAtIndex(i);


                stateEditor.showProperty = stateProperty.FindPropertyRelative(nameof(State.show));
                stateEditor.stateNameProperty = stateProperty.FindPropertyRelative(nameof(State.name));

                stateEditor.showFoldout = EditorUtils.CreateFoldout(stateEditor.stateNameProperty.stringValue, 5f, Color.white, FlexDirection.Column);
                stateEditor.showFoldout.BindProperty(stateEditor.showProperty);

                SerializedProperty stateValueProperty = stateProperty.FindPropertyRelative(nameof(State.stateValue));

                stateEditor.valueProperty = stateValueProperty.FindPropertyRelative("m_value");
                stateEditor.valueField = new PropertyField();
                stateEditor.valueField.label = stateEditor.valueProperty.managedReferenceValue.GetType().Name;
                stateEditor.valueField.BindProperty(stateEditor.valueProperty);

                SerializedProperty methodProperty = stateProperty.FindPropertyRelative("m_stateMethod");
                InitializeMethodEditor(stateEditor, methodProperty, m_stateMethods);

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
                stateEditor.removeStateButton.style.width = new Length(15f, LengthUnit.Percent);
                stateEditor.removeStateButton.style.alignSelf = Align.FlexEnd;
                stateEditor.removeStateButton.style.position = Position.Absolute;
                stateEditor.removeStateButton.style.right = 5f;

                m_worldStateEditor.stateEditors.Add(stateEditor);
            }
        }

        private void InitializeActionSet()
        {
            m_actionSetEditor = new ActionSetEditor();
            m_actionSetEditor.actionEditors = new List<ActionEditor>();

            InitializeGOAPPanelButton(ref m_actionSetEditor.showButton, m_actionSetEditor.GetType(), "Action Set");

            m_actionSetEditor.actionSetProperty = m_goapObject.FindProperty("m_actionSet");

            m_actionSetEditor.actionNameField = new TextField("Action Name");

            m_actionSetEditor.addActionButton = new Button(delegate
            {
                string actionName = m_actionSetEditor.actionNameField.text;

                if (string.IsNullOrWhiteSpace(actionName))
                    return;

                int actionCount = m_actionSetEditor.actionSetProperty.arraySize - 1;

                if (actionCount == -1)
                    actionCount = 0;

                Action newAction = new Action();
                newAction.name = actionName;

                goap.ActionSet.Add(newAction);
                m_goapObject.Update();

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

                actionEditor.currentActionPanelProperty = actionEditor.actionProperty.FindPropertyRelative(nameof(Action.currentActionPanelType));

                InitializeActionPanelButton(actionEditor, ref actionEditor.showPrecondition, typeof(PreconditionListEditor), "Preconditions");
                InitializeActionPanelButton(actionEditor, ref actionEditor.showEffect, typeof(StateIdListEditor), "Effects");



                actionEditor.showProperty = actionEditor.actionProperty.FindPropertyRelative(nameof(Action.show));
                SerializedProperty actionNameProperty = actionEditor.actionProperty.FindPropertyRelative(nameof(Action.name));
                actionEditor.showFoldout = EditorUtils.CreateFoldout(actionNameProperty.stringValue, 5f, Color.white, FlexDirection.Column);
                actionEditor.showFoldout.BindProperty(actionEditor.showProperty);

                InitializeMethodEditor(actionEditor, actionEditor.actionProperty, m_actionMethods);

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
                actionEditor.stateIdListEditor.showProperty = actionEditor.actionProperty.FindPropertyRelative(nameof(Action.showEffects));
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
            preconditionListEditor.preconditionNameField = new TextField("Precondition Name");
            preconditionListEditor.preconditionNameField.style.width = new Length(70f, LengthUnit.Percent);
            preconditionListEditor.addPreconditionButton = new Button(delegate
            {
                string preconditionName = preconditionListEditor.preconditionNameField.text;

                if (string.IsNullOrWhiteSpace(preconditionName))
                    return;

                int preconditionCount = preconditionListEditor.preconditionsProperty.arraySize - 1;

                if (preconditionCount < 0)
                    preconditionCount = 0;

                int actionId = m_actionSetEditor.actionEditors.IndexOf(ActionEditor);
                goap.ActionSet[actionId].Preconditions.Add(new Action.Precondition() { name = preconditionName });
                m_goapObject.Update();

                InitializePreconditionList(ActionEditor);
                Compose();
            });
            preconditionListEditor.addPreconditionButton.text = "Add Precondition";
            preconditionListEditor.addPreconditionButton.style.width = new Length(29f, LengthUnit.Percent);
            preconditionListEditor.addPreconditionButton.style.alignSelf = Align.Center;

            preconditionListEditor.showProperty = ActionEditor.actionProperty.FindPropertyRelative(nameof(Action.showPreconditions));

            preconditionListEditor.showFoldout = EditorUtils.CreateFoldout("Preconditions", 5f, Color.white, FlexDirection.Column);
            preconditionListEditor.showFoldout.BindProperty(preconditionListEditor.showProperty);

            int preconditionCount = preconditionListEditor.preconditionsProperty.arraySize;

            for (int i = 0; i < preconditionCount; i++)
                InitializePreconditionEditor(ActionEditor, preconditionListEditor, i);
        }
        private void InitializePreconditionEditor(ActionEditor ActionEditor, PreconditionListEditor PreconditionListEditor, int Index)
        {
            PreconditionEditor preconditionEditor = new PreconditionEditor();
            preconditionEditor.stateIdListEditor = new StateIdListEditor();

            SerializedProperty preconditionProperty = PreconditionListEditor.preconditionsProperty.GetArrayElementAtIndex(Index);

            string preconditionName = preconditionProperty.FindPropertyRelative(nameof(Action.Precondition.name)).stringValue;
            preconditionEditor.showProperty = preconditionProperty.FindPropertyRelative(nameof(Action.Precondition.show));
            preconditionEditor.showFoldout = EditorUtils.CreateFoldout(preconditionName, 15, Color.white, FlexDirection.Column);
            preconditionEditor.showFoldout.BindProperty(preconditionEditor.showProperty);


            preconditionEditor.costProperty = preconditionProperty.FindPropertyRelative("cost");
            preconditionEditor.costField = new PropertyField();
            preconditionEditor.costField.BindProperty(preconditionEditor.costProperty);

            preconditionEditor.stateIdListEditor.stateIdsProperty = preconditionProperty.FindPropertyRelative("states");
            preconditionEditor.stateIdListEditor.stateIdEditors = new List<StateIdEditor>();
            preconditionEditor.stateIdListEditor.showProperty = preconditionProperty.FindPropertyRelative(nameof(Action.Precondition.showStates));
            InitializeStateIdList(preconditionEditor.stateIdListEditor, "States");

            preconditionEditor.removePreconditionButton = new Button(
                delegate
                {
                    int id = PreconditionListEditor.preconditionEditors.IndexOf(preconditionEditor);
                    PreconditionListEditor.preconditionsProperty.DeleteArrayElementAtIndex(id);
                    m_goapObject.ApplyModifiedProperties();

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

            StateIdListEditor.showFoldout = EditorUtils.CreateFoldout(FoldoutText, 5f, Color.white, FlexDirection.Column);
            StateIdListEditor.showFoldout.BindProperty(StateIdListEditor.showProperty);

            InitializeAddStateIdButton(StateIdListEditor);
        }
        private void InitializeStateId(StateIdListEditor StateIdListEditor, int Index)
        {
            StateIdEditor stateIdEditor = new StateIdEditor();

            stateIdEditor.stateIdProperty = StateIdListEditor.stateIdsProperty.GetArrayElementAtIndex(Index);
            stateIdEditor.idOfStateProperty = stateIdEditor.stateIdProperty.FindPropertyRelative(nameof(StateId.id));

            stateIdEditor.showProperty = stateIdEditor.stateIdProperty.FindPropertyRelative(nameof(StateId.show));

            string stateName = m_worldStateEditor.stateEditors[stateIdEditor.idOfStateProperty.intValue].stateNameProperty.stringValue;
            stateIdEditor.showFoldout = EditorUtils.CreateFoldout(stateName, 5f, Color.white, FlexDirection.Column);
            stateIdEditor.showFoldout.BindProperty(stateIdEditor.showProperty);

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
                    m_goapObject.ApplyModifiedProperties();

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
                    Type type = goap.WorldState.states[id].stateValue.Value.GetType();
                    object value = Activator.CreateInstance(type);

                    StateValue stateValue = new StateValue();
                    stateValue.Value = value;

                    int stateIdCount = StateIdListEditor.stateIdsProperty.arraySize - 1;

                    if (stateIdCount < 0)
                        stateIdCount = 0;

                    StateIdListEditor.stateIdsProperty.InsertArrayElementAtIndex(stateIdCount);

                    stateIdCount = StateIdListEditor.stateIdsProperty.arraySize - 1;

                    SerializedProperty stateIdProperty = StateIdListEditor.stateIdsProperty.GetArrayElementAtIndex(stateIdCount);
                    stateIdProperty.FindPropertyRelative("id").intValue = id;
                    SerializedProperty stateValueProperty = stateIdProperty.FindPropertyRelative("stateValue");
                    stateValueProperty.FindPropertyRelative("m_value").managedReferenceValue = stateValue.Value;
                    m_goapObject.ApplyModifiedPropertiesWithoutUndo();

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

            InitializeGOAPPanelButton(ref m_goalListEditor.showButton, m_goalListEditor.GetType(), "Goal");


            m_goalListEditor.goalsProperty = m_goapObject.FindProperty("m_goals");

            m_goalListEditor.goalNameField = new TextField("Goal Name");
            m_goalListEditor.goalNameField.style.width = new Length(70f, LengthUnit.Percent);

            m_goalListEditor.addGoalButton = new Button(delegate
            {
                string goalName = m_goalListEditor.goalNameField.text;

                if (string.IsNullOrWhiteSpace(goalName))
                    return;

                goap.Goals.Add(new Goal() { name = goalName});
                m_goapObject.Update();

                InitializeGoalEditor(goap.Goals.Count - 1);
                Compose();
            });
            m_goalListEditor.addGoalButton.text = "Add Goal";
            m_goalListEditor.addGoalButton.style.width = new Length(29f, LengthUnit.Percent);
            m_goalListEditor.addGoalButton.style.alignSelf = Align.FlexEnd;

            int goalCount = goap.Goals.Count;

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
            goalEditor.stateEditor.showProperty = goalProperty.FindPropertyRelative(nameof(Goal.showStates));
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
                        m_goapObject.ApplyModifiedProperties();

                        InitializeGoalListEditor();
                        Compose();
                    });

            goalEditor.removeGoalButton.text = "Remove";
            goalEditor.removeGoalButton.style.width = new Length(25f, LengthUnit.Percent);
            goalEditor.removeGoalButton.style.alignSelf = Align.FlexEnd;
            goalEditor.removeGoalButton.style.position = Position.Absolute;


            goalEditor.showProperty = goalProperty.FindPropertyRelative(nameof(Goal.show));
            string goalName = goalProperty.FindPropertyRelative(nameof(Goal.name)).stringValue;
            goalEditor.showFoldout = EditorUtils.CreateFoldout(goalName, 5f, Color.white, FlexDirection.Column);
            goalEditor.showFoldout.BindProperty(goalEditor.showProperty);

            SerializedProperty methodProperty = goalProperty.FindPropertyRelative("m_considerationMethod");
            InitializeMethodEditor(goalEditor, methodProperty, m_considerationMethods);

            m_goalListEditor.goalEditors.Add(goalEditor);
        }

        private void InitializeMethodEditor(MethodEditor methodEditor, SerializedProperty methodProperty, Dictionary<string, string[]> methods)
        {
            methodEditor.componentNameProperty = methodProperty.FindPropertyRelative("m_componentName");
            methodEditor.methodNameProperty = methodProperty.FindPropertyRelative("m_methodName");

            methodEditor.componentPopupField = new PopupField<string>("Component");
            methodEditor.componentPopupField.BindProperty(methodEditor.componentNameProperty);
            SetComponentPopupField(methodEditor, methods);
            methodEditor.componentPopupField.RegisterValueChangedCallback(delegate { SetMethodPopupField(methodEditor, methods); });

            methodEditor.methodPopupField = new PopupField<string>("Methods");
            methodEditor.methodPopupField.BindProperty(methodEditor.methodNameProperty);
            SetMethodPopupField(methodEditor, methods);
        }

        private void InitializeGOAPPanelButton(ref Button button, Type goapPanelType, string textButton)
        {
            button = new Button(
                    delegate
                    {
                        m_currentGOAPPanel = goapPanelType;
                        Compose();
                    });

            button.text = textButton;
            button.style.fontSize = 15f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.width = new Length(33f, LengthUnit.Percent);
            button.style.height = new Length(35f);
            button.style.right = 3f;
        }

        private void InitializeActionPanelButton(ActionEditor actionEditor, ref Button button, Type actionPanelType, string textButton)
        {
            button = new Button(
                    delegate
                    {
                        actionEditor.currentActionPanelProperty.stringValue = actionPanelType.FullName;
                        m_goapObject.ApplyModifiedPropertiesWithoutUndo();
                        Compose();
                    });

            button.text = textButton;
            button.style.fontSize = 15f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.width = new Length(49.5f, LengthUnit.Percent);
            button.style.height = new Length(30f);
            button.style.right = 3f;
        }
        #endregion

        #region Compose
        private void ComposeGOAP()
        {
            if (goap == null)
                return;

            VisualElement goapPanelLabel = EditorUtils.CreateLabel(1f, 5f, 10f, 20f, 20f, m_borderColor, m_backgroundColor, FlexDirection.Row);

            m_panelScrolView.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));
            m_panelScrolView.Add(goapPanelLabel);

            goapPanelLabel.Add(m_worldStateEditor.showButton);
            goapPanelLabel.Add(m_actionSetEditor.showButton);
            goapPanelLabel.Add(m_goalListEditor.showButton);

            if (m_currentGOAPPanel == typeof(WorldStateEditor))
            {
                ComposeWorldState();
                m_worldStateEditor.showButton.style.color = Color.white;
                m_actionSetEditor.showButton.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                m_goalListEditor.showButton.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            }
            else if (m_currentGOAPPanel == typeof(ActionSetEditor))
            {
                ComposeActionSet();
                m_worldStateEditor.showButton.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                m_actionSetEditor.showButton.style.color = Color.white;
                m_goalListEditor.showButton.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            }
            else
            {
                ComposeGoal();
                m_worldStateEditor.showButton.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                m_actionSetEditor.showButton.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                m_goalListEditor.showButton.style.color = Color.white;
            }

        }

        private void ComposeWorldState()
        {
            VisualElement addStateLabel = EditorUtils.CreateLabel(1f, 5f, 10f, 50f, 50f, m_borderColor, m_backgroundColor, FlexDirection.Row);

            addStateLabel.Add(m_worldStateEditor.stateNameField);
            addStateLabel.Add(m_worldStateEditor.statePopupField);
            addStateLabel.Add(m_worldStateEditor.addStateButton);

            m_panelScrolView.Add(addStateLabel);

            int stateCount = m_worldStateEditor.stateEditors.Count;
            for (int i = 0; i < stateCount; ++i)
            {
                StateEditor stateEditor = m_worldStateEditor.stateEditors[i];


                stateEditor.showFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 10f)));
                stateEditor.showFoldout.Add(stateEditor.valueField);
                stateEditor.showFoldout.Add(stateEditor.componentPopupField);
                stateEditor.showFoldout.Add(stateEditor.methodPopupField);

                VisualElement stateFoldoutLabel = EditorUtils.CreateLabel(1f, 0f, 15f, 20f, 20f, m_borderColor, m_backgroundColor, FlexDirection.Column);

                VisualElement foldoutLabel = EditorUtils.CreateFoldoutLabel(m_borderColor, m_backgroundColor);

                stateFoldoutLabel.Add(foldoutLabel);
                stateFoldoutLabel.Add(stateEditor.showFoldout);
                stateFoldoutLabel.Add(stateEditor.removeStateButton);
                m_panelScrolView.Add(stateFoldoutLabel);
            }
        }
        private void ComposeActionSet()
        {
            VisualElement addActionLabel = EditorUtils.CreateLabel(1f, 5f, 10f, 50f, 50f, m_borderColor, m_backgroundColor, FlexDirection.Column);

            addActionLabel.Add(m_actionSetEditor.actionNameField);
            addActionLabel.Add(m_actionSetEditor.addActionButton);

            m_panelScrolView.Add(addActionLabel);


            int actionCount = m_actionSetEditor.actionEditors.Count;
            for (int i = 0; i < actionCount; ++i)
            {
                ActionEditor actionEditor = m_actionSetEditor.actionEditors[i];
                actionEditor.showFoldout.Clear();
                actionEditor.stateIdListEditor.showFoldout.Clear();
                actionEditor.preconditionListEditor.showFoldout.Clear();

                actionEditor.showFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 10f)));
                actionEditor.showFoldout.Add(actionEditor.componentPopupField);
                actionEditor.showFoldout.Add(actionEditor.methodPopupField);
                actionEditor.showFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));

                VisualElement actionPanelLabel = EditorUtils.CreateLabel(1f, 5f, 10f, 80f, 80f, m_borderColor, m_backgroundColor, FlexDirection.Row);
                actionPanelLabel.Add(actionEditor.showEffect);
                actionPanelLabel.Add(actionEditor.showPrecondition);

                actionEditor.showFoldout.Add(actionPanelLabel);

                if (actionEditor.currentActionPanelProperty.stringValue == typeof(PreconditionListEditor).FullName)
                {
                    ComposePreconditionListEditor(actionEditor.preconditionListEditor, actionEditor.showFoldout);
                    actionEditor.showEffect.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    actionEditor.showPrecondition.style.color = Color.white;
                }
                else
                {
                    actionEditor.showFoldout.Add(ComposeStateIdEditor(actionEditor.stateIdListEditor));
                    actionEditor.showPrecondition.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    actionEditor.showEffect.style.color = Color.white;
                }

                VisualElement actionFoldoutLabel = EditorUtils.CreateLabel(1f, 0f, 15f, 20f, 20f, m_borderColor, m_backgroundColor, FlexDirection.Column);
                VisualElement foldoutOverlay = EditorUtils.CreateFoldoutLabel(m_borderColor, m_backgroundColor);

                actionFoldoutLabel.Add(foldoutOverlay);
                actionFoldoutLabel.Add(actionEditor.showFoldout);
                actionFoldoutLabel.Add(actionEditor.removeActionButton);

                m_panelScrolView.Add(actionFoldoutLabel);
            }

            m_panelScrolView.Add(EditorUtils.CreateSpace(new Vector2(0, 20f)));
        }

        private void ComposePreconditionListEditor(PreconditionListEditor preconditionListEditor, VisualElement root)
        {
            VisualElement addPreconditionLabel = EditorUtils.CreateLabel(1f, 5f, 10f, 50f, 50f, m_borderColor, m_backgroundColor, FlexDirection.Row);
            addPreconditionLabel.Add(preconditionListEditor.preconditionNameField);
            addPreconditionLabel.Add(preconditionListEditor.addPreconditionButton);

            root.Add(addPreconditionLabel);
            root.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));

            int preconditionCount = preconditionListEditor.preconditionEditors.Count;
            for (int j = 0; j < preconditionCount; ++j)
            {
                PreconditionEditor preconditionEditor = preconditionListEditor.preconditionEditors[j];
                preconditionEditor.showFoldout.Clear();
                preconditionEditor.stateIdListEditor.showFoldout.Clear();

                VisualElement preconditionFoldoutLabel = EditorUtils.CreateLabel(1f, 0f, 20f, 0f, 20f, m_borderColor, m_backgroundColor, FlexDirection.Column);
                VisualElement preconditionFoldoutOverlay = EditorUtils.CreateFoldoutLabel(m_borderColor, m_backgroundColor);

                preconditionFoldoutLabel.Add(preconditionFoldoutOverlay);
                preconditionFoldoutLabel.Add(preconditionEditor.showFoldout);
                preconditionFoldoutLabel.Add(preconditionEditor.removePreconditionButton);

                preconditionEditor.showFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 10f)));
                preconditionEditor.showFoldout.Add(preconditionEditor.costField);
                preconditionEditor.showFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));
                preconditionEditor.showFoldout.Add(ComposeStateIdEditor(preconditionEditor.stateIdListEditor));

                root.Add(preconditionFoldoutLabel);
            }
        }

        private void ComposeGoal()
        {
            VisualElement addGoalLabel = EditorUtils.CreateLabel(1f, 5f, 10f, 50f, 50f, m_borderColor, m_backgroundColor, FlexDirection.Row);
            addGoalLabel.Add(m_goalListEditor.goalNameField);
            addGoalLabel.Add(m_goalListEditor.addGoalButton);

            m_panelScrolView.Add(addGoalLabel);
            m_panelScrolView.Add(EditorUtils.CreateSpace(new Vector2(0f, 10f)));

            for (int i = 0; i < m_goalListEditor.goalEditors.Count; i++)
            {
                GoalEditor goalEditor = m_goalListEditor.goalEditors[i];
                VisualElement goalSetLabel = EditorUtils.CreateLabel(1f, 0f, 20f, 50f, 50f, m_borderColor, m_backgroundColor, FlexDirection.Column);
                VisualElement goalFoldoutOverlay = EditorUtils.CreateFoldoutLabel(m_borderColor, m_backgroundColor);

                goalEditor.showFoldout.Clear();

                goalEditor.showFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 10f)));
                goalEditor.showFoldout.Add(goalEditor.textElement);
                goalEditor.showFoldout.Add(goalEditor.componentPopupField);
                goalEditor.showFoldout.Add(goalEditor.methodPopupField);
                goalEditor.showFoldout.Add(goalEditor.curveField);
                goalEditor.showFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));

                goalEditor.stateEditor.showFoldout.Clear();
                goalEditor.showFoldout.Add(ComposeStateIdEditor(goalEditor.stateEditor));

                goalSetLabel.Add(goalFoldoutOverlay);
                goalSetLabel.Add(goalEditor.showFoldout);
                goalSetLabel.Add(goalEditor.removeGoalButton);

                m_panelScrolView.Add(goalSetLabel);
            }
        }

        private VisualElement ComposeStateIdEditor(StateIdListEditor stateIdListEditor)
        {
            VisualElement stateIdListFoldoutLabel = EditorUtils.CreateLabel(1f, 0f, 15f, 0f, 20f, m_borderColor, m_backgroundColor, FlexDirection.Column);
            VisualElement stateIdListFoldoutOverlay = EditorUtils.CreateFoldoutLabel(m_borderColor, m_backgroundColor);

            stateIdListFoldoutLabel.Add(stateIdListFoldoutOverlay);
            stateIdListFoldoutLabel.Add(stateIdListEditor.showFoldout);
            stateIdListFoldoutLabel.Add(stateIdListEditor.stateTypePopup);
            stateIdListFoldoutLabel.Add(stateIdListEditor.addStateIdButton);

            stateIdListEditor.showFoldout.style.left = 10f;
            stateIdListEditor.showFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));
            int stateIdCount = stateIdListEditor.stateIdEditors.Count;

            for (int j = 0; j < stateIdCount; ++j)
            {
                StateIdEditor stateIdEditor = stateIdListEditor.stateIdEditors[j];
                stateIdEditor.showFoldout.Clear();
                stateIdEditor.showFoldout.style.left = 10f;

                stateIdEditor.showFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 10f)));
                stateIdEditor.showFoldout.Add(stateIdEditor.valueField);

                VisualElement stateIdFoldoutLabel = EditorUtils.CreateLabel(1f, 0f, 20f, 0f, 20f, m_borderColor, m_backgroundColor, FlexDirection.Column);
                VisualElement stateIdFoldoutOverlay = EditorUtils.CreateFoldoutLabel(m_borderColor, m_backgroundColor);

                stateIdFoldoutLabel.Add(stateIdFoldoutOverlay);
                stateIdFoldoutLabel.Add(stateIdEditor.showFoldout);
                stateIdFoldoutLabel.Add(stateIdEditor.removeStateIdButton);

                stateIdListEditor.showFoldout.Add(stateIdFoldoutLabel);
            }

            return stateIdListFoldoutLabel;
        }

        #endregion
    }
}