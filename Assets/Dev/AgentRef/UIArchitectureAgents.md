# SurgeCity UI Architecture Reference

## Overview
SurgeCity uses a layered hybrid UI architecture composed of:

1. **Full-Screen UI** — menus and scenario flow  
2. **Persistent Toolkit UI** — management/interface layer  
3. **Contextual Toolkit UI** — gameplay-mode controls  
4. **World-Space Canvas UI** — spatial simulation feedback and previews  
5. **Messaging Layers** — alerts and attention management  

The entire UI is built around a shared forecast simulation backend that powers both global flood overlays and localized infrastructure previews.

The overall design philosophy is:
- visually driven
- spatially readable
- simulation-focused
- forecast-centric
- low on excessive numerical clutter

---

# 1. Full-Screen UI

## Main Menu
Primary entry screen for the game.

Example contents:
- New Game
- Load Game
- Options
- Quit

Visual direction:
- atmospheric flood/disaster presentation
- strong environmental backdrop
- clear, minimal navigation hierarchy

---

## Campaign Selection Screen
Scenario and region selection interface.

Example contents:
- Stylized map of the U.S. Southeast
- Highlightable/selectable regions
- Scenario cards tied to regions
- Campaign descriptions and difficulty indicators

Example campaigns:
- Mobile, Alabama — Hurricane Sally
- New Orleans — Storm Surge Scenario
- Appalachian Flash Flooding

Visual direction:
- map-centric
- geographic storytelling
- regions feel distinct and vulnerable

---

## Scoring Scene
Post-scenario evaluation and summary scene.

Example contents:
- Damage statistics
- Reputation changes
- Infrastructure survival rates
- Civilian impact summaries
- Faction reactions
- Final city status

Visual direction:
- report/debrief style
- layered overlays and animated summaries
- communicates consequences clearly

---

# 2. Persistent In-Game UI

## General Header
Persistent top-bar UI used during gameplay.

Example contents:
- Current time/day
- Money/budget
- Reputation/public trust
- Scenario name
- Pause/settings button

Behavior:
- almost always visible
- may grey out during interruptions or transitions

Visual direction:
- compact and readable
- strategy-game inspired
- minimal obstruction of map view

---

## Right Sidebar
Primary information and alert feed.

Example contents:
- Active flood alerts
- Infrastructure failures
- Faction notifications
- Selected tile information
- Pump/power status details

Behavior:
- updates dynamically
- supports expandable alerts
- contextual based on player selection

Visual direction:
- dense but scannable
- warning-focused
- operational command-center feel

---

## Left Sidebar
Collapsible strategic overlay and management panel.

Example contents:
- Drainage overlay
- Electrical grid overlay
- Levee integrity view
- Flood forecast filter toggle
- Faction overview panel

Behavior:
- controls strategic map visualizations
- supports switching between infrastructure layers

Visual direction:
- utility-focused
- map-analysis oriented
- GIS-inspired hierarchy

---

# 3. Contextual In-Game UI

## Crisis Footer
Quick-response gameplay controls shown during crisis phases.

Example contents:
- Sandbags
- Temporary barriers
- Emergency pumps
- Evacuation tools
- Rapid deployment actions

Behavior:
- optimized for rapid interaction
- appears primarily during active flooding

Visual direction:
- urgent/emergency-management tone
- high readability under pressure

---

## Placement Footer
Context-sensitive infrastructure placement controls.

Example contents:
- Levee placement tool
- Pump placement tool
- Demolition tool
- Water tower placement
- Infrastructure upgrade tools

Behavior:
- changes based on current placement mode
- unified system rather than separate tool interfaces

Visual direction:
- construction/planning focused
- highly tool-oriented

---

## Preparation Actions Screen
Unified strategic planning and policy management screen.

Example contents:
- Drainage Maintenance Blitz
- Pump Station Upgrades
- Floodwall Construction
- Building Code Improvements
- Early Warning Systems

Includes:
- available actions
- active actions
- action effects
- costs and tradeoffs
- faction reactions/opinion shifts

Visual direction:
- policy/planning dashboard
- emphasizes strategic tradeoffs

---

