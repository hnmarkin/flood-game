using UnityEngine;

/// <summary>
/// Dev-only controller seam for the future flood-projection feature.
///
/// Projection will own subscriptions to Game State, completed-defense, modifier,
/// and water-step events. It will clone the current water state, ask the water
/// controller to project that clone using the active profile, then hand an
/// immutable result to a separate overlay renderer. It must never mutate or
/// render the live water state.
/// </summary>
public sealed class Dev_ProjectionController : MonoBehaviour
{
    /// <summary>
    /// Deliberate no-op placeholder for future hazard classification.
    ///
    /// The completed version will accept an immutable water projection and hazard
    /// configuration, classify hazardous cells, and pass that separate result to
    /// the projection overlay renderer. Do not add threshold, icon, or renderer
    /// behavior here until the hazard design is defined.
    /// </summary>
    public void CalculateHazards()
    {
        Debug.LogWarning(
            "[Dev_ProjectionController] CalculateHazards is a deliberate placeholder. " +
            "No hazard classification or overlay rendering has been implemented.");
    }
}
