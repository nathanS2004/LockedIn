using System;
using UnityEngine;

namespace LockedIn
{
    internal sealed partial class FocusHomeMenu
    {
        private RectTransform borderTools, borderCursor;
        private UnityEngine.UI.Text screenTool, keyboardTool, editHint;
        private GameObject removeTool, undoTool, applyTool;

        public void BuildBorderTools(Action screen, Action keyboard, Action remove, Action undo, Action apply, Action cancel)
        {
            borderTools = CreateRect("Border tools", panel.parent, new Vector2(320, 278), Vector2.zero);
            borderTools.localPosition = new Vector3(0.36f, 0.015f, 1.1f);
            borderTools.localScale = panel.localScale;
            var canvas = borderTools.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = panel.GetComponent<Canvas>().worldCamera;
            canvas.sortingOrder = 6;
            AddImage(borderTools, Panel);
            AddText(borderTools, "Edit borders", new Vector2(288, 32), new Vector2(0, 113), 24, Accent, FontStyle.Bold);
            screenTool = BorderTool("Screen", 0, 76, screen).Label;
            keyboardTool = BorderTool("Keyboard", 0, 36, keyboard).Label;
            removeTool = BorderTool("Remove", -74, -7, remove).Surface.gameObject;
            undoTool = BorderTool("Undo", 74, -7, undo).Surface.gameObject;
            applyTool = BorderTool("Apply", -74, -50, apply).Surface.gameObject;
            BorderTool("Cancel", 74, -50, cancel);
            editHint = AddText(borderTools, "", new Vector2(288, 54), new Vector2(0, -101), 16, Ink);
            borderCursor = CreateRect("Border tool pointer", borderTools, new Vector2(11, 11), Vector2.zero);
            borderCursor.localRotation = Quaternion.Euler(0, 0, 45);
            AddImage(borderCursor, Accent);
            borderTools.gameObject.SetActive(false);
        }

        private MenuItem BorderTool(string label, float x, float y, Action action)
        {
            AddItem(borderTools, label, new Vector2(x == 0 ? 292 : 138, 35), new Vector2(x, y), action);
            MenuItem item = items[items.Count - 1];
            item.Label.fontSize = 18;
            return item;
        }

        public void ShowBorderTools(WorkspaceBorderDraft draft, WorkspaceBorder selected, string hint, bool placing)
        {
            if (draft == null)
            {
                borderTools.gameObject.SetActive(false);
                return;
            }

            borderTools.gameObject.SetActive(true);
            screenTool.text = draft.Screen.HasValue ? (selected == WorkspaceBorder.Screen ? "Screen  *" : "Screen") : "Add screen";
            keyboardTool.text = draft.Keyboard.HasValue ? (selected == WorkspaceBorder.Keyboard ? "Keyboard  *" : "Keyboard") : "Add keyboard";
            screenTool.transform.parent.gameObject.SetActive(!placing);
            keyboardTool.transform.parent.gameObject.SetActive(!placing);
            removeTool.SetActive(!placing && draft.Get(selected).HasValue);
            undoTool.SetActive(!placing && draft.CanUndo);
            applyTool.SetActive(!placing && (draft.Screen.HasValue || draft.Keyboard.HasValue));
            editHint.text = hint;
        }

        public bool UpdateBorderToolPointer(bool hasRay, Ray ray, bool pressed, BackgroundStyle selected) =>
            UpdatePointerOnPanel(borderTools, borderCursor, hasRay, ray, pressed, selected);
    }
}
