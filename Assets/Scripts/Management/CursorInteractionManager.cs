using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CursorInteractionManager : MonoBehaviour
{
    private Camera _camera;
    private IClickable _previousHovered;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    void Update()
    {
        CheckPointerInteraction();
    }

    private void CheckPointerInteraction()
    {
        var cursorPosition = _camera.ScreenToWorldPoint(Input.mousePosition);
        var currentHovered = Physics2D.OverlapPoint(cursorPosition);

        if (_previousHovered != null)
        {
            var prevGameObject = _previousHovered.GetGameObject();
            if (prevGameObject != currentHovered)
            {
                _previousHovered.OnPointerExit();
            }
        }

        _previousHovered = null;
        if (!currentHovered || !currentHovered.TryGetComponent(out IClickable currentHoveredInterface)) return;

        _previousHovered = currentHoveredInterface;
        if (currentHoveredInterface != null)
        {
            currentHoveredInterface.OnPointerEnter();

            if (Input.GetMouseButtonDown(0)) currentHoveredInterface.OnPointerDown();
            if (Input.GetMouseButtonUp(0)) currentHoveredInterface.OnPointerUp();
        }
    }
}
