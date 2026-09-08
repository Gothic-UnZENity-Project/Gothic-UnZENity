# ADR-0002: AI-assisted run review (oracle checks for functional test artifacts)

- **Status:** Proposed
- **Date:** 2026-09-08
- **Deciders:** Gothic Unity maintainers
- **Depends on:** [ADR-0001](./0001-headless-functional-test-suite.md) — this ADR consumes the artifact
  bundle ADR-0001 produces; it defines no capture mechanism of its own.
- **Supersedes / Superseded by:** —

---

## 1. Context

### 1.1 What ADR-0001 already gives us

Every functional run produces, per scenario, a bundle on disk:

| Artifact | Content |
|---|---|
| `shots/*.png` | Screenshots at step boundaries, on error, on failure, on watchdog trip |
| `trace.jsonl` | One JSONL stream merging test steps, simulated input, every `GlobalEventDispatcher` event, player-state samples and log lines on one clock |
| `log.txt` / `errors.txt` | Full and error-only log output |
| `manifest.json` | Git SHA, Unity version, config, Gothic install fingerprint, seed, machine |

That bundle is enough to debug a failure an assertion caught. It does nothing for a failure no
assertion was written to catch.

### 1.2 The gap: assertions structurally cannot catch "it looks broken"

`WaitUntil(predicate, timeout, because)` only ever checks what someone thought to name in a predicate.
It will never notice:

- a T-posing NPC,
- a texture applied to the wrong mesh,
- the player fallen through the world,
- a black screen,
- an NPC standing inside a wall.

A Gothic port — runtime-loaded assets, hand-ported animation and material pipelines, a world never
built for Unity's coordinate and lighting conventions — generates exactly these failure modes
constantly, and every one of them is invisible to event- or state-based assertions. The scenario can
pass every `WaitUntil` in ADR-0001 while the screenshot next to it shows a corpse floating a meter
above its own grave. This ADR is about closing that specific gap, not about replacing assertions.

---

## 2. Decision

**D1. AI as the oracle, not the actor.** Two roles get conflated whenever "AI-driven testing" comes
up, and they have opposite risk profiles. This ADR adopts one and explicitly defers the other (§3.1).

**D2. The oracle runs as a post-run CI step, not inside Unity.** Unity's job ends at writing PNGs and
JSONL to disk; a separate script reads them after the test process has exited (§3.2).

**D3. Checks are tiered, cheapest first, model calls last.** Deterministic pixel checks run on every
frame for free; a text-only model reads the trace; a vision model looks only at the handful of frames
the earlier tiers flagged (§3.3–§3.5).

**D4. Findings annotate — they never gate.** At least initially, oracle output changes no exit code. It
appends to `summary.md` and the CI job summary and nothing else (§3.6).

---

## 3. Detailed design

### 3.1 AI as oracle, not actor — and why the actor role is deferred

- **(a) AI as actor** — a model decides what the player does next: an autonomous explorer, a "TAS run"
  reacting live to what appears on screen.
- **(b) AI as oracle** — a model decides, after the fact, whether what happened was acceptable.

**This ADR adopts (b) and explicitly defers (a).** Stated plainly, the actor role is rejected for this
purpose because:

- **It makes the run nondeterministic twice over.** Both the game under test and the tester driving it
  become sources of variance, so a failure cannot be attributed to either side with confidence. That is
  disqualifying for anything that gates a PR.
- **Its latency budget doesn't fit CI.** Without a dedicated GPU, local inference costs seconds per
  decision. A 30-second scenario at two decisions per second is dozens of inferences in the critical
  path of the run itself — a run that was supposed to take 30 seconds now overruns the nightly window
  before it does anything else.
- **Its legitimate home, if ever revisited, is nightly *exploration*** — a wandering session that files
  issues for a human to triage — never a gate a PR has to pass through.

The oracle role has the opposite economics on every one of those points: it runs **after** the run, so
it has no latency budget to fit inside; it adds no nondeterminism to the run itself, because the run has
already finished and produced fixed artifacts; it reuses screenshots and trace data that ADR-0001 is
already capturing for other reasons, so its marginal capture cost is zero; and its worst failure mode is
a false flag on a report nobody is forced to act on — not a flaky, unreproducible red PR.

### 3.2 Where it runs — a post-run CI step, any language

```
unity test … ──► Artifacts/…/shots/*.png + trace.jsonl
                          │
                  oracle script (CI step, any language)
                          │  HTTP
                          ▼
            http://<host>:11434/v1/chat/completions
            (Ollama / llama.cpp server / vLLM / LM Studio)
```

