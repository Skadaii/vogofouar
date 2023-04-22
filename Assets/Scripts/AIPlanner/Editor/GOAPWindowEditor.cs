using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIPlanner.GOAP.Editor
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

        class DebugGoalEditor
        {
            public int goalId;
            public Slider considerationSlider;
        }

        [Serializable]
        class DebugEditor
        {
            public Button showButton;

            public WorldState debugWorldState;
            public WorldStateEditor debugWorldStateEditor;
            public Foldout showWorldStateFoldout;

            public Foldout showGoalFoldout;
            public List<DebugGoalEditor> debugGoalEditors;

            public Button resetButton;
            public Button generatePlanButton;

            public TextElement outputTextElement;
        }

        private WorldStateEditor m_worldStateEditor;
        private ActionSetEditor m_actionSetEditor;
        private GoalListEditor m_goalListEditor;
        [SerializeField] private DebugEditor m_debugEditor;

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
        private Color m_unselectedPanelBtnColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        private Color m_selectedPanelBtnColor = Color.white;

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
            if (m_serializedObject == null || m_panelScrolView == null)
                return;

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
            InitializeDebugEditor();
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

                InitializeWorldStateList(m_worldStateEditor, false);
                InitializeDebugEditor();

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

            InitializeWorldStateList(m_worldStateEditor, false);
        }
        private void InitializeWorldStateList(WorldStateEditor worldStateEditor, bool isDebugWorldState)
        {
            worldStateEditor.stateEditors.Clear();

            if (goap.WorldState.states == null)
                goap.WorldState.states = new List<State>();

            worldStateEditor.statesProperty = worldStateEditor.worldStateProperty.FindPropertyRelative(nameof(WorldState.states));

            int stateCount = worldStateEditor.statesProperty.arraySize;

            for (int i = 0; i < stateCount; ++i)
            { 
                StateEditor stateEditor = new StateEditor();

                SerializedProperty stateProperty = worldStateEditor.statesProperty.GetArrayElementAtIndex(i);

                stateEditor.showProperty = stateProperty.FindPropertyRelative(nameof(State.show));
                stateEditor.stateNameProperty = stateProperty.FindPropertyRelative(nameof(State.name));

                stateEditor.showFoldout = EditorUtils.CreateFoldout(stateEditor.stateNameProperty.stringValue, 5f, Color.white, FlexDirection.Column);
                stateEditor.showFoldout.BindProperty(stateEditor.showProperty);

                SerializedProperty stateValueProperty = stateProperty.FindPropertyRelative(nameof(State.stateValue));

                stateEditor.valueProperty = stateValueProperty.FindPropertyRelative("m_value");
                stateEditor.valueField = new PropertyField();
                stateEditor.valueField.label = stateEditor.valueProperty.managedReferenceValue.GetType().Name;
                stateEditor.valueField.BindProperty(stateEditor.valueProperty);

                if (!isDebugWorldState)
                {
                    SerializedProperty methodProperty = stateProperty.FindPropertyRelative("m_stateMethod");
                    InitializeMethodEditor(stateEditor, methodProperty, m_stateMethods);

                    stateEditor.removeStateButton = new Button(
                        delegate
                        {
                            int id = worldStateEditor.stateEditors.IndexOf(stateEditor);

                            List<StateIdEditor> linkedStateIdEditors;
                            if (StateIdEditor.allStateIdEditor.TryGetValue(id, out linkedStateIdEditors))
                            {
                                foreach (StateIdEditor stateIdEditor in linkedStateIdEditors)
                                    stateIdEditor.stateIdProperty.DeleteCommand();
                            }

                            worldStateEditor.statesProperty.serializedObject.ApplyModifiedProperties();
                            InitializeActionSet();
                            InitializeGoalListEditor();

                            worldStateEditor.statesProperty.DeleteArrayElementAtIndex(id);

                            foreach (KeyValuePair<int, List<StateIdEditor>> keyValuePair in StateIdEditor.allStateIdEditor)
                            {
                                if (keyValuePair.Key <= id)
                                    continue;

                                int newStateEditorId = keyValuePair.Key - 1;

                                foreach (StateIdEditor linkedStateIdEditor in keyValuePair.Value)
                                    linkedStateIdEditor.idOfStateProperty.intValue = newStateEditorId;
                            }

                            worldStateEditor.statesProperty.serializedObject.ApplyModifiedProperties();

                            InitializeWorldStateList(worldStateEditor, false);
                            InitializeActionSet();
                            InitializeGoalListEditor();
                            InitializeDebugEditor();

                            Compose();
                        });

                    stateEditor.removeStateButton.text = "Remove";
                    stateEditor.removeStateButton.style.width = new Length(15f, LengthUnit.Percent);
                    stateEditor.removeStateButton.style.alignSelf = Align.FlexEnd;
                    stateEditor.removeStateButton.style.position = Position.Absolute;
                    stateEditor.removeStateButton.style.right = 5f;

                }

                worldStateEditor.stateEditors.Add(stateEditor);
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
                newAction.Preconditions.Add(new Action.Precondition() { name = "DefaultPrecondition" });

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
            button.style.width = new Length(24.5f, LengthUnit.Percent);
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

        private void InitializeDebugEditor()
        {
            m_debugEditor = new DebugEditor();
            m_debugEditor.debugWorldState = WorldState.Clone(goap.WorldState);
            m_serializedObject.Update();

            m_debugEditor.showWorldStateFoldout = EditorUtils.CreateFoldout("WorldState", 5f, Color.white, FlexDirection.Column);

            InitializeGOAPPanelButton(ref m_debugEditor.showButton, typeof(DebugEditor), "Debug");

            m_debugEditor.debugWorldStateEditor = new WorldStateEditor();
            m_debugEditor.debugWorldStateEditor.stateEditors = new List<StateEditor>();

            SerializedProperty debugEditorProperty = m_serializedObject.FindProperty("m_debugEditor");
            m_debugEditor.debugWorldStateEditor.worldStateProperty = debugEditorProperty.FindPropertyRelative(nameof(DebugEditor.debugWorldState));

            InitializeWorldStateList(m_debugEditor.debugWorldStateEditor, true);

            m_debugEditor.showGoalFoldout = EditorUtils.CreateFoldout("Goals", 5f, Color.white, FlexDirection.Column);

            m_debugEditor.debugGoalEditors = new List<DebugGoalEditor>();

            int goalCount = m_goalListEditor.goalEditors.Count;
            for (int i = 0; i < goalCount; ++i)
            {
                DebugGoalEditor debugGoalEditor = new DebugGoalEditor()
                {
                    goalId = i,
                    considerationSlider = new Slider(0f, 1f)
                };

                debugGoalEditor.considerationSlider.label = m_goalListEditor.goalEditors[i].showFoldout.text;
                debugGoalEditor.considerationSlider.showInputField = true;

                m_debugEditor.debugGoalEditors.Add(debugGoalEditor);
            }

            m_debugEditor.resetButton = new Button(
                    delegate
                    {
                        InitializeDebugEditor();
                        Compose();
                    });
            m_debugEditor.resetButton.text = "Reset";
            m_debugEditor.resetButton.style.fontSize = 15f;
            m_debugEditor.resetButton.style.width = new Length(25f, LengthUnit.Percent);
            m_debugEditor.resetButton.style.alignSelf = Align.Center;
            m_debugEditor.resetButton.style.height = new Length(30f);

            m_debugEditor.generatePlanButton = new Button(DebugGeneratePlan);
            m_debugEditor.generatePlanButton.text = "Generate Plan";
            m_debugEditor.generatePlanButton.style.fontSize = 15f;
            m_debugEditor.generatePlanButton.style.width = new Length(25f, LengthUnit.Percent);
            m_debugEditor.generatePlanButton.style.alignSelf = Align.Center;
            m_debugEditor.generatePlanButton.style.height = new Length(30f);

            m_debugEditor.outputTextElement = new TextElement();
            m_debugEditor.outputTextElement.text = "No plan generated";
            m_debugEditor.outputTextElement.style.alignSelf = Align.Center;
            m_debugEditor.outputTextElement.style.fontSize = 15f;


        }

        public void DebugGeneratePlan()
        {
            float heuristic = 0f;
            int bestGoalId = -1;
            int goalCount = m_debugEditor.debugGoalEditors.Count;
            for (int i = 0; i < goalCount; ++i)
            {
                float value = m_debugEditor.debugGoalEditors[i].considerationSlider.value;

                AnimationCurve considerationCurve = goap.Goals[i].Curve;
                float newHeuristic = considerationCurve.Evaluate(value);

                if (newHeuristic > heuristic)
                {
                    heuristic = newHeuristic;
                    bestGoalId = i;
                }
            }

            if (bestGoalId == -1)
            {
                m_debugEditor.outputTextElement.text = "No goal is valid";
                return;
            }

            string output = $"Goal: {goap.Goals[bestGoalId].name}\n";

            goap.Initialize();
            m_debugEditor.debugWorldState.ComputeHashValues();

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Node startNode = Node.CreateEmptyNode(m_debugEditor.debugWorldState);
            Node leave = Node.CreateEmptyNode(m_debugEditor.debugWorldState, int.MaxValue);
            GOAP.BuildGraph(ref startNode, ref leave, goap.ActionSet, goap.Goals[bestGoalId].States);
            stopwatch.Stop();

            if (leave.parent == null)
            {
                output += $"No plan available";
                m_debugEditor.outputTextElement.text = output;
                return;
            }

            output += "\nPlan : \n";
            void GetOutputPlan(Node node, ref string refOutput, ref int nodeCount)
            {
                if (node.parent == null)
                    return;

                GetOutputPlan(node.parent, ref refOutput, ref nodeCount);
                nodeCount++;

                refOutput += $"{nodeCount}. <b>{node.action.name}</b> - Precondition : {node.action.Preconditions[node.preconditionId].name}\n";
            }

            int nodeCount = 0;
            GetOutputPlan(leave, ref output, ref nodeCount);

            output += $"\nAction count: {nodeCount} \n";
            output += $"Generation time: {stopwatch.ElapsedMilliseconds}ms";


            m_debugEditor.outputTextElement.text = output;

        }

        #endregion

        #region Compose
        private void ComposeGOAP()
        {
            if (goap == null)
                return;

            VisualElement goapPanelLabel = EditorUtils.CreateLabel(1f, 5f, 10f, m_borderColor, m_backgroundColor, FlexDirection.Row);
            goapPanelLabel.style.alignSelf = Align.Center;
            goapPanelLabel.style.width = new Length(90f, LengthUnit.Percent);

            m_panelScrolView.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));
            m_panelScrolView.Add(goapPanelLabel);

            goapPanelLabel.Add(m_worldStateEditor.showButton);
            goapPanelLabel.Add(m_actionSetEditor.showButton);
            goapPanelLabel.Add(m_goalListEditor.showButton);
            goapPanelLabel.Add(m_debugEditor.showButton);

            if (m_currentGOAPPanel == typeof(WorldStateEditor))
            {
                ComposeWorldState(m_worldStateEditor, false, m_panelScrolView);
                m_worldStateEditor.showButton.style.color = m_selectedPanelBtnColor;
                m_actionSetEditor.showButton.style.color = m_unselectedPanelBtnColor;
                m_goalListEditor.showButton.style.color = m_unselectedPanelBtnColor;
                m_debugEditor.showButton.style.color = m_unselectedPanelBtnColor;
            }
            else if (m_currentGOAPPanel == typeof(ActionSetEditor))
            {
                ComposeActionSet();
                m_worldStateEditor.showButton.style.color = m_unselectedPanelBtnColor;
                m_actionSetEditor.showButton.style.color = m_selectedPanelBtnColor;
                m_goalListEditor.showButton.style.color = m_unselectedPanelBtnColor;
                m_debugEditor.showButton.style.color = m_unselectedPanelBtnColor;
            }
            else if (m_currentGOAPPanel == typeof(GoalListEditor))
            {
                ComposeGoal();
                m_worldStateEditor.showButton.style.color = m_unselectedPanelBtnColor;
                m_actionSetEditor.showButton.style.color = m_unselectedPanelBtnColor;
                m_goalListEditor.showButton.style.color = m_selectedPanelBtnColor;
                m_debugEditor.showButton.style.color = m_unselectedPanelBtnColor;
            }
            else
            {
                ComposeDebug();
                m_worldStateEditor.showButton.style.color = m_unselectedPanelBtnColor;
                m_actionSetEditor.showButton.style.color = m_unselectedPanelBtnColor;
                m_goalListEditor.showButton.style.color = m_unselectedPanelBtnColor;
                m_debugEditor.showButton.style.color = m_selectedPanelBtnColor;
            }

        }

        private void ComposeWorldState(WorldStateEditor worldStateEditor, bool isDebugWorldState, VisualElement root)
        {
            if (!isDebugWorldState)
            {
                VisualElement addStateLabel = EditorUtils.CreateLabel(1f, 5f, 10f, 50f, 50f, m_borderColor, m_backgroundColor, FlexDirection.Row);

                addStateLabel.Add(worldStateEditor.stateNameField);
                addStateLabel.Add(worldStateEditor.statePopupField);
                addStateLabel.Add(worldStateEditor.addStateButton);

                root.Add(addStateLabel);
            }

            int stateCount = worldStateEditor.stateEditors.Count;
            for (int i = 0; i < stateCount; ++i)
            {
                StateEditor stateEditor = worldStateEditor.stateEditors[i];
                stateEditor.showFoldout.Clear();
                
                if (isDebugWorldState)
                    stateEditor.showFoldout.style.left = 10f;

                stateEditor.showFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 10f)));
                stateEditor.showFoldout.Add(stateEditor.valueField);

                if (!isDebugWorldState)
                {
                    stateEditor.showFoldout.Add(stateEditor.componentPopupField);
                    stateEditor.showFoldout.Add(stateEditor.methodPopupField);
                }

                VisualElement stateFoldoutLabel = EditorUtils.CreateLabel(1f, 0f, 15f, 20f, 20f, m_borderColor, m_backgroundColor, FlexDirection.Column);

                VisualElement foldoutLabel = EditorUtils.CreateFoldoutLabel(m_borderColor, m_backgroundColor);

                stateFoldoutLabel.Add(foldoutLabel);
                stateFoldoutLabel.Add(stateEditor.showFoldout);

                if (!isDebugWorldState)
                    stateFoldoutLabel.Add(stateEditor.removeStateButton);

                root.Add(stateFoldoutLabel);
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
                    actionEditor.showEffect.style.color = m_unselectedPanelBtnColor;
                    actionEditor.showPrecondition.style.color = m_selectedPanelBtnColor;
                }
                else
                {
                    actionEditor.showFoldout.Add(ComposeStateIdEditor(actionEditor.stateIdListEditor));
                    actionEditor.showPrecondition.style.color = m_unselectedPanelBtnColor;
                    actionEditor.showEffect.style.color = m_selectedPanelBtnColor;
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

        private void ComposeDebug()
        {
            m_panelScrolView.Add(m_debugEditor.resetButton);
            m_panelScrolView.Add(EditorUtils.CreateSpace(new Vector2(0f, 15f)));

            VisualElement stateFoldoutLabel = EditorUtils.CreateLabel(1f, 0f, 15f, 20f, 20f, m_borderColor, m_backgroundColor, FlexDirection.Column);
            VisualElement statefoldoutOverlay = EditorUtils.CreateFoldoutLabel(m_borderColor, m_backgroundColor);
            stateFoldoutLabel.Add(statefoldoutOverlay);
            stateFoldoutLabel.Add(m_debugEditor.showWorldStateFoldout);

            m_debugEditor.showWorldStateFoldout.Clear();

            m_debugEditor.showWorldStateFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));
            ComposeWorldState(m_debugEditor.debugWorldStateEditor, true, m_debugEditor.showWorldStateFoldout);

            m_panelScrolView.Add(stateFoldoutLabel);

            VisualElement goalFoldoutLabel = EditorUtils.CreateLabel(1f, 0f, 15f, 20f, 20f, m_borderColor, m_backgroundColor, FlexDirection.Column);
            VisualElement goalfoldoutOverlay = EditorUtils.CreateFoldoutLabel(m_borderColor, m_backgroundColor);
            goalFoldoutLabel.Add(goalfoldoutOverlay);
            goalFoldoutLabel.Add(m_debugEditor.showGoalFoldout);

            m_debugEditor.showGoalFoldout.Clear();
            m_debugEditor.showGoalFoldout.Add(EditorUtils.CreateSpace(new Vector2(0f, 20f)));

            int goalCount = m_debugEditor.debugGoalEditors.Count;
            for (int i = 0; i < goalCount; ++i)
            {
                m_debugEditor.showGoalFoldout.Add(m_debugEditor.debugGoalEditors[i].considerationSlider);
            }

            m_panelScrolView.Add(goalFoldoutLabel);
            m_panelScrolView.Add(EditorUtils.CreateSpace(new Vector2(0f, 15f)));
            m_panelScrolView.Add(m_debugEditor.generatePlanButton);
            m_panelScrolView.Add(EditorUtils.CreateSpace(new Vector2(0f, 15f)));
            m_panelScrolView.Add(m_debugEditor.outputTextElement);
        }
        #endregion
    }
}