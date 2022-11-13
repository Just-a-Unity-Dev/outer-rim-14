﻿using System.Linq;
using Content.Server._00OuterRim.Worldgen2.Components;
using Content.Server._00OuterRim.Worldgen2.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._00OuterRim.Worldgen2.Systems.Biomes;

/// <summary>
/// This handles...
/// </summary>
public sealed class BiomeSelectionSystem : BaseWorldSystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _ser = default!;
    [Dependency] private readonly NoiseIndexSystem _noiseIdx = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BiomeSelectionComponent, ComponentStartup>(OnBiomeSelectionStartup);
        SubscribeLocalEvent<BiomeSelectionComponent, WorldChunkAddedEvent>(OnWorldChunkAdded);
    }

    private void OnWorldChunkAdded(EntityUid uid, BiomeSelectionComponent component, ref WorldChunkAddedEvent args)
    {
        var coords = GetChunkCoords(args.Chunk);
        foreach (var biomeId in component.Biomes)
        {
            var biome = _proto.Index<BiomePrototype>(biomeId);
            if (!CheckBiomeValidity(args.Chunk, biome, coords))
                continue;

            biome.Apply(args.Chunk, _ser, EntityManager, _compFactory);
            return;
        }

        Logger.Error($"Biome selection ran out of biomes to select? See biomes list: {component.Biomes}");
    }

    private void OnBiomeSelectionStartup(EntityUid uid, BiomeSelectionComponent component, ComponentStartup args)
    {
        // surely this can't be THAAAAAAAAAAAAAAAT bad right????
        var sorted = component.Biomes
            .Select(x => (Id: x, _proto.Index<BiomePrototype>(x).Priority))
            .OrderBy(x => x.Priority)
            .Select(x => x.Id)
            .ToList();

        component.Biomes = sorted; // my hopes and dreams rely on this being pre-sorted by priority.
    }

    private bool CheckBiomeValidity(EntityUid chunk, BiomePrototype biome, Vector2i coords)
    {
        foreach (var (noise, ranges) in biome.NoiseRanges)
        {
            var value = _noiseIdx.Evaluate(chunk, noise, coords);
            var anyValid = false;
            foreach (var range in ranges)
            {
                if (range.X < value && value < range.Y)
                {
                    anyValid = true;
                    break;
                }
            }

            if (!anyValid)
                return false;
        }

        return true;
    }
}