Unity's responsibility ends at writing PNGs and JSONL to disk (ADR-0001, §3.3–§3.5). The oracle is a
plain script that runs as its own CI step after the test job, reads those artifacts off disk, and calls
a model over HTTP. This buys:

- **Nothing new in the game loop.** No HTTP client, no async model call, no new failure surface inside
  Unity's test process.
- **No HTTP timeouts inside a test.** A slow or unreachable model server degrades the oracle step, not
  the test run that produced the artifacts it is reviewing.
- **Identical behaviour in the Editor lane and the player lane.** Lane A and Lane B (ADR-0001, D1) both
  just need to have written the artifact bundle; the oracle does not care which lane produced it.
- **The run still produces a result when the model server is down** — the test's pass/fail stands on its
  own regardless of the oracle's availability (§3.6).

For completeness: `UnityWebRequest` does work in headless batchmode, so calling a model from inside
Unity is technically possible. It is simply unnecessary once the actor role (§3.1) is out of scope, and
it would reintroduce exactly the coupling the post-run design avoids.

**The interface: OpenAI-compatible chat completions.** Ollama, llama.cpp's server, vLLM and LM Studio
all serve an OpenAI-compatible `/v1/chat/completions` endpoint. Writing the oracle's client against that
schema — rather than any one server's native API — makes `OPENAI_BASE_URL` and `OPENAI_MODEL`
environment variables the only thing separating a local model on the LAN from a hosted one; no code
change is needed to point the same script at either. Images are sent as base64 data URLs in the
standard `image_url` content part. No secret is required for a local server; the model host may be the
CI runner itself or another machine on the network.

### 3.3 Tier 1 — deterministic image checks (no model, microseconds each)

Filter hard before spending an inference: on CPU, a vision model costs seconds to a minute per image.
That is affordable for a handful of images per scenario and hopeless for a full storyboard of a
20-minute nightly suite. Tier 1 exists to make that filtering cheap and to catch, for free, a share of
"looks broken" that never needed a model at all:

| Check | Detects |
|---|---|
| Mean luminance below a floor | Black / blank frame |
| Consecutive frames byte-identical | The game hung while the harness kept ticking (a stall the watchdog's event/movement check might miss if it's purely visual) |
| Perceptual hash vs. a golden shot per waypoint | "This view changed a lot relative to the known-good baseline, look here" |

Tier 1 runs against every screenshot in the bundle. Its output is not a verdict — it's a shortlist of
frames worth spending a model call on, fed into Tier 2 (§3.5).

### 3.4 Tier 1.5 — a text-only model over `trace.jsonl`

Much cheaper than vision: a 7–8B text model is usable on CPU where a vision model is not, because there
are no image tokens to encode. It catches a different class of problem than pixels ever could, because
the trace (ADR-0001, §3.5) is already symbolic:

- an implausible event sequence (e.g. `FightHit` firing before `DrawWeapon` completed),
- an NPC that reads as never having reached a state the scenario expected it to reach,
- the same event firing hundreds of times where the scenario implies it should fire once.

Because the trace is already structured and small relative to an image, this may be better value per
watt than looking at pixels at all, and it is worth running unconditionally on every scenario's
`trace.jsonl`, not gated behind Tier 1.

### 3.5 Tier 2 — the vision model, only on flagged frames

The vision model runs only on: frames Tier 1 flagged, the failure frame (if the scenario failed), and
the watchdog frame (if the watchdog tripped, ADR-0001 §3.6). Realistically 1–5 images per scenario —
which is what makes a CPU-only vision pass fit inside a nightly window at all. The prompt asks a narrow
question against the screenshot (and the scenario/step name for context): does anything in this frame
look visually broken — T-pose, misapplied texture, geometry clipping, character out of bounds, a blank
or corrupted frame? The response is a short structured finding, not a paragraph.

### 3.6 Reporting posture — annotate only, never gate

Findings from Tier 1.5 and Tier 2 append to the run's `summary.md` (ADR-0001, §3.7) and to the CI job
summary. **They never change the exit code, at least initially.** A test's pass/fail is decided entirely
by ADR-0001's mechanisms (assertions, the error baseline, the watchdog); the oracle adds a "here's what a
model noticed" section underneath, for a human to read or ignore. Gating is deliberately not on the table
until the false-positive rate is known from real runs (§5, §6).

Graceful degradation follows from §3.2: if the model server is unreachable, the oracle step logs that
and produces no findings section — it does not fail the job, and it does not block artifact upload.

### 3.7 Relationship to `com.unity.ai.inference` (Sentis)

