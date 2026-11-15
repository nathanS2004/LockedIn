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
        private RectTransform guideBack, guideProgress, guideDial;
        private readonly Image[] progressMarks = new Image[5];
        private void BuildInstructions()
        {
            instructions = CreateRect("Field guide", home.parent, panel.sizeDelta, Vector2.zero);
            AddText(instructions, "LOCKEDIN / FIELD GUIDE", new Vector2(800, 28), new Vector2(-32, 211), 18, Accent);
            instructionTitle = Heading(instructions, "", new Vector2(800, 72), new Vector2(-32, 144), 48);
            Rule(instructions, new Vector2(-420, -29), new Vector2(4, 224), Accent);
            instructionBody = AddText(instructions, "", new Vector2(790, 238), new Vector2(0, -35), 27, Ink);
            instructionBody.lineSpacing = 1.15f;
            instructionBody.resizeTextForBestFit = true;
            instructionBody.resizeTextMinSize = 23;
            instructionBody.resizeTextMaxSize = 27;
            AddItem(instructions, "< Home", new Vector2(200, 50), new Vector2(-332, -218), () => ShowBackgrounds(false));
            AddItem(instructions, "Continue", new Vector2(250, 50), new Vector2(307, -218), () => instructionNext?.Invoke());
            continueLabel = items[items.Count - 1].Label;
            AddItem(instructions, "< Previous", new Vector2(160, 50), new Vector2(-126, -218), () => { walkthroughPage--; ShowWalkthrough(); });
            guideBack = items[items.Count - 1].Surface.rectTransform;
            guideProgress = CreateRect("Guide progress", instructions, new Vector2(128, 3), new Vector2(-367, -171));
            for (int i = 0; i < progressMarks.Length; i++)
                progressMarks[i] = AddImage(CreateRect("Step " + (i + 1), guideProgress, new Vector2(20, 3), new Vector2(i * 27 - 54, 0)), Line);
            guideDial = CreateRect("Physical control illustration", instructions, new Vector2(206, 240), new Vector2(322, -22));
            var face = CreateRect("Knob face", guideDial, new Vector2(190, 190), new Vector2(0, 18)).gameObject.AddComponent<FocusDialGraphic>();
            face.color = Accent; face.Amount = .65f; face.raycastTarget = false;
            Heading(guideDial, "TURN", new Vector2(160, 60), new Vector2(0, 20), 40, TextAnchor.MiddleCenter);
            AddText(guideDial, "LESS ROOM\nMORE FOCUS", new Vector2(190, 65), new Vector2(0, -88), 18, Muted, alignment: TextAnchor.MiddleCenter);
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
            pageFade.alpha = 0;
            lastHovered = null;
            guideBack.gameObject.SetActive(false); guideProgress.gameObject.SetActive(false); guideDial.gameObject.SetActive(false);
            instructionBody.rectTransform.sizeDelta = new Vector2(790, 238);
            instructionBody.rectTransform.anchoredPosition = new Vector2(0, -35);
        }
        private void StartWalkthrough() { walkthroughPage = 0; ShowWalkthrough(); }
        private void ShowWalkthrough()
        {
            string[] titles = { "01 / 05    Make yourself at home.", "02 / 05    Keep the important things clear.", "03 / 05    Find your somewhere.", "04 / 05    A physical way to focus.", "05 / 05    Make it yours." };
            string[] bodies = {
                "Use the RIGHT controller.\nPoint at a button and squeeze the index-finger trigger to select.\nSqueeze the side grip to open or close Home. Help is always available there.",
                "Start with Detect workspace. Look at the whole screen and keyboard, then scan.\nReview the cyan borders. Press A to accept, B to edit or add borders, or trigger to retry.",
                "After selecting a workspace, open Home > Surroundings.\nTry Secluded Beach, Deep Space, Sundowner Overlook, or Shanghai Bund. Your screen and keyboard stay clear. These panoramas are not walkable spaces.\nKnob minimum or right A restores the room.",
                "Turn the physical knob to adjust your surroundings.\nRoom Blur: more turn adds blur to real camera images outside your workspace. Other backgrounds: more turn adds opacity.\nMinimum or NO SIGNAL restores the room. Keep the Nano powered by USB; it sends directly over Wi-Fi.",
                "While focusing, right A hides/shows the effect. Right B starts a NEW manual border.\nBorders stay in the room: scan again if you move your laptop.\nRoom Blur needs camera + scene access. A soft camera-derived fill covers unseen surroundings; your workspace stays clear."
            };
            Explain(titles[walkthroughPage], bodies[walkthroughPage], walkthroughPage == titles.Length - 1 ? "Get started" : "Next", () => {
                if (++walkthroughPage < titles.Length) ShowWalkthrough();
                else { PlayerPrefs.SetInt("LockedIn.Walkthrough.v2", 1); PlayerPrefs.Save(); ShowBackgrounds(false); }
            });
            guideBack.gameObject.SetActive(walkthroughPage > 0);
            guideProgress.gameObject.SetActive(true);
            for (int i = 0; i < progressMarks.Length; i++) progressMarks[i].color = i <= walkthroughPage ? Accent : Line;
            if (walkthroughPage == 3)
            {
                guideDial.gameObject.SetActive(true);
                instructionBody.rectTransform.sizeDelta = new Vector2(550, 256);
                instructionBody.rectTransform.anchoredPosition = new Vector2(-120, -31);
                instructionBody.text = "Turn your knob to adjust your surroundings.\n\nRoom Blur adds blur. Photo environments add opacity.\n\nMinimum or NO SIGNAL restores your room. Keep the Nano powered by USB; it sends directly over Wi-Fi.";
            }
        }
        private void ShowCredits() => Explain("The view, credited.",
            "Poly Haven / CC0: Sundowner Overlook, Secluded Beach, Shanghai Bund, Sunrise Beach.\nLibrary: Waldyrious, Biblioteca Publica de Evora / CC BY-SA 4.0. Preview is resized and cropped.\nDeep Space: NASA/Goddard SVS, Ernie Wright & Tom Bridgman. No endorsement.\nFonts: Barlow & Barlow Condensed / SIL Open Font License. Full sources and licenses are included with the app.",
            "Back to collection", () => ShowBackgrounds(true));
        private void ExplainAI(Action start) => Explain("AI: screen + keyboard",
            "Look at both objects in good lighting. Point your RIGHT controller from your seat. If several screens are visible, aim at the one you want.\nAllow camera/scene access when asked. Trigger starts the scan; keep your head still.\nA accepts the preview. B edits after a scan. Trigger retries. Grip cancels.", "Start AI", start);
        private void ExplainScreen(Action start) => Explain("Auto Select: screen only",
            "This mode finds screen edges using depth. It does not detect a keyboard separately.\nPoint at the screen CENTER and press the right trigger. A successful scan applies immediately.\nIf edges are unclear, retry or press B for manual placement.", "Start selection", start);
        private void ExplainManual(Action start) => Explain("Manual: place three corners",
            "Move the controller close to each screen corner: top-left, top-right, then bottom-left. The cyan dot sits just ahead of the controller; it is NOT a distant laser hit.\nTrigger records each point. Then add the keyboard or press A for screen only. This replaces your previous border.", "Start manual", start);
        private void ExplainBackgrounds() => Explain("Choose your background",
            "First create or detect a workspace so it stays clear.\nRoom Blur combines real-camera blur with soft fill around the entire workspace. Unseen areas are approximated. The knob changes blur strength. Photo environments use opacity.\nClose Home with right grip. A bypasses. No signal restores the room.", "Browse", () => ShowBackgrounds(true));
    }
}
