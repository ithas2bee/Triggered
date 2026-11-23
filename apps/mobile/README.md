# Triggered Mobile

The mobile app is a TypeScript, React Native, and Expo application. Its initial
screen and typed navigation root are in place; the Food and Observation routes
are navigation placeholders only.

## Prerequisites

- Node.js 22.13+ (22.x), 24.3+ (24.x), 26.x, or 27+
- npm
- Expo Go on a device, or an Android emulator. Running an iOS simulator
  requires macOS and Xcode.

## Setup and commands

From this directory, install dependencies and start Expo:

```sh
npm install
npm start
```

Use the Expo Go app to scan the displayed QR code, or start a connected
emulator with `npm run android` or `npm run ios`.

Run the app tests and TypeScript check with:

```sh
npm test -- --runInBand
npm run typecheck
```

## Project structure

- `index.ts` registers the application with Expo.
- `src/presentation` contains the root navigator and rendered screens.
- `src/application` contains typed client navigation contracts. Use cases will
  live here as they are introduced.
- `src/infrastructure` is reserved for API communication and other external
  integrations; no backend client is included in this foundation.
- `tests` covers startup rendering and the initial navigation behavior.

Presentation can depend on application contracts, while future infrastructure
will provide external integrations to application behavior. Product business
rules remain in the shared backend rather than being duplicated in the app.
