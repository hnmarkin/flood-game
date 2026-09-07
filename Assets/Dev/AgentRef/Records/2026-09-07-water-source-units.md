# Water source units

Split `WaterSourceSpec.depth` into explicit `initialDepth` and `continuousDepthPerSecond` fields. Updated physics, scenario defaults, the Dev scenario asset, and water tests so initial sources consume absolute depth while continuous sources consume rate-per-simulated-second. Unity EditMode execution was not available because the project was already open in another Unity instance.
