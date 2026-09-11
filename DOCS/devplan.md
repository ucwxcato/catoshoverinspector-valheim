# CatosHoverInspector - Development Plan

> **Status:** Authored design; no CatosHoverInspector source, artifact,
> deployment, or gameplay verification exists yet. The native hover pipeline is
> implemented in the sibling CatosChestViewer source, but its reuse must be
> revalidated against the installed assemblies before this project is built.
>
> **Purpose:** Let a player look at a supported Valheim object and immediately
> see useful, read-only status information, including honest estimated time to
> the next or final output when the game's native state provides a reliable
> timer.
>
> **Authority:** This document owns the CatosHoverInspector plugin scope,
> inspection contract, ETA rules, configuration, test harness, and release
> gates. The sibling [CatosChestViewer plan](../../CatosChestViewer/DEVPLAN.md)
> is the reference for native hover/HUD integration. The shared
> [world setup](../../world-setup.md) owns Valheim paths, world storage,
> launcher safety, and deployment rules.
>
> **Target:** Valheim `1.0.7`, network version `39`, Unity
> `6000.0.75.2503836`, BepInExPack Valheim `5.4.2350`, BepInEx `5.4.23.5`,
> and .NET Framework `4.8`, as specified by `world-setup.md`. These values must
> be checked against the installed client/server files before implementation.

## 0. Outcome

When the local player looks at a supported object within Valheim's normal hover
range, the native hover text contains a compact inspector block. The block
identifies the object's useful state without opening, changing, reserving, or
simulating anything.

Examples:

```text
Smelter
Input: Iron Ore x8
Fuel: Coal 12/20
Output: Empty
Next ingot: 00:43
Batch complete: 05:44
```

```text
Beehive
Honey: 3/4
Status: Producing
Next honey: 12:18
Full: 24:36
```

```text
Fermenter
Recipe: Barley Wine Base
Status: Fermenting
Ready in: 1d 12h
```

```text
Portal
Tag: mainbase
Linked: Mountain Outpost
Status: Clear
Other portals with tag: 1
```

Complete when:

```text
player aims at supported object -> native hover target is resolved ->
read-only snapshot is selected -> accurate status/ETA is rendered ->
target/state changes or becomes invalid -> text clears without stale output
```

## 1. Locked decisions

- **Standalone plugin:** CatosHoverInspector is a separate plugin with its own
  GUID, project, configuration, package, and test harness. It must not require
  CatosChestViewer at runtime.
- **Shared hover pipeline:** Use the CatosChestViewer pattern: Valheim's native
  `Player.GetHoverObject()` target, the native `Hud.UpdateCrosshair` lifecycle,
  and the native `Hud.m_hoverName` text. Do not add a second camera raycast for
  the MVP.
- **One HUD text owner during validation:** CatosHoverInspector will be tested
  in a clean client profile without CatosChestViewer. Co-installation with both
  plugins is not promised until an explicit text-ownership compatibility gate
  passes. This prevents competing Harmony postfixes and `m_hoverName` writers
  from producing order-dependent output.
- **Chest parity:** CatosHoverInspector will include a bounded chest/container
  inspector so it can eventually replace CatosChestViewer for users who want
  one unified hover mod. Its read/access behavior must preserve the verified
  safety rules from CatosChestViewer: native range, native hover selection,
  privacy checks, guard-stone checks, and no inventory mutation.
- **Client-only:** The plugin runs only in `valheim.exe` and carries
  `[BepInProcess("valheim.exe")]`. It must not be installed, loaded, or
  required by the dedicated server. The server is only a multiplayer test
  endpoint.
- **Read-only behavior:** The mod may inspect local replicated state and format
  text. It must never open a container, move items, consume fuel, harvest,
  start/stop processing, repair, build, teleport, write world state, or send a
  gameplay RPC.
- **Native truth over inferred truth:** An ETA is shown only when a verified
  native timer or a deterministic native progress value can support it. The
  mod must omit the ETA or show `ETA unavailable` when state is paused,
  blocked, unloaded, ambiguous, or not exposed reliably.
- **Two ETA meanings:** Where possible, show both `Next output` and `Batch
  complete`. `Next output` means the next unit or slot completion. `Batch
  complete` means all currently loaded work is expected to finish. If only one
  value can be proven, show only that value and label it precisely.
- **No false precision:** Display ETAs rounded to whole seconds for short jobs,
  whole minutes for longer jobs, and days/hours/minutes for multi-day jobs.
  Never imply sub-second accuracy.
- **No permanent data:** The MVP stores no world database, player history,
  container index, telemetry, or ETA state. All snapshots and caches are
  transient and discarded on scene change, target change, plugin shutdown, or
  invalidation.
- **Native target rules:** A target is eligible only when it is the same object
  Valheim considers the local hover/interact target. No wallhack, distance
  bypass, unloaded-object lookup, or map-wide inspection is allowed.
