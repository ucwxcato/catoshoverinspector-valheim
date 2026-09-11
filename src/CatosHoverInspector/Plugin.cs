using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CatosHoverInspector
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInProcess("valheim.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.catosaur.catoshoverinspector";
        public const string Name = "Catos Hover Inspector";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            ModConfig.Bind(Config);

            if (ModConfig.EnablePipelineSmokeTest.Value)
                InspectorRegistry.Register(new NativeHoverSmokeInspector());

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(HoverInspectorPatches));

            Logger.LogInfo($"{Name} {Version} client-only loaded; native hover pipeline ready.");
        }

        // Run after Valheim's normal HUD update and after any later HUD
        // postfix. No second camera raycast is performed here.
        private void LateUpdate()
        {
            try
            {
                HoverOverlay.Apply(Hud.instance, Player.m_localPlayer);
            }
            catch (System.Exception ex)
            {
                Log?.LogError($"Hover overlay update failed: {ex}");
                HoverOverlay.Clear(Hud.instance);
            }
        }

        private void OnDestroy()
        {
            HoverOverlay.Clear(Hud.instance);
            _harmony?.UnpatchAll(Guid);
        }
    }
}
