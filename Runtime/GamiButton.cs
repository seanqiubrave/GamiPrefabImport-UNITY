// GamiButton.cs  v1.4.7
// Shipped as part of: com.bravegames.gamiprefabimport (Runtime/)
// Original location: Assets/ArteditorTool/
//
// v1.4.7 — Fix "selected button jumps to rightmost slot" bug.
//          v1.4.6 added transform.SetAsLastSibling() on SetSelected(true)
//          to prevent neighbors' rounded corners from occluding the selected
//          button's grown bg. Problem: inside a HorizontalLayoutGroup, sibling
//          order IS layout order. SetAsLastSibling reparented the selected
//          button to the rightmost slot on every tap, so tapping any button
//          shuffled it to the far right of the bar.
//          Fix: remove SetAsLastSibling. The Z-order concern is also
//          unfounded — Bg.offsetMax.y extends UPWARD into empty space above
//          the bar; neighbors stay at their original rect height and never
//          occupy that strip, so there's nothing to occlude.
//          Kept v1.4.6's other change: grow icon scale 1.26→1.40, rise 6→14
//          (web-space, ≈19px in Unity). Matches the browser's dominant-icon
//          look on the selected button.
// v1.4.6 — Two polish fixes (SetAsLastSibling reverted in v1.4.7; icon bump kept).
// v1.4.5 — Consolidated canonical version after a series of attempted Claude
//          rewrites that regressed. This file is the reference implementation
//          going forward. Key architectural decisions below — DO NOT revert:
//
//          1. Animate Bg child's RectTransform.offsetMax.y to "grow" the bg,
//             NOT LayoutElement.preferredHeight on the button root.
//             Why: LayoutElement.preferredHeight compounds across selections
//             because the HorizontalLayoutGroup writes it back every frame,
//             and any capture-rest-pose logic re-reads the ballooned value
//             as the new baseline. Animating the Bg child's offsetMax.y is
//             layout-invisible: the button's own RectTransform size stays
//             fixed, but the Bg visually extends above its bounds. Same
//             visual effect, zero layout conflicts.
//
//          2. Text animates color.a + localScale, NOT SetActive.
//             Why: SetActive toggles destroy the fade, and the importer
//             calls SetActive(false) at bake time — toggling back on causes
//             a flash-of-visible-text on spawn. Keeping TMP always-active
//             and animating alpha 0↔1 matches the web preview's opacity
//             transition and avoids the flash.
//
//          3. _bgBaseHeight, _iconBasePos, _textBaseColor captured exactly
//             once, guarded by _baseCaptured. Subsequent animations
//             reference these fixed rest values — no compounding.
//
//          4. Constants derived from 175px Unity button × web 130px
//             reference. If the browser ever exports a different BTN_H,
//             update `BgHeight` below in lockstep.
//
// v1.4.4 — (previous) Fix Pop feel by understanding the web's bezier curve.
// v1.4.3 — Text color flash fix + Pop shortened.
// v1.4.2 — Added text fade/scale animation alongside Grow and Pop.
// v1.4.1 — Added proper cubic-bezier evaluator matching CSS / WAAPI exactly.
// v1.4.0 — Animation constants ported 1:1 from arteditor.art source.
// v1.3.0 — Pop = Grow minus bg extension (superseded).
// v1.2.0 — Added ClickEffect enum (Grow / Pop). Per-prefab animation style,
//          set by importer from layout.json's click_effect field.
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class AEBChildColors
{
    public string childName;   // "Bg" | "Icon" | "Text"
    public Color  color = Color.white;
}

[Serializable]
public class AEBState
{
    public string          stateName;           // "Normal" | "Selected" | "Disabled"
    public AEBChildColors[] children = Array.Empty<AEBChildColors>();
}

public class GamiButton : MonoBehaviour
{
    public enum ClickEffect { Grow, Pop }