- **Graceful unknowns:** Unsupported or partially readable objects fall back to
  normal vanilla hover text. A failed inspector must not clear another valid
  vanilla hover target or flood the log.
- **BuildSight is a gated expansion:** Build ghost/material requirements are
  client-side and read-only, but they are a separate inspector mode and are not
  allowed to delay or destabilize the processing/station MVP.

## 2. Goals and non-goals

### Goals

- [ ] Provide one extensible registry for object-specific hover inspectors.
- [ ] Preserve the vanilla object name and add a compact status section below
  it, subject to a configurable display mode.
- [ ] Port the CatosChestViewer's safe container read behavior into a unified
  `ChestInspector`.
- [ ] Show useful status for workbenches, forges, smelters, kilns, fermenters,
  windmills, fires, cooking stations, beehives, and portals where APIs support
  it.
- [ ] Show honest live ETAs for processing, fermentation, cooking, fire fuel,
  and beehive production when the timer/progress is reliable.
- [ ] Distinguish active, paused, blocked, full, empty, finished, unlinked,
  invalid, and unavailable states.
- [ ] Update dynamic countdowns at a bounded cadence without allocating or
  formatting excessively every frame.
- [ ] Clear or refresh text when looking away, leaving range, changing target,
  changing inventory/state, destroying the target, changing scene, or disabling
  the mod.
- [ ] Provide a reproducible net48 build, clean client profile, and isolated
  multiplayer smoke test based on `world-setup.md`.
- [ ] Leave unrelated players, server state, inventories, processing timers,
  and world saves unchanged.

### Explicitly out of scope

- [ ] Opening, remotely accessing, sorting, transferring, repairing, fueling,
  harvesting, or mutating any object.
- [ ] A custom Canvas, custom world-space UI, map overlay, ESP, wallhack, or
  inspection of objects outside native hover distance.
- [ ] Server-side installation, server authority, custom RPCs, admin commands,
  permissions, or admin-list access.
- [ ] Persisting production history, completed-job history, player statistics,
  or world object indexes.
- [ ] Automatically fixing broken portals, renaming portals, or changing
  portal tags.
- [ ] Predicting ETAs for objects whose timers are not exposed or whose state
  cannot be synchronized safely.
- [ ] Replacing CraftFromChests or implementing resource transfer from chests.
- [ ] Full support for arbitrary custom modded machines before a compatibility
  adapter/API exists and the machine has been tested.
- [ ] A global workshop dashboard or remote view of every processor in the
  world.
- [ ] Making CatosChestViewer and CatosHoverInspector co-own the same native
  HUD text without an explicit compatibility design.

## 3. User experience / operational flow

1. The player looks at an object. Valheim's normal hover path resolves the
   target and updates the native hover name.
2. CatosHoverInspector receives the same local context and resolves a
   supported component from the target or its parent.
3. The registry selects the highest-priority compatible inspector. It creates a
   detached snapshot containing only display data; it does not retain Unity or
   Valheim item/component references across frames.
4. The inspector reports status fields and, if supported, an ETA descriptor.
   The formatter renders the object name, status, quantities, warnings, and
   ETA lines within configured line/character limits.
5. The overlay applies the result to the native hover label. A dynamic target
   such as a smelter or beehive is refreshed on a bounded cadence so the
   countdown changes visibly without requiring the player to look away.
6. If the target changes, becomes invalid, leaves native range, is destroyed,
   the scene changes, or the feature is disabled, the overlay releases its
   cached state and lets vanilla hover text return.

### Display examples by object type

#### Crafting stations

```text
Workbench
Level: 4
Upgrades: 3 detected
Repairing: Available
Sheltered: Yes
```

```text
Forge
Level: 5
Upgrades: 4 detected
Repairing: Available
```

The exact maximum level must not be assumed unless the runtime exposes it. Use
`Level: 4` rather than `Level: 4/5` when the maximum is unknown.

#### Smelter and kiln

```text
Smelter
Status: Smelting
Input: Iron Ore x8
Fuel: Coal 12/20
Output: Empty
Next ingot: 00:43
Batch complete: 05:44
```

If output is full:

```text
Smelter
Status: Blocked
Output full
Next ingot: Paused
```

#### Fermenter

```text
Fermenter
Recipe: Barley Wine Base
Status: Fermenting
Ready in: 1d 12h
```

When finished:

```text
Fermenter
Status: Ready
Output: 4 available
```

#### Windmill

```text
Windmill
Status: Running
Input: Barley x10
Progress: 72%
Next flour: 00:18
Batch complete: 03:00
```

If wind or another native condition pauses it:

```text
Windmill
Status: Paused
Reason: No wind
ETA: Paused
```

#### Fire and cooking

```text
Campfire
Fuel: 18 minutes
Sheltered: Yes
Cooking: 2/6
Next food: 00:21
```

Fuel ETA means time until the fire becomes inactive, not time until food is
ready. If native fuel duration is unavailable, show fuel quantity/status only.

