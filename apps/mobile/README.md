# Triggered Mobile

The mobile app is a TypeScript, React Native, and Expo application. The Food
list and creation flow use the backend API; Observation routes remain
placeholders.

## Prerequisites

- Node.js 22.13+ (22.x), 24.3+ (24.x), 26.x, or 27+
- npm
- Expo Go on a device, or an Android emulator. Running an iOS simulator
  requires macOS and Xcode.

## Setup and commands

From this directory, install dependencies, configure the API base URL, and
start Expo:

```sh
npm install
```

Set `EXPO_PUBLIC_API_BASE_URL` to the address where the backend API is
reachable from the device, including its scheme and port. The local API's
HTTP development profile listens on port `5276`. Set the variable and run Expo
from the same terminal:

```sh
# iOS simulator
export EXPO_PUBLIC_API_BASE_URL=http://localhost:5276
npm start
```

For an Android emulator, `10.0.2.2` routes to the development computer hosting
the emulator. In Windows Command Prompt:

```bat
set EXPO_PUBLIC_API_BASE_URL=http://10.0.2.2:5276
npm start
```

In Windows PowerShell:

```powershell
$env:EXPO_PUBLIC_API_BASE_URL="http://10.0.2.2:5276"
npm start
```

For a physical device, use the development computer's LAN IP address instead
of `10.0.2.2` (for example, `http://192.168.1.10:5276`). The phone and
computer must be on a network that permits the phone to reach that address
and port; allow the backend through the computer's firewall if needed.
`localhost` on a device refers to that device, not the development computer.

Start the backend from the repository root with
`dotnet run --project apps/api/src/Presentation --launch-profile http`.
Use the Expo Go app to scan the displayed QR code, or start a connected
emulator with `npm run android` or `npm run ios`.

Run the complete mobile test suite and TypeScript check from this directory:

```sh
npm test -- --runInBand
npm run typecheck
npx expo-doctor
```

## Project structure

- `index.ts` registers the application with Expo.
- `src/presentation` contains the root navigator and rendered screens.
- `src/application` contains typed navigation and Food contracts.
- `src/infrastructure` contains the Food API client and API configuration.
- `tests` covers startup, navigation, API requests, and Food list/create flows.

Presentation can depend on application contracts, while future infrastructure
implements the Food repository contract. Product business rules remain in the
shared backend rather than being duplicated in the app.
