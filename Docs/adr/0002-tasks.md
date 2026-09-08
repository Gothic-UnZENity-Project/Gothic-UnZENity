# ADR-0002 — Task checklist

Companion to [`0002-ai-assisted-run-review.md`](./0002-ai-assisted-run-review.md).
Section references (§) point into that ADR. Depends on ADR-0001's artifact bundle
([`0001-tasks.md`](./0001-tasks.md)) — the oracle has nothing to read until that bundle exists.

**Legend:** 🔴 blocker — nothing downstream works until it is done · 🟡 degrades a feature · ⚪ plain work
· ❓ needs a decision before it can be started.

Groups are listed in dependency order; within a group, items are roughly ordered too.

---

## Model serving (§3.2)

- [ ] ❓ Decide which model(s) to standardise on for Tier 1.5 (text) and Tier 2 (vision) — different
      quality/throughput trade-offs apply to each (§6).
- [ ] ❓ Decide where the model server lives: the CI runner itself, or a separate host multiple runners
      share (§6).
- [ ] 🔴 Provision an OpenAI-compatible model server (Ollama / llama.cpp server / vLLM / LM Studio) and
      confirm `/v1/chat/completions` responds to both a text-only and an image-bearing request.
- [ ] 🔴 Establish the `OPENAI_BASE_URL` / `OPENAI_MODEL` environment-variable contract as the only thing
      distinguishing a local model from a hosted one — no code branch on which is in use (§3.2).
- [ ] ⚪ Add a health-check call the oracle script runs before any tier that needs the model, so
      unreachability is detected once, not per image (feeds graceful degradation, below).

## Tier 1 — deterministic image checks (§3.3)

- [ ] ⚪ Mean-luminance check — flag frames below a black/blank floor.
- [ ] ⚪ Consecutive-frame identity check — flag byte-identical frames as a possible silent stall.
- [ ] ⚪ Perceptual-hash check against a golden shot per waypoint — flag frames that diverged materially
      from the known-good baseline.
- [ ] 🔴 Golden-image store: where baselines live, how they're keyed (waypoint × game version), and how
      a scenario looks one up. Nothing in Tier 1's hash check works without this.
- [ ] ⚪ Wire Tier 1 to run against every screenshot in the bundle unconditionally — it's the free tier,
      it should never be skipped.
- [ ] ⚪ Tier 1 output format: a shortlist of flagged frame paths + reason, consumed by Tier 2 (§3.5).

## Tier 1.5 — trace review (§3.4)

- [ ] ⚪ Prompt design for a text-only model reading `trace.jsonl`: implausible event ordering, an NPC
      never reaching an expected state, an event firing far more than the scenario implies it should.
- [ ] ⚪ Trace-to-prompt serialization — decide how much of a (potentially large) `trace.jsonl` fits in
      context per call; chunk or summarise if it doesn't.
- [ ] ⚪ Run Tier 1.5 unconditionally per scenario, independent of whether Tier 1 flagged anything —
      it's reading symbolic data, not pixels, so it isn't gated behind the image filter.
- [ ] ⚪ Findings format: structured (not prose) so `summary.md` generation (below) can render it
      consistently.

## Tier 2 — vision review (§3.5)

- [ ] ⚪ Frame selection: Tier 1-flagged frames + the failure frame + the watchdog frame, deduplicated.
- [ ] 🔴 Enforce a hard per-scenario frame cap (target: 1–5 images) — this is the number that keeps a
      CPU-only vision pass inside the nightly window; without a cap, cost scales with scenario count.
- [ ] ⚪ Base64 data-URL encoding of selected frames into the `image_url` content part.
- [ ] ⚪ Prompt design: a narrow, structured question per frame (visually broken: T-pose / bad texture /
      clipping geometry / out-of-bounds character / corrupted frame — yes/no + short description), not
      an open-ended "describe this image."
- [ ] ⚪ Findings format matching Tier 1.5's, so `summary.md` can merge both under one "oracle findings"
      section.

## Oracle script & reporting (§3.2, §3.6)

- [ ] ⚪ Artifact discovery: locate `shots/*.png`, `trace.jsonl`, `manifest.json` under
      `Artifacts/functional/<run>/<scenario>/` (ADR-0001 §3.7) without hardcoding a single run path.
- [ ] 🔴 OpenAI-compatible HTTP client (any language) — the one piece every tier above calls through.
- [ ] ⚪ Tier orchestration: Tier 1 → shortlist → Tier 1.5 (independent) → Tier 2 (on the shortlist),
      running as one CI step after the test job.
- [ ] ⚪ `summary.md` annotation — append an "oracle findings" section per scenario, never touching
      pass/fail (§3.6).
- [ ] ⚪ CI job summary annotation — surface the same findings in the GitHub Actions job summary.
- [ ] 🔴 Graceful degradation: model server unreachable → log it, skip findings, exit 0. The oracle step
      must never fail the job or block artifact upload (§3.2, §3.6).
- [ ] ⚪ Explicitly verify the oracle step changes no exit code anywhere in the pipeline — this is the
      property that makes "annotate only" true rather than aspirational.

## CI

- [ ] ⚪ Add the oracle step to the Lane 2/3 workflows (ADR-0001 §4.1) as a job step that runs after
      artifact upload, reading from the uploaded (or still-local) bundle.
- [ ] ⚪ Confirm the oracle step runs even when the test job itself failed — a failure is exactly when
      the findings are most useful.
- [ ] ⚪ Decide artifact retention for anything the oracle step itself writes (e.g. a findings JSON)
      separately from ADR-0001's existing retention policy.

## Docs

- [ ] ⚪ Document the `OPENAI_BASE_URL` / `OPENAI_MODEL` contract and how to point the oracle at a local
      vs. hosted server.
- [ ] ⚪ Document the golden-image workflow: how to add one, how to regenerate one after an intentional
      visual change.
- [ ] ⚪ Wiki → *Resources & Tools → Build Pipelines*: link this ADR alongside ADR-0001 and describe the
      tiered pipeline in one paragraph.
- [ ] ⚪ Write down how to read an oracle finding: it's a pointer for a human (or a deterministic check)
      to confirm, never evidence of a defect on its own (§3.7).
- [ ] ⚪ Set this ADR's status to Accepted once the model-serving decisions (above) are made.
