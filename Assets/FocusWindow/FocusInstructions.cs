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
            string[] titles = { "1 / 5   Your controls", "2 / 5   Keep your workspace clear", "3 / 5   Choose your surroundings", "4 / 5   Your physical knob", "5 / 5   Adjust and try again" };
            string[] bodies = {
                "Use the RIGHT controller.\nPoint at a button and squeeze the index-finger trigger to select.\nSqueeze the side grip to open or close Home. Help is always available there.",
                "Start with Detect Workspace (AI). Look at the whole screen and keyboard, then scan.\nReview the cyan borders. Press right A to accept, or trigger to retry. Right B starts manual placement.",
                "After selecting a workspace, open Home > Choose Background.\nTry Sundowner Overlook, Shanghai Bund, or another environment. Your screen and keyboard stay clear. Photos surround you but are not walkable spaces.\nKnob minimum or right A restores the room.",
                "Turn the physical knob to adjust your surroundings.\nRoom Blur: more turn adds blur to real camera images outside your workspace. Other backgrounds: more turn adds opacity.\nMinimum or NO SIGNAL restores the room. Keep the Nano powered by USB; it sends directly over Wi-Fi.",
                "While focusing, right A hides/shows the effect. Right B starts a NEW manual border.\nBorders stay in the room: scan again if you move your laptop.\nRoom Blur needs camera + scene access. A soft camera-derived fill covers unseen surroundings; your workspace stays clear."
            };
            Explain(titles[walkthroughPage], bodies[walkthroughPage], walkthroughPage == titles.Length - 1 ? "Get started" : "Next", () => {
                if (++walkthroughPage < titles.Length) ShowWalkthrough();
                else { PlayerPrefs.SetInt("LockedIn.Walkthrough.v2", 1); PlayerPrefs.Save(); ShowBackgrounds(false); }
            });
        }
        private void ExplainAI(Action start) => Explain("AI: screen + keyboard",
            "Look at both objects in good lighting. Point your RIGHT controller at the screen.\nAllow camera/scene access when asked. Trigger starts the scan; keep your head still.\nA accepts the preview. Trigger retries. B switches to manual. Grip returns to Home.", "Start AI", start);
        private void ExplainScreen(Action start) => Explain("Auto Select: screen only",
            "This mode finds screen edges using depth. It does not detect a keyboard separately.\nPoint at the screen CENTER and press the right trigger. A successful scan applies immediately.\nIf edges are unclear, retry or press B for manual placement.", "Start selection", start);
        private void ExplainManual(Action start) => Explain("Manual: place three corners",
            "Move the controller close to each screen corner: top-left, top-right, then bottom-left. The cyan dot sits just ahead of the controller; it is NOT a distant laser hit.\nTrigger records each point. Then add the keyboard or press A for screen only. This replaces your previous border.", "Start manual", start);
        private void ExplainBackgrounds() => Explain("Choose your background",
            "First create or detect a workspace so it stays clear.\nRoom Blur combines real-camera blur with soft fill around the entire workspace. Unseen areas are approximated. The knob changes blur strength. Photos/colors use opacity.\nClose Home with right grip. A bypasses. No signal restores the room.", "Browse", () => ShowBackgrounds(true));
    }
}