# 4. Messaging / Attention Systems

## Faction Message Pop-up
High-priority center-screen interruption system.

Purpose:
- major player mistakes
- critical infrastructure failures
- faction outrage/support
- major escalation moments

Example contents:
- Mayor warning player about public anger
- Utility director warning about grid collapse
- Emergency manager urging evacuation

May include:
- character portrait/avatar
- dialogue text
- emotional reaction indicators

Behavior:
- intentionally disruptive
- designed to immediately capture attention

Visual direction:
- dramatic but readable
- emergency broadcast feel

---

## Alert Variant
Lower-priority non-blocking messaging system.

Example contents:
- “Hospital district flooding increasing”
- “Drainage capacity exceeded”
- “Citizens responding positively to evacuation order”

Behavior:
- appears in sidebar alert feed
- accumulates over time
- expandable for details

Visual direction:
- operational notification style
- lightweight and persistent

---

# 5. World-Space / Canvas UI Systems

## Flood Forecast System
Global forecast visualization overlay.

Features:
- map-wide flood vulnerability shading
- projected flood depth/intensity
- infrastructure vulnerability indicators
- forecast visualization tilemap

Example visualizations:
- blue-to-red flood severity shading
- flashing danger zones
- projected overtopping regions

Behavior:
- fully togglable
- persistent strategic analysis layer

Visual direction:
- GIS/weather-radar inspired
- readable at multiple zoom levels

---

## Critical Site Alert Markers
World-space warning icons for vulnerable critical infrastructure.

Example contents:
- hospital danger markers
- power plant warnings
- fire station flooding alerts
- water treatment risk icons

Behavior:
- spawned dynamically from forecast thresholds
- tied to forecast and infrastructure state

Visual direction:
- highly visible
- minimal but urgent

---

## Placement Visualization System
Localized infrastructure preview and simulation system.

Used for:
- levees
- pumps
- power infrastructure
- future mitigation systems

Example contents:
- projected flood reduction
- water flow rerouting arrows
- affected tile highlighting
- cost/effectiveness popup
- placement validity indicators

Behavior:
- reuses Flood Forecast logic locally
- previews changes before placement confirmation

Includes:
- ghost placement objects
- valid/invalid placement coloring
- localized simulation previews

Visual direction:
- highly spatial
- simulation-focused
- communicates impact visually first

---

# 6. Shared Forecast Architecture

## Forecast Preview Engine
Shared backend simulation and prediction system.

Responsibilities:
- flood prediction
- vulnerability calculation
- infrastructure risk estimation
- localized forecast simulation

Feeds:
- Flood Forecast System
- Placement Visualization System
- Critical Site Alert Markers

Architectural rule:
- forecast logic must remain independent from UI presentation systems

---

# 7. Architectural Principles

## Separation of Concerns
Keep distinct:
- simulation logic
- forecast evaluation
- tile visualization
- world-space UI spawning
- UI Toolkit presentation

Avoid tightly coupling gameplay logic to visual systems.

---

## Unified Systems
Prefer reusable generalized systems over one-off implementations.

Examples:
- single Placement Footer
- single Placement Visualization System
- shared Forecast Preview Engine

---

## Information Escalation Hierarchy
Communicate urgency in escalating layers:

1. Passive overlays  
2. Sidebar alerts  
3. Center-screen interruptions  

Critical events should escalate visually and spatially.

---

# 8. Technical Direction

## UI Toolkit
Recommended for:
- menus
- sidebars
- headers/footers
- planning screens
- structured management UI

---

## Canvas / World-Space UI
Recommended for:
- placement previews
- forecast indicators
- map warnings
- spatial simulation feedback

---

## Tilemap Layering
Recommended separate tilemaps/layers for:
- terrain
- active flooding
- forecast overlay
- infrastructure networks
- placement previews
- critical warnings

---

# 9. Core UX Philosophy

Players should primarily understand the disaster through:
- map state
- flood forecasts
- world-space indicators
- visualized infrastructure impacts
- escalating alerts

The interface should communicate:
- consequence
- vulnerability
- urgency
- cascading failure
- mitigation effectiveness

primarily through spatial visualization rather than dense statistics.