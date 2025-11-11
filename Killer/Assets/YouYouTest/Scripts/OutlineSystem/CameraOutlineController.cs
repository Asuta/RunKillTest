using UnityEngine;
using EPOOutline;

namespace YouYouTest.OutlineSystem
{
    [DisallowMultipleComponent]
    public class CameraOutlineController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private Color hoverColor = Color.blue;
        [SerializeField] private Color clickColor = Color.red;
        [SerializeField] private float maxDistance = 100f;

        private OutlineReceiver hoveredReceiver;
        private OutlineReceiver clickedReceiver;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void Update()
        {
            DoRaycast();
            HandleInput();
            CleanupDestroyedReceivers();
        }

        private void DoRaycast()
        {
            if (targetCamera == null)
                return;

            var ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, maxDistance, raycastMask))
            {
                var receiver = hit.collider.gameObject.GetComponentInParent<OutlineReceiver>();
                SetHovered(receiver);
            }
            else
            {
                SetHovered(null);
            }
        }

        private void SetHovered(OutlineReceiver receiver)
        {
            if (hoveredReceiver == receiver)
                return;

            if (hoveredReceiver != null && hoveredReceiver != clickedReceiver)
                hoveredReceiver.SetState(OutlineState.None, Color.clear);

            hoveredReceiver = receiver;

            if (hoveredReceiver != null && hoveredReceiver != clickedReceiver)
                hoveredReceiver.SetState(OutlineState.Hover, hoverColor);
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (hoveredReceiver != null)
                    SetClicked(hoveredReceiver);
                else
                    ClearClicked();
            }
        }

        private void SetClicked(OutlineReceiver receiver)
        {
            if (clickedReceiver == receiver)
                return;

            if (clickedReceiver != null)
                clickedReceiver.SetState(OutlineState.None, Color.clear);

            clickedReceiver = receiver;

            if (clickedReceiver != null)
                clickedReceiver.SetState(OutlineState.Selected, clickColor);
        }

        private void ClearClicked()
        {
            if (clickedReceiver != null)
            {
                clickedReceiver.SetState(OutlineState.None, Color.clear);
                clickedReceiver = null;

                if (hoveredReceiver != null)
                    hoveredReceiver.SetState(OutlineState.Hover, hoverColor);
            }
        }

        private void CleanupDestroyedReceivers()
        {
            if (hoveredReceiver != null && hoveredReceiver.Equals(null))
                hoveredReceiver = null;

            if (clickedReceiver != null && clickedReceiver.Equals(null))
                clickedReceiver = null;
        }

        private void OnDisable()
        {
            if (hoveredReceiver != null)
                hoveredReceiver.Restore();

            if (clickedReceiver != null)
                clickedReceiver.Restore();

            hoveredReceiver = null;
            clickedReceiver = null;
        }
    }
}