The project already depends on `com.unity.ai.inference` (Sentis) for its speech-to-text feature. That
is the right tool for a small on-device model running inside the game — it is the wrong tool for a
vision-language oracle. Sentis runs a model in-process, inside Unity, on the machine running the test;
routing the oracle through it would reintroduce the in-engine coupling §3.2 explicitly avoids, and would
tie the oracle's model choice to whatever Sentis can execute rather than to whatever an OpenAI-compatible
server can serve. Do not route this feature through it.

Findings are advisory in the strictest sense: a model's description of a screenshot is not evidence of a
defect on its own. It is a pointer for a human — or a deterministic check — to confirm or dismiss.

---

## 4. Alternatives considered

**A. Golden-image diffing alone, no model.** The cheapest option, and not rejected outright — it
survives as Tier 1's perceptual hash. Rejected as the *whole* answer: an exact or near-exact diff is
brittle across any intentional visual change (a lighting tweak, a new asset, a shader fix), and produces
constant false positives that train reviewers to ignore it.

**B. Gate the nightly lane on model output.** Rejected. False positives from a vision model destroy
trust in a CI signal faster than the missed bugs it would have caught — one ignored red build and the
gate has already failed at its job. Revisit only once real-run false-positive data exists (§6).

**C. Call the model from inside Unity.** Possible — `UnityWebRequest` works in batchmode (§3.2) — but
unnecessary once the actor role is deferred, and it couples the game loop to an external service's
availability for no benefit the post-run design doesn't already provide.

**D. Hosted-model-only design.** Rejected as the default. The artifacts under review are screenshots of
an unreleased game; a LAN-hosted model keeps them from leaving the network entirely. The
OpenAI-compatible interface (§3.2) leaves a hosted model as an option via `OPENAI_BASE_URL`, but it is
not where this ADR starts.

---

## 5. Consequences

**Positive**
- Catches an entire class of bug — "it looks broken" — that ADR-0001's assertions structurally cannot,
  using artifacts that ADR-0001 is already capturing for other reasons.
- Costs nothing extra per run when nothing looks wrong: Tier 1 is microseconds, and Tier 1.5/2 findings
  are advisory, so there is no new way for a run to go red.
- The OpenAI-compatible contract keeps the model swap (local ↔ hosted, one vendor ↔ another) to an
  environment variable, not a rewrite.
- Reuses the exact bundle ADR-0001 defines — no new capture code inside Unity, no new dependency in the
  game loop.

**Negative / costs**
- **A model server has to be provisioned and kept running** — someone owns its uptime, its model
  updates, and its disk footprint, the same way ADR-0001's self-hosted runner needs an owner.
- **CPU inference throughput is the binding constraint without a dedicated GPU.** Tier 2's frame cap
  (§3.5) exists specifically to keep this survivable; a suite that grows its scenario count without
  growing the frame cap will erode the nightly time budget the same way ADR-0001's software-raster path
  does.
- **False positives are certain, not hypothetical**, especially early. This is why §3.6 makes annotation
  the only mode until real data says otherwise.
- **Artifact volume sent per run grows** — every reviewed image is base64-encoded into an HTTP request;
  this is bandwidth and, for a hosted model, a cost line, not just disk space.
- **Tier 1's golden images need maintaining** as the game legitimately changes — the same maintenance
  burden ADR-0001 accepts for its error baseline (ADR-0001 §3.3), applied to screenshots instead of log
  lines.

---

## 6. Open questions

1. **Which model(s) to standardise on**, separately for the Tier 1.5 text role and the Tier 2 vision
   role? Different size/quality trade-offs apply to each.
2. **Where does the model server live** — the CI runner itself, or a separate machine on the network
   that multiple runners share?
3. **Are golden images kept per waypoint per game version** (Gothic 1 vs. Gothic 2, and across engine
   changes), and who regenerates them when a change is intentional?
4. **When, if ever, does Tier 1 alone — deterministic checks only, no model — get promoted to gating a
   run?** Tier 1.5 and Tier 2 are not proposed as gates in this ADR under any circumstance discussed
   here; whether Tier 1 alone ever crosses that line is a separate, later decision.

---

## 7. References

- [ADR-0001: Headless functional test suite](./0001-headless-functional-test-suite.md) — the artifact
  bundle (`shots/*.png`, `trace.jsonl`, `log.txt`, `errors.txt`, `manifest.json`) this ADR consumes.
- [Ollama OpenAI compatibility](https://github.com/ollama/ollama/blob/main/docs/openai.md) — one of
  several servers implementing `/v1/chat/completions`.
- [OpenAI Chat Completions API reference](https://platform.openai.com/docs/api-reference/chat) — the
  schema this ADR's client is written against.