#### Beehive

```text
Beehive
Honey: 3/4
Status: Producing
Next honey: 12:18
Full: 24:36
```

When full:

```text
Beehive
Honey: 4/4
Status: Ready to harvest
Production: Paused while full
```

The inspector must not claim that a hive is producing if the native component
is in a no-production, shelter, player-distance, or full-output state.

#### Portal

```text
Portal
Tag: mainbase
Linked: Mountain Outpost
Status: Clear
Other portals with tag: 1
```

```text
Portal
Tag: ironrun
Linked: No
Status: Duplicate tag
Other portals with tag: 2
```

Destination names and duplicate counts must be based only on portal data that
is legitimately available to the local client; no map-wide world scan is part
of the MVP.

## 4. Architecture and ownership

The following layout is proposed. It intentionally mirrors the proven
CatosChestViewer separation while replacing chest-specific names with generic
inspection boundaries.

```text
CatosHoverInspector/
  AGENTS.md
  README.md
  CHANGELOG.md
  CatosHoverInspector.sln
  src/
    CatosHoverInspector/
      CatosHoverInspector.csproj
      Plugin.cs
      ModConfig.cs
      HoverTargetController.cs
      HoverOverlay.cs
      InspectorRegistry.cs
      Models/
        InspectionContext.cs
        InspectionResult.cs
        InspectionSnapshot.cs
        EtaDescriptor.cs
        DisplayLine.cs
      Formatting/
        HoverTextFormatter.cs
        EtaFormatter.cs
        StatusFormatter.cs
      Inspectors/
        ChestInspector.cs
        CraftingStationInspector.cs
        ProcessingInspector.cs
        FireInspector.cs
        CookingStationInspector.cs
        BeehiveInspector.cs
        PortalInspector.cs
        BuildSightInspector.cs
      Runtime/
        ComponentResolver.cs
        NativeTimeReader.cs
        SafeAccessReader.cs
        RateLimitedDiagnostics.cs
  scripts/
    setup-references.ps1
    build.ps1
    package.ps1
  TEST_SERVER/
    start_catoshoverinspector_test.bat
    adminlist.txt
    com.catosaur.catoshoverinspector.cfg
  thunderstore/
    manifest.json
    README.md
    icon.png
  DOCS/
    devplan.md
```

### Verified/reference boundaries

- The sibling CatosChestViewer source uses `Player.GetHoverObject()` and a
  native `Hud.m_hoverName` overlay. Its plan records `Hud.UpdateCrosshair` as
  the native update point and `Unity.TextMeshPro.dll` as a required compile
  reference. These are reference facts from sibling source/plan, not a claim
  that CatosHoverInspector has been built or loaded.
- `Container.GetInventory()` and
  `Inventory.GetAllItemsInGridOrder()` are recorded by CatosChestViewer as
  available native APIs. CatosHoverInspector must preserve its fail-closed
  access behavior.
- Exact runtime members for `CraftingStation`, `Smelter`, `Fermenter`,
  `Fire`, cooking components, `Beehive`, `TeleportWorld`, `Windmill`, `Piece`,
  and `Recipe` are **TBD until Phase 0 assembly inspection**. Names in this
  plan are conceptual adapters, not verified signatures.

### Component ownership

| Component | Owns | Must not own |
|---|---|---|
| `Plugin` | BepInEx entry point, config, Harmony lifecycle, shutdown cleanup | Target-specific inspection or gameplay mutation |
| `HoverTargetController` | Native hover target identity, validity, target changes, cadence | Custom raycasts, formatting, object-specific rules |
| `InspectorRegistry` | Inspector registration, priority, safe selection, exception isolation | Reading arbitrary world objects or deciding display text |
| `ComponentResolver` | Resolving supported components from the native target/parent hierarchy | Distance bypass, world scans, object mutation |
| `ChestInspector` | Read-only container snapshot and access-safe chest lines | Opening, moving, or changing inventory |
| `ProcessingInspector` | Smelter, kiln, fermenter, and windmill snapshots/ETAs | Starting, stopping, fueling, or collecting jobs |
| `CraftingStationInspector` | Workbench/forge level and extension information | Repairing or changing station state |
| `FireInspector` | Fire fuel, shelter/status, and cooking summary | Adding fuel, cooking, or claiming heat |
| `CookingStationInspector` | Cooking slots and native cooking ETAs where available | Consuming, moving, or starting food |
| `BeehiveInspector` | Honey count, production state, and native ETA where available | Harvesting or changing hive state |
| `PortalInspector` | Local portal tag/link/status diagnostics | Renaming, linking, teleporting, or discovering remote objects illegally |
| `BuildSightInspector` | Read-only build-ghost requirements and material comparison | Placing pieces, consuming materials, or bypassing build rules |
| `NativeTimeReader` | Converting verified native timer/progress data into an ETA descriptor | Inventing production timers or driving game time |
| `HoverTextFormatter` | Line ordering, localization fallback, colors, truncation, ETA wording | Reading Unity components or deciding authority |
| `HoverOverlay` | Applying/clearing owned native HUD text and restoring safe lifecycle | Target discovery, data reads, or persistent UI |

