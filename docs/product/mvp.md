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
not medical diagnoses or risk classifications. The analysis algorithm remains
to be defined by a future story.

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

