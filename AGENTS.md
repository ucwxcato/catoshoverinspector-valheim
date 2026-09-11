# CatosHoverInspector development

## Scope and source of truth

CatosHoverInspector is a client-only Valheim/BepInEx mod. It displays
read-only status information for the object under the local player's native
hover target. It may show production ETAs, but it never controls production or
changes game state.

The canonical behavior and delivery contract is
[DOCS-ignored/devplan.md](DOCS-ignored/devplan.md). The sibling CatosChestViewer source and
plan are the reference implementation for the native hover/HUD pipeline. The
shared repository rules are in
[`../world-setup.md`](../world-setup.md).

Do not claim that this mod is implemented, installed, or gameplay-verified
until source, release artifact, runtime load evidence, and the relevant manual
tests exist. A successful build is not gameplay verification.

## Non-negotiable product boundaries

- Keep `[BepInProcess("valheim.exe")]` on the plugin.
- Do not install or deploy the DLL to the dedicated server.
- Use Valheim's native hover target and HUD text lifecycle. Do not add a second
  camera raycast for the MVP.
- The intended target path is Valheim's `Player.GetHoverObject()` result and
  the native `Hud.UpdateCrosshair` / `Hud.m_hoverName` display path. Reconfirm
  exact signatures against the installed assemblies before implementation.
- All inspectors are read-only. Never open containers, move items, consume
  fuel, harvest, build, repair, teleport, rename portals, or send gameplay RPCs.
- Inspect only the native local target and its bounded parent hierarchy. Do not
  scan the world or inspect unloaded objects.
- Do not retain live Unity/Valheim component or item references in snapshots
  across frames. Copy only detached display data.
- ETAs are informational. Show them only when a verified native timer or
  deterministic native progress source supports them. Never invent a countdown
  from arbitrary polling when the job can pause or block.
- Distinguish `Next output`, `Batch complete`, `Ready`, `Paused`, `Blocked`,
  and `Unavailable`. Do not display `00:00` as a fake ready state.
- Do not inspect chest/container contents; CatosChestViewer remains the owner
  of that feature and its privacy/guard-stone behavior.
- Keep lines, characters, refresh rate, warnings, and log output bounded.
- Do not promise compatibility with CatosChestViewer until the two plugins'
  native HUD ownership has been explicitly tested and documented.

## Target environment

The baseline specified by `world-setup.md` is:

- Valheim `1.0.7`, network version `39`.
- Unity `6000.0.75.2503836`.
- BepInExPack Valheim `5.4.2350` and BepInEx `5.4.23.5`.
- Target framework `.NET Framework 4.8`.
- Client: `C:\Program Files (x86)\Steam\steamapps\common\Valheim`.
- Dedicated server: `C:\Program Files (x86)\Steam\steamapps\common\Valheim dedicated server`.
- Managed game references: the installed client's
  `valheim_Data\Managed` directory.
- BepInEx references: the selected r2modman profile's `BepInEx\core`.
- Default client profile:
  `C:\Users\magni\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\CatosHoverInspector`.

These are the shared setup values, not proof that the current installed files
still match them. Refresh and inspect references before a release build.

## Plugin metadata and project layout

Proposed initial metadata:

```text
GUID:    com.catosaur.catoshoverinspector
Name:    Catos Hover Inspector
Version: 0.1.0
```

Keep the following ownership boundaries:

```text
src/CatosHoverInspector/Plugin.cs             entry point/lifecycle
src/CatosHoverInspector/ModConfig.cs          configuration/defaults
src/CatosHoverInspector/HoverTargetController.cs native target/cache
src/CatosHoverInspector/HoverOverlay.cs       native HUD ownership/cleanup
src/CatosHoverInspector/InspectorRegistry.cs  priority/exception isolation
src/CatosHoverInspector/Models/               detached display contracts
src/CatosHoverInspector/Formatting/           text and ETA formatting
src/CatosHoverInspector/Inspectors/           object-specific read-only logic
src/CatosHoverInspector/Runtime/              safe component/time helpers
scripts/                                      references/build/package
TEST_SERVER/                                  tracked harness seeds/launcher
DOCS-ignored/devplan.md                       canonical implementation plan
```

An inspector must not reach into another inspector's state. The registry owns
selection, and the overlay owns application/clearing of text.

## Native hover and overlay rules

The preferred pipeline is:

```text
Player.GetHoverObject()
  -> component/parent resolution
  -> InspectorRegistry
  -> detached InspectionResult
  -> HoverTextFormatter
  -> Hud.m_hoverName
```

Use the sibling CatosChestViewer implementation as a reference, but do not
hard-reference its assembly. Port its native target/HUD lifecycle pattern only;
do not add chest/container inspection to CatosHoverInspector. CatosChestViewer
remains the separate owner of chest contents. During development, use a clean
client profile containing CatosHoverInspector only; two independent writers to
`Hud.m_hoverName` are order-dependent.

The overlay must clear on:

- no HUD, no local player, disabled config, or scene transition;
- target change, target destruction, target invalidation, or range loss;
- inspection/read/format failure;
- plugin shutdown.

Vanilla hover text must remain available when no inspector applies. If the
chosen display mode appends details, preserve the native object name and keep
the appended section bounded.

## Inspector and ETA rules

Initial planned inspectors are:

