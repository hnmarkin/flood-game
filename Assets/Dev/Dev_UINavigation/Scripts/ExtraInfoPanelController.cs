using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FloodGame.UI.Navigation
{
    /// <summary>Uses the drainage content already authored in Extra_Info_Panel.</summary>
    public sealed class ExtraInfoPanelController : IDisposable
    {
        private readonly UIManager _uiManager;
        private readonly Button _headerCloseButton;
        private readonly Button _footerCloseButton;

        public bool IsBound { get; private set; }
        public VisualElement DefaultFocusElement => _headerCloseButton;

        public ExtraInfoPanelController(UIManager uiManager, VisualElement root)
        {
            _uiManager = uiManager;
            // Both buttons are unnamed; each has a unique named container.
            _headerCloseButton = root.Q<VisualElement>("info_header")?.Q<Button>();
            _footerCloseButton = root.Q<VisualElement>("info_footer")?.Q<Button>();
            IsBound = _headerCloseButton != null && _footerCloseButton != null;

            if (!IsBound)
            {
                Debug.LogError("[ExtraInfoPanelController] Expected the existing close buttons in info_header and info_footer.", uiManager);
                return;
            }

            _headerCloseButton.clicked += OnCloseClicked;
            _footerCloseButton.clicked += OnCloseClicked;
        }

        public bool SetInformation(PreparationInfoId information)
        {
            // No generated labels or replacement content: this template already
            // contains the complete Drainage Maintenance Blitz information.
            return IsBound && information == PreparationInfoId.DrainageMaintenanceBlitz;
        }

        public void Dispose()
        {
            if (!IsBound)
                return;

            _headerCloseButton.clicked -= OnCloseClicked;
            _footerCloseButton.clicked -= OnCloseClicked;
            IsBound = false;
        }

        private void OnCloseClicked()
        {
            _uiManager.ClosePanel(UIPanelId.ExtraInfo);
        }
    }
}
