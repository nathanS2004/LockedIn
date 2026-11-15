using System;
using UnityEngine;
using UnityEngine.UI;
namespace LockedIn
{
    internal sealed partial class FocusHomeMenu
    {
        private RectTransform instructions;
        private Text instructionTitle, instructionBody, continueLabel;
        private Action instructionNext;
        private int walkthroughPage;
        private void BuildInstructions()
        {
            instructions = CreateRect("Instructions", panel, panel.sizeDelta, Vector2.zero);
            instructionTitle = AddText(instructions, "", new Vector2(560, 44), new Vector2(0, 110), 28, Accent, FontStyle.Bold);
            instructionBody = AddText(instructions, "", new Vector2(560, 205), new Vector2(0, -18), 24, Ink);
            instructionBody.resizeTextForBestFit = true;
            instructionBody.resizeTextMinSize = 20;
            instructionBody.resizeTextMaxSize = 24;
            AddItem(instructions, "Back to Home", new Vector2(268, 48), new Vector2(-144, -146), () => ShowBackgrounds(false));
            AddItem(instructions, "Continue", new Vector2(268, 48), new Vector2(144, -146), () => instructionNext?.Invoke());
            continueLabel = items[items.Count - 1].Label;
            instructions.gameObject.SetActive(false);
        }
        private void Explain(string title, string body, string next, Action action)
        {
            home.gameObject.SetActive(false);
            backgrounds.gameObject.SetActive(false);
            instructions.gameObject.SetActive(true);
            instructionTitle.text = title;
            instructionBody.text = body;
            continueLabel.text = next;
            instructionNext = action;
        }
        private void StartWalkthrough() { walkthroughPage = 0; ShowWalkthrough(); }
        private void ShowWalkthrough()
        {
            string[] titles = { "1 / 4   Your controls", "2 / 4   Keep your workspace clear", "3 / 4   Choose your surroundings", "4 / 4   Adjust and try again" };
            string[] bodies = {
                "Use the RIGHT controller.\nPoint at a button and squeeze the index-finger trigger to select.\nSqueeze the side grip to open or close Home. Help is always available there.",
                "Start with Detect Workspace (AI). Look at the whole screen and keyboard, then scan.\nReview the cyan borders. Press right A to accept, or trigger to retry. Right B starts manual placement.",
                "After selecting a workspace, open Home > Choose Background.\nYour screen and keyboard stay clear. Passthrough shows your room. Photo environments surround you but are not walkable spaces.",
                "While focusing, right A hides/shows the background. Right B starts a NEW manual border.\nBorders stay in the room: scan again if you move your laptop.\nDim Room strength: right thumbstick up/down."
            };
            Explain(titles[walkthroughPage], bodies[walkthroughPage], walkthroughPage == 3 ? "Get started" : "Next", () => {
                if (++walkthroughPage < titles.Length) ShowWalkthrough();
                else { PlayerPrefs.SetInt("LockedIn.Walkthrough.v1", 1); PlayerPrefs.Save(); ShowBackgrounds(false); }
            });
        }
        private void ExplainAI(Action start) => Explain("AI: screen + keyboard",
            "Look at both objects in good lighting. Point your RIGHT controller at the screen.\nAllow camera/scene access when asked. Trigger starts the scan; keep your head still.\nA accepts the preview. Trigger retries. B switches to manual. Grip returns to Home.", "Start AI", start);
        private void ExplainScreen(Action start) => Explain("Auto Select: screen only",
            "This mode finds screen edges using depth. It does not detect a keyboard separately.\nPoint at the screen CENTER and press the right trigger. A successful scan applies immediately.\nIf edges are unclear, retry or press B for manual placement.", "Start selection", start);
        private void ExplainManual(Action start) => Explain("Manual: place three corners",
            "Move the controller close to each screen corner: top-left, top-right, then bottom-left. The cyan dot sits just ahead of the controller; it is NOT a distant laser hit.\nTrigger records each point. Then add the keyboard or press A for screen only. This replaces your previous border.", "Start manual", start);
        private void ExplainBackgrounds() => Explain("Choose your background",
            "First create or detect a workspace so it stays clear. Without one, backgrounds remain partly transparent.\nChoose a photo, solid color, or Dim Room. Passthrough restores your room.\nClose Home with the right grip. Outside menus/scans, right A toggles the background.", "Browse", () => ShowBackgrounds(true));
    }
}
