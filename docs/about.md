# About Triggered

Triggered is a mobile-first personal food-trigger tracking application. A user
can quickly record what they ate, when they ate it, and how they felt
afterward. Over time, those observations form a history that can reveal
possible personal patterns.

The core workflow is:

```text
Eat -> Log Food -> Record Reaction -> Store Observation
    -> Accumulate History -> Analyze Pattern -> Display Trigger Level
```

Triggered communicates observations such as “your reports suggest a possible
pattern.” It does not diagnose disease, determine whether a food is medically
safe or unsafe, prescribe treatment, or replace a medical professional.

The mobile experience prioritizes fast capture. The web experience provides a
secondary interface for reviewing history, trends, observations, and analysis.
Both interfaces use the same backend and database and represent one product.

See [`docs/product/mvp.md`](product/mvp.md) for scope and
[`docs/architecture/system-overview.md`](architecture/system-overview.md) for
technical boundaries.

