using UnityEngine;

[DisallowMultipleComponent]
public sealed class ZoneOutlineVisibilityController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ZoneController zoneController;
    [SerializeField] private ZoneHoverInputController hoverInput;
    [SerializeField] private BarrierController barrierController;

    [Header("Barrier Visibility")]
    [SerializeField] private bool hideWhileBarrierModeIsActive = true;
    [SerializeField] private bool hidePersistentWhileBarrierModeIsActive = true;

    private ZoneController _policyController;

    private void Awake()
    {
        RefreshOutlineSettings();
    }

    private void OnEnable()
    {
        RefreshOutlineSettings();
    }

    private void Start()
    {
        RefreshOutlineSettings();
    }

    private void Update()
    {
        RefreshOutlineSettings();

        if (barrierController == null)
        {
            ProcessHover(true);
            SetPersistentOutlineVisibility(false);
            return;
        }

        if (hideWhileBarrierModeIsActive && barrierController.IsZoneBoundaryModeActive)
        {
            ProcessHover(true);

            if (hidePersistentWhileBarrierModeIsActive)
                SetPersistentOutlineVisibility(false);

            return;
        }

        if (hidePersistentWhileBarrierModeIsActive &&
            zoneController != null && !zoneController.PersistentOutlinesVisible)
            SetPersistentOutlineVisibility(true);

        ProcessHover(false);
    }

    // Compatibility requests may ask the active policy owner to refresh its
    // decision before a show, without becoming another policy writer.
    internal void RefreshOutlineSettings()
    {
        if (!isActiveAndEnabled)
            return;

        if (!ReferenceEquals(_policyController, null) && _policyController != zoneController)
            ReleasePolicy();

        if (zoneController == null)
            return;

        _policyController = zoneController;
        bool barrierModeActive = barrierController != null && barrierController.IsZoneBoundaryModeActive;
        zoneController.SetOutlineVisibilityPolicy(
            hoverInput == null || !hoverInput.IsHoverInputAvailable || barrierController == null ||
                (hideWhileBarrierModeIsActive && barrierModeActive),
            barrierController == null || (hidePersistentWhileBarrierModeIsActive && barrierModeActive));
    }

    private void ProcessHover(bool suppressed)
    {
        if (hoverInput != null)
            hoverInput.ProcessHover(this, zoneController, suppressed);
    }

    private void SetPersistentOutlineVisibility(bool visible)
    {
        if (zoneController != null)
            zoneController.SetPersistentOutlineVisibility(visible);
    }

    private void ReleasePolicy()
    {
        if (ReferenceEquals(_policyController, null))
            return;

        // Retain the last target for cleanup even if its serialized reference
        // was cleared. Clearing this token makes disable/destroy cleanup idempotent.
        ZoneController controller = _policyController;
        _policyController = null;

        if (hoverInput != null)
            hoverInput.InvalidateHoverCache();

        if (controller != null)
            controller.ResetOutlineVisibilityPolicy();
    }

    private void OnDisable()
    {
        ReleasePolicy();
    }

    private void OnDestroy()
    {
        ReleasePolicy();
    }
}