    [Header("Click Effect")]
    [Tooltip("Grow: bg extends upward + icon lifts.  Pop: whole button scales up.")]
    public ClickEffect clickEffect = ClickEffect.Grow;

    [Header("State Data")]
    public AEBState[] stateData = Array.Empty<AEBState>();

    // ── Runtime state ─────────────────────────────────────────────────
    private bool _selected  = false;
    private bool _disabled  = false;

    // ── Child refs (resolved once) ────────────────────────────────────
    private RectTransform _bgRT;
    private Image         _bgImage;
    private Image         _iconImage;
    private TextMeshProUGUI _tmp;

    private float _bgBaseHeight;      // recorded at Start — offsetMax.y in Normal state
    private Vector3 _iconBasePos;     // recorded at Start — anchoredPosition3D in Normal state
    private bool _baseCaptured;       // guard: only capture once

    // Text animation state
    private RectTransform _textRT;
    private Color _textBaseColor = Color.white;

    // ── Animation constants ported 1:1 from arteditor.art NavButtonDesigner.tsx ──
    //
    // Web reference (iconWrap.animate keyframes, WAAPI):
    //   GROW: 100ms ease-out
    //     [0]   translateY(0)    scale(1)
    //     [1]   translateY(-6px) scale(1.26)
    //     + bg height 130→146px over 100ms (+16px extension)
    //
    //   POP: 560ms cubic-bezier(.34,1.56,.64,1) — back-out spring
    //     [0%]  translateY(0)    scale(1)    rotate(0°)
    //     [25%] translateY(-10)  scale(1.08) rotate(-6°)
    //     [55%] translateY(-28)  scale(1.25) rotate(+8°)   ← peak
    //     [75%] translateY(-22)  scale(1.20) rotate(-2°)
    //     [100%]translateY(-20)  scale(1.18) rotate(0°)    ← rest
    //     No bg height change.
    //
    // Web button height is 130px; Unity button height is ~175px. We scale the
    // translateY values by 175/130 ≈ 1.35 so the proportions look identical.
    // Web bg extension is +16px over 130px = 12.3%. Unity: 175 * 0.123 ≈ 22px.
    private const float BgHeight     = 175f;           // matches layoutElement.preferredHeight
    private const float WebBgHeight  = 130f;
    private const float PxScale      = BgHeight / WebBgHeight;   // ≈ 1.35

    private const float GrowY        = 16f * PxScale;  // bg extension for Grow (web: +16px on 130) ≈ 22
    // Grow icon gets noticeably bigger AND higher to match the browser where the
    // selected icon dominates the button's upper half. Previous 1.26 / 6px
    // felt too subtle in-engine.
    private const float GrowIconY    = 14f * PxScale;  // icon lift (≈ 19px)
    private const float GrowIconSc   = 1.40f;          // icon scale peak
    private const float GrowTime     = 0.10f;          // web 100ms

    private const float PopIconY0    = 10f * PxScale;  // ≈ 13
    private const float PopIconY1    = 28f * PxScale;  // ≈ 38  (peak)
    private const float PopIconY2    = 22f * PxScale;  // ≈ 30
    private const float PopIconY3    = 20f * PxScale;  // ≈ 27  (rest)
    private const float PopSc0       = 1.08f;
    private const float PopSc1       = 1.25f;          // peak
    private const float PopSc2       = 1.20f;
    private const float PopSc3       = 1.18f;          // rest
    private const float PopRot0      = -6f;
    private const float PopRot1      = 8f;
    private const float PopRot2      = -2f;
    private const float PopRot3      = 0f;
    private const float PopTime      = 0.25f;          // total motion time.
                                                       // Web nominally 560ms but overshoot-clamp
                                                       // means only ~225ms is active motion; this
                                                       // matches that effective window.

    // ── Label/text animation (runs for both Grow and Pop) ─────────────
    // Web (inline style):  opacity 0→1 over 150ms ease; transform scale 1→1.1 over 100ms ease
    // Unity: shortened to match the snappier-than-web Pop timing.
    private const float TextScale    = 1.10f;          // web scale(1.1)
    private const float TextFadeTime = 0.10f;          // web 150ms → shortened to 100ms
    private const float TextScaleTime = 0.08f;         // web 100ms → shortened to 80ms

