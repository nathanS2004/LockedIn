using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LockedIn
{
    internal sealed partial class FocusHomeMenu
    {
        private RectTransform scanDebugPanel, scanImage;
        private RawImage cameraPreview;
        private Text scanDebugTitle;
        private readonly List<GameObject> scanBoxObjects = new List<GameObject>();

        private void BuildScanDebug()
        {
            scanDebugPanel = CreateRect("Scan debug preview", panel.parent, new Vector2(500, 426), Vector2.zero);
            scanDebugPanel.localPosition = new Vector3(0f, 0.115f, 1.1f);
            scanDebugPanel.localScale = panel.localScale;
            var canvas = scanDebugPanel.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = panel.GetComponent<Canvas>().worldCamera;
            canvas.sortingOrder = 5;
            AddImage(scanDebugPanel, Panel);
            scanDebugTitle = AddText(scanDebugPanel, "", new Vector2(470, 42), new Vector2(0, 183),
                19, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            scanImage = CreateRect("Frozen camera frame", scanDebugPanel, new Vector2(450, 348), new Vector2(0, -16));
            cameraPreview = scanImage.gameObject.AddComponent<RawImage>();
            cameraPreview.raycastTarget = false;
            scanDebugPanel.gameObject.SetActive(false);
        }

        public void ShowScanDebug(WorkspaceScanDebug debug)
        {
            foreach (var boxObject in scanBoxObjects)
            {
                boxObject.SetActive(false);
                UnityEngine.Object.Destroy(boxObject);
            }
            scanBoxObjects.Clear();
            cameraPreview.texture = debug?.Frame;
            scanDebugPanel.gameObject.SetActive(debug?.Frame != null);
            if (debug?.Frame == null) return;

            float aspect = (float)debug.Frame.width / debug.Frame.height;
            Vector2 imageSize = new Vector2(Mathf.Min(450, 348 * aspect), Mathf.Min(348, 450 / aspect));
            scanImage.sizeDelta = imageSize;
            scanDebugTitle.text = $"Scan #{debug.Id} | Mint: kept by filter | Amber: filtered";
            foreach (var box in debug.Boxes.OrderByDescending(b => debug.Kept?.Contains(b) == true)
                         .ThenByDescending(b => b.Score).Take(12))
            {
                Rect r = box.Rect;
                if (!(r.width > 0 && r.height > 0)) continue;
                Color color = debug.Kept?.Contains(box) == true ? Accent : new Color32(255, 199, 100, 255);
                Vector2 size = Vector2.Scale(r.size, imageSize);
                Vector2 position = Vector2.Scale(new Vector2(r.center.x - 0.5f, 0.5f - r.center.y), imageSize);
                DrawScanBox(debug.BoxNumber(box), box, color, size, position);
            }
        }

        private void DrawScanBox(int number, WorkspaceBox box, Color color, Vector2 size, Vector2 position)
        {
            RectTransform rect = CreateRect("Model box", scanImage, size, position);
            scanBoxObjects.Add(rect.gameObject);
            for (int side = -1; side <= 1; side += 2)
            {
                AddImage(CreateRect("Horizontal edge", rect, new Vector2(size.x, 2),
                    new Vector2(0, side * size.y / 2)), color);
                AddImage(CreateRect("Vertical edge", rect, new Vector2(2, size.y),
                    new Vector2(side * size.x / 2, 0)), color);
            }
            const float labelWidth = 172f;
            float labelX = Mathf.Clamp(position.x - size.x / 2 + labelWidth / 2,
                -scanImage.sizeDelta.x / 2 + labelWidth / 2, scanImage.sizeDelta.x / 2 - labelWidth / 2);
            RectTransform caption = CreateRect("Model label", rect, new Vector2(labelWidth, 22),
                new Vector2(labelX - position.x, size.y / 2 - 11));
            AddImage(caption, Panel);
            AddText(caption, $"#{number} {WorkspaceScanDebug.Label(box.ClassId)} {box.Score:F2}",
                caption.sizeDelta, Vector2.zero, 15, color, alignment: TextAnchor.MiddleCenter);
        }
    }
}