### Inspector priority

Priority prevents multiple adapters from producing conflicting output for the
same target. The initial proposed order is:

```text
BuildSight ghost (only when explicitly enabled)
Portal
Beehive
Processing station
Cooking station
Fire
Crafting station
Container
Vanilla fallback
```

Phase 0 may adjust this after inspecting actual component hierarchies. A target
must produce exactly one CatosHoverInspector result per refresh.

## 5. Data, ETA, lifecycle, and failure handling

### 5.1 Detached snapshot model

Each refresh produces immutable or effectively immutable display data detached
from live Unity objects:

| Field | Purpose |
|---|---|
| `TargetIdentity` | Stable per-target identity for cache invalidation during the current session |
| `DisplayName` | Localized/native object name or safe fallback |
| `Status` | Active, paused, blocked, ready, empty, unavailable, or custom status |
| `Lines` | Bounded display fields such as input, fuel, output, level, or capacity |
| `Eta` | Optional `EtaDescriptor` with label, seconds, paused flag, and evidence level |
| `Fingerprint` | Detects meaningful changes without retaining live component references |
| `Warnings` | Bounded diagnostics such as duplicate portal tag or output full |
| `GeneratedAt` | Local monotonic time used only for refresh bookkeeping |

No `ItemDrop.ItemData`, `Container`, `Smelter`, `Beehive`, `TeleportWorld`, or
other Unity component reference may be retained as the display snapshot.

### 5.2 ETA contract

An `EtaDescriptor` is valid only when all of the following are true:

- The native component exposes a remaining duration, completion timestamp, or
  deterministic progress/rate that Phase 0 has verified.
- The job is in a state where time is actually advancing.
- The target remains valid and the relevant output/input capacity is known.
- The value is finite, non-negative, and within a configured safety maximum.
- The formatter can label what the ETA refers to: next item, ready batch, full
  batch, food, honey, or fire fuel.

ETA states:

```text
Active(seconds)       -> display a countdown
Ready                 -> display Ready; do not display 00:00
Paused(reason)        -> display Paused or omit ETA, according to config
Blocked(reason)       -> display Paused/Blocked; never count down
Unavailable           -> omit ETA by default or display ETA unavailable
Invalid               -> discard the snapshot and keep vanilla text
```

The mod must choose one consistent time basis during Phase 0. Preferred order:

1. A native absolute completion time from the same game clock as the object.
2. A native remaining-seconds field.
3. A verified native progress fraction plus native fixed duration.
4. No ETA.

Wall-clock extrapolation from two arbitrary polls is not acceptable for an ETA
unless it is explicitly verified against the object's native pause/resume
behavior. The inspector displays an estimate; it never schedules or advances
the job.

ETA rounding contract:

```text
0-59 seconds       -> whole seconds, e.g. 43s
1-59 minutes       -> whole minutes plus seconds when useful, e.g. 05:44
1-23 hours         -> hours and minutes, e.g. 1h 12m
1+ days            -> days and hours, e.g. 1d 12h
```

The exact threshold and style are configuration tuning points, but the output
must not flicker between formats as a countdown crosses a boundary. The
formatter should clamp negative values to `Ready` and suppress one-second
oscillation caused by floating-point/network rounding.

### 5.3 Production-specific ETA rules

- **Smelter/kiln:** `Next ingot`/`Next coal` refers to the next completed output
  only when input, fuel, and output capacity allow progress. `Batch complete`
  includes currently accepted input, not items that may be added later.
- **Fermenter:** Prefer `Ready in` based on the native fermentation completion
  state. Do not display a batch countdown for an empty or harvest-ready
  fermenter.
- **Windmill:** Display ETA only while native wind/progress state is active.
  When paused for no wind or full output, show the reason and paused ETA.
- **Cooking station:** `Next food` refers to the earliest active cooking slot.
  A full queue with no active cook is `Ready`/`Blocked` according to native
  state, not an invented countdown.
- **Fire:** `Fuel remaining` refers to fire activity duration. `Next food` is a
  separate timer and must not be confused with fuel ETA.
- **Beehive:** `Next honey` refers to the next honey increment. `Full` refers
  to the expected time until the hive reaches its currently known capacity.
  When full, production is paused and no future countdown is shown.
- **Unknown/modded processors:** Use a compatibility adapter only after the
  state fields and timer semantics are verified. Otherwise show static fields
  and omit ETA.

### 5.4 Runtime lifecycle

```text
Hidden
  -> Tracking target
  -> Reading snapshot
  -> Rendered static/dynamic result
  -> Refreshing countdown/state
  -> Hidden on invalidation
```

Required invalidation events:

