using System;
using System.Collections.Generic;
using Godot;
using Omerta.Domain.Missions;
using Omerta.GodotAdapters.Businesses;
using Omerta.GodotAdapters.Camera;
using Omerta.GodotAdapters.Characters;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Economy;
using Omerta.GodotAdapters.Missions;
using Omerta.GodotAdapters.Navigation;
using Omerta.GodotAdapters.StageFive;
using Omerta.GodotAdapters.Territories;
using Omerta.GodotAdapters.Interaction;
using Omerta.Domain.Combat.Weapons;
using Omerta.GodotAdapters.Vehicles;
using Omerta.Presentation;

namespace Omerta.GodotAdapters.Diagnostics;

public sealed partial class PlaytestDiagnosticsController : Node
{
    private static readonly StringName ToggleDiagnostics = "toggle_diagnostics";

    private HudController? _hud;
    private PlayerController? _player;
    private IsometricCameraController? _camera;
    private StageFiveController? _stage;
    private WorkshopController? _workshop;
    private BarLaNieblaController? _bar;
    private DepotSanTelmoController? _depot;
    private TerritoryController? _territory;
    private SabotageMissionController? _sabotageMission;
    private EconomyController? _economy;
    private HitscanWeaponController? _weapon;
    private PlayerWeaponLoadoutController? _weaponLoadout;
    private VehicleArsenalInteraction? _vehicleArsenal;
    private DriveableVehicleController? _stolenVehicle;
    private GarageRegistrationInteraction? _vehicleGarage;
    private SharedNavigationWorld? _navigation;
    private bool _visible;
    private bool _runSmokeTest;
    private bool _runNavigationSmokeTest;
    private bool _runCrowdSmokeTest;
    private bool _runCameraSmokeTest;
    private bool _runTraversalSmokeTest;
    private bool _runBarSabotageSmokeTest;
    private bool _runTerritorySmokeTest;
    private bool _runDefenseMissionSmokeTest;
    private bool _runWeaponLoadoutSmokeTest;
    private bool _runVehicleOwnershipSmokeTest;
    private int _barSabotageSmokeStage;
    private int _barSabotageWaitFrames = 120;
    private int _barBalanceBeforeSabotage;
    private string _barOwnerBeforeSabotage = string.Empty;
    private int _traversalSmokeStage;
    private int _traversalSmokeObservationFrames = 90;
    private float _traversalStartZ;
    private bool _cameraSmokePositioned;
    private int _cameraSmokeObservationFrames = 90;
    private readonly List<NavigationCrowdProbe> _crowdProbes = new();
    private bool _crowdSpawned;
    private int _crowdFramesRemaining = 480;
    private float _minimumCrowdDistance = float.MaxValue;
    private ulong _crowdStartedAt;
    private int _smokeFramesRemaining = 120;
    private float _refreshRemaining;