- workbench and forge station level/extensions;
- smelter and kiln input/fuel/output/capacity;
- fermenter recipe/status/ready time;
- windmill status/progress/output/ETA;
- fire fuel/activity and cooking summary;
- cooking station slots and next-food ETA;
- beehive honey/capacity/production and next/full ETA;
- portal tag/link/duplicate/status diagnostics;
- BuildSight as a separately gated later phase.

The exact game members and timer semantics are unknown until assembly
inspection. Candidate component types and fields in the plan are proposed
adapters, not permission to assume signatures.

ETA requirements:

- Prefer a native absolute completion time, then native remaining seconds,
  then a verified progress/duration pair.
- Do not use wall-clock extrapolation unless pause/resume behavior has been
  proven against the native job.
- `Next output` is the next unit/slot completion; `Batch complete` includes
  only currently accepted work.
- A full output or paused job must not continue counting down.
- A beehive's `Next honey` and `Full` values are separate from any harvest
  action; looking at the hive must never harvest it.
- Refresh dynamic display at a bounded cadence and redraw only when the rounded
  visible value changes.
- Clamp negative timer values to `Ready`; never show negative time.

## Build and reference workflow

Do not commit game or BepInEx reference DLLs. Use the setup script to copy
only the required references into the ignored `lib/` directory:

```powershell
.\scripts\setup-references.ps1 `
  -GameManagedDirectory "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed" `
  -BepInExCoreDirectory "C:\Users\magni\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\CatosHoverInspector\BepInEx\core"
```

Build with:

```powershell
dotnet build src/CatosHoverInspector/CatosHoverInspector.csproj -c Release
```

Expected output:

```text
src/CatosHoverInspector/bin/Release/net48/net48/CatosHoverInspector.dll
```

The release package must not include `lib/`, `bin/`, `obj/`, logs, worlds,
credentials, or BepInEx runtime state.

## Local test server and world contract

Use `TEST_SERVER/start_catoshoverinspector_test.bat`. It mirrors the
CatosChestViewer launcher and `world-setup.md`:

- World source:
  `C:\Users\magni\Downloads\Dedicated`.
- Save root:
  `C:\Users\magni\Downloads`.
- World name: `Dedicated`.
- World mount:
  `C:\Users\magni\Downloads\worlds_local\Dedicated`.
- Port: `2462`.
- Password: `696969`.
- Public server: `0`.
- Client profile:
  `...profiles\CatosHoverInspector`.

The selected client profile's `BepInEx\core\BepInEx.dll` is the launcher
baseline for the dedicated server. On each launch, after confirming both
processes are stopped, the launcher compares the core BepInEx file versions
and updates the server only when it is older. That sync is limited to
`BepInEx\core\*.dll`, `winhttp.dll`, and `doorstop_config.ini`; it never copies
client plugins, client config, worlds, credentials, or the full profile.

The source world is a Valheim 1.0 directory containing `.db2` and `.fwl2`
files. The launcher must expose it through a directory junction at the mount
path; it must not copy, regenerate, repair, delete, or place a nested
`worlds_local` inside the source. Stop the client and server before any world
backup or save operation.

The launcher must:

1. Validate the server executable, managed assembly, client profile, BepInEx,
   seed config, admin list, and source world.
2. Refuse to run while `valheim.exe` or `valheim_server.exe` is open.
3. Rebuild the newest plugin and verify the expected DLL.
4. Refuse a stale dedicated-server assembly compared with refreshed references.
5. Compare the client/server BepInEx core versions and update the server loader
   files only when the server is older.
6. Create the junction only when the mount is absent; fail closed if a normal
   directory or wrong junction already occupies the mount.
7. Copy the tracked seed `TEST_SERVER/adminlist.txt` to the active save root.
8. Deploy only the client DLL to the clean CatosHoverInspector profile.
9. Seed the client config only when it does not already exist.
10. Print exact source, save root, mount, profile, destinations, and log paths.
11. Launch with `-world "Dedicated" -savedir "C:\Users\magni\Downloads"`.

The test admin list is harness parity only. This client-only mod must not read
it or use client-side admin claims.

## Gameplay verification minimum

Use the clean CatosHoverInspector profile connected to the matching local
dedicated server. At minimum verify:

- every implemented inspector's normal, empty, ready, paused, blocked, and
  unsupported states;
- ETA accuracy from display to native completion, including pause/resume;
- target changes, looking away, range loss, destruction, scene transitions,
  reconnect, and plugin shutdown;
- chest/container contents remain owned by CatosChestViewer and are not
  duplicated by this plugin;
- no inventory, fuel, honey, processing, portal, build, or world mutation;
- no repeated client exceptions or stale text;
- a clean client can connect and play normally while the server lacks this DLL.

Retain the client log, server log, launcher output, build output, and manual
test notes required by the verification matrix. Do not package or publish until
the clean client/server smoke test and failure-path checks pass.

## Change discipline

- Keep changes scoped to the current phase in `DOCS-ignored/devplan.md`.
- Do not mark plan checkboxes complete merely because source exists. A checkbox
  is complete only when the stated implementation and verification evidence
  exists.
- If a native signature differs from the plan, update the plan's TBD/decision
  and adapter boundary before coding around it.
- Do not modify the source world or unrelated CatoHeim projects while working
  on this plugin.
- Preserve existing user changes in a dirty worktree.