- Target changes or no longer resolves from the native hover object.
- Target is outside native range or destroyed.
- Scene/world transition, local player absence, HUD absence, or plugin disable.
- Inspector throws while reading or formatting.
- The target's meaningful fingerprint changes.

On a read/format exception, clear only CatosHoverInspector-owned text, log a
rate-limited diagnostic containing the inspector type and safe target name, and
retry on the next allowed cadence. Never retry in a tight loop.

### 5.5 Refresh and caching

- Static fields may be fingerprint-cached until the target or state changes.
- Dynamic ETA text must refresh often enough to feel live but no faster than
  the configured minimum interval.
- The default proposed interval is `100ms` for target/state checks and
  `1000ms` for visible ETA text; Phase 0/1 profiling may consolidate these.
- Countdown formatting should update only when the displayed rounded value
  changes.
- Maximum lines and characters apply after all status and ETA lines are
  composed. Truncation must happen at line boundaries where possible.

## 6. Configuration, permissions, and integrations

Proposed config file:
`BepInEx/config/com.catosaur.catoshoverinspector.cfg`.

```ini
[General]
Enabled = true
UpdateIntervalMs = 100
EtaRefreshIntervalMs = 1000
MaxLines = 20
MaxTextCharacters = 1000
ShowVanillaName = true
ShowWarnings = true
ShowUnavailableEta = false

[Inspectors]
EnableChestInspector = true
EnableCraftingStationInspector = true
EnableProcessingInspector = true
EnableFireInspector = true
EnableCookingStationInspector = true
EnableBeehiveInspector = true
EnablePortalInspector = true
EnableBuildSight = true

[Display]
UseRichTextColors = true
ShowCapacities = true
ShowInput = true
ShowFuel = true
ShowOutput = true
ShowBatchEta = true
EtaStyle = Compact
PausedText = Paused
UnavailableEtaText = ETA unavailable

[Portal]
ShowDestinationName = true
ShowDuplicateTagWarning = true
ShowPortalStatus = true

[BuildSight]
Enabled = true
ActivationMode = HoverGhost
ShowOwnedMaterials = true
ShowMissingMaterials = true
ShowBuildability = true
```

All numeric values require safe ranges. Invalid enum/string settings must fall
back to documented defaults and log one bounded warning.

### Permissions and network behavior

- No permissions or admin status are used.
- No custom RPCs are required for the MVP.
- The mod reads only the local client's legitimately replicated object state.
- A client that does not install CatosHoverInspector is unaffected.
- A dedicated server does not need the DLL and must not load it.

### CatosChestViewer relationship

The first implementation should copy/adapt the sibling's safe patterns rather
than reference its assembly. Before release, choose one of these outcomes and
record the evidence:

1. CatosHoverInspector becomes the recommended unified replacement and users
   remove CatosChestViewer; or
2. A documented compatibility bridge gives exactly one plugin ownership of
   `Hud.m_hoverName`; or
3. The two plugins remain explicitly incompatible when both are enabled.

The project must not silently advertise co-installation while both plugins can
overwrite one another's native hover text.

## 7. Safety, security, and product constraints

- The plugin is visual/read-only and must not mutate `Inventory`, `ItemDrop`,
  `Container`, processing components, `TeleportWorld`, `Piece`, or world/ZDO
  state.
- Chest/container inspection must fail closed for private or guard-stone
  inaccessible containers, matching CatosChestViewer's access contract.
- Portal inspection must not perform an unrestricted world scan or expose
  hidden/private data beyond the local portal state available through native
  gameplay.
- BuildSight must compare materials for display only. It must not call build,
  place, remove, consume, or inventory-transfer methods.
- Component resolution must use the native target and bounded parent lookup.
  It must not search the whole scene every frame.
- All Unity/game-object access must occur on the Unity main thread.
- Every inspector must tolerate destroyed/null Unity objects and missing
  optional components.
- Dynamic text must be bounded by maximum refresh rate, lines, characters, and
  warning count.
- Item names and object names must use safe localized/native display names with
  fallback text for missing or modded prefabs.
- Rich-text output must escape or constrain any item/object text that can
  contain markup, preventing malformed hover UI.
- Exceptions must be isolated per inspector and rate-limited.
- The `[BepInProcess("valheim.exe")]` guard must remain in the final plugin.
- The test launcher must never copy the client-only DLL to the dedicated
  server, copy credentials, overwrite the active player config, or point
  `-savedir` at the world directory itself.
- Successful compilation, packaging, or a BepInEx load line is not gameplay
  verification.

## 8. Verification matrix

