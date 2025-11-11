using System.Collections.Generic;
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

        private Outlinable hovered;
        private Outlinable clicked;
        private Dictionary<Outlinable, OriginalColors> originalColors = new Dictionary<Outlinable, OriginalColors>();

        private struct OriginalColors
        {
            public Color single;
            public Color front;
            public Color back;
            public bool stored;
        }

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void Update()
        {
            DoRaycast();
            HandleInput();
            CleanupDestroyedKeys();
        }

        private void DoRaycast()
        {
            if (targetCamera == null)
                return;

            var ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, maxDistance, raycastMask))
            {
                var o = hit.collider.gameObject.GetComponentInParent<Outlinable>();
                SetHovered(o);
            }
            else
            {
                SetHovered(null);
            }
        }

        private void SetHovered(Outlinable o)
        {
            if (hovered == o)
                return;

            if (hovered != null && hovered != clicked)
                RestoreColors(hovered);

            hovered = o;

            if (hovered != null && hovered != clicked)
            {
                StoreOriginalIfNeeded(hovered);
                ApplyColor(hovered, hoverColor);
            }
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (hovered != null)
                    SetClicked(hovered);
                else
                    ClearClicked();
            }
        }

        private void SetClicked(Outlinable o)
        {
            if (clicked == o)
                return;

            if (clicked != null)
                RestoreColors(clicked);

            clicked = o;

            if (clicked != null)
            {
                StoreOriginalIfNeeded(clicked);
                ApplyColor(clicked, clickColor);
            }
        }

        private void ClearClicked()
        {
            if (clicked != null)
            {
                RestoreColors(clicked);
                clicked = null;

                if (hovered != null)
                {
                    StoreOriginalIfNeeded(hovered);
                    ApplyColor(hovered, hoverColor);
                }
            }
        }

        private void StoreOriginalIfNeeded(Outlinable o)
        {
            if (o == null)
                return;

            if (originalColors.TryGetValue(o, out var existing) && existing.stored)
                return;

            var oc = new OriginalColors();
            if (o.RenderStyle == RenderStyle.Single)
            {
                oc.single = o.OutlineParameters.Color;
            }
            else
            {
                oc.front = o.FrontParameters.Color;
                oc.back = o.BackParameters.Color;
            }

            oc.stored = true;
            originalColors[o] = oc;
        }

        private void ApplyColor(Outlinable o, Color color)
        {
            if (o == null)
                return;

            if (o.RenderStyle == RenderStyle.Single)
                o.OutlineParameters.Color = color;
            else
            {
                o.FrontParameters.Color = color;
                o.BackParameters.Color = color;
            }
        }

        private void RestoreColors(Outlinable o)
        {
            if (o == null)
                return;

            if (!originalColors.TryGetValue(o, out var oc))
                return;

            if (o.RenderStyle == RenderStyle.Single)
                o.OutlineParameters.Color = oc.single;
            else
            {
                o.FrontParameters.Color = oc.front;
                o.BackParameters.Color = oc.back;
            }

            originalColors.Remove(o);
        }

        private void CleanupDestroyedKeys()
        {
            var toRemove = new List<Outlinable>();
            foreach (var kv in originalColors)
            {
                if (kv.Key == null)
                    toRemove.Add(kv.Key);
            }

            foreach (var k in toRemove)
                originalColors.Remove(k);

            if (hovered != null && hovered.Equals(null))
                hovered = null;

            if (clicked != null && clicked.Equals(null))
                clicked = null;
        }

        private void OnDisable()
        {
            // restore all stored colors
            foreach (var kv in new List<KeyValuePair<Outlinable, OriginalColors>>(originalColors))
            {
                if (kv.Key == null)
                    continue;

                var o = kv.Key;
                var oc = kv.Value;
                if (o.RenderStyle == RenderStyle.Single)
                    o.OutlineParameters.Color = oc.single;
                else
                {
                    o.FrontParameters.Color = oc.front;
                    o.BackParameters.Color = oc.back;
                }
            }

            originalColors.Clear();
            hovered = null;
            clicked = null;
        }
    }
}