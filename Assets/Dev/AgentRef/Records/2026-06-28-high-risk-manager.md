# 2026-06-28 High Risk Manager

Added `HighRiskManager` to bind `action_Button3` to cached baseline-risk inspection, outline the top-risk zones, and spawn one warning marker per zone without per-frame recalculation. Extended the existing risk and outline controllers with small public helpers so the manager can reuse cached risk data, existing GEOID zone lookup, and persistent outline behavior instead of building a parallel zone system.
