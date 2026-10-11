using System;
using System.Collections.Generic;
using Godot;

namespace Omerta.GodotAdapters.Navigation;

public sealed partial class SharedNavigationWorld : Node3D
{
    public const uint PedestrianLayer = 1u;
    public const uint VehicleLayer = 2u;

    private NavigationRegion3D? _pedestrianRegion;
    private NavigationRegion3D? _vehicleRegion;

    public bool IsSynchronized =>
        NavigationServer3D.MapGetIterationId(GetWorld3D().NavigationMap) > 0;

    public override void _Ready()
    {
        AddToGroup("shared_navigation");
        _pedestrianRegion = CreateRegion(
            "PedestrianRegion",
            PedestrianLayer,
            new SurfaceRect(-10.0f, -20.0f, -5.5f, -15.5f, 0.05f),
            new SurfaceRect(-5.5f, -20.0f, 5.5f, -15.5f, 0.05f),
            new SurfaceRect(5.5f, -20.0f, 10.0f, -15.5f, 0.05f),
            new SurfaceRect(-10.0f, -15.5f, -5.5f, -4.0f, 0.05f),
            new SurfaceRect(5.5f, -15.5f, 10.0f, -4.0f, 0.05f),
            new SurfaceRect(-10.0f, -4.0f, -5.5f, 12.0f, 0.05f),
            new SurfaceRect(-5.5f, -4.0f, 5.5f, 12.0f, 0.05f),
            new SurfaceRect(5.5f, -4.0f, 10.0f, 12.0f, 0.05f),
            new SurfaceRect(-18.0f, 12.0f, -10.0f, 18.0f, 0.05f),
            new SurfaceRect(-10.0f, 12.0f, -5.5f, 18.0f, 0.05f),
            new SurfaceRect(-5.5f, 12.0f, 5.5f, 18.0f, 0.05f),
            new SurfaceRect(5.5f, 12.0f, 10.0f, 18.0f, 0.05f),
            new SurfaceRect(-10.0f, 18.0f, -5.5f, 28.0f, 0.05f),
            new SurfaceRect(-5.5f, 18.0f, 5.5f, 28.0f, 0.05f),
            new SurfaceRect(5.5f, 18.0f, 10.0f, 28.0f, 0.05f),
            new SurfaceRect(45.0f, -5.0f, 55.0f, 5.0f, 0.25f));
        _vehicleRegion = CreateRegion(
            "VehicleRegion",
            VehicleLayer,
            new SurfaceRect(-4.0f, -4.0f, 4.0f, 17.5f, 0.05f),
            new SurfaceRect(-4.0f, 17.5f, 4.0f, 28.0f, 0.05f),
            new SurfaceRect(4.0f, 17.5f, 20.0f, 28.0f, 0.05f));
    }

    public Vector3[] QueryPath(
        Vector3 origin,
        Vector3 destination,
        uint navigationLayers)
    {
        if (!IsSynchronized)
        {
            return Array.Empty<Vector3>();
        }

        return NavigationServer3D.MapGetPath(
            GetWorld3D().NavigationMap,
            origin,
            destination,
            true,
            navigationLayers);
    }

    private NavigationRegion3D CreateRegion(
        string regionName,
        uint navigationLayers,
        params SurfaceRect[] surfaces)
    {
        NavigationMesh mesh = BuildMesh(surfaces);
        NavigationRegion3D region = new()
        {
            Name = regionName,
            NavigationLayers = navigationLayers,
            NavigationMesh = mesh
        };
        AddChild(region);
        return region;
    }

    private static NavigationMesh BuildMesh(IReadOnlyList<SurfaceRect> surfaces)
    {
        List<Vector3> vertices = new(surfaces.Count * 4);
        Dictionary<Vector3, int> vertexIndices = new();
        List<int[]> polygons = new(surfaces.Count);
        NavigationMesh mesh = new();
        for (int index = 0; index < surfaces.Count; index++)
        {
            SurfaceRect surface = surfaces[index];
            polygons.Add(new[]
            {
                GetOrAddVertex(
                    vertices,
                    vertexIndices,
                    new Vector3(surface.MinimumX, surface.Height, surface.MinimumZ)),
                GetOrAddVertex(
                    vertices,
                    vertexIndices,
                    new Vector3(surface.MinimumX, surface.Height, surface.MaximumZ)),
                GetOrAddVertex(
                    vertices,
                    vertexIndices,
                    new Vector3(surface.MaximumX, surface.Height, surface.MaximumZ)),
                GetOrAddVertex(
                    vertices,
                    vertexIndices,
                    new Vector3(surface.MaximumX, surface.Height, surface.MinimumZ))
            });
        }

        mesh.SetVertices(vertices.ToArray());
        foreach (int[] polygon in polygons)
        {
            mesh.AddPolygon(polygon);
        }

        return mesh;
    }

    private static int GetOrAddVertex(
        ICollection<Vector3> vertices,
        IDictionary<Vector3, int> vertexIndices,
        Vector3 vertex)
    {
        if (vertexIndices.TryGetValue(vertex, out int index))
        {
            return index;
        }

        index = vertices.Count;
        vertices.Add(vertex);
        vertexIndices.Add(vertex, index);
        return index;
    }

    private readonly record struct SurfaceRect(
        float MinimumX,
        float MinimumZ,
        float MaximumX,
        float MaximumZ,
        float Height);
}
