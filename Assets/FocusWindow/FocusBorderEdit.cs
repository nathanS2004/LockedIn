using UnityEngine;
using Volume = LockedIn.ScreenDepthSelection.ScreenVolume;

namespace LockedIn
{
    public sealed partial class FocusWindow
    {
        private WorkspaceBorderDraft borderDraft;
        private WorkspaceBorder activeBorder;
        private bool draggingBorder, addingBorder, haveAddCorner;
        private BorderHandle dragHandle;
        private Volume dragStart, addPlane;
        private Vector2 dragPoint;
        private bool dragMoved;

        private void BeginBorderEdit()
        {
            var volumes = workspacePreview.Volumes;
            borderDraft = new WorkspaceBorderDraft(volumes[0], volumes.Count > 1 ? volumes[1] : (Volume?)null);
            activeBorder = WorkspaceBorder.Screen;
            menu.HideCalibration();
            menu.ShowScanDebug(null);
            ShowBorderTools();
        }

        private void ShowBorderTools(string hint = null)
        {
            if (hint == null && addingBorder)
                hint = haveAddCorner ? "Point at the opposite corner and pull the trigger. B backs out." :
                    "Point at one corner on the real surface and pull the trigger. B backs out.";
            menu.ShowBorderTools(borderDraft, activeBorder, hint ?? "Hold trigger on an edge to resize, or inside to move.", addingBorder);
        }

        private void SelectBorder(WorkspaceBorder region)
        {
            activeBorder = region;
            addingBorder = !borderDraft.Get(region).HasValue;
            haveAddCorner = false;
            ShowBorderTools();
        }

        private void RemoveDraftBorder()
        {
            borderDraft.Remember();
            borderDraft.Set(activeBorder, null);
            ShowBorderTools();
        }

        private void UndoDraftBorder()
        {
            borderDraft.Undo();
            ShowBorderTools();
        }

        private void ApplyDraftBorders() => ApplyWorkspaceBorders(borderDraft.Screen, borderDraft.Keyboard);

        private void ClearBorderEdit()
        {
            borderDraft = null;
            draggingBorder = addingBorder = haveAddCorner = false;
            menu?.ShowBorderTools(null, default, null, false);
        }

        private void UpdateBorderEdit(bool hasRay, Ray ray, bool pressed)
        {
            if (reset.WasPressedThisFrame() && addingBorder)
            {
                addingBorder = haveAddCorner = false;
                ShowBorderTools();
                RenderMask(false, default, true);
                return;
            }

            if (draggingBorder && (!hasRay || !place.IsPressed()))
                draggingBorder = false;

            bool overTools = menu.UpdateBorderToolPointer(hasRay && !draggingBorder, ray,
                pressed && !draggingBorder, background);
            if (borderDraft == null)
            {
                RenderMask(false, default);
                return;
            }

            if (!hasRay || overTools)
            {
                RenderMask(false, default, true);
                return;
            }

            if (addingBorder)
            {
                UpdateBorderPlacement(ray, pressed);
                return;
            }

            if (!draggingBorder && TryPickDraftBorder(ray, out var region, out var handle, out var point))
            {
                Volume volume = borderDraft.Get(region).Value;
                if (pressed)
                {
                    activeBorder = region;
                    dragStart = volume;
                    dragPoint = point;
                    dragHandle = handle;
                    draggingBorder = true;
                    dragMoved = false;
                    ShowBorderTools();
                }
                RenderMask(true, BorderPoint(volume, point), true);
                return;
            }

            if (draggingBorder && WorkspaceBorderDraft.Project(ray, dragStart, out var current, out _))
            {
                Vector2 delta = current - dragPoint;
                if (!dragMoved && delta.sqrMagnitude > 0.000004f)
                {
                    borderDraft.Remember();
                    dragMoved = true;
                    ShowBorderTools();
                }
                if (dragMoved) borderDraft.Set(activeBorder, WorkspaceBorderDraft.Adjust(dragStart, dragHandle, delta));
                RenderMask(true, BorderPoint(dragStart, current), true);
                return;
            }

            RenderMask(false, default, true);
        }

        private bool TryPickDraftBorder(Ray ray, out WorkspaceBorder region, out BorderHandle handle, out Vector2 point)
        {
            region = default;
            handle = default;
            point = default;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < 2; i++)
            {
                var candidate = (WorkspaceBorder)i;
                Volume? v = borderDraft.Get(candidate);
                if (!v.HasValue || !WorkspaceBorderDraft.Project(ray, v.Value, out var hit, out float distance) ||
                    distance >= nearest || !WorkspaceBorderDraft.PickHandle(v.Value, hit, out var picked)) continue;
                nearest = distance;
                region = candidate;
                point = hit;
                handle = picked;
            }
            return !float.IsPositiveInfinity(nearest);
        }

        private void UpdateBorderPlacement(Ray ray, bool pressed)
        {
            if (!haveAddCorner)
            {
                bool hasSurface = TryGetPlacementPlane(ray, out Volume surface);
                if (pressed)
                {
                    if (hasSurface)
                    {
                        addPlane = surface;
                        haveAddCorner = true;
                        ShowBorderTools();
                    }
                    else ShowBorderTools("Could not read that surface. Aim at a corner and try again. B backs out.");
                }
                RenderMask(hasSurface, surface.Origin, true);
                return;
            }

            bool projected = WorkspaceBorderDraft.Project(ray, addPlane, out Vector2 corner, out _);
            if (pressed && projected)
            {
                if (Mathf.Abs(corner.x) >= 0.06f && Mathf.Abs(corner.y) >= 0.06f)
                {
                    const float padding = 0.012f;
                    var min = new Vector3(Mathf.Min(0, corner.x) - padding, Mathf.Min(0, corner.y) - padding, -0.035f);
                    var max = new Vector3(Mathf.Max(0, corner.x) + padding, Mathf.Max(0, corner.y) + padding, 0.035f);
                    borderDraft.Remember();
                    borderDraft.Set(activeBorder, new Volume(addPlane.Origin, addPlane.Right, addPlane.Up, addPlane.Normal, min, max));
                    addingBorder = haveAddCorner = false;
                    ShowBorderTools();
                }
                else ShowBorderTools("Choose a corner farther away in both directions. B backs out.");
            }
            RenderMask(addingBorder && projected, BorderPoint(addPlane, corner), true);
        }

        private bool TryGetPlacementPlane(Ray ray, out Volume plane)
        {
            plane = default;
            if (!screenSelection.TryDepthPoint(ray, out Vector3 point) ||
                Vector3.Distance(ray.origin, point) > 3f) return false;

            Vector3 right, up, normal;
            if (activeBorder == WorkspaceBorder.Screen)
            {
                // Reuse the scan's verified orientation, even when depth at the corner
                // is too close to the bezel to fit a fresh plane reliably.
                Volume detected = workspacePreview.Volumes[0];
                right = detected.Right;
                up = detected.Up;
                normal = detected.Normal;
            }
            else
            {
                normal = Vector3.up;
                right = Vector3.ProjectOnPlane(borderDraft.Screen?.Right ?? transform.right, normal).normalized;
                if (right.sqrMagnitude < 0.9f) return false;
                up = Vector3.Cross(normal, right).normalized;
            }
            plane = new Volume(point, right, up, normal, default, default);
            return true;
        }

        private static Vector3 BorderPoint(Volume v, Vector2 point) => v.Origin + v.Right * point.x + v.Up * point.y;
    }
}
