// Simple Scroll-Snap - https://assetstore.unity.com/packages/tools/gui/simple-scroll-snap-140884
// Version: 1.2.1
// Author: Daniel Lochner

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DanielLochner.Assets.SimpleScrollSnap
{
    [AddComponentMenu("UI/Simple Scroll-Snap")]
    [RequireComponent(typeof(ScrollRect))]
    public class SimpleScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        #region Fields
        public MovementType movementType = MovementType.Fixed;
        public MovementAxis movementAxis = MovementAxis.Horizontal;
        public bool automaticallyLayout = true;
        public SizeControl sizeControl = SizeControl.Fit;
        public Vector2 size = new Vector2(400, 250);
        public float automaticLayoutSpacing = 0.25f;
        public float leftMargin, rightMargin, topMargin, bottomMargin;
        public bool infinitelyScroll = false;
        public float infiniteScrollingEndSpacing = 0f;
        public bool useOcclusionCulling = false;
        public int startingPanel = 0;
        public bool swipeGestures = true;
        public float minimumSwipeSpeed = 0f;
        public Button previousButton = null;
        public Button nextButton = null;
        public GameObject pagination = null;
        public bool toggleNavigation = true;
        public SnapTarget snapTarget = SnapTarget.Next;
        public float snappingSpeed = 10f;
        public float thresholdSnappingSpeed = -1f;
        public bool hardSnap = true;
        public bool useUnscaledTime = false;
        public UnityEvent onPanelChanged, onPanelSelecting, onPanelSelected, onPanelChanging;
        public List<TransitionEffect> transitionEffects = new List<TransitionEffect>();

        private bool dragging, selected = true, pressing;
        private float releaseSpeed, contentLength;
        private Direction releaseDirection;
        private Canvas canvas;
        private RectTransform canvasRectTransform;
        private CanvasScaler canvasScaler;
        private ScrollRect scrollRect;
        private Vector2 previousContentAnchoredPosition, velocity;
        private Dictionary<int, Graphic[]> panelGraphics = new Dictionary<int, Graphic[]>();
        #endregion

        #region Properties
        public RectTransform Content
        {
            get { return scrollRect.content; }
        }
        public RectTransform Viewport
        {
            get { return scrollRect.viewport; }
        }

        public int CurrentPanel { get; set; }
        public int TargetPanel { get; set; }
        public int NearestPanel { get; set; }

        private RectTransform[] PanelsRT
        { get; set; }
        public GameObject[] Panels { get; set; }
        public Toggle[] Toggles { get; set; }

        public int NumberOfPanels
        {
            get { return Content.childCount; }
        }
        #endregion

        #region Enumerators
        public enum MovementType
        {
            Fixed,
            Free
        }
        public enum MovementAxis
        {
            Horizontal,
            Vertical
        }
        public enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }
        public enum SnapTarget
        {
            Nearest,
            Previous,
            Next
        }
        public enum SizeControl
        {
            Manual,
            Fit
        }
        #endregion

        #region Methods
        private void Awake()
        {
            Initialize();
        }
        private void Start()
        {
            if (Validate())
            {
                Setup();
            }
            else
            {
                throw new Exception("Invalid configuration.");
            }
        }
        private void Update()
        {
            if (NumberOfPanels == 0) return;

            OnOcclusionCulling();
            OnSelectingAndSnapping();
            OnInfiniteScrolling();
            OnTransitionEffects();
            OnSwipeGestures();

            DetermineVelocity();
        }
        #if UNITY_EDITOR
        private void OnValidate()
        {
            Initialize();
        }
        #endif

        public void OnPointerDown(PointerEventData eventData)
        {
            pressing = true;
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            pressing = false;
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (hardSnap)
            {
                scrollRect.inertia = true;
            }

            selected = false;
            dragging = true;
        }
        public void OnDrag(PointerEventData eventData)
        {
            if (dragging)
            {
                onPanelSelecting.Invoke();
            }
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;

            if (movementAxis == MovementAxis.Horizontal)
            {
                releaseDirection = scrollRect.velocity.x > 0 ? Direction.Right : Direction.Left;
            }
            else if (movementAxis == MovementAxis.Vertical)
            {
                releaseDirection = scrollRect.velocity.y > 0 ? Direction.Up : Direction.Down;
            }

            releaseSpeed = scrollRect.velocity.magnitude;
        }

        private void Initialize()
        {
            scrollRect = GetComponent<ScrollRect>();
            canvas = GetComponentInParent<Canvas>();

            if (canvas != null)
            {
                canvasScaler = canvas.GetComponentInParent<CanvasScaler>();
                canvasRectTransform = canvas.GetComponent<RectTransform>();
            }

            panelGraphics = new Dictionary<int, Graphic[]>();
            for (int i = 0; i < Content.childCount; i++)
            {
                panelGraphics.Add(i, Content.GetChild(i).GetComponentsInChildren<Graphic>());
            }
        }
        private bool Validate()
        {
            bool valid = true;

            if (pagination != null)
            {
                int numberOfToggles = pagination.transform.childCount;

                if (numberOfToggles != NumberOfPanels)
                {
                    Debug.LogError("<b>[SimpleScrollSnap]</b> The number of Toggles should be equivalent to the number of Panels. There are currently " + numberOfToggles + " Toggles and " + NumberOfPanels + " Panels. If you are adding Panels dynamically during runtime, please update your pagination to reflect the number of Panels you will have before adding.", gameObject);
                    valid = false;
                }
            }

            if (snappingSpeed < 0)
            {
                Debug.LogError("<b>[SimpleScrollSnap]</b> Snapping speed cannot be negative.", gameObject);
                valid = false;
            }

            return valid;
        }
        private void Setup()
        {
            if (NumberOfPanels == 0) return;

            // ScrollRect
            if (movementType == MovementType.Fixed)
            {
                scrollRect.horizontal = (movementAxis == MovementAxis.Horizontal);
                scrollRect.vertical = (movementAxis == MovementAxis.Vertical);
            }
            else
            {
                scrollRect.horizontal = scrollRect.vertical = true;
            }

            // Panels
            size = (sizeControl == SizeControl.Manual) ? size : new Vector2(GetComponent<RectTransform>().rect.width, GetComponent<RectTransform>().rect.height);

            Panels = new GameObject[NumberOfPanels];
            PanelsRT = new RectTransform[NumberOfPanels];
            for (int i = 0; i < NumberOfPanels; i++)
            {
                Panels[i] = Content.GetChild(i).gameObject;
                PanelsRT[i] = Panels[i].GetComponent<RectTransform>();

                if (movementType == MovementType.Fixed && automaticallyLayout)
                {
                    PanelsRT[i].anchorMin = new Vector2(movementAxis == MovementAxis.Horizontal ? 0f : 0.5f, movementAxis == MovementAxis.Vertical ? 0f : 0.5f);
                    PanelsRT[i].anchorMax = new Vector2(movementAxis == MovementAxis.Horizontal ? 0f : 0.5f, movementAxis == MovementAxis.Vertical ? 0f : 0.5f);

                    float x = (rightMargin + leftMargin) / 2f - leftMargin;
                    float y = (topMargin + bottomMargin) / 2f - bottomMargin;
                    Vector2 marginOffset = new Vector2(x / size.x, y / size.y);
                    PanelsRT[i].pivot = new Vector2(0.5f, 0.5f) + marginOffset;
                    PanelsRT[i].sizeDelta = size - new Vector2(leftMargin + rightMargin, topMargin + bottomMargin);

                    float panelPosX = (movementAxis == MovementAxis.Horizontal) ? i * (automaticLayoutSpacing + 1f) * size.x + (size.x / 2f) : 0f;
                    float panelPosY = (movementAxis == MovementAxis.Vertical) ? i * (automaticLayoutSpacing + 1f) * size.y + (size.y / 2f) : 0f;
                    PanelsRT[i].anchoredPosition = new Vector2(panelPosX, panelPosY);
                }
            }

            // Content
            if (movementType == MovementType.Fixed)
            {
                // Automatic Layout
                if (automaticallyLayout)
                {
                    Content.anchorMin = new Vector2(movementAxis == MovementAxis.Horizontal ? 0f : 0.5f, movementAxis == MovementAxis.Vertical ? 0f : 0.5f);
                    Content.anchorMax = new Vector2(movementAxis == MovementAxis.Horizontal ? 0f : 0.5f, movementAxis == MovementAxis.Vertical ? 0f : 0.5f);
                    Content.pivot = new Vector2(movementAxis == MovementAxis.Horizontal ? 0f : 0.5f, movementAxis == MovementAxis.Vertical ? 0f : 0.5f);

                    Vector2 min = PanelsRT[0].anchoredPosition;
                    Vector2 max = PanelsRT[NumberOfPanels - 1].anchoredPosition;

                    float contentWidth = (movementAxis == MovementAxis.Horizontal) ? (NumberOfPanels * (automaticLayoutSpacing + 1f) * size.x) - (size.x * automaticLayoutSpacing) : size.x;
                    float contentHeight = (movementAxis == MovementAxis.Vertical) ? (NumberOfPanels * (automaticLayoutSpacing + 1f) * size.y) - (size.y * automaticLayoutSpacing) : size.y;
                    Content.sizeDelta = new Vector2(contentWidth, contentHeight);
                }

                // Infinite Scrolling
                if (infinitelyScroll)
                {
                    scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
                    contentLength = (movementAxis == MovementAxis.Horizontal) ? (Content.rect.width + size.x * infiniteScrollingEndSpacing) : Content.rect.height + size.y * infiniteScrollingEndSpacing;

                    OnInfiniteScrolling(true);
                }

                // Occlusion Culling
                if (useOcclusionCulling)
                {
                    OnOcclusionCulling(true);
                }
            }
            else
            {
                automaticallyLayout = infinitelyScroll = useOcclusionCulling = false;
            }

            // Starting Panel
            float xOffset = (movementAxis == MovementAxis.Horizontal || movementType == MovementType.Free) ? Viewport.rect.width / 2f : 0f;
            float yOffset = (movementAxis == MovementAxis.Vertical || movementType == MovementType.Free) ? Viewport.rect.height / 2f : 0f;
            Vector2 offset = new Vector2(xOffset, yOffset);
            previousContentAnchoredPosition = Content.anchoredPosition = -PanelsRT[startingPanel].anchoredPosition + offset;
            CurrentPanel = TargetPanel = NearestPanel = startingPanel;

            // Previous Button
            if (previousButton != null)
            {
                previousButton.onClick.RemoveAllListeners();
                previousButton.onClick.AddListener(GoToPreviousPanel);
            }

            // Next Button
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(GoToNextPanel);
            }

            // Pagination
            if (pagination != null)
            {
                Toggles = pagination.GetComponentsInChildren<Toggle>();
                for (int i = 0; i < Toggles.Length; i++)
                {
                    if (Toggles[i] != null)
                    {
                        Toggles[i].isOn = (i == startingPanel);
                        Toggles[i].interactable = (i != TargetPanel);
                        int panelNum = i;

                        Toggles[i].onValueChanged.RemoveAllListeners();
                        Toggles[i].onValueChanged.AddListener(delegate
                        {
                            if (Toggles[panelNum].isOn && toggleNavigation)
                            {
                                GoToPanel(panelNum);
                            }
                        });
                    }
                }
            }
        }

        private Vector2 DisplacementFromCenter(int index)
        {
            return PanelsRT[index].anchoredPosition + Content.anchoredPosition - new Vector2(Viewport.rect.width * (0.5f - Content.anchorMin.x), Viewport.rect.height * (0.5f - Content.anchorMin.y));
        }
        private int DetermineNearestPanel()
        {
            int panelNumber = NearestPanel;
            float[] distances = new float[NumberOfPanels];
            for (int i = 0; i < Panels.Length; i++)
            {
                distances[i] = DisplacementFromCenter(i).magnitude;
            }
            float minDistance = Mathf.Min(distances);
            for (int i = 0; i < Panels.Length; i++)
            {
                if (minDistance == distances[i])
                {
                    panelNumber = i;
                    break;
                }
            }
            return panelNumber;
        }
        private void DetermineVelocity()
        {
            Vector2 displacement = Content.anchoredPosition - previousContentAnchoredPosition;
            float time = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            velocity = displacement / time;

            previousContentAnchoredPosition = Content.anchoredPosition;
        }
        private void SelectTargetPanel()
        {
            Vector2 displacementFromCenter = DisplacementFromCenter(NearestPanel = DetermineNearestPanel());

            if (snapTarget == SnapTarget.Nearest || releaseSpeed <= minimumSwipeSpeed)
            {
                GoToPanel(NearestPanel);
            }
            else if (snapTarget == SnapTarget.Previous)
            {
                if ((releaseDirection == Direction.Right && displacementFromCenter.x < 0f) || (releaseDirection == Direction.Up && displacementFromCenter.y < 0f))
                {
                    GoToNextPanel();
                }
                else if ((releaseDirection == Direction.Left && displacementFromCenter.x > 0f) || (releaseDirection == Direction.Down && displacementFromCenter.y > 0f))
                {
                    GoToPreviousPanel();
                }
                else
                {
                    GoToPanel(NearestPanel);
                }
            }
            else if (snapTarget == SnapTarget.Next)
            {
                if ((releaseDirection == Direction.Right && displacementFromCenter.x > 0f) || (releaseDirection == Direction.Up && displacementFromCenter.y > 0f))
                {
                    GoToPreviousPanel();
                }
                else if ((releaseDirection == Direction.Left && displacementFromCenter.x < 0f) || (releaseDirection == Direction.Down && displacementFromCenter.y < 0f))
                {
                    GoToNextPanel();
                }
                else
                {
                    GoToPanel(NearestPanel);
                }
            }
        }
        private void SnapToTargetPanel()
        {
            float xOffset = (movementAxis == MovementAxis.Horizontal || movementType == MovementType.Free) ? Viewport.rect.width / 2f : 0f;
            float yOffset = (movementAxis == MovementAxis.Vertical || movementType == MovementType.Free) ? Viewport.rect.height / 2f : 0f;
            Vector2 offset = new Vector2(xOffset, yOffset);

            Vector2 targetPosition = -PanelsRT[TargetPanel].anchoredPosition + offset;
            Content.anchoredPosition = Vector2.Lerp(Content.anchoredPosition, targetPosition, (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * snappingSpeed);

            if (CurrentPanel != TargetPanel)
            {
                if (DisplacementFromCenter(TargetPanel).magnitude < (Viewport.rect.width / 10f))
                {
                    CurrentPanel = TargetPanel;

                    onPanelChanged.Invoke();
                }
                else
                {
                    onPanelChanging.Invoke();
                }
            }
        }

        private void OnSelectingAndSnapping()
        {
            if (selected)
            {
                if (!((dragging || pressing) && swipeGestures))
                {
                    SnapToTargetPanel();
                }
            }
            else if (!dragging && (scrollRect.velocity.magnitude <= thresholdSnappingSpeed || thresholdSnappingSpeed == -1f))
            {
                SelectTargetPanel();
            }
        }
        private void OnOcclusionCulling(bool forceUpdate = false)
        {
            if (useOcclusionCulling && (velocity.magnitude > 0f || forceUpdate))
            {
                for (int i = 0; i < NumberOfPanels; i++)
                {
                    if (movementAxis == MovementAxis.Horizontal)
                    {
                        Panels[i].SetActive(Mathf.Abs(DisplacementFromCenter(i).x) <= Viewport.rect.width / 2f + size.x);
                    }
                    else if (movementAxis == MovementAxis.Vertical)
                    {
                        Panels[i].SetActive(Mathf.Abs(DisplacementFromCenter(i).y) <= Viewport.rect.height / 2f + size.y);
                    }
                }
            }
        }
        private void OnInfiniteScrolling(bool forceUpdate = false)
        {
            if (infinitelyScroll && (velocity.magnitude > 0 || forceUpdate))
            {
                if (movementAxis == MovementAxis.Horizontal)
                {
                    for (int i = 0; i < NumberOfPanels; i++)
                    {
                        if (DisplacementFromCenter(i).x > Content.rect.width / 2f)
                        {
                            PanelsRT[i].anchoredPosition -= new Vector2(contentLength, 0);
                        }
                        else if (DisplacementFromCenter(i).x < Content.rect.width / -2f)
                        {
                            PanelsRT[i].anchoredPosition += new Vector2(contentLength, 0);
                        }
                    }
                }
                else if (movementAxis == MovementAxis.Vertical)
                {
                    for (int i = 0; i < NumberOfPanels; i++)
                    {
                        if (DisplacementFromCenter(i).y > Content.rect.height / 2f)
                        {
                            PanelsRT[i].anchoredPosition -= new Vector2(0, contentLength);
                        }
                        else if (DisplacementFromCenter(i).y < Content.rect.height / -2f)
                        {
                            PanelsRT[i].anchoredPosition += new Vector2(0, contentLength);
                        }
                    }
                }
            }
        }
        private void OnTransitionEffects()
        {
            if (transitionEffects.Count == 0) return;

            for (int i = 0; i < NumberOfPanels; i++)
            {
                foreach (TransitionEffect transitionEffect in transitionEffects)
                {
                    // Displacement
                    float displacement = 0f;
                    if (movementType == MovementType.Fixed)
                    {
                        if (movementAxis == MovementAxis.Horizontal)
                        {
                            displacement = DisplacementFromCenter(i).x;
                        }
                        else if (movementAxis == MovementAxis.Vertical)
                        {
                            displacement = DisplacementFromCenter(i).y;
                        }
                    }
                    else
                    {
                        displacement = DisplacementFromCenter(i).magnitude;
                    }

                    // Value
                    RectTransform panel = PanelsRT[i];
                    switch (transitionEffect.Label)
                    {
                        case "localPosition.z":
                            panel.transform.localPosition = new Vector3(panel.transform.localPosition.x, panel.transform.localPosition.y, transitionEffect.GetValue(displacement));
                            break;
                        case "localScale.x":
                            panel.transform.localScale = new Vector2(transitionEffect.GetValue(displacement), panel.transform.localScale.y);
                            break;
                        case "localScale.y":
                            panel.transform.localScale = new Vector2(panel.transform.localScale.x, transitionEffect.GetValue(displacement));
                            break;
                        case "localRotation.x":
                            panel.transform.localRotation = Quaternion.Euler(new Vector3(transitionEffect.GetValue(displacement), panel.transform.localEulerAngles.y, panel.transform.localEulerAngles.z));
                            break;
                        case "localRotation.y":
                            panel.transform.localRotation = Quaternion.Euler(new Vector3(panel.transform.localEulerAngles.x, transitionEffect.GetValue(displacement), panel.transform.localEulerAngles.z));
                            break;
                        case "localRotation.z":
                            panel.transform.localRotation = Quaternion.Euler(new Vector3(panel.transform.localEulerAngles.x, panel.transform.localEulerAngles.y, transitionEffect.GetValue(displacement)));
                            break;
                        case "color.r":
                            foreach (Graphic graphic in panelGraphics[i])
                            {
                                graphic.color = new Color(transitionEffect.GetValue(displacement), graphic.color.g, graphic.color.b, graphic.color.a);
                            }
                            break;
                        case "color.g":
                            foreach (Graphic graphic in panelGraphics[i])
                            {
                                graphic.color = new Color(graphic.color.r, transitionEffect.GetValue(displacement), graphic.color.b, graphic.color.a);
                            }
                            break;
                        case "color.b":
                            foreach (Graphic graphic in panelGraphics[i])
                            {
                                graphic.color = new Color(graphic.color.r, graphic.color.g, transitionEffect.GetValue(displacement), graphic.color.a);
                            }
                            break;
                        case "color.a":
                            foreach (Graphic graphic in panelGraphics[i])
                            {
                                graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, transitionEffect.GetValue(displacement));
                            }
                            break;
                    }
                }
            }
        }
        private void OnSwipeGestures()
        {
            if (swipeGestures)
            {
                scrollRect.horizontal = movementAxis == MovementAxis.Horizontal || movementType == MovementType.Free;
                scrollRect.vertical = movementAxis == MovementAxis.Vertical || movementType == MovementType.Free;
            }
            else
            {
                scrollRect.horizontal = scrollRect.vertical = !dragging;
            }
        }

        public void GoToPanel(int panelNumber)
        {
            TargetPanel = panelNumber;
            selected = true;
            onPanelSelected.Invoke();

            if (pagination != null)
            {
                for (int i = 0; i < Toggles.Length; i++)
                {
                    if (Toggles[i] != null)
                    {
                        Toggles[i].isOn = (i == TargetPanel);
                        Toggles[i].interactable = (i != TargetPanel);
                    }
                }
            }

            if (hardSnap)
            {
                scrollRect.inertia = false;
            }
        }
        public void GoToPreviousPanel()
        {
            NearestPanel = DetermineNearestPanel();
            if (NearestPanel != 0)
            {
                GoToPanel(NearestPanel - 1);
            }
            else
            {
                if (infinitelyScroll)
                {
                    GoToPanel(NumberOfPanels - 1);
                }
                else
                {
                    GoToPanel(NearestPanel);
                }
            }
        }
        public void GoToNextPanel()
        {
            NearestPanel = DetermineNearestPanel();
            if (NearestPanel != (NumberOfPanels - 1))
            {
                GoToPanel(NearestPanel + 1);
            }
            else
            {
                if (infinitelyScroll)
                {
                    GoToPanel(0);
                }
                else
                {
                    GoToPanel(NearestPanel);
                }
            }
        }

        public void AddToFront(GameObject panel)
        {
            Add(panel, 0);
        }
        public void AddToBack(GameObject panel)
        {
            Add(panel, NumberOfPanels);
        }
        public void Add(GameObject panel, int index)
        {
            if (NumberOfPanels != 0 && (index < 0 || index > NumberOfPanels))
            {
                Debug.LogError("<b>[SimpleScrollSnap]</b> Index must be an integer from 0 to " + NumberOfPanels + ".", gameObject);
                return;
            }
            else if (!automaticallyLayout)
            {
                Debug.LogError("<b>[SimpleScrollSnap]</b> \"Automatic Layout\" must be enabled for content to be dynamically added during runtime.");
                return;
            }

            panel = Instantiate(panel, Content, false);
            panel.transform.SetSiblingIndex(index);

            Initialize();
            if (Validate())
            {
                if (TargetPanel <= index)
                {
                    startingPanel = TargetPanel;
                }
                else
                {
                    startingPanel = TargetPanel + 1;
                }
                Setup();
            }
        }

        public void RemoveFromFront()
        {
            Remove(0);
        }
        public void RemoveFromBack()
        {
            if (NumberOfPanels > 0)
            {
                Remove(NumberOfPanels - 1);
            }
            else
            {
                Remove(0);
            }
        }
        public void Remove(int index)
        {
            if (NumberOfPanels == 0)
            {
                Debug.LogError("<b>[SimpleScrollSnap]</b> There are no panels to remove.", gameObject);
                return;
            }
            else if (index < 0 || index > (NumberOfPanels - 1))
            {
                Debug.LogError("<b>[SimpleScrollSnap]</b> Index must be an integer from 0 to " + (NumberOfPanels - 1) + ".", gameObject);
                return;
            }
            else if (!automaticallyLayout)
            {
                Debug.LogError("<b>[SimpleScrollSnap]</b> \"Automatic Layout\" must be enabled for content to be dynamically removed during runtime.");
                return;
            }

            DestroyImmediate(Panels[index]);

            Initialize();
            if (Validate())
            {
                if (TargetPanel == index)
                {
                    if (index == NumberOfPanels)
                    {
                        startingPanel = TargetPanel - 1;
                    }
                    else
                    {
                        startingPanel = TargetPanel;
                    }
                }
                else if (TargetPanel < index)
                {
                    startingPanel = TargetPanel;
                }
                else
                {
                    startingPanel = TargetPanel - 1;
                }
                Setup();
            }
        }

        public void AddVelocity(Vector2 velocity)
        {
            scrollRect.velocity += velocity;
            selected = false;
        }
        #endregion

        #region Inner Classes
        [Serializable] public class TransitionEffect
        {
            #region Fields
            [SerializeField] protected float minDisplacement, maxDisplacement, minValue, maxValue, defaultMinValue, defaultMaxValue, defaultMinDisplacement, defaultMaxDisplacement;
            [SerializeField] protected bool showPanel, showDisplacement, showValue;
            [SerializeField] private string label;
            [SerializeField] private AnimationCurve function;
            [SerializeField] private AnimationCurve defaultFunction;
            [SerializeField] private SimpleScrollSnap simpleScrollSnap;
            #endregion

            #region Properties
            public string Label
            {
                get { return label; }
                set { label = value; }
            }
            public float MinValue
            {
                get { return MinValue; }
                set { minValue = value; }
            }
            public float MaxValue
            {
                get { return maxValue; }
                set { maxValue = value; }
            }
            public float MinDisplacement
            {
                get { return minDisplacement; }
                set { minDisplacement = value; }
            }
            public float MaxDisplacement
            {
                get { return maxDisplacement; }
                set { maxDisplacement = value; }
            }
            public AnimationCurve Function
            {
                get { return function; }
                set { function = value; }
            }
            #endregion

            #region Methods
            public TransitionEffect(string label, float minValue, float maxValue, float minDisplacement, float maxDisplacement, AnimationCurve function, SimpleScrollSnap simpleScrollSnap)
            {
                this.label = label;
                this.simpleScrollSnap = simpleScrollSnap;
                this.minValue = minValue;
                this.maxValue = maxValue;
                this.minDisplacement = minDisplacement;
                this.maxDisplacement = maxDisplacement;
                this.function = function;

                SetDefaultValues(minValue, maxValue, minDisplacement, maxDisplacement, function);
                #if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                #endif
            }

            private void SetDefaultValues(float minValue, float maxValue, float minDisplacement, float maxDisplacement, AnimationCurve function)
            {
                defaultMinValue = minValue;
                defaultMaxValue = maxValue;
                defaultMinDisplacement = minDisplacement;
                defaultMaxDisplacement = maxDisplacement;
                defaultFunction = function;
            }
            #if UNITY_EDITOR
            public void Init()
            {
                GUILayout.BeginVertical("HelpBox");
                showPanel = EditorGUILayout.Foldout(showPanel, label, true);
                if (showPanel)
                {
                    EditorGUI.indentLevel++;
                    float x = minDisplacement;
                    float y = minValue;
                    float width = maxDisplacement - minDisplacement;
                    float height = maxValue - minValue;

                    // Min/Max Values
                    showValue = EditorGUILayout.Foldout(showValue, "Value", true);
                    if (showValue)
                    {
                        EditorGUI.indentLevel++;
                        minValue = EditorGUILayout.FloatField(new GUIContent("Min"), minValue);
                        maxValue = EditorGUILayout.FloatField(new GUIContent("Max"), maxValue);
                        EditorGUI.indentLevel--;
                    }

                    // Min/Max Displacements
                    showDisplacement = EditorGUILayout.Foldout(showDisplacement, "Displacement", true);
                    if (showDisplacement)
                    {
                        EditorGUI.indentLevel++;
                        minDisplacement = EditorGUILayout.FloatField(new GUIContent("Min"), minDisplacement);
                        maxDisplacement = EditorGUILayout.FloatField(new GUIContent("Max"), maxDisplacement);
                        EditorGUI.indentLevel--;
                    }

                    // Function
                    function = EditorGUILayout.CurveField("Function", function, Color.white, new Rect(x, y, width, height));

                    // Reset
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUI.indentLevel * 16);
                    if (GUILayout.Button("Reset"))
                    {
                        Reset();
                    }

                    // Remove
                    if (GUILayout.Button("Remove"))
                    {
                        simpleScrollSnap.transitionEffects.Remove(this);
                    }
                    GUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;
                }
                GUILayout.EndVertical();
            }
            #endif
            public void Reset()
            {
                minValue = defaultMinValue;
                maxValue = defaultMaxValue;
                minDisplacement = defaultMinDisplacement;
                maxDisplacement = defaultMaxDisplacement;
                function = defaultFunction;
            }
            public float GetValue(float displacement)
            {
                return (function != null) ? function.Evaluate(displacement) : 0f;
            }
            #endregion
        }
        #endregion
    }
}