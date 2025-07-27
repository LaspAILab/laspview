using System;
using RuntimeSceneGizmo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

// Scene gizmo renderer, adapted to UI Toolkit
public class SceneGizmoRendererUiToolkit : MonoBehaviour
{
    public string imageContainerName = "GizmoPanel";

    private int pointerId = -1;
    private VisualElement imageContainer;
    private Vector2 imageContainerSize;

    [SerializeField] private SceneGizmoController controller;

    [SerializeField] [Tooltip("Should gizmo's hovered components turn yellow")]
    private bool highlightHoveredComponents = true;

    [SerializeField] private ComponentClickedEvent m_onComponentClicked;

    public ComponentClickedEvent OnComponentClicked
    {
        get { return m_onComponentClicked; }
    }

    private void Awake()
    {
        imageContainer = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>(imageContainerName);
        controller = (SceneGizmoController)Instantiate(controller);

        imageContainer.style.backgroundImage =
            new StyleBackground(Background.FromRenderTexture(controller.TargetTexture));
    }

    private void OnEnable()
    {
        if( controller != null && !controller.Equals( null ) )
            controller.gameObject.SetActive( true );
        
        imageContainer.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        imageContainer.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        imageContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        imageContainer.RegisterCallback<GeometryChangedEvent>(OnSizeChanged);
        imageContainer.RegisterCallback<ClickEvent>(OnPointerClick);
    }

    private void OnDisable()
    {
        if( controller != null && !controller.Equals( null ) )
            controller.gameObject.SetActive( false );
        
        imageContainer.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
        imageContainer.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
        imageContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        imageContainer.UnregisterCallback<GeometryChangedEvent>(OnSizeChanged);
    }

    private void Start()
    {
        imageContainerSize = new(imageContainer.style.width.value.value, imageContainer.style.height.value.value);
    }

    private void OnSizeChanged(GeometryChangedEvent e)
    {
        imageContainerSize = e.newRect.size;
    }

    private void OnPointerEnter(PointerEnterEvent e)
    {
        if (highlightHoveredComponents)
        {
            pointerId = e.pointerId;
        }
    }

    private void OnPointerLeave(PointerLeaveEvent e)
    {
        if (pointerId == e.pointerId)
        {
            pointerId = -1;
            controller.OnPointerHover(new Vector3(-10f, -10f, 0f));
        }
    }

    private void OnPointerMove(PointerMoveEvent eventData)
    {
        if (pointerId != -1 && eventData.pointerId == pointerId)
        {
            controller.OnPointerHover(ToNormalizedPointerPosition(eventData.localPosition));
        }
    }

    private void OnPointerClick(ClickEvent e)
    {
        if (e.pointerId != pointerId)
        {
            return;
        }

        GizmoComponent hitComponent = controller.Raycast(ToNormalizedPointerPosition(e.localPosition));
        if (hitComponent != GizmoComponent.None)
        {
            m_onComponentClicked.Invoke(hitComponent);
        }
    }

    private Vector3 ToNormalizedPointerPosition(Vector2 localPosition)
    {
        return new Vector3(localPosition.x / imageContainerSize.x, 1 - localPosition.y / imageContainerSize.y);
    }
}