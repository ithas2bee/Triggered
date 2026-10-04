# MVP Product Definition

## Purpose

The MVP helps a user discover personal relationships between foods consumed
and reported reactions afterward.

## Core capabilities

- Record a food and when it was consumed.
- Record how the user felt afterward.
- Store and review food observations.
- Accumulate historical observations.
- Analyze repeated observations for personal patterns.
- Present an understandable trigger assessment.
- Show **Insufficient Data** when observations do not support a meaningful
  pattern.

Initial conceptual levels are:

1. Insufficient Data
2. Low
3. Possible Trigger
4. Moderate
5. Likely Trigger
6. Strong Trigger Pattern

These levels describe the strength of an observed personal pattern. They are
not medical diagnoses or risk classifications.

### Initial food-response pattern calculation

The first calculation uses only observations associated with the requested
food. Fewer than three observations produce **Insufficient Data**; this
minimum is a simple guard against drawing a pattern from one or two reports,
not a clinically validated sample size.

An observation counts as a reported reaction unless its recorded reaction is
exactly “No reaction” after trimming surrounding whitespace and ignoring case.
All other recorded values count equally; their wording and severity are not
interpreted. The result is based on the percentage of that food's observations
with a reported reaction:

| Reported-reaction percentage | Level |
| --- | --- |
| 0–20% | Low |
| More than 20–40% | Possible Trigger |
| More than 40–60% | Moderate |
| More than 60–80% | Likely Trigger |
| More than 80% | Strong Trigger Pattern |

The five equal-width bands make the initial calculation easy to explain and
reproduce. For example, three reported reactions among four observations yield
75% and **Likely Trigger**. These are initial product rules, not medical
thresholds or evidence of causation; they should be revisited if the product
defines a more suitable evidence model.

## Product principles

- **Fast capture:** recording an observation should take only a few seconds.
- **Evidence over conclusions:** one observation must not become a trigger
  conclusion.
- **Personal context:** the system describes the user's history, not universal
  food safety.
- **Clear boundaries:** product language must not imply diagnosis or treatment.
- **Mobile first:** capture is optimized for a phone; review and analysis are
  especially useful on the web.

## Explicitly out of scope

Weight, calories, exercise, sleep, medication, wearables, medical records,
provider portals, diagnosis, AI diagnosis, medical recommendations, meal
planning, nutrition coaching, social or community features, restaurant
integrations, and grocery integrations are not MVP capabilities.