    // Back-out spring: cubic-bezier(.34, 1.56, .64, 1)
    // We approximate with a custom evaluator in AnimateRoutinePop.

    // ─────────────────────────────────────────────────────────────────
    void OnEnable()
    {
        // Only apply runtime colour logic in play mode.
        // Edit-mode colours are baked by ArteditorImporter post-process.
        if (!Application.isPlaying) return;
        ApplyState("Normal", immediate: true);
    }

    void Start()
    {
        if (!Application.isPlaying) return;
        ResolveChildren();
        CaptureBasePositions();
        ApplyState("Normal", immediate: true);
    }

    private void CaptureBasePositions()
    {
        if (_baseCaptured) return;
        if (_bgRT != null) _bgBaseHeight = _bgRT.offsetMax.y;
        var iconRT = _iconImage != null ? _iconImage.rectTransform : null;
        if (iconRT != null) _iconBasePos = iconRT.anchoredPosition3D;

        // Text base state — we keep the text GameObject always active and animate
        // alpha + scale instead of toggling SetActive. This matches the web's
        // label animation (opacity 0→1, scale 1→1.1, bottom-center origin).
        if (_tmp != null)
        {
            // Use the SELECTED state's text color as the displayed color.
            // Text is only ever visible when selected, so that's the only color
            // that matters. This avoids the white→black flash that occurred
            // when ApplyState rewrote color mid-fade.
            var selState = FindState("Selected");
            if (selState != null && selState.children != null)
            {
                foreach (var c in selState.children)
                {
                    if (c.childName == "Text") { _textBaseColor = c.color; break; }
                }
            }
            if (_textBaseColor.a == 0f) _textBaseColor = Color.white; // safety fallback

            _textRT = _tmp.rectTransform;
            _tmp.gameObject.SetActive(true);   // always active; alpha controls visibility
            // Start with alpha=0 and scale=1. First SetSelected(true) fades in;
            // SetSelected(false) keeps at 0. Prevents flash-of-visible-text on spawn.
            _tmp.color = new Color(_textBaseColor.r, _textBaseColor.g, _textBaseColor.b, 0f);
            if (_textRT != null) _textRT.localScale = Vector3.one;
        }

        _baseCaptured = true;
    }

    // ── Public API ────────────────────────────────────────────────────

    public void SetSelected(bool selected, bool immediate = false)
    {
        _selected = selected;

        // NOTE on Z-order: an earlier revision called transform.SetAsLastSibling()
        // here, intending to prevent neighbor buttons' rounded corners from
        // drawing over the selected button's grown bg. That was a bug inside a
        // HorizontalLayoutGroup: sibling order IS layout order. Calling
        // SetAsLastSibling reparented the selected button to the rightmost
        // slot in the bar on every tap. Removed.
        //
        // The Z-order concern was also unfounded: Bg.offsetMax.y += 22 extends
        // the selected button's bg upward, but neighbors stay at their
        // original rect height. Nothing draws into the extended strip except
        // the selected button itself. No occlusion possible.

        ApplyState(CurrentStateName(), immediate);
        AnimateGrowth(selected, immediate);
    }

    public void SetDisabled(bool disabled)
    {
        _disabled = disabled;
        ApplyState(CurrentStateName(), immediate: false);
    }

    public void SetState(string stateName, bool immediate = false)
    {
        ApplyState(stateName, immediate);
    }

    // ── Internal ──────────────────────────────────────────────────────

    private string CurrentStateName()
    {
        if (_disabled)  return "Disabled";
        if (_selected)  return "Selected";
        return "Normal";
    }

