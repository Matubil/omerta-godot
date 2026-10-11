using System;
using Godot;
using Omerta.GodotAdapters.Businesses;
using Omerta.GodotAdapters.Camera;
using Omerta.GodotAdapters.Characters;
using Omerta.GodotAdapters.Combat;
using Omerta.GodotAdapters.Navigation;
using Omerta.GodotAdapters.StageFive;
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
    private HitscanWeaponController? _weapon;
    private SharedNavigationWorld? _navigation;
    private bool _visible;
    private bool _runSmokeTest;
    private bool _runNavigationSmokeTest;
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
    }

    private void ResolveDependencies()
    {
        _hud ??= GetTree().GetFirstNodeInGroup("hud") as HudController;
        _player ??= GetTree().GetFirstNodeInGroup("player") as PlayerController;
        _camera ??= GetTree().GetFirstNodeInGroup("camera_rig") as IsometricCameraController;
        _stage ??= GetTree().GetFirstNodeInGroup("stage_five") as StageFiveController;
        _workshop ??= GetTree().GetFirstNodeInGroup("workshop") as WorkshopController;
        _weapon ??= _player?.GetNodeOrNull<HitscanWeaponController>("WeaponController");
        _navigation ??= GetTree().GetFirstNodeInGroup("shared_navigation") as SharedNavigationWorld;
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

    private static void Require(bool condition, string requirement)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Missing or invalid requirement: {requirement}.");
        }
    }
}
