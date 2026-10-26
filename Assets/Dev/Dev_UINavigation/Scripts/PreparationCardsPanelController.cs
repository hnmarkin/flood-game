using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FloodGame.UI.Navigation
{
    /// <summary>Owns callbacks in one cloned Preparation_Cards subtree.</summary>
    public sealed class PreparationCardsPanelController : IDisposable
    {
        private readonly UIManager _uiManager;
        private readonly Button _informationButton;
        private readonly Button _closeButton;

        public bool IsBound { get; private set; }
        public VisualElement DefaultFocusElement => _closeButton;

        public PreparationCardsPanelController(UIManager uiManager, VisualElement root)
        {
            _uiManager = uiManager;
            Button drainageCard = root.Q<Button>("drainage_action_card");
            // info_btn is repeated on other cards. Keep this query inside drainage.
            _informationButton = drainageCard?.Q<Button>("info_btn");
            // The existing X button is unnamed and is the only button in pc_header.
            _closeButton = root.Q<VisualElement>("pc_header")?.Q<Button>();

            IsBound = _informationButton != null && _closeButton != null;
            if (!IsBound)
            {
                Debug.LogError("[PreparationCardsPanelController] Expected drainage_action_card/info_btn and the existing pc_header close button.", uiManager);
                return;
            }

            _informationButton.clicked += OnInformationClicked;
            _informationButton.RegisterCallback<ClickEvent>(StopInformationClick, TrickleDown.TrickleDown);
            _informationButton.RegisterCallback<PointerDownEvent>(StopInformationPointerDown);
            _informationButton.RegisterCallback<PointerUpEvent>(StopInformationPointerUp);
            _informationButton.RegisterCallback<NavigationSubmitEvent>(StopInformationSubmit);
            _closeButton.clicked += OnCloseClicked;
        }

        public void Dispose()
        {
            if (!IsBound)
                return;

            _informationButton.clicked -= OnInformationClicked;
            _informationButton.UnregisterCallback<ClickEvent>(StopInformationClick, TrickleDown.TrickleDown);
            _informationButton.UnregisterCallback<PointerDownEvent>(StopInformationPointerDown);
            _informationButton.UnregisterCallback<PointerUpEvent>(StopInformationPointerUp);
            _informationButton.UnregisterCallback<NavigationSubmitEvent>(StopInformationSubmit);
            _closeButton.clicked -= OnCloseClicked;
            IsBound = false;
        }

        private void OnInformationClicked()
        {
            _uiManager.OpenPanel(UIPanelId.ExtraInfo, PreparationInfoId.DrainageMaintenanceBlitz);
        }

        private void OnCloseClicked()
        {
            _uiManager.ClosePanel(UIPanelId.PreparationCards);
        }

        // Keep Button's own Clickable/default behavior; stop events before they
        // reach the parent card. No main-card action is registered in this stage.
        private static void StopInformationClick(ClickEvent evt) => evt.StopPropagation();
        private static void StopInformationPointerDown(PointerDownEvent evt) => evt.StopPropagation();
        private static void StopInformationPointerUp(PointerUpEvent evt) => evt.StopPropagation();
        private static void StopInformationSubmit(NavigationSubmitEvent evt) => evt.StopPropagation();
    }
}