    private void ResolveChildren()
    {
        var bgT    = transform.Find("Bg");
        var iconT  = transform.Find("Icon");
        var textT  = transform.Find("Text");

        if (bgT   != null) { _bgImage = bgT.GetComponent<Image>();         _bgRT = bgT as RectTransform; }
        if (iconT != null)   _iconImage = iconT.GetComponent<Image>();
        if (textT != null)   _tmp = textT.GetComponent<TextMeshProUGUI>();
    }

    private void ApplyState(string stateName, bool immediate)
    {
        AEBState state = FindState(stateName);
        if (state == null) return;

        if (_bgImage   == null) ResolveChildren();

        foreach (var c in state.children)
        {
            switch (c.childName)
            {
                case "Bg":   if (_bgImage   != null) _bgImage.color   = c.color; break;
                case "Icon": if (_iconImage != null) _iconImage.color = c.color; break;
                // Text: color is owned by the animation (captured once from the
                // Selected state at startup). Do NOT rewrite it here — doing so
                // caused a flash-of-wrong-color during deselect fades.
            }
        }
    }

    private void AnimateGrowth(bool selected, bool immediate)
    {
        if (_bgRT == null) ResolveChildren();
        if (_bgRT == null) return;
        CaptureBasePositions();   // safe to call multiple times — guarded by _baseCaptured

        if (immediate)
        {
            // No animation — snap to final state based on clickEffect.
            SetImmediateState(selected);
            return;
        }

        StopAllCoroutines();
        if (clickEffect == ClickEffect.Pop)
            StartCoroutine(AnimatePopRoutine(selected));
        else
            StartCoroutine(AnimateGrowRoutine(selected));

        // Text fade + scale runs alongside whichever icon animation is playing.
        // Web applies this to both effects via inline-style + CSS transition.
        if (_tmp != null)
            StartCoroutine(AnimateTextRoutine(selected));
    }

    // Snap to final state for a given clickEffect, no animation.
    // Used for deselected→selected transitions at scene startup (immediate=true).
    private void SetImmediateState(bool selected)
    {
        bool isGrow = (clickEffect == ClickEffect.Grow);

        // Bg: Grow extends +GrowY when selected; Pop keeps bg flat.
        float targetBgY = _bgBaseHeight + (selected && isGrow ? GrowY : 0f);
        _bgRT.offsetMax = new Vector2(_bgRT.offsetMax.x, targetBgY);

        // Icon: final rest pose.
        var iconRT = _iconImage != null ? _iconImage.rectTransform : null;
        if (iconRT == null) return;

        if (!selected)
        {
            iconRT.localScale = Vector3.one;
            iconRT.anchoredPosition3D = _iconBasePos;
            iconRT.localRotation = Quaternion.identity;
            ApplyTextImmediate(false);
            return;
        }

        // Selected rest pose differs per effect:
        float iconDY;
        float iconSc;
        if (isGrow)
        {
            iconDY = GrowIconY;
            iconSc = GrowIconSc;
        }
        else // Pop rest (last keyframe, offset 1.0)
        {
            iconDY = PopIconY3;
            iconSc = PopSc3;
        }
        iconRT.anchoredPosition3D = new Vector3(_iconBasePos.x,
                                                _iconBasePos.y + iconDY,
                                                _iconBasePos.z);
        iconRT.localScale    = new Vector3(iconSc, iconSc, 1f);
        iconRT.localRotation = Quaternion.identity;

        // Text: snap to final state
        ApplyTextImmediate(selected);
    }

    private void ApplyTextImmediate(bool selected)
    {
        if (_tmp == null) return;
        float alpha = selected ? 1f : 0f;
        _tmp.color = new Color(_textBaseColor.r, _textBaseColor.g, _textBaseColor.b, alpha);
        if (_textRT != null)
        {
            float s = selected ? TextScale : 1f;
            _textRT.localScale = new Vector3(s, s, 1f);
        }
    }



