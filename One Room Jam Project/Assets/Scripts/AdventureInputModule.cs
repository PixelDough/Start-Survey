using System;
using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("Event/Adventure Input Module")]
public class AdventureInputModule : BaseInputModule
{
    [SerializeField]
    AdventureEventData _clickEvent = new AdventureEventData();

    [SerializeField]
    bool _deselectOnLookAway = true;
    public bool DeselectOnLookAway
    {
        get { return _deselectOnLookAway; }
        private set { _deselectOnLookAway = value; }
    }

    [SerializeField]
    float _defaultInteractionDistance = float.PositiveInfinity;
    public float DefaultInteractionDistance
    {
        get { return _defaultInteractionDistance; }
        private set { _defaultInteractionDistance = value; }
    }

    private PointerEventData pointerData;
    public PointerEventData PointerData { get { return pointerData; } }

    protected override void Awake()
    {
        pointerData = new PointerEventData(eventSystem);
    }

    public override void Process()
    {
        bool usedEvent = SendUpdateEventToSelectedObject();

        if (!usedEvent)
        {
            ProcessPointerEvent();
        }
    }

    /// <summary>
    /// Process all mouse events.
    /// </summary>
	void ProcessPointerEvent()
    {
        var pointerEvent = GetPointerEventData();
        _clickEvent.ButtonData = pointerEvent;

        LimitInteractionDistance(pointerEvent);

        if (_deselectOnLookAway && pointerEvent.pointerCurrentRaycast.gameObject == null)
        {
            ClearSelection();
        }

        // Process the first mouse button fully
        ProcessPointerPress(_clickEvent);
        ProcessMove(_clickEvent.ButtonData);
        ProcessDrag(_clickEvent.ButtonData);

        if (!Mathf.Approximately(_clickEvent.ButtonData.scrollDelta.sqrMagnitude, 0.0f))
        {
            var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(_clickEvent.ButtonData.pointerCurrentRaycast.gameObject);
            ExecuteEvents.ExecuteHierarchy(scrollHandler, _clickEvent.ButtonData, ExecuteEvents.scrollHandler);
        }
    }

    void LimitInteractionDistance(PointerEventData pointerEvent)
    {
        //var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;
        //if (currentOverGo != null)
        //{
        //    InteractionDistance currentOverIntDis = currentOverGo.GetComponentInParent<InteractionDistance>();

        //    float interDist = currentOverIntDis == null ? _defaultInteractionDistance : currentOverIntDis.interactionDistance;
        //    float pointDist = pointerEvent.pointerCurrentRaycast.distance;

        //    if (pointDist > interDist)
        //    {
        //        pointerEvent.pointerCurrentRaycast = new RaycastResult(); //empty the raycast as if nothing was hit
        //    }
        //}
    }

