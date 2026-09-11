using BepInEx.Configuration;

namespace CatosHoverInspector
{
    internal static class ModConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<int> MaxLines;
        internal static ConfigEntry<int> MaxTextCharacters;
        internal static ConfigEntry<int> MaxWarnings;
        internal static ConfigEntry<bool> ShowVanillaName;
        internal static ConfigEntry<bool> ShowWarnings;
        internal static ConfigEntry<bool> ShowUnavailableEta;
        internal static ConfigEntry<bool> EnablePipelineSmokeTest;

        internal static ConfigEntry<bool> EnableCraftingStationInspector;
        internal static ConfigEntry<bool> EnableProcessingInspector;
        internal static ConfigEntry<bool> EnableFireInspector;
        internal static ConfigEntry<bool> EnableCookingStationInspector;
        internal static ConfigEntry<bool> EnableBeehiveInspector;
        internal static ConfigEntry<bool> EnablePortalInspector;
        internal static ConfigEntry<bool> EnableBuildSight;

        internal static ConfigEntry<bool> UseRichTextColors;
        internal static ConfigEntry<bool> ShowCapacities;
        internal static ConfigEntry<bool> ShowInput;
        internal static ConfigEntry<bool> ShowFuel;
        internal static ConfigEntry<bool> ShowOutput;
        internal static ConfigEntry<bool> ShowBatchEta;
        internal static ConfigEntry<string> EtaStyle;
        internal static ConfigEntry<string> PausedText;
        internal static ConfigEntry<string> UnavailableEtaText;
        internal static ConfigEntry<bool> ShowDestinationName;
        internal static ConfigEntry<bool> ShowDuplicateTagWarning;
        internal static ConfigEntry<bool> ShowPortalStatus;
        internal static ConfigEntry<string> BuildSightActivationMode;
        internal static ConfigEntry<bool> ShowOwnedMaterials;
        internal static ConfigEntry<bool> ShowMissingMaterials;
        internal static ConfigEntry<bool> ShowBuildability;

        internal static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true,
                "Enable the client-only hover inspector.");
            MaxLines = config.Bind("Display", "MaxLines", 20,
                new ConfigDescription("Maximum inspector lines appended to native hover text.",
                    new AcceptableValueRange<int>(1, 100)));
            MaxTextCharacters = config.Bind("Display", "MaxTextCharacters", 1000,
                new ConfigDescription("Maximum formatted hover-text length.",
                    new AcceptableValueRange<int>(100, 4096)));
            MaxWarnings = config.Bind("Display", "MaxWarnings", 3,
                new ConfigDescription("Maximum warning lines for one inspection result.",
                    new AcceptableValueRange<int>(0, 10)));
            ShowVanillaName = config.Bind("Display", "ShowVanillaName", true,
                "Preserve the native hover name above inspector details.");
            ShowWarnings = config.Bind("Display", "ShowWarnings", true,
                "Show bounded warnings produced by supported inspectors.");
            ShowUnavailableEta = config.Bind("Display", "ShowUnavailableEta", false,
                "Show an explicit unavailable ETA when native timing cannot be proven.");
            EnablePipelineSmokeTest = config.Bind("Debug", "EnablePipelineSmokeTest", false,
                "Show a temporary native-hover pipeline test line on any valid hover target.");

            EnableCraftingStationInspector = config.Bind("Inspectors", "EnableCraftingStationInspector", true,
                "Enable workbench and forge inspection.");
            EnableProcessingInspector = config.Bind("Inspectors", "EnableProcessingInspector", true,
                "Enable smelter, kiln, fermenter, and windmill inspection.");
            EnableFireInspector = config.Bind("Inspectors", "EnableFireInspector", true,
                "Enable fireplace/fuel inspection.");
            EnableCookingStationInspector = config.Bind("Inspectors", "EnableCookingStationInspector", true,
                "Enable cooking station inspection.");
            EnableBeehiveInspector = config.Bind("Inspectors", "EnableBeehiveInspector", true,
                "Enable beehive inspection.");
            EnablePortalInspector = config.Bind("Inspectors", "EnablePortalInspector", true,
                "Enable portal inspection.");
            EnableBuildSight = config.Bind("Inspectors", "EnableBuildSight", false,
                "Enable the later read-only build-sight inspector.");

            UseRichTextColors = config.Bind("Display", "UseRichTextColors", true,
                "Allow bounded native rich-text color tags in inspector output.");
            ShowCapacities = config.Bind("Display", "ShowCapacities", true,
                "Show capacities when an inspector can read them safely.");
            ShowInput = config.Bind("Display", "ShowInput", true,
                "Show accepted input when an inspector can read it safely.");
            ShowFuel = config.Bind("Display", "ShowFuel", true,
                "Show fuel when an inspector can read it safely.");
            ShowOutput = config.Bind("Display", "ShowOutput", true,
                "Show output when an inspector can read it safely.");
            ShowBatchEta = config.Bind("Display", "ShowBatchEta", true,
                "Show batch-completion ETA when native state makes it reliable.");
            EtaStyle = config.Bind("Display", "EtaStyle", "Compact",
                "ETA style reserved for the object-specific inspector phases.");
            PausedText = config.Bind("Display", "PausedText", "Paused",
                "Text for a native job that is not advancing.");
            UnavailableEtaText = config.Bind("Display", "UnavailableEtaText", "ETA unavailable",
                "Fallback text when native ETA evidence is insufficient.");

            ShowDestinationName = config.Bind("Portal", "ShowDestinationName", true,
                "Show a portal destination only when locally and safely identifiable.");
            ShowDuplicateTagWarning = config.Bind("Portal", "ShowDuplicateTagWarning", true,
                "Show bounded duplicate-tag warnings when detectable.");
            ShowPortalStatus = config.Bind("Portal", "ShowPortalStatus", true,
                "Show native portal link/status information.");

            BuildSightActivationMode = config.Bind("BuildSight", "ActivationMode", "HoverGhost",
                "BuildSight activation mode reserved for its gated implementation phase.");
            ShowOwnedMaterials = config.Bind("BuildSight", "ShowOwnedMaterials", true,
                "Show owned build materials when BuildSight is implemented.");
            ShowMissingMaterials = config.Bind("BuildSight", "ShowMissingMaterials", true,
                "Show missing build materials when BuildSight is implemented.");
            ShowBuildability = config.Bind("BuildSight", "ShowBuildability", true,
                "Show buildability when BuildSight is implemented.");
        }
    }
}
