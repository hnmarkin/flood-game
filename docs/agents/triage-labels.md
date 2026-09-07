# Triage Labels

The skills speak in terms of five canonical triage roles. This file maps those roles to the actual label strings used in this repo's issue tracker.

| Label in mattpocock/skills | Label in our tracker | Meaning |
| --- | --- | --- |
| `needs-triage` | `needs-triage` | Maintainer needs to evaluate this issue |
| `needs-info` | `needs-info` | Waiting on reporter for more information |
| `ready-for-agent` | `ready-for-agent` | Fully specified, ready for an AFK agent |
| `ready-for-human` | `ready-for-human` | Requires human implementation |
| `wontfix` | `wontfix` | Will not be actioned |

When a skill mentions a role (for example, applying the AFK-ready triage label), use the corresponding label string from this table.

## Auxiliary dependency labels

| Label | Meaning |
| --- | --- |
| `waiting-on-other-system` | Progress is blocked until another system is implemented or revised. |

This is not a canonical triage state and does not replace the required state label. When a specific GitHub issue is the blocker, prefer GitHub's native issue dependency relationship.