| Scenario | Expected result | Evidence required |
|---|---|---|
| Clean client loads plugin | Plugin load line appears once; no server load attempt | Client log and DLL path/timestamp |
| Dedicated server starts without plugin | Server starts normally and does not require the DLL | Server log and launcher output |
| Look at a workbench/forge | Level and verified station details appear | Manual screenshot/video and client log |
| Look at smelter with active job | Input/fuel/output and `Next` ETA appear accurately | Before/after state and timed comparison |
| Smelter output full | Status reports blocked/paused; ETA does not continue falsely | Manual test and log |
| Kiln active/empty/full | Correct state and ETA/fallback for each case | Manual matrix |
| Fermenter active | Recipe and `Ready in` are accurate | Timed completion comparison |
| Fermenter finished | Shows ready/output state, not `00:00` countdown | Screenshot and manual check |
| Windmill running | Progress and ETA update | Timed display check |
| Windmill paused | Shows paused reason or safe unavailable state | Manual no-wind/full-output test |
| Campfire fuel | Fuel/activity status is shown without changing fuel | Inventory/world state comparison |
| Fire cooking slot | Next food ETA is distinct from fuel remaining | Timed comparison |
| Beehive producing | Honey count and next/full ETA are accurate | Timed honey production comparison |
| Beehive full | Shows ready/full and no continuing ETA | Full hive test |
| Portal paired | Tag, destination, and clear status are correct | Two-portal manual test |
| Portal unpaired | Unlinked state appears without changing tag | Manual screenshot |
| Duplicate portal tag | Bounded warning appears when detectable | Duplicate-tag test |
| Chest accessible | Container lines appear with native access/range rules | Manual test |
| Chest inaccessible | Contents are not exposed; vanilla text remains safe | Private/ward test |
| Empty container | Empty state is concise and configurable | Manual test |
| BuildSight disabled | Build ghost does not change normal hover behavior | Client test |
| BuildSight enabled | Missing/owned materials are display-only and accurate | Build preview test; inventory unchanged |
| Look away/out of range | Inspector lines clear immediately or within the configured cadence | Manual test |
| Target destroyed | No stale text or repeated exceptions | Destroy-target test and log |
| Rapid target/state changes | Text follows the current target and never shows old ETA | Repeated target-switch test |
| Scene transition/reconnect | Cache and overlay clear; plugin remains usable after return | Client log and manual test |
| Long-running countdown | Display rounds consistently and does not flicker | Timed observation |
| Timer pauses/resumes | ETA pauses/resumes with the native job or falls back safely | Pause/resume comparison |
| Unsupported modded object | Vanilla hover remains; no crash or spam | Optional-mod profile test |
| Both CatosChestViewer and CatosHoverInspector installed | Behavior matches documented compatibility decision | Co-installation test and release docs |
| Multiplayer clean client joins | Unmodded/clean player connects normally; no server dependency | Server/client logs |
| No mutation audit | No inventory, fuel, portal, build, or world changes caused by looking | Before/after state and code review |
| Performance observation | No visible frame hitch, allocation spike, or repeated log flood | Profiling/log evidence |

## 9. Phased checklist

### Phase 0 - Repository, compatibility, and design lock

- [ ] Create the `CatosHoverInspector/` repository structure and this
  `DOCS/devplan.md` as the canonical plan.
- [ ] Add project-local `AGENTS.md` that links to this plan and adopts the
  shared `world-setup.md` rules.
- [ ] Record the final plugin metadata: proposed GUID
  `com.catosaur.catoshoverinspector`, name `Catos Hover Inspector`, initial
  version `0.1.0`.
- [ ] Inspect the installed client and server managed assemblies directly
  before copying references.
- [ ] Reconfirm Valheim version, network version, Unity version, BepInEx
  version, target framework, and `Unity.TextMeshPro.dll` availability.
- [ ] Reconfirm the sibling CatosChestViewer `Hud.UpdateCrosshair`,
  `Hud.m_hoverName`, `Player.GetHoverObject()`, and lifecycle behavior against
  the installed assembly.
- [ ] Inspect candidate members and signatures for `CraftingStation`, `Forge`,
  `Smelter`, `Fermenter`, `Fire`, cooking components, `Beehive`,
  `TeleportWorld`, `Windmill`, `Piece`, and `Recipe`.
- [ ] For every candidate ETA source, record whether the runtime provides an
  absolute completion time, remaining seconds, progress/rate, or no reliable
  timer.
- [ ] Verify how paused, full-output, empty-input, no-wind, sheltered, and
  ready states are represented for each supported object.
- [ ] Decide whether the native time source is game time, network time, or a
  component-specific timer and document the evidence.
- [ ] Inspect how native hover targets resolve parent components so the
  registry does not need a second raycast.
- [ ] Decide the initial inspector priority order after hierarchy inspection.
- [ ] Decide the final CatosChestViewer relationship and document whether the
  new mod is a replacement, bridged companion, or explicitly incompatible.
- [ ] Add `.gitignore` rules for `bin/`, `obj/`, references, test server state,
  logs, world files, configs, and credentials.
- [ ] **Verify:** Save assembly-inspection notes, exact reference paths, the
  ETA evidence table, and the co-installation decision in the repository docs.

### Phase 1 - Buildable client-only skeleton and native hover ownership

