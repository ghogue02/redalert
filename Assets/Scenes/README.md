# Skirmish_Graybox (Week 3)

Extends Week 2 with AI (Standard bot), Minimap, and Alerts polish. Unity 2022.3.40f1 LTS, URP 14. WebGL-friendly: no Addressables, no new packages. FoW remains OFF.

## 1) Scene Prep (carry from Week 2)
- Retain `GameSystems` with:
  - `SelectionSystem` (Assets/Scripts/Units/SelectionSystem.cs)
  - `CommandSystem` (Assets/Scripts/Units/CommandSystem.cs)
  - `PlayerEconomy` (Assets/Scripts/Economy/PlayerEconomy.cs)
  - `HUDController` (Assets/Scripts/UI/HUDController.cs)
  - `EventBus` (Assets/Scripts/Core/EventBus.cs)
  - `UpdateDriver` (Assets/Scripts/Core/UpdateDriver.cs)

## 2) Player Building System (Week 4 additions)
1. Add `BuildPlacementController` (Assets/Scripts/Build/BuildPlacementController.cs) to `GameSystems`:
   - Assign:
     - `GhostPreview` and `PlacementValidator`
     - Building prefabs: Refinery, Factory, HQ (optional)
     - `PlayerEconomy`
   - Usage:
     - UI `BuildMenuPanel` begins placement.
     - LMB confirms if valid; RMB cancels.
     - On Factory placement, creates Exit/Rally if missing and wires to `BuildQueue`.

2. Add `BuildMenuPanel` (Assets/Scripts/UI/BuildMenuPanel.cs) to HUD Canvas:
   - Buttons: "Place Refinery", "Place Factory".
   - Bind to `BuildPlacementController.BeginPlacement(...)`.
   - Buttons disabled while active; costs from `PlacementRules`.

3. Add `RallyController` (Assets/Scripts/Build/RallyController.cs) to `GameSystems`:
   - When Factory selected and rally mode toggled, next RMB sets rally Transform under Factory.

## 3) Minimap
1. Create a Canvas child `Minimap` with:
   - An `Image` as the minimap background (RectTransform is the clickable area)
   - Under it, a child RectTransform `Blips` for icon instances
   - Add `MinimapController` (Assets/Scripts/UI/MinimapController.cs):
     - Minimap Rect: assign the background Image RectTransform
     - Blip Parent: assign `Blips`
     - Blip Prototype: create a small Image with an atlas sprite; disable it; assign here
     - World Camera: main camera
     - World Bounds: set worldMin/worldMax to your playable area (X,Z)
     - Command System: assign scene CommandSystem
   - Behavior:
     - Left-click inside minimap recenters main camera.
     - Right-click inside minimap issues move or attack-move (if A is held).
     - Blips update at SlowTick cadence using pooled UI Images.

2. Hook providers on Units/Buildings:
   - Implement `IMinimapIconProvider` on units/structures and register/unregister on enable/disable.

## 4) Alerts Polish
- `AlertsPanel` remains as in Week 3.

## 5) Testing Checklist (Week 3 + Building/Rally + Balance)
- Player Building:
  - Click "Place Refinery" or "Place Factory" in BuildMenuPanel.
  - Move ghost to a valid pad; LMB to place. If insufficient resources, an alert appears and placement does not start.
  - Factory auto-creates Exit/Rally if missing and assigns to its `BuildQueue`.
- Rally:
  - Select Factory, toggle Rally mode in HUD, then RMB on ground to set rally. New units will move to that point on spawn.
- Production:
  - Use production panel or a test button wired to `BuildQueue.TryEnqueueById("BasicVehicle", prefab)` to queue a unit.
  - Cancel-last refunds the last item's cost.
- Balance targets:
  - Economy pacing: Refinery 1200/25s, Factory 1000/30s, BasicVehicle 375/19s.
  - Wave pacing: 4–6 units form around ~2.5–3.0 minutes with StandardBot default WaveWindowSeconds (150–180s).
  - Unit survivability: BasicVehicle ~5–7s TTK under focus fire with SmallArms baseline.
- AI:
  - StandardBot uses PlacementRules costs/times and exposes simple scaling via serialized multipliers. Optionally initialize scaling on scene start.
- Minimap/Alerts: as Week 3 list.

## 6) WebGL Notes
- Non-alloc where applicable; 4 Hz slow ticks.
- FoW OFF.
</content>