    public override void _Ready()
    {
        AddToGroup("playtest_diagnostics");
        ResolveDependencies();
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument == "--stage-six-smoke")
            {
                _runSmokeTest = true;
            }
            else if (argument == "--navigation-smoke")
            {
                _runNavigationSmokeTest = true;
                _smokeFramesRemaining = 180;
            }
            else if (argument == "--crowd-smoke")
            {
                _runCrowdSmokeTest = true;
                _smokeFramesRemaining = 180;
            }
            else if (argument == "--camera-occlusion-smoke")
            {
                _runCameraSmokeTest = true;
                _smokeFramesRemaining = 180;
            }
            else if (argument == "--traversal-smoke")
            {
                _runTraversalSmokeTest = true;
                _smokeFramesRemaining = 120;
            }
            else if (argument == "--bar-sabotage-smoke")
            {
                _runBarSabotageSmokeTest = true;
                _smokeFramesRemaining = 120;
            }
            else if (argument == "--territory-smoke")
            {
                _runTerritorySmokeTest = true;
                _smokeFramesRemaining = 120;
            }
            else if (argument == "--defense-mission-smoke")
            {
                _runDefenseMissionSmokeTest = true;
                _smokeFramesRemaining = 120;
            }
            else if (argument == "--weapon-loadout-smoke")
            {
                _runWeaponLoadoutSmokeTest = true;
                _smokeFramesRemaining = 120;
            }
            else if (argument == "--vehicle-ownership-smoke")
            {
                _runVehicleOwnershipSmokeTest = true;
                _smokeFramesRemaining = 120;
            }
        }
    }

    public override void _Process(double delta)
    {
        ResolveDependencies();
        if (Godot.Input.IsActionJustPressed(ToggleDiagnostics))
        {
            _visible = !_visible;
            RefreshOverlay();
        }

        _refreshRemaining -= (float)delta;
        if (_visible && _refreshRemaining <= 0.0f)
        {
            _refreshRemaining = 0.25f;
            RefreshOverlay();
        }

        if (_runSmokeTest && --_smokeFramesRemaining <= 0)
        {
            _runSmokeTest = false;
            RunStageSixSmokeTest();
        }
        else if (_runNavigationSmokeTest && --_smokeFramesRemaining <= 0)
        {
            _runNavigationSmokeTest = false;
            RunNavigationSmokeTest();
        }
        else if (_runCrowdSmokeTest)
        {
            AdvanceCrowdSmokeTest();
        }
        else if (_runCameraSmokeTest)
        {
            AdvanceCameraOcclusionSmokeTest();
        }
        else if (_runTraversalSmokeTest)
        {
            AdvanceTraversalSmokeTest();
        }
        else if (_runBarSabotageSmokeTest)
        {
            AdvanceBarSabotageSmokeTest();
        }
        else if (_runTerritorySmokeTest && --_smokeFramesRemaining <= 0)
        {
            _runTerritorySmokeTest = false;
            RunTerritorySmokeTest();
        }
        else if (_runDefenseMissionSmokeTest && --_smokeFramesRemaining <= 0)
        {
            _runDefenseMissionSmokeTest = false;
            RunDefenseMissionSmokeTest();
        }
        else if (_runWeaponLoadoutSmokeTest && --_smokeFramesRemaining <= 0)
        {
            _runWeaponLoadoutSmokeTest = false;
            RunWeaponLoadoutSmokeTest();
        }
        else if (_runVehicleOwnershipSmokeTest && --_smokeFramesRemaining <= 0)
        {
            _runVehicleOwnershipSmokeTest = false;
            RunVehicleOwnershipSmokeTest();
        }
    }

    private void ResolveDependencies()
    {
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        _player ??= GetTree().GetFirstNodeInGroup("player") as PlayerController;
        _camera ??= GetTree().GetFirstNodeInGroup("camera_rig") as IsometricCameraController;
        _stage ??= GetTree().GetFirstNodeInGroup("stage_five") as StageFiveController;
        _workshop ??= GetTree().GetFirstNodeInGroup("workshop") as WorkshopController;
        _bar ??= GetTree().GetFirstNodeInGroup("la_niebla") as BarLaNieblaController;
        _depot ??= GetTree().GetFirstNodeInGroup("san_telmo") as DepotSanTelmoController;
        _territory ??= GetTree().GetFirstNodeInGroup("territory") as TerritoryController;
        _sabotageMission ??= GetTree().GetFirstNodeInGroup("sabotage_mission") as SabotageMissionController;
        _economy ??= GetTree().GetFirstNodeInGroup("economy") as EconomyController;
        _weapon ??= _player?.GetNodeOrNull<HitscanWeaponController>("WeaponController");
        _weaponLoadout ??= _player?.GetNodeOrNull<PlayerWeaponLoadoutController>("WeaponLoadout");
        Node3D? familyVehicle = GetTree().GetFirstNodeInGroup("family_vehicle") as Node3D;
        _vehicleArsenal ??= familyVehicle?.GetNodeOrNull<VehicleArsenalInteraction>("ArsenalTrunk");
        _vehicleGarage ??= GetTree().GetFirstNodeInGroup("vehicle_garage")
            as GarageRegistrationInteraction;
        if (_stolenVehicle is null)
        {
            foreach (Node node in GetTree().GetNodesInGroup("driveable_vehicle"))
            {
                if (node is DriveableVehicleController vehicle && vehicle.PersistState)
                {
                    _stolenVehicle = vehicle;
                    break;
                }
            }
        }

        _navigation ??= GetTree().GetFirstNodeInGroup("shared_navigation") as SharedNavigationWorld;
    }

    private void RunWeaponLoadoutSmokeTest()
    {
        int exitCode = 1;
        try
        {
            ResolveDependencies();
            Require(_weaponLoadout is not null, "player weapon loadout");
            Require(_vehicleArsenal is not null, "family vehicle arsenal trunk");
            Require(_weaponLoadout!.EquippedWeaponId == PlayerWeaponLoadoutController.PistolId,
                "pistol equipped at start");
            Require(!_weaponLoadout.IsLongGunCarried, "shotgun begins inside the family vehicle");
            Require(!_weaponLoadout.TryEquipLongGun(), "stored shotgun cannot be selected");

            DriveableVehicleController vehicle =
                _vehicleArsenal!.GetParent<DriveableVehicleController>();
            vehicle.Visible = true;
            vehicle.SetDriveEnabled(true);
            Require(_vehicleArsenal.CanInteract, "visible family trunk can be used");
            _vehicleArsenal.Interact();
            Require(_weaponLoadout.IsLongGunCarried, "shotgun transferred to long-gun slot");
            Require(_weaponLoadout.TryEquipLongGun(), "carried shotgun can be selected");
            Require(_weaponLoadout.EquippedRuntime?.Specification.PelletCount == 8,
                "Unity shotgun pellet parity");
            int ammunitionBefore = _weaponLoadout.EquippedRuntime!.AmmoInMagazine;
            Require(_weaponLoadout.EquippedRuntime.TryFire(0.0), "shotgun runtime fires");
            Require(_weaponLoadout.EquippedRuntime.AmmoInMagazine == ammunitionBefore - 1,
                "one shell consumed for all pellets");

            _weaponLoadout.ToggleHolster();
            Require(_weaponLoadout.IsHolstered, "weapon can be holstered");
            _vehicleArsenal.Interact();
            Require(!_weaponLoadout.IsLongGunCarried, "shotgun returned to trunk");
            Require(_weaponLoadout.LongGunController.Runtime.AmmoInMagazine == ammunitionBefore - 1,
                "trunk storage preserves ammunition");

            GD.Print(
                "Weapon loadout smoke passed: limited slots, holster, shotgun pellets and " +
                "family-vehicle trunk transfer verified.");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            GD.PushError($"Weapon loadout smoke failed: {exception}");
        }
        finally
        {
            GetTree().Quit(exitCode);
        }
    }

    private void RunVehicleOwnershipSmokeTest()
    {
        int exitCode = 1;
        try
        {
            ResolveDependencies();
            Require(_player is not null, "player");
            Require(_stolenVehicle is not null, "persistent stolen street car");
            Require(_vehicleGarage is not null, "El Faro registration garage");
            Require(_stage is not null, "police response controller");
            Require(_workshop is not null, "El Faro workshop");
            Require(_economy is not null, "economy");
            Require(!_stolenVehicle!.IsFamilyOwned, "street car begins stolen");
            VehicleArsenalInteraction trunk =
                _stolenVehicle.GetNode<VehicleArsenalInteraction>("ArsenalTrunk");
            Require(!trunk.CanInteract, "stolen car cannot access family arsenal");

            _stolenVehicle.GlobalPosition = _vehicleGarage!.GlobalPosition + Vector3.Forward * 2.0f;
            Require(_stolenVehicle.TryEnterDriver(_player!), "player enters stolen vehicle");
            Require(_player!.IsDriving && _stolenVehicle.IsDriven, "driver state synchronized");
            Require(_stolenVehicle.IsPoliceMarked, "nearby witness reports vehicle theft");
            _stolenVehicle.ForceExitDriver();
            Require(!_player.IsDriving && !_stolenVehicle.IsDriven, "player exits vehicle");
            Require(_vehicleGarage.FindRegistrationCandidate() is null,
                "garage rejects a police-marked vehicle");
            Require(_stage!.EscapeVehiclePursuitForDiagnostics(),
                "vehicle pursuit ends after losing police contact");
            Require(!_stolenVehicle.IsPoliceMarked, "escape clears police vehicle mark");

            _workshop!.RestorePersistentState(
                WorkshopController.PlayerFactionId,
                true,
                _workshop.MerchantHealth,
                false);
            _economy!.RestoreBalance(100);
            int balanceBefore = _economy.Economy.Balance;
            Require(_vehicleGarage.FindRegistrationCandidate() == _stolenVehicle,
                "garage detects parked stolen car");
            _vehicleGarage.Interact();
            Require(_stolenVehicle.IsFamilyOwned, "garage registers car as family owned");
            Require(
                _economy.Economy.Balance == balanceBefore - _vehicleGarage.RegistrationCost,
                "garage charges registration exactly once");
            Require(trunk.CanInteract, "registered car unlocks shared family arsenal");

            GD.Print(
                "Vehicle ownership smoke passed: theft report, pursuit escape, enter/exit, " +
                "El Faro registration payment and family arsenal unlock verified.");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            GD.PushError($"Vehicle ownership smoke failed: {exception}");
        }
        finally
        {
            GetTree().Quit(exitCode);
        }
    }

    private void RefreshOverlay()
    {
        if (_hud is null)
        {
            return;
        }

        int nodeCount = (int)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
        string text = _player is null || _camera is null || _stage is null
            ? "DIAGNÓSTICO · esperando dependencias"
            : $"DIAGNÓSTICO ETAPA 6\n" +
              $"FPS {Engine.GetFramesPerSecond()} · NODOS {nodeCount} · FÍSICA {Engine.PhysicsTicksPerSecond} Hz\n" +
              $"JUGADOR {_player.CurrentPlanarSpeed:0.00} m/s · CÁMARA {_camera.CurrentDistance:0.0} m / {_camera.CurrentFieldOfView:0.0}°\n" +
              $"TRAVERSAL {(_player.IsTraversing ? "VAULT" : _player.IsInCover ? _player.CurrentCoverType.ToString().ToUpperInvariant() : "LIBRE")}\n" +
              $"MISIÓN {_stage.MissionStatus}";
        _hud.SetDiagnostics(text, _visible);
    }

    private void RunStageSixSmokeTest()
    {
        int exitCode = 1;
        try
        {
            ResolveDependencies();
            Require(_player is not null, "player");
            Require(_camera?.Camera is not null, "isometric camera");
            Require(_stage?.HasRequiredRuntimeActors == true, "stage 5 runtime actors");
            Require(_workshop?.Business is not null, "El Faro business");
            Require(_bar?.Business is not null, "Bar La Niebla business");
            Require(_depot?.Business is not null, "Depósito San Telmo business");
            Require(_territory?.State is not null, "South District territory");
            Require(_sabotageMission is not null, "La Niebla sabotage mission");
            Require(_weapon?.Runtime is not null, "player weapon");
            Require(GetTree().GetNodesInGroup("companion").Count == 1, "one initial companion");
            Require(GetTree().GetNodesInGroup("reserve_companion").Count == 2, "two reserve allies");
            Require(Mathf.IsEqualApprox(_player!.MovementSpeed, 5.0f), "Unity movement parity");
            Require(Mathf.IsEqualApprox(_camera!.Distance, 18.0f), "Unity camera distance parity");
            Require(Mathf.IsEqualApprox(_camera.PitchDegrees, 58.0f), "Unity camera pitch parity");
            Require(Mathf.IsEqualApprox(_camera.DefaultFieldOfView, 50.0f), "Unity FOV parity");
            Require(_weapon!.Runtime.Specification.Damage == 25, "Unity pistol damage parity");
            Require(Mathf.IsEqualApprox(
                _weapon.Runtime.Specification.FireIntervalSeconds,
                0.35f), "Unity fire interval parity");

            int nodeCount = (int)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
            GD.Print(
                $"Stage 6 smoke passed: composition, reference parameters and 120-frame stability " +
                $"verified ({nodeCount} nodes, {Engine.GetFramesPerSecond()} FPS reported).");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            GD.PushError($"Stage 6 smoke failed: {exception}");
        }
        finally
        {
            GetTree().Quit(exitCode);
        }
    }

    private void RunNavigationSmokeTest()
    {
        int exitCode = 1;
        try
        {
            ResolveDependencies();
            Require(_navigation?.IsSynchronized == true, "synchronized navigation map");

            Vector3[] companionPath = _navigation!.QueryPath(
                new Vector3(-2.0f, 0.05f, 5.0f),
                new Vector3(0.0f, 0.05f, -3.2f),
                SharedNavigationWorld.PedestrianLayer);
            Vector3[] counterattackPath = _navigation.QueryPath(
                new Vector3(-17.0f, 0.05f, 15.0f),
                new Vector3(0.0f, 0.05f, -3.4f),
                SharedNavigationWorld.PedestrianLayer);
            Vector3[] reinforcementPath = _navigation.QueryPath(
                new Vector3(0.0f, 0.45f, 24.0f),
                new Vector3(0.0f, 0.45f, 8.0f),
                SharedNavigationWorld.VehicleLayer);
            Vector3[] policePath = _navigation.QueryPath(
                new Vector3(18.0f, 0.45f, 18.0f),
                new Vector3(3.5f, 0.45f, 4.5f),
                SharedNavigationWorld.VehicleLayer);

            GD.Print(
                $"Navigation route points: companion={companionPath.Length}, " +
                $"counterattack={counterattackPath.Length}, reinforcement={reinforcementPath.Length}, " +
                $"police={policePath.Length} [{string.Join("; ", policePath)}]");

            Require(companionPath.Length >= 2, "companion pedestrian route");
            Require(counterattackPath.Length >= 2, "counterattack pedestrian route");
            Require(reinforcementPath.Length >= 2, "reinforcement vehicle route");
            Require(policePath.Length >= 3, "police vehicle street turn");

            ulong startedAt = Time.GetTicksUsec();
            int routePointCount = 0;
            int[] loadSteps = { 8, 16, 32, 64 };
            foreach (int routeCount in loadSteps)
            {
                for (int index = 0; index < routeCount; index++)
                {
                    bool vehicle = index % 2 == 0;
                    Vector3[] path = vehicle
                        ? _navigation.QueryPath(
                            new Vector3(18.0f, 0.45f, 18.0f),
                            new Vector3(3.5f, 0.45f, 4.5f),
                            SharedNavigationWorld.VehicleLayer)
                        : _navigation.QueryPath(
                            new Vector3(-17.0f, 0.05f, 15.0f),
                            new Vector3(0.0f, 0.05f, -3.4f),
                            SharedNavigationWorld.PedestrianLayer);
                    Require(path.Length >= 2, $"load step {routeCount}, route {index + 1}");
                    routePointCount += path.Length;
                }
            }

            ulong elapsedUsec = Time.GetTicksUsec() - startedAt;
            GD.Print(
                $"Navigation smoke passed: pedestrian and vehicle layers, street turn and " +
                $"8/16/32/64 route-query load verified ({routePointCount} total points in " +
                $"{elapsedUsec / 1000.0:0.00} ms).");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            GD.PushError($"Navigation smoke failed: {exception}");
        }
        finally
        {
            GetTree().Quit(exitCode);
        }
    }

    private void AdvanceCrowdSmokeTest()
    {
        if (!_crowdSpawned)
        {
            if (--_smokeFramesRemaining > 0)
            {
                return;
            }

            try
            {
                ResolveDependencies();
                Require(_navigation?.IsSynchronized == true, "synchronized navigation map");
                RequireProductionAvoidanceConfiguration();
                Node scene = GetTree().CurrentScene ?? throw new InvalidOperationException(
                    "The current scene is unavailable for the crowd smoke test.");
                for (int lane = 0; lane < 10; lane++)
                {
                    float z = 2.5f + lane * 2.2f;
                    AddCrowdProbe(
                        scene,
                        new Vector3(-8.0f, 0.05f, z),
                        new Vector3(8.0f, 0.05f, z));
                    AddCrowdProbe(
                        scene,
                        new Vector3(8.0f, 0.05f, z),
                        new Vector3(-8.0f, 0.05f, z));
                }

                _crowdStartedAt = Time.GetTicksMsec();
                _crowdSpawned = true;
                GD.Print("Crowd smoke started: 20 active pedestrian navigation agents.");
            }
            catch (Exception exception)
            {
                FinishCrowdSmokeTest(exception);
            }

            return;
        }

        _minimumCrowdDistance = Mathf.Min(_minimumCrowdDistance, GetMinimumCrowdDistance());
        if (--_crowdFramesRemaining > 0)
        {
            return;
        }

        try
        {
            Require(_crowdProbes.Count == 20, "20 crowd probes");
            int safeVelocitySamples = 0;
            int deflectionSamples = 0;
            float distanceTravelled = 0.0f;
            foreach (NavigationCrowdProbe probe in _crowdProbes)
            {
                Require(
                    probe.SafeVelocitySamples >= 120,
                    $"avoidance callbacks for {probe.Name} " +
                    $"(received {probe.SafeVelocitySamples})");
                safeVelocitySamples += probe.SafeVelocitySamples;
                deflectionSamples += probe.DeflectionSamples;
                distanceTravelled += probe.DistanceTravelled;
            }

            Require(deflectionSamples > 0, "at least one avoidance correction");
            Require(distanceTravelled >= 100.0f, "crowd movement under load");
            Require(_minimumCrowdDistance >= 0.35f, "crowd separation");
            double elapsedSeconds = (Time.GetTicksMsec() - _crowdStartedAt) / 1000.0;
            int nodeCount = (int)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
            GD.Print(
                $"Crowd smoke passed: 20 active NPC probes, {safeVelocitySamples} safe-velocity " +
                $"callbacks, {deflectionSamples} avoidance corrections and " +
                $"{distanceTravelled:0.0} m travelled in {elapsedSeconds:0.00} s " +
                $"(minimum separation {_minimumCrowdDistance:0.00} m, {nodeCount} nodes, " +
                $"{Engine.GetFramesPerSecond()} FPS reported).");
            FinishCrowdSmokeTest();
        }
        catch (Exception exception)
        {
            FinishCrowdSmokeTest(exception);
        }
    }

    private void AdvanceCameraOcclusionSmokeTest()
    {
        if (!_cameraSmokePositioned)
        {
            if (--_smokeFramesRemaining > 0)
            {
                return;
            }

            ResolveDependencies();
            if (_player is null)
            {
                GD.PushError("Camera occlusion smoke failed: player is unavailable.");
                GetTree().Quit(1);
                return;
            }

            _player.GlobalPosition = new Vector3(50.0f, 0.25f, 0.0f);
            _cameraSmokePositioned = true;
            return;
        }

        if (--_cameraSmokeObservationFrames > 0)
        {
            return;
        }

        _runCameraSmokeTest = false;
        RunCameraOcclusionSmokeTest();
    }

    private void RunCameraOcclusionSmokeTest()
    {
        int exitCode = 1;
        try
        {
            ResolveDependencies();
            Require(_camera is not null, "isometric camera controller");
            Require(_workshop is not null, "workshop for roof fade validation");
            Require(
                GetTree().GetNodesInGroup("camera_fade_occluder").Count == 8,
                "two building and six business roof fade occluders");
            Require(_camera!.FadedGeometryCount >= 1, "runtime roof fade selection");
            MeshInstance3D interiorRoof = _workshop!.GetNode<MeshInstance3D>(
                "Interior/Roof/Mesh");
            Require(interiorRoof.Transparency >= 0.5f, "runtime roof transparency");

            CameraOcclusionProbe wallProbe = _camera.ProbeOcclusion(
                new Vector3(50.0f, 1.0f, 0.0f),
                new Vector3(50.0f, 1.0f, -10.0f));
            Require(wallProbe.HasCollision, "interior wall camera collision");
            Require(
                wallProbe.LimitedDistance < wallProbe.RequestedDistance - 1.0f,
                "camera arm shortening");

            CameraOcclusionProbe closeWallProbe = _camera.ProbeOcclusion(
                new Vector3(50.0f, 1.0f, -4.3f),
                new Vector3(50.0f, 1.0f, -10.0f));
            Require(closeWallProbe.HasCollision, "close wall camera collision");
            Require(
                closeWallProbe.LimitedDistance <= 0.11f,
                "close wall clipping prevention");

            CameraOcclusionProbe roofProbe = _camera.ProbeOcclusion(
                new Vector3(50.0f, 1.0f, 0.0f),
                new Vector3(50.0f, 10.0f, 0.0f));
            Require(roofProbe.FadeOccluderCount >= 1, "interior roof fading");

            CameraOcclusionProbe buildingProbe = _camera.ProbeOcclusion(
                new Vector3(10.0f, 2.0f, 7.0f),
                new Vector3(20.0f, 2.0f, 7.0f));
            Require(buildingProbe.FadeOccluderCount >= 1, "city building fading");

            CameraOcclusionProbe clearProbe = _camera.ProbeOcclusion(
                new Vector3(30.0f, 1.0f, 30.0f),
                new Vector3(30.0f, 10.0f, 40.0f));
            Require(!clearProbe.HasCollision, "clear camera arm");
            Require(clearProbe.FadeOccluderCount == 0, "clear camera sightline");

            GD.Print(
                $"Camera occlusion smoke passed: arm shortened from " +
                $"{wallProbe.RequestedDistance:0.00} m to {wallProbe.LimitedDistance:0.00} m, " +
                $"roof/building fade layers detected and clear sightline preserved.");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            GD.PushError($"Camera occlusion smoke failed: {exception}");
        }
        finally
        {
            GetTree().Quit(exitCode);
        }
    }

    private void AdvanceTraversalSmokeTest()
    {
        try
        {
            ResolveDependencies();
            Require(_player is not null, "player for traversal smoke");
            if (_traversalSmokeStage == 0)
            {
                if (--_smokeFramesRemaining > 0)
                {
                    return;
                }

                _player!.GlobalPosition = new Vector3(-5.0f, 0.02f, 11.0f);
                _player.Velocity = Vector3.Zero;
                Require(_player.TryEnterCoverForDiagnostics(Vector3.Forward), "high cover entry");
                Require(_player.CurrentCoverType == CoverType.High, "high cover classification");
                _player.ExitCoverForDiagnostics();

                _player.GlobalPosition = new Vector3(5.0f, 0.02f, 11.0f);
                _player.Velocity = Vector3.Zero;
                Require(_player.TryEnterCoverForDiagnostics(Vector3.Forward), "low cover entry");
                Require(_player.CurrentCoverType == CoverType.Low, "low cover classification");
                _traversalStartZ = _player.GlobalPosition.Z;
                Require(_player.TryVaultForDiagnostics(Vector3.Forward), "vault from low cover");
                _player.ExitCoverForDiagnostics();
                Require(_player.IsTraversing, "active vault state");
                _traversalSmokeStage = 1;
                return;
            }

            if (--_traversalSmokeObservationFrames > 0)
            {
                return;
            }

            Require(!_player!.IsTraversing, "completed vault state");
            Require(_player.GlobalPosition.Z < _traversalStartZ - 1.0f, "vault crossed obstacle");
            Require(
                !_player.GetNode<CollisionShape3D>("CollisionShape3D").Disabled,
                "player collision restored after vault");
            PlayerTraversalController traversal = _player.GetNode<PlayerTraversalController>(
                "PlayerTraversalController");
            PlayerCoverController cover = _player.GetNode<PlayerCoverController>(
                "PlayerCoverController");
            Require(Mathf.IsEqualApprox(traversal.MinimumVaultHeight, 0.4f), "Unity minimum vault height parity");
            Require(Mathf.IsEqualApprox(traversal.MaximumVaultHeight, 1.25f), "Unity maximum vault height parity");
            Require(Mathf.IsEqualApprox(traversal.VaultDuration, 0.55f), "Unity vault duration parity");
            Require(Mathf.IsEqualApprox(cover.MovementSpeed, 2.6f), "Unity cover speed parity");
            Require(Mathf.IsEqualApprox(cover.DetectionDistance, 1.35f), "Unity cover probe parity");

            GD.Print(
                $"Traversal smoke passed: high/low cover classification, cover entry and " +
                $"vault completion verified ({_traversalStartZ - _player.GlobalPosition.Z:0.00} m crossed)."
            );
            _runTraversalSmokeTest = false;
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            _runTraversalSmokeTest = false;
            GD.PushError($"Traversal smoke failed: {exception}");
            GetTree().Quit(1);
        }
    }

    private void AdvanceBarSabotageSmokeTest()
    {
        try
        {
            ResolveDependencies();
            Require(_player is not null, "player for Bar La Niebla smoke");
            Require(_stage is not null, "El Faro prerequisite mission");
            Require(_workshop is not null, "El Faro prerequisite business");
            Require(_bar is not null, "Bar La Niebla business");
            Require(_sabotageMission is not null, "Bar La Niebla sabotage mission");
            Require(_economy is not null, "economy for sabotage reward");

            if (_barSabotageSmokeStage == 0)
            {
                if (--_smokeFramesRemaining > 0)
                {
                    return;
                }

                Require(
                    _sabotageMission!.MissionStatus == BusinessSabotageMissionStatus.Locked,
                    "sabotage mission initially locked");
                Require(_bar!.Operations.IsCriticalAssetOperational, "La Niebla initially operational");
                Require(
                    _bar.Business.OwnerId == _bar.Definition.InitialOwnerId,
                    "La Niebla initially rival-owned");

                _stage!.StartMission();
                for (int interaction = 0; interaction < 4; interaction++)
                {
                    _workshop!.InteractWithMerchant();
                }

                Require(
                    _stage.MissionStatus == PropertyTakeoverMissionStatus.Completed,
                    "El Faro prerequisite completed");
                _barSabotageSmokeStage = 1;
                _barSabotageWaitFrames = 3;
                return;
            }

            if (_barSabotageSmokeStage == 1)
            {
                if (--_barSabotageWaitFrames > 0)
                {
                    return;
                }

                Require(
                    _sabotageMission!.MissionStatus == BusinessSabotageMissionStatus.Available,
                    "sabotage mission unlocked by El Faro");
                _sabotageMission.StartMission();
                Require(
                    _sabotageMission.MissionStatus == BusinessSabotageMissionStatus.ReachProperty,
                    "sabotage mission accepted");
                _player!.GlobalPosition = _bar!.ExteriorEntrancePosition + Vector3.Up * 0.05f;
                _player.Velocity = Vector3.Zero;
                _barSabotageSmokeStage = 2;
                _barSabotageWaitFrames = 3;
                return;
            }

            if (--_barSabotageWaitFrames > 0)
            {
                return;
            }

            Require(
                _sabotageMission!.MissionStatus == BusinessSabotageMissionStatus.SabotageTarget,
                "arrival advances sabotage objective");
            _barOwnerBeforeSabotage = _bar!.Business.OwnerId;
            _barBalanceBeforeSabotage = _economy!.Economy.Balance;
            _bar.InteractWithOperations();
            Require(
                _sabotageMission.MissionStatus == BusinessSabotageMissionStatus.Completed,
                "sabotage mission completion");
            Require(!_bar.Operations.IsCriticalAssetOperational, "La Niebla sabotaged operations");
            Require(
                _bar.Business.OwnerId == _barOwnerBeforeSabotage,
                "sabotage preserves rival ownership");
            Require(
                _economy.Economy.Balance == _barBalanceBeforeSabotage +
                    _sabotageMission.MissionReward,
                "sabotage reward granted exactly once");

            GD.Print(
                $"Bar sabotage smoke passed: chained unlock, arrival, sabotage, rival ownership " +
                $"and ${_sabotageMission.MissionReward} reward verified.");
            _runBarSabotageSmokeTest = false;
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            _runBarSabotageSmokeTest = false;
            GD.PushError($"Bar sabotage smoke failed: {exception}");
            GetTree().Quit(1);
        }
    }

    private void RunTerritorySmokeTest()
    {
        int exitCode = 1;
        try
        {
            ResolveDependencies();
            Require(_workshop is not null, "El Faro business");
            Require(_bar is not null, "La Niebla business");
            Require(_depot is not null, "San Telmo business");
            Require(_territory?.State is not null, "South District territory");

            _workshop!.ResetBusinessEncounter();
            _bar!.ResetBusinessEncounter();
            _depot!.ResetBusinessEncounter();
            string rivalFaction = _workshop.Definition.InitialOwnerId;
            Require(_territory!.State.Businesses.Count == 3, "three territory businesses");
            Require(_territory.State.RequiredBusinessCount == 2, "two-business majority");
            Require(_territory.State.ControllerId == rivalFaction, "initial rival control");
            Require(_territory.State.CountControlledBy(rivalFaction) == 3, "three rival businesses");

            for (int interaction = 0; interaction < 4; interaction++)
            {
                _workshop.InteractWithMerchant();
            }

            Require(
                _workshop.Business.OwnerId == WorkshopController.PlayerFactionId,
                "El Faro captured by direct pressure");
            Require(
                _territory.State.ControllerId == rivalFaction,
                "one business does not transfer territory");

            for (int interaction = 0; interaction < 4; interaction++)
            {
                _depot.InteractWithMerchant();
            }

            Require(
                _depot.Business.OwnerId == WorkshopController.PlayerFactionId,
                "San Telmo captured by direct pressure");
            Require(
                _territory.State.ControllerId == WorkshopController.PlayerFactionId,
                "second business transfers territory");
            Require(
                _territory.State.CountControlledBy(WorkshopController.PlayerFactionId) == 2,
                "player controls exact majority");
            Require(
                _bar.Business.OwnerId == rivalFaction,
                "La Niebla ownership remains independent");

            GD.Print(
                "Territory smoke passed: three businesses, 2-of-3 majority, sequential " +
                "capture and independent La Niebla ownership verified.");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            GD.PushError($"Territory smoke failed: {exception}");
        }
        finally
        {
            GetTree().Quit(exitCode);
        }
    }

    private void RunDefenseMissionSmokeTest()
    {
        int exitCode = 1;
        try
        {
            ResolveDependencies();
            Require(_stage is not null, "defense mission controller");
            Require(_workshop is not null, "El Faro defense target");
            Require(_economy is not null, "defense reward economy");

            _workshop!.ResetBusinessEncounter();
            _stage!.ResetStageFiveEncounter();
            int balanceBeforeDefense = _economy!.Economy.Balance;
            for (int interaction = 0; interaction < 4; interaction++)
            {
                _workshop.InteractWithMerchant();
            }

            Require(
                _stage.DefenseMissionStatus == BusinessDefenseMissionStatus.Warning,
                "urgent warning after El Faro capture");
            Require(_stage.LivingCounterattackerCount == 0, "attackers wait during warning");
            _stage.AdvanceDefenseForDiagnostics(_stage.CounterattackWarningDuration);
            Require(
                _stage.DefenseMissionStatus == BusinessDefenseMissionStatus.Defending,
                "active defense after warning");
            Require(_stage.LivingCounterattackerCount == 2, "two living attackers");
            Require(_stage.ActiveCounterattackerCount == 2, "two physical attackers spawned");

            Require(_stage.DefeatCounterattackerForDiagnostics(0), "first attacker defeated");
            Require(_stage.LivingCounterattackerCount == 1, "one attacker remains");
            Require(_stage.DefeatCounterattackerForDiagnostics(1), "second attacker defeated");
            Require(
                _stage.DefenseMissionStatus == BusinessDefenseMissionStatus.Succeeded,
                "successful defense state");
            Require(_stage.ActiveCounterattackerCount == 0, "temporary attackers cleaned up");
            Require(_workshop.Operations.IsOperational, "El Faro remains operational");
            Require(
                _economy.Economy.Balance == balanceBeforeDefense + _stage.DefenseMissionReward,
                "defense reward granted once");
            Require(
                !_stage.DefeatCounterattackerForDiagnostics(1),
                "terminal defense cannot reward twice");

            _workshop.ResetBusinessEncounter();
            _stage.ResetStageFiveEncounter();
            for (int interaction = 0; interaction < 4; interaction++)
            {
                _workshop.InteractWithMerchant();
            }

            _stage.AdvanceDefenseForDiagnostics(_stage.CounterattackWarningDuration);
            Require(_stage.ForceDefenseFailureForDiagnostics(), "forced sabotage failure");
            Require(
                _stage.DefenseMissionStatus == BusinessDefenseMissionStatus.Failed,
                "failed defense state");
            Require(!_workshop.Operations.IsOperational, "failed defense sabotages operations");
            Require(
                _workshop.Business.OwnerId == WorkshopController.PlayerFactionId,
                "failed defense preserves ownership");

            _workshop.InteractWithOperations();
            Require(_workshop.Operations.IsOperational, "player repairs El Faro");
            Require(
                _stage.DefenseMissionStatus == BusinessDefenseMissionStatus.Warning,
                "repair schedules defense retry");

            GD.Print(
                $"Defense mission smoke passed: 12 s warning, two physical attackers, " +
                $"${_stage.DefenseMissionReward} single reward, failure and repair retry verified.");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            GD.PushError($"Defense mission smoke failed: {exception}");
        }
        finally
        {
            GetTree().Quit(exitCode);
        }
    }

    private void AddCrowdProbe(Node parent, Vector3 start, Vector3 destination)
    {
        NavigationCrowdProbe probe = new()
        {
            Name = $"CrowdProbe{_crowdProbes.Count + 1:00}"
        };
        probe.Configure(start, destination, 2.4f);
        parent.AddChild(probe);
        _crowdProbes.Add(probe);
    }

    private void RequireProductionAvoidanceConfiguration()
    {
        Godot.Collections.Array<Node> companions = GetTree().GetNodesInGroup("companion");
        Require(companions.Count == 1, "one active companion for avoidance validation");
        Require(
            companions[0].GetNode<NavigationAgent3D>("NavigationAgent3D").AvoidanceEnabled,
            "active companion avoidance");

        foreach (Node reserve in GetTree().GetNodesInGroup("reserve_companion"))
        {
            Require(
                !reserve.GetNode<NavigationAgent3D>("NavigationAgent3D").AvoidanceEnabled,
                $"inactive reserve avoidance for {reserve.Name}");
        }

        Node? rival = GetTree().GetFirstNodeInGroup("rival");
        Require(
            rival?.GetNode<NavigationAgent3D>("NavigationAgent3D").AvoidanceEnabled == true,
            "active rival avoidance");
        Require(
            _stage?.GetNode<NavigationAgent3D>("PoliceOfficer/NavigationAgent3D")
                .AvoidanceEnabled == false,
            "inactive police avoidance");
        Require(
            _stage?.GetNode<NavigationAgent3D>("Counterattacker/NavigationAgent3D")
                .AvoidanceEnabled == false,
            "inactive counterattacker avoidance");
        Require(
            _stage?.GetNode<NavigationAgent3D>("Counterattacker2/NavigationAgent3D")
                .AvoidanceEnabled == false,
            "inactive second counterattacker avoidance");
    }

    private float GetMinimumCrowdDistance()
    {
        float minimum = float.MaxValue;
        for (int first = 0; first < _crowdProbes.Count; first++)
        {
            for (int second = first + 1; second < _crowdProbes.Count; second++)
            {
                Vector3 offset = _crowdProbes[first].GlobalPosition -
                    _crowdProbes[second].GlobalPosition;
                offset.Y = 0.0f;
                minimum = Mathf.Min(minimum, offset.Length());
            }
        }

        return minimum;
    }

    private void FinishCrowdSmokeTest(Exception? exception = null)
    {
        _runCrowdSmokeTest = false;
        if (exception is not null)
        {
            GD.PushError($"Crowd smoke failed: {exception}");
        }

        GetTree().Quit(exception is null ? 0 : 1);
    }

    private static void Require(bool condition, string requirement)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Missing or invalid requirement: {requirement}.");
        }
    }
}