    /// <summary>
    /// Process the current mouse press.
    /// </summary>
	void ProcessPointerPress(AdventureEventData data)
    {
        var pointerEvent = data.ButtonData;
        var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

        // PointerDown notification
        if (data.PressedThisFrame())
        {
            pointerEvent.eligibleForClick = true;
            pointerEvent.delta = Vector2.zero;
            pointerEvent.dragging = false;
            pointerEvent.useDragThreshold = true;
            pointerEvent.pressPosition = pointerEvent.position;
            pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

            DeselectIfSelectionChanged(currentOverGo, pointerEvent);

            // search for the control that will receive the press
            // if we can't find a press handler set the press
            // handler to be what would receive a click.
            var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

            // didnt find a press handler... search for a click handler
            newPressed = newPressed ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // Debug.Log("Pressed: " + newPressed);

            float time = Time.unscaledTime;

            if (newPressed == pointerEvent.lastPress)
            {
                var diffTime = time - pointerEvent.clickTime;
                if (diffTime < 0.3f)
                    ++pointerEvent.clickCount;
                else
                    pointerEvent.clickCount = 1;

                pointerEvent.clickTime = time;
            }
            else
            {
                pointerEvent.clickCount = 1;
            }

            pointerEvent.pointerPress = newPressed;
            pointerEvent.rawPointerPress = currentOverGo;

            pointerEvent.clickTime = time;

            // Save the drag handler as well
            pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

            if (pointerEvent.pointerDrag != null)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
        }

        // PointerUp notification
        if (data.ReleasedThisFrame())
        {
            // Debug.Log("Executing pressup on: " + pointer.pointerPress);
            ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

            // Debug.Log("KeyCode: " + pointer.eventData.keyCode);

            // see if we mouse up on the same element that we clicked on...
            var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

            // PointerClick and Drop events
            if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
            }
            else if (pointerEvent.pointerDrag != null)
            {
                ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
            }

            pointerEvent.eligibleForClick = false;
            pointerEvent.pointerPress = null;
            pointerEvent.rawPointerPress = null;

            if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

            pointerEvent.dragging = false;
            pointerEvent.pointerDrag = null;

            // redo pointer enter / exit to refresh state
            // so that if we moused over somethign that ignored it before
            // due to having pressed on something else
            // it now gets it.
            if (currentOverGo != pointerEvent.pointerEnter)
            {
                HandlePointerExitAndEnter(pointerEvent, null);
                HandlePointerExitAndEnter(pointerEvent, currentOverGo);
            }
        }
    }

    void ProcessMove(PointerEventData pointerEvent)
    {
        var targetGO = pointerEvent.pointerCurrentRaycast.gameObject;
        HandlePointerExitAndEnter(pointerEvent, targetGO);
    }

    void ProcessDrag(PointerEventData pointerEvent)
    {
        if (pointerEvent.pointerDrag != null
            && !pointerEvent.dragging
            && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position, eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
        {
            ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
            pointerEvent.dragging = true;
        }

        // Drag notification
        if (pointerEvent.dragging && pointerEvent.pointerDrag != null)
        {
            // Before doing drag we should cancel any pointer down state
            // And clear selection!
            if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;
            }
            ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
        }
    }

    static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
    {
        if (!useDragThreshold)
            return true;

        return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
    }

    /// <summary>
    /// Send the update event to the selected object (if any)
    /// Returns whether or not the event is used.
    /// </summary>
    bool SendUpdateEventToSelectedObject()
    {
        if (eventSystem.currentSelectedGameObject == null)
            return false;

        var data = GetBaseEventData();
        ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
        return data.used;
    }



    void ClearSelection()
    {
        if (eventSystem.currentSelectedGameObject)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
    {
        // Selection tracking
        var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
        // if we have clicked something new, deselect the old thing
        // leave 'selection handling' up to the press event though.
        if (selectHandlerGO != eventSystem.currentSelectedGameObject)
            eventSystem.SetSelectedGameObject(null, pointerEvent);
    }



    private PointerEventData GetPointerEventData()
    {
        Vector2 screenMid;
        screenMid.x = Screen.width / 2f;
        screenMid.y = Screen.height / 2f;
        pointerData.Reset();
        pointerData.delta = Vector2.zero;
        pointerData.position = screenMid;
        pointerData.scrollDelta = Vector2.zero;
        pointerData.useDragThreshold = false;

        eventSystem.RaycastAll(pointerData, m_RaycastResultCache);
        pointerData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        m_RaycastResultCache.Clear();

        return pointerData;
    }




    [Serializable]
    private class AdventureEventData
    {
        [SerializeField]
        string _clickAxis = "Fire1";
        string ClickAxis
        {
            get { return _clickAxis; }
            set { _clickAxis = value; }
        }

        public PointerEventData ButtonData { get; set; }

        public bool PressedThisFrame()
        {
            return Input.GetButtonDown(_clickAxis);
        }

        public bool ReleasedThisFrame()
        {
            return Input.GetButtonUp(_clickAxis);
        }
    }
}