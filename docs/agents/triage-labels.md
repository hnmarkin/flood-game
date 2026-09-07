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

## Bootstrap and verification

Run `gh auth login` (or securely supply `GH_TOKEN` in automation) before managing labels. A fresh GitHub repository needs these six labels: the five canonical roles in the first table and `waiting-on-other-system`. GitHub provides `wontfix` by default; create any missing labels with the documented descriptions:

```powershell
gh label create "needs-triage" --description "Maintainer needs to evaluate this issue" --color "FBCA04"
gh label create "needs-info" --description "Waiting on reporter for more information" --color "D876E3"
gh label create "ready-for-agent" --description "Fully specified, ready for an AFK agent" --color "0E8A16"
gh label create "ready-for-human" --description "Requires human implementation" --color "1D76DB"
gh label create "waiting-on-other-system" --description "Progress is blocked until another system is implemented or revised." --color "5319E7"
```

Skip a command when that label already exists. Verify the complete vocabulary before issue work:

```powershell
gh label list --limit 100 --json name --jq '.[].name'
```

The result must include `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`, and `waiting-on-other-system`.
