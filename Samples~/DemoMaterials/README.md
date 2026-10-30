# Hypeline Demo Materials

These optional scripts support the developer walkthrough at https://muxy.github.io/gateway-unity/. They are examples, not ready-made prefabs. Import this sample from Package Manager in each Unity project where you want to use it.

## Quick start with the Hypeline prefabs

1. Add the MuxyHypelineManager and MuxyHypelineMenu prefabs to your scene. Assign the scene manager to the menu.
2. On the manager, create a Game Text with Label `Health`, Value `50`, and Icon `fa-solid:heart`.
3. Create an empty scene GameObject named `HypelineActionDemo`. Add the `HypelineActionDemo` component and assign the same scene manager in its Hypeline Manager field. The GameObject name is up to you; the script and component are named `HypelineActionDemo`.
4. On the manager, create the `healthpack` Game Action as described in the guide. Bind its On Game Action Used event to the HypelineActionDemo GameObject and select `HypelineActionDemo.OnHealthpack` under Dynamic GameActionUsed.
5. Enter Play mode. The sample controls show a separate health counter; use `Local test: add 10 health` to test it. The controls hide while the Hypeline connection panel is open. Test a real viewer action after connecting to Twitch.

The sample changes its own counter, not your game's player health. Adapt the handler to your gameplay before shipping. The sample accepts transactions; replace it with your own fulfillment and refund handling rather than running a second handler for the same action. Add transaction deduplication and other production safeguards described in the guide.

You can make a prefab from the configured sample GameObject for reuse, but assign its scene's manager after placing it in each scene. MuxyHypelineManager, MuxyHypelineMenu, MuxyHypelineInteractionAlert, and MuxyHypelinePolling are separate prefabs in the package.

## Code-only alternative

`HypelineCodeDemo.cs` creates its own SDK connection and basic PIN UI. Use it in a separate scene without a prefab manager or the Hypeline menu. The guide explains this alternative.

## Removing the examples

Remove sample components and their UnityEvent bindings from scenes before deleting the imported `Assets/Samples/Muxy Hypeline/<version>/DemoMaterials` folder. Preserve any code you adapted for your game. The package and its prefabs remain separate.

If you previously downloaded either script, keep only one copy of each class to avoid duplicate compile errors. Preserve custom changes and reassign scene components and event bindings as needed.
