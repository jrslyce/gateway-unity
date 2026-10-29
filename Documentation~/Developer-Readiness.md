# Developer integration and release checklist

## Before your first connection

1. Install the package with Unity's Package Manager and select its `package.json`. Keep the extracted folder in place when using a local package.
2. Add one `MuxyGatewayManager` to the scene. Set its Game ID to the exact ID listed under **Linked Games** in your broadcaster's Twitch extension configuration. The example ID in older documentation is not necessarily linked to your channel.
3. Set the game name. To use a logo, assign a texture to **Game Logo**. Enable **Read/Write** and choose an uncompressed RGB24, RGBA32, or ARGB32 format in its import settings. The text Logo field is a legacy encoded-data fallback.
4. Add `MuxyHypelineMenu`, assign the manager, and ensure the scene has one EventSystem using the input module appropriate to your project's input settings.
5. Activate the Gateway extension on the broadcaster's channel. Open Configure, generate a fresh six-character PIN, and enter it in the game. A PIN expires after five minutes. Do not share the PIN or authentication tokens.

The manager Inspector warns about missing IDs and incompatible logo imports. Its UI is generated at runtime; position and size fields on the menu are applied when the UI is built. Restart Play mode after changing them in edit mode.

## Verify the game, not just the connection

Use a separate viewer session and trigger a registered action. The [logo first-action guide](Logo-First-Action.md) provides a visible test. Confirm the game receives the intended action ID and performs the expected effect. A connected indicator alone does not prove gameplay is wired. Handle each transaction's outcome explicitly: accept only after the effect succeeds, and refund when it cannot be fulfilled. Check your own duplicate-delivery and timeout behavior; the sample is not a guarantee of exactly-once delivery.

Try at least a fresh PIN, an invalid PIN, saved-session reconnect, disconnect/reconnect, scene reload, and a standalone player on each platform you intend to support. Check pause, loading and unavailable states, and stop advertising an action when it cannot be fulfilled. Do not test paid transactions without understanding the broadcaster's configuration and refund behavior.

## When something fails

- **No PIN input:** check that there is one active EventSystem and the correct input module. The new Input System requires its UI input module in projects configured only for that input system.
- **Authentication fails:** request a fresh PIN from the same broadcaster and compare the manager's Game ID character for character with Linked Games. The current native authentication response exposes a failure flag, not a verified detailed rejection reason.
- **Connected but no action:** confirm the extension is active on that channel, the action is registered and available, and your game has a handler for the matching ID. An alert alone does not fulfill an action.
- **Logo missing:** confirm Read/Write and a compatible uncompressed format on the target platform; check the Unity Console. The logo is encoded as PNG when metadata is submitted.
- **Editor works but build fails:** verify native plugin import settings, architecture and the actual scripting backend in a standalone build. Test the platform you ship; importing a library is not proof of authenticated networking.

The manager saves a refresh token in Unity `PlayerPrefs`; this storage is not encrypted. Review your game's credential storage needs. Keep PINs, tokens, and unredacted logs out of bug reports and source control.

## Compatibility evidence

This package declares Unity 2020.3 as its minimum and includes native libraries for Windows x64, Linux x64 and macOS. Library presence is not a support guarantee: consult the package's verification records and run your own player tests for the exact Unity version, architecture and backend you ship. WebGL, mobile and consoles are not supported by this native PC transport. TikTok is a coming-soon label, not an active integration.

For current test evidence and its limits, see [Hypeline Unity setup](Hypeline-Unity-Setup.md) and [documentation verification](VERIFICATION.md). For support, use the contact in `package.json` and include the package version, Unity version, target platform, backend, reproduction steps and redacted error details.
