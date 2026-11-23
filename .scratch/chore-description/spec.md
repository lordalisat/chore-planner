# A Description on Chores

Grilled 2026-11-23; small enough to implement in one go, without tickets.

## Decisions

1. **Term**: Description, "optional free text on a Chore saying what doing it involves, such as which cleaner to use"; avoid Notes, details.
2. **Both kinds, optional**: Recurring and One-off Chores, empty by default.
3. **Plain text with line breaks**, no Markdown or other formatting, at most 2,000 characters; links show as plain text.
4. **Where it shows**: a multi-line field under Name in the Chore form (New Chore, Edit, and the form beside All Chores), and in an unfolded Overview row, above its actions. Not in the All Chores list or table; no marker on a folded Overview row.
5. **Save and add another clears it**, like the Name.
6. **Seed**: "Clean bathroom sink" gets "Descaler under the sink; rinse the plug hole too".
