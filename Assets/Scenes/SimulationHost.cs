using System;
using System.Threading;
using Assets.Scenes;
using Myriad.ECS;
using Myriad.ECS.Command;
using Myriad.ECS.Components;
using Myriad.ECS.Queries;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Extensions;
using UnityEngine;
using Random = System.Random;

public class SimulationHost
    : GameTimeWorldHost
{
    [NonSerialized] private World _world;
    public override World World => _world;

    private void Awake()
    {
        _world = new WorldBuilder().Build();

        var cmd = new CommandBuffer(World);

        // Create some entities
        var rng = new Random(324523);
        for (var i = 0; i < 50; i++)
        {
            // Setup normal ECS stuff
            var buffered = cmd
                .Create()
                .Set(new DemoComponent { Value = 1 })
                .Set(new GenericDemoComponent<int> { Value = 2 })
                .Set(new OuterGenericClass<float>.InnerDemoComponent { Value = 3 })
                .Set(new OuterGenericClass<byte>.InnerGenericDemoComponent<int> { ValueT = 0, ValueU = 1 });

            if (rng.NextDouble() < 0.5f)
                buffered.Set(new PhantomComponent());
            if (rng.NextDouble() < 0.5f)
                buffered.Set(new DisposableComponent());

            // Create a GameObject to represent this entity
            var go = new GameObject($"Entity binding {i}");
            buffered.SetupGameObjectBinding(go);
        }
        cmd.Playback().Dispose();

        // Delete some entities
        foreach (var (e, _) in World.Query<PhantomComponent>())
            if (rng.NextDouble() < 0.1f)
                cmd.Delete(e);
        cmd.Playback().Dispose();

        var systems = new SystemGroup<GameTime>(
            "main",
            new IncrementTheNumberSystem(World),
            new MyriadEntityBindingSystem<GameTime>(World),
            new WasteTimeSystem(),
            new SystemGroup<GameTime>(
                "sub group",
                new WasteTimeSystem(),
                new WasteTimeSystem(),
                new GenericOuterSystem<float>(World),
                new MoreOuterClass.OuterClass<byte>.AlmostOuterClass.GenericOuterInnerSystem<GameTime>(World)
            ),
            new OrderedParallelSystemGroup<GameTime>(
                "parallel group",
                new WasteTimeSystem(),
                new EmptySystem(),
                new EmptySystem(),
                new EmptySystem()
            ),
            new PhasedParallelSystemGroup<GameTime>(
                "parallel group",
                new WasteTimeSystem(),
                new EmptySystem(),
                new EmptySystem(),
                new EmptySystem()
            )
        );
        systems.Init();
        Add(systems);
    }
}

public class IncrementTheNumberSystem
    : BaseSystem<GameTime>
{
    private readonly World _world;

    public IncrementTheNumberSystem(World world)
    {
        _world = world;
    }

    public override void Update(GameTime data)
    {
        _world.ExecuteParallel<Inc, DemoComponent>(new Inc());
    }

    private readonly struct Inc
        : IQuery<DemoComponent>
    {
        public void Execute(Entity e, ref DemoComponent t0)
        {
            t0.Value++;
        }
    }
}

public class GenericOuterSystem<T>
    : BaseSystem<GameTime>, ISystemDeclare<GameTime>
{
    private readonly World _world;

    public GenericOuterSystem(World world)
    {
        _world = world;
    }

    public override void Update(GameTime data)
    {
        foreach (var (_, demo) in _world.Query<OuterGenericClass<T>.InnerDemoComponent>())
            demo.Ref.Value = (T)(object)((int)data.Frame / 2f);
    }

    public void Declare(ref SystemDeclaration declaration)
    {
        declaration.Write<OuterGenericClass<T>.InnerDemoComponent>();
    }
}

public class MoreOuterClass
{
    public class OuterClass<T>
    {
        public class AlmostOuterClass
        {
            public class GenericOuterInnerSystem<U>
                : BaseSystem<GameTime>, ISystemDeclare<GameTime>
            {
                private readonly World _world;

                public GenericOuterInnerSystem(World world)
                {
                    _world = world;
                }

                public override void Update(GameTime data)
                {
                    foreach (var (_, demo) in _world.Query<OuterGenericClass<T>.InnerGenericDemoComponent<U>>())
                    {
                        demo.Ref.ValueT = (T)(object)(byte)((int)data.Frame / 2f);
                        demo.Ref.ValueU = (U)(object)(int)((int)data.Frame / 3f);
                    }
                }

                public void Declare(ref SystemDeclaration declaration)
                {
                    declaration.Write<OuterGenericClass<T>.InnerGenericDemoComponent<U>>();
                }
            }
        }
    }
}

public class WasteTimeSystem
    : BaseSystem<GameTime>, ISystemDeclare<GameTime>
{
    private readonly Random _random = new();
    private int _milliseconds;

    public override void Update(GameTime data)
    {
        if (_random.NextDouble() < 1 / 60f)
            _milliseconds = _random.Next(0, 5);

        Thread.Sleep(_milliseconds);
    }

    public void Declare(ref SystemDeclaration declaration)
    {
    }
}

public class EmptySystem
    : BaseSystem<GameTime>, ISystemDeclare<GameTime>, ISystemQueryEntityCount
{
    public override void Update(GameTime data)
    {
    }

    public void Declare(ref SystemDeclaration declaration)
    {
        declaration.Read<DemoComponent>();
    }

    public int QueryEntityCount => 3;
}
