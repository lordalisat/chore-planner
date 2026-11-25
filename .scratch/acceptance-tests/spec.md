# Acceptance tests

Drafted 2026-11-25 from an analysis of the Chores sessions; not grilled yet. Every decision below is a proposal, with the alternative it beats. Grill it, then cut tickets from Slices.

## Why

The hand test is still where behaviour bugs surface. Across 23 sessions (12–24 Nov), about 21 of ~60 corrections were found by clicking through the app after `./check`, `/code-review` and `/experience-review` had passed, among them:

- the Archived toggle on All Chores didn't filter, and both options showed as selected;
- the Overview said "Nothing needs doing yet" while a One-off Chore was scheduled;
- a Season, once picked, couldn't be cleared.

The Vitest suite can't catch these: it renders in jsdom against a stubbed API, so no real browser, no real layout, no real server. The reviews read a diff. Nothing runs the shipped app against the spec. Acceptance tests do: each spec states what must be true, and a browser test proves it on every `./check`. The household's part moves from finding bugs to agreeing the criteria.

## Decisions

1. **Terms**: an **Acceptance Criterion** is one numbered, observable statement in a spec's `## Acceptance` section, such as "A3. Ticking every Item doesn't complete the Chore". An **acceptance test** is the browser test that proves one. (Not glossary terms: they're about the project, not the household.)

2. **Criteria are written during grilling** and confirmed by the user before implementation starts, like the Decisions. Each one is observable on a screen or through the API, in glossary terms, and states one behaviour. A criterion no test can judge (how something looks or feels) is marked `(review)` and left to `/experience-review`.

3. **Tool: Playwright Test**, TypeScript, in `web/e2e/`. It drives a real Chromium, waits for the page on its own, finds controls by role and name (the names the UI SDK agent bridge already requires), freezes the browser's clock, and writes a trace and screenshot on failure that an agent can read.
   - _Not Playwright for .NET_ in `tests/`: one `dotnet test` would be tidy, but the screens and their names live in TypeScript, and the typed API client is there too.
   - _Not agent-browser scripts_: good for exploring and for `/experience-review`, but they're not a test runner (no assertions, retries or reports).
   - _Not more Vitest_: jsdom is where the escaped bugs lived.

4. **The app as it ships**: the tests run against the built front end served by the API (one process, like the Docker image), not Vite. Playwright's `webServer` builds the front end, then starts `dotnet run` in the `Testing` environment on a throwaway database and a port of its own, chosen per run. It never touches 5080/5173, so it runs beside `./serve` and in any worktree.

5. **One Today for both sides**, the Wednesday the API tests already use (2027-03-10):
   - API: outside Production, a `Clock:Today` setting registers a `FixedClock` instead of `TimeProvider.System` (move `FixedClock` from the tests into the API).
   - Browser: `page.clock.setFixedTime` with the same date, so `today()` in `dates.ts` agrees.
   - Both come from one constant in `web/e2e/fixtures.ts`.

6. **Every test arranges its own data** through the API with the `X-Api-Key` header, using the typed client from `schema.d.ts`, never through the screens it isn't testing, and never through `./seed` (which stays for hand tests and reviews).
   - An empty-state criterion needs an empty database, so in `Testing` only, the API maps `POST /api/testing/reset`, and a fixture calls it before every test.
   - Tests run one at a time (`workers: 1`) until the suite is slow enough to need a database per worker.

7. **Both widths, always**: two Playwright projects, phone (390×844) and desktop (1280×800), the widths `/experience-review` uses. Every test runs in both unless tagged `@phone` or `@desktop` for a layout-only behaviour.

8. **Logged in once**: a setup project logs in through the login screen and saves the cookie (`storageState`); the login tests themselves start logged out.

9. **Traceable**: a test's title starts with the spec's folder and the criterion's number: `test('chore-items A3: ticking every Item doesn't complete the Chore', ...)`. A `./check` step, `acceptance coverage`, fails when an Acceptance Criterion in any `.scratch/*/spec.md` has no test with its `<folder> A<n>` in a title. `(review)` criteria are exempt. Specs without an `## Acceptance` section are skipped, so older specs don't need backfilling.

10. **In `./check` and CI**: a `web e2e` step after `web test`. `post-create.sh` installs Chromium with `npx playwright install --with-deps chromium`, and `./check` installs it when missing, as it does `npm ci`. On failure, the step prints the failing titles and the path to the trace. Budget: the whole step under a minute. Past that, revisit decision 6.

11. **The workflow around it**:
    - **Grilling** ends with the Acceptance section, confirmed by the user.
    - **Implementing** starts with the acceptance tests, failing, as the outer loop; `/tdd` stays the inner loop for the Domain and API. A ticket is done when its criteria pass at both widths.
    - **`/code-review`, Spec axis**: checks every criterion has a test that really proves it (asserts the outcome, not just that a control exists).
    - **`/experience-review`**: keeps judging look and feel and checks the `(review)` criteria. A behaviour bug it finds becomes a criterion and a test.
    - **A hand-test bug** becomes a new criterion in its feature's spec and a failing test before the fix. The user's hand test then only answers "is this what we wanted?".
    - **`agent.test.tsx`** stays: it guards the agent bridge's naming contract, which is cheaper to check in jsdom.

## Acceptance

- A1. `./check` runs the acceptance tests at phone and desktop width and fails when one fails, locally and in CI.
- A2. The acceptance tests run while `./serve` is serving, and from two worktrees at once, without either failing.
- A3. A Due Date or Urgency shown during a test is the same on any day the suite runs.
- A4. An Acceptance Criterion without a test makes `./check` fail and name it; a `(review)` criterion doesn't.
- A5. A failing acceptance test leaves a trace and screenshot whose path `./check` prints.
- A6. The three escaped bugs under Why each have a test, and each test fails when its fix is reverted.
- A7. The `web e2e` step takes under a minute on the devcontainer. (review)

## Slices

1. **Tracer**: Playwright in `web/e2e/`, the `Testing` clock and reset, the login setup, one test (log in, see the empty Overview) at both widths, wired into `./check`, `post-create.sh` and CI. Covers A1, A2, A3, A5.
2. **Regressions**: the three escaped bugs plus the core flows the household uses daily: add a Recurring Chore and see it on the Overview, Done moves its Due Date, Postpone and Skip, tick Items. Covers A6.
3. **Coverage and process**: the `acceptance coverage` check, an Acceptance section added to the latest spec (`chore-items`) as the example, and the workflow in `CODING_STANDARDS.md` (review rules) and `docs/agents/dev-environment.md` (running a single test, reading a trace). Covers A4.

## For the grilling

- Is a reset endpoint (even Testing-only) acceptable in the API, or should each test get a fresh database and API process (slower, nothing test-only in the API)?
- Should acceptance tests replace parts of `/experience-review`, or only take the behavioural findings off its plate?
- Do One-off fixes and small changes (no spec) need a criterion too, for example in a `regressions` spec, or is a test enough?
- Budget: is a one-minute `./check` step acceptable for every commit, or do agents run only the touched specs and CI runs everything?
