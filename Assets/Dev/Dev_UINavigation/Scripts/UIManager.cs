using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FloodGame.UI.Navigation
{
    /// <summary>Navigation for the three Stage 1 templates in one UIDocument.</summary>
    [RequireComponent(typeof(UIDocument))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Flood Game/UI/Navigation/UI Manager (Stage 1)")]
    [DefaultExecutionOrder(-50)]
    public sealed class UIManager : MonoBehaviour
    {
        private sealed class Panel
        {
            public VisualElement Host;
            public VisualElement Root;
            public bool Visible;
        }

        [Header("Independent UXML Templates")]
        [SerializeField] private VisualTreeAsset actionsPanelTemplate;
        [SerializeField] private VisualTreeAsset preparationCardsTemplate;
        [SerializeField] private VisualTreeAsset extraInfoTemplate;

        [Header("Existing Actions Controller")]
        [SerializeField] private ActionsPanelController actionsPanelController;

        public event Action<UIPanelId, bool> PanelVisibilityChanged;

        private readonly Dictionary<UIPanelId, Panel> _panels = new();
        private readonly List<UIPanelId> _popupStack = new();
        private readonly Dictionary<UIPanelId, VisualElement> _returnFocus = new();
        private UIDocument _document;
        private VisualElement _observedDocumentRoot;
        private VisualElement _mainLayer;
        private VisualElement _popupLayer;
        private PreparationCardsPanelController _preparationController;
        private ExtraInfoPanelController _informationController;
        private bool _configurationErrorLogged;

        public bool IsInitialized => isActiveAndEnabled && GetDocumentReady() && _preparationController != null && _informationController != null;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            _configurationErrorLogged = false;
            TryInitialize();
        }

        private void Update()
        {
            // UIDocument rebuilds its tree when it is re-enabled or live-reloaded.
            // Move retained instances into the new layers, keeping existing HUD
            // controllers' cached Button references valid.
            VisualElement root = _document != null ? _document.rootVisualElement : null;
            if (!ReferenceEquals(root, _observedDocumentRoot))
            {
                bool initialized = TryInitialize();
                RefreshVisibility();
                if (initialized && _popupStack.Count > 0)
                    GetDefaultFocusElement(_popupStack[_popupStack.Count - 1])?.Focus();
            }
        }

        private void OnDisable()
        {
            _popupStack.Clear();
            _returnFocus.Clear();
            RefreshVisibility();
            DisposePopupControllers();
            // Retain the Actions tree for unrelated HUD bindings, and retain
            // popup state until this manager is enabled again or destroyed.
        }

        private void OnDestroy()
        {
            DisposePopupControllers();
            if (_panels.TryGetValue(UIPanelId.Actions, out Panel actions))
                actionsPanelController?.ReleasePanelRoot(actions.Root);

            foreach (KeyValuePair<UIPanelId, Panel> entry in _panels)
            {
                entry.Value.Host.RemoveFromHierarchy();
                if (entry.Value.Visible)
                {
                    entry.Value.Visible = false;
                    PanelVisibilityChanged?.Invoke(entry.Key, false);
                }
            }

            _panels.Clear();
            _popupStack.Clear();
            _returnFocus.Clear();
            PanelVisibilityChanged = null;
        }

        public bool TryGetPanelRoot(UIPanelId panelId, out VisualElement root)
        {
            root = null;
            if (!_panels.TryGetValue(panelId, out Panel panel))
                return false;

            root = panel.Root;
            return true;
        }

        public bool GetPanelVisibility(UIPanelId panelId)
        {
            return GetDocumentReady() && _panels.TryGetValue(panelId, out Panel panel) && panel.Visible;
        }

        public bool GetPanelOpen(UIPanelId panelId)
        {
            return panelId == UIPanelId.Actions ? GetPanelVisibility(panelId) : _popupStack.Contains(panelId);
        }

        public bool OpenPanel(UIPanelId panelId, PreparationInfoId? information = null)
        {
            if (!IsInitialized || !_panels.ContainsKey(panelId))
                return false;

            if (panelId == UIPanelId.Actions)
                return true;

            if (panelId == UIPanelId.ExtraInfo)
            {
                if (!information.HasValue || !_informationController.SetInformation(information.Value))
                {
                    Debug.LogWarning("[UIManager] ExtraInfo requires the typed DrainageMaintenanceBlitz information identifier.", this);
                    return false;
                }

                if (!GetPanelOpen(UIPanelId.PreparationCards))
                    OpenPanel(UIPanelId.PreparationCards);
            }

            if (_popupStack.Contains(panelId))
                return true;

            _returnFocus[panelId] = _document.rootVisualElement.focusController?.focusedElement as VisualElement;
            _popupStack.Add(panelId);
            RefreshVisibility();
            GetDefaultFocusElement(panelId)?.Focus();
            return true;
        }

        public bool ClosePanel(UIPanelId panelId)
        {
            int index = _popupStack.IndexOf(panelId);
            if (index < 0)
                return false;

            _returnFocus.TryGetValue(panelId, out VisualElement focus);
            // Closing a lower popup also closes its descendants; ExtraInfo can
            // never remain open without Preparation Cards beneath it.
            for (int i = _popupStack.Count - 1; i >= index; i--)
            {
                _returnFocus.Remove(_popupStack[i]);
                _popupStack.RemoveAt(i);
            }

            RefreshVisibility();
            if (focus != null && focus.panel != null && focus.enabledInHierarchy)
                focus.Focus();
            return true;
        }

        public bool TogglePanel(UIPanelId panelId)
        {
            if (!IsInitialized || panelId == UIPanelId.Actions)
                return false;

            // A modal information panel owns navigation until it is dismissed.
            if (panelId == UIPanelId.PreparationCards && GetPanelOpen(UIPanelId.ExtraInfo))
                return false;

            return GetPanelOpen(panelId) ? ClosePanel(panelId) : OpenPanel(panelId);
        }

        private bool TryInitialize()
        {
            if (_document == null)
                _document = GetComponent<UIDocument>();

            VisualElement documentRoot = _document != null ? _document.rootVisualElement : null;
            _observedDocumentRoot = documentRoot;
            if (documentRoot == null)
                return false;

            VisualElement mainLayer = documentRoot.Q<VisualElement>("MainLayer");
            VisualElement popupLayer = documentRoot.Q<VisualElement>("PopupLayer");
            if (mainLayer == null || popupLayer == null || actionsPanelTemplate == null || preparationCardsTemplate == null || extraInfoTemplate == null || actionsPanelController == null)
            {
                LogConfigurationError("Assign Dev_SharedUIRoot.uxml, all three templates, and the existing ActionsPanelController. The root must contain MainLayer and PopupLayer.");
                return false;
            }

            _mainLayer = mainLayer;
            _popupLayer = popupLayer;
            _mainLayer.pickingMode = PickingMode.Ignore;
            _popupLayer.pickingMode = PickingMode.Ignore;

            if (_panels.Count == 0)
            {
                // The shared root is deliberately empty: templates are cloned
                // exactly once here, never also assigned to another UIDocument.
                if (documentRoot.Q<VisualElement>("actions_root") != null)
                {
                    LogConfigurationError("The shared root already contains ActionsPanel. Remove the static Actions template to prevent a duplicate instance.");
                    return false;
                }

                if (!RegisterPanel(UIPanelId.Actions, actionsPanelTemplate, "actions_root") ||
                    !RegisterPanel(UIPanelId.PreparationCards, preparationCardsTemplate, "prep_cards_root") ||
                    !RegisterPanel(UIPanelId.ExtraInfo, extraInfoTemplate, "info_panel_root"))
                {
                    ReleaseFailedPanels();
                    return false;
                }
            }

            if (!ReferenceEquals(_panels[UIPanelId.Actions].Host.parent, _mainLayer))
                _mainLayer.Add(_panels[UIPanelId.Actions].Host);
            if (!ReferenceEquals(_panels[UIPanelId.PreparationCards].Host.parent, _popupLayer))
                _popupLayer.Add(_panels[UIPanelId.PreparationCards].Host);
            if (!ReferenceEquals(_panels[UIPanelId.ExtraInfo].Host.parent, _popupLayer))
                _popupLayer.Add(_panels[UIPanelId.ExtraInfo].Host);

            if (_preparationController == null)
                _preparationController = new PreparationCardsPanelController(this, _panels[UIPanelId.PreparationCards].Root);
            if (_informationController == null)
                _informationController = new ExtraInfoPanelController(this, _panels[UIPanelId.ExtraInfo].Root);

            if (!_preparationController.IsBound || !_informationController.IsBound)
            {
                ReleaseFailedPanels();
                return false;
            }

            actionsPanelController.SetPanelRoot(_panels[UIPanelId.Actions].Root, this);
            RefreshVisibility();
            return true;
        }

        private bool RegisterPanel(UIPanelId id, VisualTreeAsset template, string rootName)
        {
            VisualElement host = new VisualElement { name = id + "Host", pickingMode = PickingMode.Ignore };
            host.AddToClassList("ui-navigation-host");
            host.AddToClassList("ui-navigation-" + id.ToString().ToLowerInvariant());
            host.style.display = DisplayStyle.None;
            template.CloneTree(host);
            VisualElement root = host.Q<VisualElement>(rootName);
            if (root == null)
            {
                LogConfigurationError($"Template '{template.name}' does not contain '{rootName}'.");
                return false;
            }

            _panels.Add(id, new Panel { Host = host, Root = root });
            return true;
        }

        private void RefreshVisibility()
        {
            bool ready = GetDocumentReady();
            UIPanelId top = _popupStack.Count > 0 ? _popupStack[_popupStack.Count - 1] : UIPanelId.Actions;
            if (_mainLayer != null)
                _mainLayer.SetEnabled(top != UIPanelId.ExtraInfo);

            List<KeyValuePair<UIPanelId, bool>> changes = new();
            foreach (KeyValuePair<UIPanelId, Panel> entry in _panels)
            {
                Panel panel = entry.Value;
                bool visible = ready && (entry.Key == UIPanelId.Actions || entry.Key == top);
                panel.Host.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                panel.Host.SetEnabled(visible);
                // Preparation leaves the Actions sidebar reachable. ExtraInfo
                // covers the entire shared root to intercept background clicks.
                panel.Host.pickingMode = entry.Key == UIPanelId.ExtraInfo ? PickingMode.Position : PickingMode.Ignore;
                if (entry.Key == top && top != UIPanelId.Actions)
                    panel.Host.BringToFront();

                if (panel.Visible != visible)
                {
                    panel.Visible = visible;
                    changes.Add(new KeyValuePair<UIPanelId, bool>(entry.Key, visible));
                }
            }

            foreach (KeyValuePair<UIPanelId, bool> change in changes)
            {
                // A subscriber can navigate during notification. Do not publish
                // a stale state after a nested visibility update.
                if (_panels.TryGetValue(change.Key, out Panel panel) && panel.Visible == change.Value)
                    PanelVisibilityChanged?.Invoke(change.Key, change.Value);
            }
        }

        private bool GetDocumentReady()
        {
            return _document != null && _document.isActiveAndEnabled && _document.rootVisualElement != null &&
                   _document.rootVisualElement.panel != null && _mainLayer != null && _popupLayer != null &&
                   ReferenceEquals(_mainLayer.parent, _popupLayer.parent) &&
                   _mainLayer.panel == _document.rootVisualElement.panel;
        }

        private VisualElement GetDefaultFocusElement(UIPanelId panelId)
        {
            return panelId == UIPanelId.ExtraInfo ? _informationController.DefaultFocusElement : _preparationController.DefaultFocusElement;
        }

        private void DisposePopupControllers()
        {
            _preparationController?.Dispose();
            _informationController?.Dispose();
            _preparationController = null;
            _informationController = null;
        }

        private void ReleaseFailedPanels()
        {
            DisposePopupControllers();
            foreach (Panel panel in _panels.Values)
                panel.Host.RemoveFromHierarchy();
            _panels.Clear();
        }

        private void LogConfigurationError(string message)
        {
            if (_configurationErrorLogged)
                return;
            _configurationErrorLogged = true;
            Debug.LogError("[UIManager] " + message, this);
        }
    }
}