- [ ] Create `CatosHoverInspector.csproj` targeting `net48` with separate
  game-managed and BepInEx-core references.
- [ ] Implement `Plugin.cs` with BepInEx metadata, the
  `[BepInProcess("valheim.exe")]` guard, config binding, Harmony lifecycle,
  and bounded startup/shutdown logging.
- [ ] Implement `ModConfig.cs` with safe defaults and validation for all MVP
  display, cadence, inspector, warning, and ETA options.
- [ ] Implement `HoverTargetController` using the native hover object and
  native range/lifecycle behavior.
- [ ] Implement `HoverOverlay` so only this plugin's owned text is applied and
  all state is cleared on invalid target, scene transition, disable, and
  shutdown.
- [ ] Implement `InspectionContext`, `InspectionResult`, `DisplayLine`, and
  `EtaDescriptor` as detached display models.
- [ ] Implement `InspectorRegistry` with priority selection and per-inspector
  exception isolation.
- [ ] Implement `HoverTextFormatter` and `EtaFormatter` with bounded lines,
  character count, localization fallback, rich-text safety, and stable
  countdown rounding.
- [ ] Add a no-op/fallback path that leaves vanilla hover text unchanged for
  unsupported objects.
- [ ] Add `scripts/setup-references.ps1`, `scripts/build.ps1`, and a release
  packaging script following the sibling project conventions.
- [ ] **Acceptance:** The plugin builds as a client-only DLL and can apply a
  controlled test line to a known supported target without a second raycast or
  gameplay mutation.
- [ ] **Verify:** Build output, client load log, server non-load evidence, and
  target-change/scene-change cleanup evidence are captured.

### Phase 2 - Chest parity and static station inspection

- [ ] Port the CatosChestViewer read-only container behavior into
  `ChestInspector` without adding a binary dependency on CatosChestViewer.
- [ ] Reuse native privacy and guard-stone access checks; fail closed when the
  check cannot be proven.
- [ ] Add `CraftingStationInspector` for workbench and forge level/status data
  supported by Phase 0 evidence.
- [ ] Add extension detection only when the runtime relationship is reliable;
  label it as `detected` rather than implying a maximum level that is unknown.
- [ ] Add stable fingerprints for chest content and station state.
- [ ] Add tests for empty, inaccessible, destroyed, modded-compatible, and
  rapidly changing containers.
- [ ] **Acceptance:** A clean client can look at chests, workbenches, and forges
  and see accurate bounded information while the native interaction behavior
  remains unchanged.
- [ ] **Verify:** Manual client/server smoke test against the configured
  `Dedicated` world, client log review, and before/after inventory/state audit.

### Phase 3 - Processing stations and first-class ETA support

- [ ] Implement `NativeTimeReader` behind an explicit evidence-based adapter
  boundary.
- [ ] Implement `ProcessingInspector` for smelters and kilns.
- [ ] Add input, fuel, output, capacity, active/paused/blocked/ready states,
  next-output ETA, and batch-complete ETA where proven.
- [ ] Implement fermenter inspection with recipe/status and `Ready in` ETA.
- [ ] Implement windmill inspection with active/paused state, progress, output,
  and ETA where native wind/progress semantics are verified.
- [ ] Add production fingerprints that include only fields required to refresh
  display and countdown state.
- [ ] Ensure a full output slot pauses or blocks ETA rather than allowing an
  inaccurate countdown to continue.
- [ ] Add fake/pure tests for ETA formatting, negative/zero values, paused jobs,
  output-full states, long durations, and countdown rounding.
- [ ] **Acceptance:** Smelter, kiln, fermenter, and windmill hover text shows
  reliable live ETA information when active and honest paused/blocked/fallback
  text in every unsupported or stopped state.
- [ ] **Verify:** Time a known job from displayed ETA to native completion,
  capture before/after screenshots, inspect logs for exceptions, and confirm
  no fuel/input/output mutation occurred.

### Phase 4 - Fires, cooking stations, and beehives

- [ ] Implement `FireInspector` with fuel/activity status and shelter/heat data
  only where native fields are verified.
- [ ] Keep fire fuel ETA separate from cooking-item ETA in both data and text.
- [ ] Implement `CookingStationInspector` for active slots, ready food, queue
  capacity, and next-food ETA where reliable.
- [ ] Implement `BeehiveInspector` with honey count/capacity and producing,
  paused, full, or ready status.
- [ ] Add next-honey and full-hive ETA support using native timer/progress data
  only; do not extrapolate from arbitrary polling unless Phase 0 approved it.
- [ ] Test beehives under empty, producing, full, sheltered/unsheltered, and
  harvest-ready conditions.
- [ ] **Acceptance:** Fires, cooking stations, and beehives show useful live
  status and ETAs without confusing fuel, food, and honey timers.
- [ ] **Verify:** Conduct timed in-game comparisons, test pause/resume/full
  behavior, inspect client logs, and confirm no harvest/cooking/fuel mutation.

