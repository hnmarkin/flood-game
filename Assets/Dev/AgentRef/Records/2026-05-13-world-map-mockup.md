# 2026-05-13 World Map Mockup

Created `Assets/Dev/AgentRef/Mockups/WM-world-map.html` as a self-contained campaign select mockup using `WorldMap.png` as the assigned background without inspecting the image.

The mockup includes selectable circular map points for Mobile, AL and multiple Florida locations. Mobile is selected by default and reveals the Hurricane Sally campaign with Difficulty 4, Complexity 4, and a short scenario description. If approved, likely production follow-up would involve the campaign select UI, scenario data binding, and map-point selection controller logic.

Updated the mockup to zoom the background out slightly, replace the campaign points with two Mobile entries plus Pensacola and Panama City Beach, and remove the Scenario Notes side panel.

Refined the layout by removing the tinted header-card containers, collapsing Mobile to a single map point, listing both Mobile hurricane campaigns in the side selection column, and adding a `BEGIN` button below the selected campaign description.

Adjusted the world map behavior so the Mobile campaign list panel only appears when `Mobile, AL` is the active selected location.
