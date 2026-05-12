## Drainage Maintenance Blitz

> [!metadata]- Machine Data  
> type:: Program (Structural/O&M)  
> residential_opinion:: +  
> corporate_opinion:: +  
> political_opinion:: +  
> money:: Low–Medium  
> action_points:: Medium  
> turns:: 1  
> prereqs:: None  
> stacks:: Yes (up to 3)  
> drainage_efficiency:: 2  
> description:: Intensive inspection, inlet clearing, channel/debris removal, and urgent repairs to restore baseline capacity before storm season.

**Type:** Program (Structural/O&M)  
**Residential:** + | **Corporate:** + | **Political:** +  
**Money:** Low–Medium | **Action Points:** Medium | **Turns:** Instant  
**Prereqs:** None | **Stacks:** Yes (up to 3)  
**Effects:** Drainage Efficiency ↑↑  
**Comms Failure:** Card Failure
**Description:** Intensive inspection, inlet clearing, channel/debris removal, and urgent repairs to restore baseline capacity before storm season.

---

## Pump Stations + Backup Power

> [!metadata]- Machine Data  
> type:: Project (Structural/Continuity)  
> residential_opinion:: +  
> corporate_opinion:: +  
> political_opinion:: +  
> money:: High  
> action_points:: High  
> turns:: 2  
> prereqs:: None  
> stacks:: Yes (up to 2)  
> drainage_efficiency:: 3  
> description:: Construct or upgrade pump stations with redundancy and backup power systems to maintain drainage during extreme rainfall and grid outages.

**Type:** Project (Structural/Continuity)  
**Residential:** + | **Corporate:** + | **Political:** +  
**Money:** High | **Action Points:** High | **Turns:** 2  
**Prereqs:** None | **Stacks:** Yes (up to 2)  
**Effects:** Enables *Pump Placement Menu*  
**Comms Failure:** Pump failure chance in Crisis Phase
**Description:** Construct or upgrade pump stations with redundancy and backup power systems to maintain drainage during extreme rainfall and grid outages.

---

## Detention / Underground Storage

> [!metadata]- Machine Data  
> type:: Project (Structural/Storage)  
> residential_opinion:: Mixed  
> corporate_opinion:: +  
> political_opinion:: +  
> money:: High  
> action_points:: High  
> turns:: 4  
> prereqs:: None  
> stacks:: Yes (up to 2)  
> drainage_efficiency:: 3  
> external_water_load:: -2  
> event_pacing:: -2  
> description:: Construct detention basins, underground tanks, or storage tunnels to absorb and delay peak runoff during prolonged rainfall events.

**Type:** Project (Structural/Storage)  
**Residential:** Mixed | **Corporate:** + | **Political:** +  
**Money:** High | **Action Points:** High | **Turns:** 4  
**Prereqs:** None | **Stacks:** Yes (up to 2)  
**Effects:** Drainage Efficiency ↑↑↑ | External Water Load ↓↓ | Event Pacing ↓↓
**Comms Failure:** Effects x 1/2
**Description:** Construct detention basins, underground tanks, or storage tunnels to absorb and delay peak runoff during prolonged rainfall events.

---

## Levee / Floodwall Construction

> [!metadata]- Machine Data  
> type:: Project (Structural/Barrier)  
> residential_opinion:: + / Mixed  
> corporate_opinion:: +  
> political_opinion:: Mixed  
> money:: High  
> action_points:: High  
> turns:: 4  
> prereqs:: None  
> stacks:: Yes (up to 2)  
> base_infrastructure_resilience:: 3  
> external_water_load:: -3  
> description:: Build structural perimeter protections to reduce surge and overbank flooding risk, acknowledging residual overtopping or failure risk.

**Type:** Project (Structural/Barrier)  
**Residential:** + / Mixed | **Corporate:** + | **Political:** Mixed  
**Money:** High | **Action Points:** High | **Turns:** 4  
**Prereqs:** None | **Stacks:** Yes (up to 2)  
**Effects:** Enabled *Levee Placement Menu* 
**Comms Failure:** +2 Turns (6 total)
**Description:** Build structural perimeter protections to reduce surge and overbank flooding risk, acknowledging residual overtopping or failure risk.

---

## Critical Facility Utility Hardening

