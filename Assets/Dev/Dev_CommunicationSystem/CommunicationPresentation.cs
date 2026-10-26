using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Transient communication hover, outlines, tooltips and panel requests.</summary>
internal sealed class CommunicationPresentation
{
    // Borrowed output ports, supplied per operation. No UI controller or owner
    // reference is retained by this helper, and no subscriptions are installed.
    internal readonly struct TooltipBindings
    {
        internal readonly Action<UIDocument> setDocument;
        internal readonly Action bindStatusSource;
        internal readonly Action<bool> setDebugLogs;
        internal readonly Action<CommunicationTowerData, Vector2> show;
        internal readonly Action hide;
        internal readonly Func<bool> isVisible;
        internal bool IsAvailable => setDocument != null;

        internal TooltipBindings(
            Action<UIDocument> setDocument, Action bindStatusSource, Action<bool> setDebugLogs,
            Action<CommunicationTowerData, Vector2> show, Action hide, Func<bool> isVisible)
        {
            this.setDocument = setDocument;
            this.bindStatusSource = bindStatusSource;
            this.setDebugLogs = setDebugLogs;
            this.show = show;
            this.hide = hide;
            this.isVisible = isVisible;
        }
    }

    internal readonly struct Context
    {
        internal readonly ZoneController zoneController;
        internal readonly Color associatedZoneHoverColor;
        internal readonly TooltipBindings tooltip;
        internal readonly bool debugLogs;
        internal readonly UnityEngine.Object diagnosticContext;

        internal Context(
            ZoneController zoneController, Color associatedZoneHoverColor,
            TooltipBindings tooltip, bool debugLogs, UnityEngine.Object diagnosticContext)
        {
            this.zoneController = zoneController;
            this.associatedZoneHoverColor = associatedZoneHoverColor;
            this.tooltip = tooltip;
            this.debugLogs = debugLogs;
            this.diagnosticContext = diagnosticContext;
        }
    }

    private CommunicationTowerData _hoveredTower;
    private bool _loggedOutlineRequestFailure;
    internal CommunicationTowerData HoveredTower => _hoveredTower;

    internal void UpdateHoveredTower(
        CommunicationTowerData tower, Vector2 mouseScreen,
        CommunicationTowerVisuals visuals, CommunicationTowerVisuals.Settings settings,
        Context context, ref UIDocument tooltipUIDocument)
    {
        if (tower == null)
        {
            ClearHoveredTower(visuals, settings, context);
            return;
        }

        if (_hoveredTower == tower)
        {
            ShowTowerTooltip(tower, mouseScreen, context.tooltip, ref tooltipUIDocument, context.debugLogs);
            return;
        }

        ClearHoveredTower(visuals, settings, context);
        _hoveredTower = tower;
        visuals.UpdateTowerVisual(_hoveredTower, _hoveredTower, settings);
        ShowAssociatedZoneHighlight(_hoveredTower, context);
        ShowTowerTooltip(_hoveredTower, mouseScreen, context.tooltip, ref tooltipUIDocument, context.debugLogs);
    }

    internal void ClearHoveredTower(
        CommunicationTowerVisuals visuals, CommunicationTowerVisuals.Settings settings, Context context)
    {
        if (_hoveredTower != null)
        {
            CommunicationTowerData previousTower = _hoveredTower;
            _hoveredTower = null;
            visuals.UpdateTowerVisual(previousTower, _hoveredTower, settings);
        }

        if (context.zoneController != null)
            context.zoneController.ClearOutlines(ZoneOutlineChannel.CommunicationZones);

        HideTowerTooltip(context.tooltip);
    }

    internal void EnsureTooltipConfigured(
        TooltipBindings tooltip, ref UIDocument tooltipUIDocument, bool debugLogs)
    {
        if (!tooltip.IsAvailable)
            return;

        if (tooltipUIDocument == null)
            tooltipUIDocument = ResolveTooltipUIDocument();

        tooltip.setDocument(tooltipUIDocument);
        tooltip.bindStatusSource();
        tooltip.setDebugLogs(debugLogs);
    }

    internal void OpenCommunicationPanel(CommunicationTowerData tower, Action<CommunicationTowerData> showPanel)
    {
        if (showPanel == null)
        {
            Debug.LogWarning("[CommunicationTowerController] CommunicationPanelController is not assigned. Cannot open CommunicationPanel.");
            return;
        }

        showPanel(tower);
    }

    internal UIDocument ResolveTooltipUIDocument()
    {
        if (GlobalHUDController.Instance != null &&
            GlobalHUDController.Instance.TryGetComponent(out UIDocument globalDocument))
        {
            return globalDocument;
        }

#if UNITY_2023_1_OR_NEWER
        UIDocument[] documents = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        UIDocument[] documents = UnityEngine.Object.FindObjectsOfType<UIDocument>(true);
#endif

        UIDocument fallback = null;

        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];

            if (document == null || document.rootVisualElement == null)
                continue;

            fallback ??= document;

            VisualElement root = document.rootVisualElement;

            if (root.Q<Label>("money_label") != null ||
                root.Q<Label>("pop_label") != null ||
                root.Q<Label>("zone_label") != null ||
                root.Q<VisualElement>("communication_tooltip_root") != null)
            {
                return document;
            }
        }

        return fallback;
    }

    private void ShowAssociatedZoneHighlight(CommunicationTowerData tower, Context context)
    {
        if (tower == null || context.zoneController == null)
            return;

        List<ZoneRiskOutlineRequest> requests = new();

        for (int i = 0; i < tower.associatedZoneGeoids.Count; i++)
        {
            string geoid = NormalizeGeoid(tower.associatedZoneGeoids[i]);

            if (!string.IsNullOrEmpty(geoid))
                requests.Add(new ZoneRiskOutlineRequest(geoid, context.associatedZoneHoverColor));
        }

        bool outlineRequestAccepted = context.zoneController.SetZoneOutlines(ZoneOutlineChannel.CommunicationZones, requests);

        if (!outlineRequestAccepted && !_loggedOutlineRequestFailure)
            Debug.LogWarning("[CommunicationTowerController] Communication outlines could not be shown because ZoneController's renderer or zone map is not ready. Tower gameplay and tooltips remain available.", context.diagnosticContext);

        _loggedOutlineRequestFailure = !outlineRequestAccepted;
    }

    private void ShowTowerTooltip(
        CommunicationTowerData tower, Vector2 screenPosition, TooltipBindings tooltip,
        ref UIDocument tooltipUIDocument, bool debugLogs)
    {
        if (tower == null)
        {
            HideTowerTooltip(tooltip);
            return;
        }

        EnsureTooltipConfigured(tooltip, ref tooltipUIDocument, debugLogs);

        if (!tooltip.IsAvailable)
            return;

        tooltip.show(tower, screenPosition);
    }

    private void HideTowerTooltip(TooltipBindings tooltip)
    {
        if (!tooltip.IsAvailable || !tooltip.isVisible())
            return;

        tooltip.hide();
    }

    private string NormalizeGeoid(string geoid)
    {
        return string.IsNullOrWhiteSpace(geoid) ? null : geoid.Trim();
    }
}
