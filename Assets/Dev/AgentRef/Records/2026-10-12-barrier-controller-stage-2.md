# Barrier Controller Stage 2

Added `Dev_BarrierSystem` with `BarrierController`, input, visuals, physics adapter,
state, docs, and focused state tests. `FloodDefenseBoxStamp` is now a forwarding
adapter that preserves legacy UI callbacks, events, zone helpers, and
`IBarrierProvider` compatibility while removing duplicate placement state and
simulation auto-start from the active placement flow. Water barriers are
prevalidated before registration; partial WaterController failures are reported
without recording committed zones because no rollback API exists.

Follow-up: exterior map edges are now counted as protected boundary edges instead
of water-barrier write requests, and duplicate stamp Inspector settings are
hidden so active barrier configuration lives on `BarrierController`.