    // ─────────────────────────────────────────────────────────────────
    // GROW (web: 100ms, icon translateY -6px, scale 1.26, + bg +16px)
    // ─────────────────────────────────────────────────────────────────
    private IEnumerator AnimateGrowRoutine(bool selected)
    {
        var iconRT = _iconImage != null ? _iconImage.rectTransform : null;

        float startBgY      = _bgRT.offsetMax.y;
        float targetBgY     = _bgBaseHeight + (selected ? GrowY : 0f);

        Vector3 iconStartScale = iconRT != null ? iconRT.localScale         : Vector3.one;
        Vector3 iconStartPos   = iconRT != null ? iconRT.anchoredPosition3D : _iconBasePos;
        Quaternion iconStartRot = iconRT != null ? iconRT.localRotation     : Quaternion.identity;

        Vector3 iconEndScale = selected ? new Vector3(GrowIconSc, GrowIconSc, 1f) : Vector3.one;
        Vector3 iconEndPos   = new Vector3(_iconBasePos.x,
                                           _iconBasePos.y + (selected ? GrowIconY : 0f),
                                           _iconBasePos.z);

        float t = 0f;
        while (t < GrowTime)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / GrowTime);
            // Web: cubic-bezier(.4, 0, .2, 1) — Material's "standard" curve.
            // Accelerates quickly, decelerates gently.
            float e = CubicBezier(n, 0.4f, 0f, 0.2f, 1f);

            _bgRT.offsetMax = new Vector2(_bgRT.offsetMax.x, Mathf.Lerp(startBgY, targetBgY, e));