> [!metadata]- Machine Data  
> type:: Project (Structural/Critical Infrastructure)  
> residential_opinion:: +  
> corporate_opinion:: +  
> political_opinion:: +  
> money:: High  
> action_points:: Medium–High  
> turns:: 3  
> prereqs:: None  
> stacks:: No  
> base_infrastructure_resilience:: 3  
> event_pacing:: -1  
> description:: Elevate and floodproof electrical and mechanical systems at hospitals, water plants, and other essential facilities to prevent cascading failures.

**Type:** Project (Structural/Critical Infrastructure)  
**Residential:** + | **Corporate:** + | **Political:** +  
**Money:** High | **Action Points:** Medium–High | **Turns:** 3  
**Prereqs:** None | **Stacks:** No  
**Effects:** Base Infrastructure Resilience ↑↑↑ | Event Pacing ↓  
**Comms Failure:** Card Failure
**Description:** Elevate and floodproof electrical and mechanical systems at hospitals, water plants, and other essential facilities to prevent cascading failures.

---

## Green Streets & Urban Trees

> [!metadata]- Machine Data  
> type:: Project (Nature-Based/Distributed GI)  
> residential_opinion:: +  
> corporate_opinion:: +  
> political_opinion:: +  
> money:: Medium  
> action_points:: Medium  
> turns:: 2  
> prereqs:: None  
> stacks:: Yes (up to 3)  
> drainage_efficiency:: 2  
> external_water_load:: -1  
> description:: Install bioswales, rain gardens, permeable corridors, and expanded tree canopy to reduce runoff and strain on stormwater systems.

**Type:** Project (Nature-Based/Distributed GI)  
**Residential:** + | **Corporate:** + | **Political:** +  
**Money:** Medium | **Action Points:** Medium | **Turns:** 2  
**Prereqs:** None | **Stacks:** Yes (up to 3)  
**Effects:** Drainage Efficiency ↑↑ | External Water Load ↓  ???
**Comms Failure:** Effects x 1/2
**Description:** Install bioswales, rain gardens, permeable corridors, and expanded tree canopy to reduce runoff and strain on stormwater systems.

---

## Higher Flood-Resistant Building Code Upgrade

> [!metadata]- Machine Data  
> type:: Policy (Codes/Standards)  
> residential_opinion:: Mixed / −  
> corporate_opinion:: −  
> political_opinion:: Mixed  
> money:: Low–Medium  
> action_points:: Medium  
> turns:: 4  
> prereqs:: None  
> stacks:: No (replaces prior code level)  
> base_infrastructure_resilience:: 3  
> wind_stress:: -1  
> description:: Adopt and enforce modern flood-resistant construction standards, including elevation requirements and strengthened structural provisions.

**Type:** Policy (Codes/Standards)  
**Residential:** Mixed / − | **Corporate:** − | **Political:** Mixed  
**Money:** Low–Medium | **Action Points:** Medium | **Turns:** 4  
**Prereqs:** None | **Stacks:** No (replaces prior code level)  
**Effects:** Base Infrastructure Resilience ↑↑↑ | Wind Stress ↓ 
**Comms Failure:** Card Failure
**Description:** Adopt and enforce modern flood-resistant construction standards, including elevation requirements and strengthened structural provisions.
**NOTE**: May want to make district specific!

---

## Early Warning & Risk Communication

> [!metadata]- Machine Data  
> type:: Operations (Public Communication)  
> residential_opinion:: +  
> corporate_opinion:: +  
> political_opinion:: +  
> money:: Low–Medium  
> action_points:: Low–Medium  
> turns:: 3  
> prereqs:: None  
> stacks:: No  
> event_pacing:: -3  
> description:: Implement integrated alert systems, multilingual communication, and coordinated public messaging to accelerate protective action.

**Type:** Operations (Public Communication)  
**Residential:** + | **Corporate:** + | **Political:** +  
**Money:** Low–Medium | **Action Points:** Low–Medium | **Turns:** 3  
**Prereqs:** None | **Stacks:** Yes (up to 2) 
**Effects:** Communication ↑↑↑
**Comms Failure:** N/A
**Description:** Implement integrated alert systems, multilingual communication, and coordinated public messaging to accelerate protective action.

---

## EOP Annex: Evacuation + Shelter Ops

> [!metadata]- Machine Data  
> type:: Operations (Planning)  
> residential_opinion:: +  
> corporate_opinion:: −  
> political_opinion:: Mixed  
> money:: Low–Medium  
> action_points:: Medium  
> turns:: 2  
> prereqs:: None  
> stacks:: Yes (up to 2)  
> event_pacing:: -2  
> base_infrastructure_resilience:: 1  
> description:: Develop and exercise evacuation routes, shelter agreements, and continuity planning to reduce casualties and cascading disruptions.

