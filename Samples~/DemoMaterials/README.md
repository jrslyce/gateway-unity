# Hypeline Demo Materials

These optional examples support the Hypeline developer guide:
https://muxy.github.io/gateway-unity/

- HypelineActionDemo.cs uses a scene's MuxyHypelineManager and provides a sample health action and poll controls.
- HypelineCodeDemo.cs demonstrates a separate code-only integration. Use it in a different scene without the prefab manager.

To remove these examples, first remove any sample components and UnityEvent bindings from your scenes. Then delete the imported DemoMaterials folder under Assets/Samples/Muxy Hypeline/. Keep any code you have adapted for your own game.

If you already downloaded either sample script, remove or migrate the older copy before importing this sample to avoid duplicate class definitions. The SDK and its prefabs are not part of this imported sample and do not need to be deleted.
