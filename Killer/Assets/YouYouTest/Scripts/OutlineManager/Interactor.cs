// Interactor.cs
using UnityEngine;

public class Interactor : MonoBehaviour
{
    public float interactionDistance = 5f;
    public LayerMask interactionLayer;
    public Camera mainCamera;

    private Interactable _currentHovered = null;
    private Interactable _currentSelected = null;
    private Interactable _currentHeld = null;

    void Update()
    {
        HandleHover();
        HandleInteraction();
    }

    private void HandleHover()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        Interactable newHovered = null;
        if (Physics.Raycast(ray, out hit, interactionDistance, interactionLayer))
        {
            newHovered = hit.collider.GetComponent<Interactable>();
        }

        // 当悬停对象发生变化时
        if (newHovered != _currentHovered)
        {
            // 如果之前有悬停对象，且它不是被选中或持有的，则恢复其None状态
            if (_currentHovered != null && _currentHovered.CurrentState == InteractableState.Hover)
            {
                _currentHovered.SetState(InteractableState.None);
            }

            // 如果新的悬停对象存在，且它当前是None状态，则设置为Hover
            if (newHovered != null && newHovered.CurrentState == InteractableState.None)
            {
                newHovered.SetState(InteractableState.Hover);
            }
            _currentHovered = newHovered;
        }
    }

    private void HandleInteraction()
    {
        // 按下鼠标左键
        if (Input.GetMouseButtonDown(0))
        {
            if (_currentHovered != null)
            {
                // 如果之前有选中的，先取消选中
                if (_currentSelected != null && _currentSelected != _currentHovered)
                {
                    _currentSelected.SetState(InteractableState.None);
                }

                _currentSelected = _currentHovered;
                _currentSelected.SetState(InteractableState.Select);

                // 同时，我们认为按下的瞬间也是Hold的开始
                _currentHeld = _currentSelected;
                _currentHeld.SetState(InteractableState.Hold);
            }
        }
        // 按住鼠标左键
        else if (Input.GetMouseButton(0))
        {
            // 确保Hold状态持续
            if (_currentHeld != null && _currentHeld.CurrentState != InteractableState.Hold)
            {
                _currentHeld.SetState(InteractableState.Hold);
            }
        }
        // 松开鼠标左键
        else if (Input.GetMouseButtonUp(0))
        {
            if (_currentHeld != null)
            {
                // 如果松开时鼠标还在物体上，则变回Select状态
                if (_currentHeld == _currentHovered) {
                    _currentHeld.SetState(InteractableState.Select);
                } 
                // 如果松开时鼠标已离开物体，可以变回None或继续保持Select，取决于你的设计
                else {
                    // 这里我们让它保持Select状态，点击其他地方取消
                    // 如果你想让它变回None, 就用_currentHeld.SetState(InteractableState.None);
                }
                _currentHeld = null;
            }
        }
        
        // 点击空白处取消选择
        if (Input.GetMouseButtonDown(0) && _currentHovered == null && _currentSelected != null)
        {
            _currentSelected.SetState(InteractableState.None);
            _currentSelected = null;
        }
    }
}
