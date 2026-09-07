# Water event cleanup

Removed WaterController lifecycle/profile mirror events for Game State-owned transitions. Collapsed initialization notification into OnWaterSimulationReset, updated ProjectionController to invalidate on reset and through NotifyTimeProfileChanged, and preserved the repeated step and reset notifications. Commented obsolete PlayMode event assertions with Game State adaptation TODOs. Added a durable Game State integration handoff and hookup checklist to the Water System README.