### Phase 5 - Portal diagnostics

- [ ] Implement `PortalInspector` for tag and native link state.
- [ ] Add paired destination name only when the destination can be identified
  through legitimate local/native data.
- [ ] Add bounded duplicate-tag detection without an unrestricted scene/world
  scan every frame.
- [ ] Add clear/unpaired/blocked/unavailable status labels with safe fallback.
- [ ] Test portal tags containing unusual characters and rich-text-sensitive
  text.
- [ ] Test paired, unpaired, duplicate-tag, destination-missing, and
  teleport-restriction cases where the native runtime exposes them.
- [ ] **Acceptance:** Looking at a portal diagnoses the common route states and
  never changes tags, links, teleport state, or world data.
- [ ] **Verify:** Two-portal manual matrix, log review, target invalidation test,
  and state comparison before/after inspection.

### Phase 6 - BuildSight expansion

- [ ] Gate BuildSight behind its own disabled-by-default config setting.
- [ ] Verify how the native build ghost, `Piece`, `Recipe`, requirements, and
  player inventory can be read without invoking placement or consumption.
- [ ] Implement `BuildSightInspector` for missing materials, owned materials,
  buildability, and required station where reliable.
- [ ] Keep BuildSight target resolution separate from ordinary placed-object
  inspection and respect native building rules.
- [ ] Add bounded output for large/modded recipes and safe fallback for missing
  requirements.
- [ ] **Acceptance:** BuildSight shows an accurate read-only checklist when
  enabled and has no effect when disabled; placing a piece behaves exactly as
  vanilla.
- [ ] **Verify:** Test sufficient/insufficient materials, missing station,
  invalid ghost, modded recipe fallback, placement, cancellation, and inventory
  before/after state.

### Phase 7 - Hardening, compatibility, and performance

- [ ] Add rate-limited diagnostics for unsupported component, timer, and
  formatter failures.
- [ ] Profile target refreshes, ETA redraws, allocations, and log volume with
  multiple nearby processors and players.
- [ ] Test scene transitions, reconnects, destroyed targets, disabled config,
  HUD absence, and plugin shutdown.
- [ ] Test with a clean client and with the intended CatoHeim client profile,
  documenting unrelated mod interactions.
- [ ] Run the explicit CatosChestViewer co-installation test and implement the
  documented compatibility outcome.
- [ ] Confirm client-only process guard and ensure the launcher never deploys
  the DLL to the dedicated server.
- [ ] **Acceptance:** No stale inspector text, repeated exception spam, visible
  frame hitch, unauthorized state mutation, or unsafe fallback remains in the
  supported matrix.
- [ ] **Verify:** Retain client/server logs, performance notes, compatibility
  results, and the completed verification matrix.

### Phase 8 - Packaging and release gate

- [ ] Create Thunderstore `manifest.json`, README, icon, changelog, and package
  layout with the client-only installation instructions.
- [ ] Document supported Valheim/BepInEx versions and the fact that the
  dedicated server does not install the plugin.
- [ ] Document ETA semantics, rounding, paused/blocked behavior, and known
  unsupported processors.
- [ ] Document the CatosChestViewer relationship and clean-profile testing
  requirement.
- [ ] Build the release DLL and package from a clean checkout or controlled
  build directory.
- [ ] Confirm the archive contains no references, credentials, logs, worlds,
  `bin/`, `obj/`, or test-server runtime state.
- [ ] **Acceptance:** The package installs into a clean client profile, loads
  on the target version, and passes the full gameplay/failure matrix.
- [ ] **Verify:** Record archive contents, DLL metadata, client load log,
  dedicated-server non-load evidence, and final manual smoke-test results.

## 10. Open tuning points

- [ ] Phase 0 owner: confirm exact native component names and timer fields for
  every planned object type.
- [ ] Phase 0 owner: choose the final game-time/network-time basis for ETA
  calculations and record proof from pause/resume tests.
- [ ] Phase 1 owner: decide whether the default display replaces the vanilla
  hover body or preserves it and appends inspector lines. The current
  preference is to preserve the native object name and append bounded details.
- [ ] Phase 1 owner: tune default `MaxLines`, `MaxTextCharacters`, and ETA
  refresh cadence after observing actual hover readability.
- [ ] Phase 3 owner: decide whether `Batch complete` is enabled by default if
  a processor's queued input is visible but future slots are ambiguous.
- [ ] Phase 4 owner: decide whether unavailable ETA is omitted silently or
  shown as a configurable diagnostic line. Default preference is omission.
- [ ] Phase 5 owner: decide how much portal destination information can be
  shown without a broader portal scan or privacy concern.
- [ ] Phase 6 owner: decide whether BuildSight remains part of this plugin or
  becomes a separate `CatosBuildSight` plugin if its target/recipe hooks become
  substantially more complex.
- [ ] Release owner: decide whether CatosHoverInspector formally supersedes
  CatosChestViewer or whether both remain separate, documented products.
