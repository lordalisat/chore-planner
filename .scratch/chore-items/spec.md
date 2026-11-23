# Items on Chores

Grilled 2026-11-23 (started as "sub-chores"); one session, one PR, no tickets.

## Decisions

1. **Term**: an **Item** is one of the things doing a Chore involves: a Name and optionally a Description, no schedule of its own, listed in the household's order but done in any order. Avoid: Sub-chore, step, subtask, checklist item.
2. **Rules**: both kinds of Chore, at most 20 Items. Name required, at most 200 characters, unique within its Chore (ignoring case and surrounding blanks). Description as a Chore's: plain text with line breaks, at most 2,000 characters. No Effort, Importance or Area.
3. **Ticked**: an Item marked done while working on its Chore. It records no Completion and moves no date. Stored on the server, so the household shares it.
4. **When ticks clear**: Done or Skip on a Recurring Chore unticks all its Items; undoing it doesn't bring them back. Postpone keeps them. A finished One-off keeps its ticks. Editing keeps the tick of every Item kept, renamed or moved; new Items start unticked.
5. **No automatic Done**: ticking every Item doesn't complete the Chore, and Done works with Items unticked.
6. **Where it shows**:
   - Chore form (New, Edit, beside All Chores): an "Items" list under Description to add, remove and reorder Items, each with a Name and optional Description. Save and add another clears the Items, like the Name and Description (not grilled; follows from Items belonging to one Chore).
   - Overview, unfolded row: below the Description and above the actions, a tick box and Name per Item; tapping an Item with a Description shows it.
   - Overview, folded row: "2 of 4 items" once at least one Item is ticked.
   - Not in the All Chores list or table.
7. **Seed**: "Clean the bathroom", weekly, in Bathroom, with Items sink (ticked), toilet ("Blue cleaner, leave it 10 minutes"), mirror, floor.