**Type:** Operations (Planning)  
**Residential:** + | **Corporate:** − | **Political:** Mixed  
**Money:** Low–Medium | **Action Points:** Medium | **Turns:** 2  
**Prereqs:** None | **Stacks:** Yes (up to 2)  
**Effects:** Communication ↑ | Evacuation Speed ↑↑ | Shelter Capacity ↑  
**Comms Failure:** N/A
**Description:** Develop and exercise evacuation routes, shelter agreements, and continuity planning to reduce casualties and cascading disruptions.

---

## Sandbag & Temporary Barrier Stockpile

> [!metadata]- Machine Data  
> type:: Operations (Temporary Defense)  
> residential_opinion:: +  
> corporate_opinion:: +  
> political_opinion:: +  
> money:: Low–Medium  
> action_points:: Medium  
> turns:: 1  
> prereqs:: None  
> stacks:: Yes (event-limited)  
> external_water_load:: -2 (temporary)  
> description:: Pre-position sandbags and temporary barriers to divert floodwaters, recognizing labor intensity and potential interior drainage challenges.

**Type:** Operations (Temporary Defense)  
**Residential:** + | **Corporate:** + | **Political:** +  
**Money:** Low–Medium | **Action Points:** Medium | **Turns:** 1  
**Prereqs:** None | **Stacks:** Yes (event-limited)  
**Effects:** Enables *Sandbags* in Crisis Phase
**Comms Failure:** Sandbags failure chance in Crisis Phase
**Description:** Pre-position sandbags and temporary barriers to divert floodwaters.

---

## Municipal Tax Increase

> [!metadata]- Machine Data  
> type:: Policy (Revenue/Finance)  
> residential_opinion:: --  
> corporate_opinion:: --  
> political_opinion:: -  
> money:: +++  
> action_points:: Low  
> turns:: 1  
> prereqs:: None  
> stacks:: Limited (once every 2 Preparation Turns; escalating penalties)  
> outrage:: +1 (base; stochastic scaling)  
> revenue:: 3  
> description:: Raise municipal taxes to increase available funding for resilience and infrastructure investments; repeated increases raise Outrage risk and can trigger political pushback.

**Type:** Policy (Revenue/Finance)  
**Residential:** −− | **Corporate:** −− | **Political:** −  
**Money:** +++ | **Action Points:** Low | **Turns:** 1  
**Prereqs:** None | **Stacks:** Limited (once every 2 Preparation Turns; escalating penalties)  
**Effects:** Money: ↑↑↑ | Outrage ↑ (stochastic; scales with repetition)  
**Comms Failure:** Outrage +1 (2 total)
**Description:** Raise municipal taxes to increase available funding for resilience and infrastructure investments; repeated increases raise Outrage risk and can trigger political pushback.

---

## Flood Defense Department Expansion

> [!metadata]- Machine Data  
> type:: Program (Capacity/Operations)  
> residential_opinion:: - (placement-weighted)  
> corporate_opinion:: - (placement-weighted)  
> political_opinion:: Mixed  
> money:: Medium  
> action_points:: Medium  
> turns:: 2  
> prereqs:: None  
> stacks:: Yes (up to 3; diminishing returns)  
> outrage:: +1 (placement-weighted; stochastic scaling)  
> action_point_generation:: 1  
> project_efficiency:: 1  
> description:: Expand the flood defense department by hiring staff, improving logistics, and increasing operational capacity; placement determines which constituency bears the opinion cost and Outrage risk.

**Type:** Program (Capacity/Operations)  
**Residential:** − (placement-weighted) | **Corporate:** − (placement-weighted) | **Political:** Mixed  
**Money:** Medium | **Action Points:** Medium | **Turns:** 2  
**Prereqs:** None | **Stacks:** Yes (up to 3; diminishing returns)  
**Effects:** Action Point Generation ↑ | Project Efficiency ↑ | Outrage ↑ (placement-weighted; stochastic)  
**Comms Failure:** Outrage +1 (2 total)
**Description:** Expand the flood defense department by hiring staff, improving logistics, and increasing operational capacity; placement determines which constituency bears the opinion cost and Outrage risk.