            if (iconRT != null)
            {
                iconRT.localScale         = Vector3.Lerp(iconStartScale, iconEndScale, e);
                iconRT.anchoredPosition3D = Vector3.Lerp(iconStartPos,   iconEndPos,   e);
                iconRT.localRotation      = Quaternion.Slerp(iconStartRot, Quaternion.identity, e);
            }
            yield return null;
        }

        _bgRT.offsetMax = new Vector2(_bgRT.offsetMax.x, targetBgY);
        if (iconRT != null)
        {
            iconRT.localScale         = iconEndScale;
            iconRT.anchoredPosition3D = iconEndPos;
            iconRT.localRotation      = Quaternion.identity;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // POP (web: 560ms, back-out spring with 5 keyframes, no bg extension)
    // Keyframes:
    //   t=0.00: y=0,     scale=1.00, rot=0°
    //   t=0.25: y=-10,   scale=1.08, rot=-6°
    //   t=0.55: y=-28,   scale=1.25, rot=+8°   (peak)
    //   t=0.75: y=-22,   scale=1.20, rot=-2°
    //   t=1.00: y=-20,   scale=1.18, rot=0°    (rest)
    //
    // Web easing cubic-bezier(.34, 1.56, .64, 1) is applied per-segment; since we
    // linearly interpolate between WAAPI keyframes (WAAPI default is linear unless
    // offsets specify otherwise), we match by lerping segment-by-segment.
    // ─────────────────────────────────────────────────────────────────
    private IEnumerator AnimatePopRoutine(bool selected)
    {
        var iconRT = _iconImage != null ? _iconImage.rectTransform : null;

        // Bg stays flat — snap to base height immediately.
        _bgRT.offsetMax = new Vector2(_bgRT.offsetMax.x, _bgBaseHeight);

        if (iconRT == null) yield break;

        Vector3 iconStartScale = iconRT.localScale;
        Vector3 iconStartPos   = iconRT.anchoredPosition3D;
        Quaternion iconStartRot = iconRT.localRotation;

        // When deselecting: animate back to base over half the time (120ms).
        if (!selected)
        {
            float dur = 0.12f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / dur);
                float e = 1f - Mathf.Pow(1f - n, 3f); // ease-out cubic
                iconRT.localScale         = Vector3.Lerp(iconStartScale, Vector3.one, e);
                iconRT.anchoredPosition3D = Vector3.Lerp(iconStartPos,   _iconBasePos, e);
                iconRT.localRotation      = Quaternion.Slerp(iconStartRot, Quaternion.identity, e);
                yield return null;
            }
            iconRT.localScale         = Vector3.one;
            iconRT.anchoredPosition3D = _iconBasePos;
            iconRT.localRotation      = Quaternion.identity;
            yield break;
        }

        // Pop selected: 5-keyframe animation over PopTime.
        // Keyframes ported 1:1 from arteditor.art NavButtonDesigner.tsx:
        //   offset 0.00: y=0,    scale=1.00, rot=0°
        //   offset 0.25: y=-10,  scale=1.08, rot=-6°
        //   offset 0.55: y=-28,  scale=1.25, rot=+8°   (peak)
        //   offset 0.75: y=-22,  scale=1.20, rot=-2°
        //   offset 1.00: y=-20,  scale=1.18, rot=0°    (rest)
        float[] ofs   = { 0.00f,   0.25f,   0.55f,   0.75f,   1.00f };
        float[] dys   = { 0f,      PopIconY0, PopIconY1, PopIconY2, PopIconY3 };
        float[] scs   = { 1.00f,   PopSc0,    PopSc1,    PopSc2,    PopSc3    };
        float[] rots  = { 0f,      PopRot0,   PopRot1,   PopRot2,   PopRot3   };

        float total = 0f;
        while (total < PopTime)
        {
            total += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(total / PopTime);

            // Web's WAAPI applies cubic-bezier(.34, 1.56, .64, 1) to the global
            // timeline. That curve overshoots past y=1 at t≈0.4, so the web
            // animation actually hits its final keyframe at ~40% of duration and
            // holds there for the remaining 60%. (Effective motion: ~225ms of
            // the 560ms total.)
            //
            // For Unity we use a simpler non-overshooting ease-out so the ENTIRE
            // PopTime is active motion — the keyframe path already contains the
            // bounce, so we don't need a bouncy timeline curve on top of it.
            // Using cubic-bezier(0, 0, .2, 1) ≈ strong ease-out (like Material's
            // "decelerate"). Matches the "look" without wasting timeline.
            float nE = CubicBezier(n, 0f, 0f, 0.2f, 1f);

            // Find which segment nE falls into.
            int seg = ofs.Length - 2;
            for (int i = 1; i < ofs.Length; i++)
            {
                if (nE <= ofs[i]) { seg = i - 1; break; }
            }

            float segStart = ofs[seg];
            float segEnd   = ofs[seg + 1];
            float k = (nE - segStart) / Mathf.Max(0.0001f, segEnd - segStart);

            // Linear lerp within each segment (WAAPI default between keyframes).
            float dy  = Mathf.Lerp(dys[seg],  dys[seg + 1],  k);
            float sc  = Mathf.Lerp(scs[seg],  scs[seg + 1],  k);
            float rot = Mathf.Lerp(rots[seg], rots[seg + 1], k);

            iconRT.anchoredPosition3D = new Vector3(_iconBasePos.x,
                                                    _iconBasePos.y + dy,
                                                    _iconBasePos.z);
            iconRT.localScale    = new Vector3(sc, sc, 1f);
            iconRT.localRotation = Quaternion.Euler(0f, 0f, rot);
            yield return null;
        }

        // Snap to final rest pose.
        iconRT.anchoredPosition3D = new Vector3(_iconBasePos.x,
                                                _iconBasePos.y + PopIconY3,
                                                _iconBasePos.z);
        iconRT.localScale    = new Vector3(PopSc3, PopSc3, 1f);
        iconRT.localRotation = Quaternion.identity;
    }

    // ─────────────────────────────────────────────────────────────────
    // TEXT / LABEL (runs for both Grow and Pop)
    // Web inline style:
    //   opacity: 0 → 1 when selected     (150ms ease)
    //   transform: scale(1) → scale(1.1) (100ms ease)
    //   transformOrigin: bottom center
    //
    // In Unity, Text pivot is (0.5, 0) — bottom-center — so localScale
    // expands upward from the baseline, matching the web origin.
    // ─────────────────────────────────────────────────────────────────
    private IEnumerator AnimateTextRoutine(bool selected)
    {
        if (_tmp == null) yield break;

        float startAlpha = _tmp.color.a;
        float endAlpha   = selected ? 1f : 0f;

        Vector3 startScale = _textRT != null ? _textRT.localScale : Vector3.one;
        float   endS       = selected ? TextScale : 1f;
        Vector3 endScale   = new Vector3(endS, endS, 1f);

        // Run both sub-animations concurrently; fade is 150ms, scale is 100ms.
        // We loop for the longer of the two and sample each on its own clock.
        float dur = Mathf.Max(TextFadeTime, TextScaleTime);
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;

            // Fade (ease — web uses default "ease" keyword = cubic-bezier(.25,.1,.25,1))
            float fT = Mathf.Clamp01(t / TextFadeTime);
            float fE = CubicBezier(fT, 0.25f, 0.1f, 0.25f, 1f);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, fE);
            _tmp.color = new Color(_textBaseColor.r, _textBaseColor.g, _textBaseColor.b, alpha);

            // Scale (100ms ease)
            if (_textRT != null)
            {
                float sT = Mathf.Clamp01(t / TextScaleTime);
                float sE = CubicBezier(sT, 0.25f, 0.1f, 0.25f, 1f);
                _textRT.localScale = Vector3.Lerp(startScale, endScale, sE);
            }

            yield return null;
        }

        _tmp.color = new Color(_textBaseColor.r, _textBaseColor.g, _textBaseColor.b, endAlpha);
        if (_textRT != null) _textRT.localScale = endScale;
    }


    // Cubic-bezier easing evaluator matching CSS / WAAPI cubic-bezier(x1,y1,x2,y2).
    // Given progress t∈[0,1], returns eased value. Uses Newton's method to solve
    // for parameter u such that bezierX(u) = t, then returns bezierY(u).
    // Y values can exceed 1.0 for spring-like curves (e.g. back-out).
    private static float CubicBezier(float t, float x1, float y1, float x2, float y2)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;

        // Newton iteration to find u where X(u) = t
        float u = t;
        for (int i = 0; i < 8; i++)
        {
            float u1  = 1f - u;
            float xAt = 3f * u1 * u1 * u * x1 + 3f * u1 * u * u * x2 + u * u * u;
            float dx  = 3f * u1 * u1 * x1 - 6f * u1 * u * x1 + 6f * u1 * u * x2
                      - 3f * u * u * x2 + 3f * u * u;
            if (Mathf.Abs(dx) < 1e-6f) break;
            u -= (xAt - t) / dx;
            u = Mathf.Clamp01(u);
        }

        float v1 = 1f - u;
        return 3f * v1 * v1 * u * y1 + 3f * v1 * u * u * y2 + u * u * u;
    }

    private AEBState FindState(string name)
    {
        foreach (var s in stateData)
            if (s.stateName == name) return s;
        return null;
    }

// ── Editor-only Inspector preview ────────────────────────────────────
#if UNITY_EDITOR
    [CustomEditor(typeof(GamiButton))]
    public class GamiButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var aeb = (GamiButton)target;
            GUILayout.Space(6);
            GUILayout.Label("Preview (Edit Mode)", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Normal"))   { Undo.RecordObject(aeb, "AEB Normal");   aeb.PreviewState("Normal");   }
            if (GUILayout.Button("Selected")) { Undo.RecordObject(aeb, "AEB Selected"); aeb.PreviewState("Selected"); }
            if (GUILayout.Button("Disabled")) { Undo.RecordObject(aeb, "AEB Disabled"); aeb.PreviewState("Disabled"); }
            GUILayout.EndHorizontal();
        }
    }
#endif

    // Called from editor preview only
    public void PreviewState(string stateName) => ApplyState(stateName, immediate: true);
}
