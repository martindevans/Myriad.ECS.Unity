using System;
using System.Threading;
using Assets.Scenes;
using Myriad.ECS.Command;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using UnityEngine;
using Random = System.Random;

public class SimulationHost
    : BaseSimulationHost<int>
{
    [NonSerialized] private ISystemGroup<int> _systems;
    public override ISystemGroup<int> Systems => _systems;

    [NonSerialized] private World _world;
    public override World World => _world;

    private void Awake()
    {
        _world = new WorldBuilder().Build();

        var cmd = new CommandBuffer(World);

        // Create some entities
        for (var i = 0; i < 10; i++)
        {
            // Setup normal ECS stuff
            var buffered = cmd
                          .Create()
                          .Set(new DemoComponent { Value = 1 })
                          .Set(new GenericDemoComponent<int> { Value = 2 })
                          .Set(new OuterGenericClass<float>.InnerDemoComponent { Value = 3 })
                          .Set(new OuterGenericClass<byte>.InnerGenericDemoComponent<int> { ValueT = 0, ValueU = 1 });

            // Create a GameObject to represent this entity, add MyriadEntity to bind it automatically
            var go = new GameObject($"Entity binding {i}");
            buffered.Set(go.AddComponent<MyriadEntity>());
        }
        
        cmd.Playback().Dispose();

        _systems = new SystemGroup<int>(
            "main",
            new IncrementTheNumberSystem(World),
            new MyriadEntityBindingSystem<int>(World),
            new WasteTimeSystem(),
            new SystemGroup<int>(
                "sub group",
                new WasteTimeSystem(),
                new WasteTimeSystem(),
                new GenericOuterSystem<float>(World),
                new MoreOuterClass.OuterClass<byte>.AlmostOuterClass.GenericOuterInnerSystem<int>(World)
            ),
            new OrderedParallelSystemGroup<int>(
                "parallel group",
                new WasteTimeSystem(),
                new EmptySystem(),
                new EmptySystem(),
                new EmptySystem()
            ),
            new PhasedParallelSystemGroup<int>(
                "parallel group",
                new WasteTimeSystem(),
                new EmptySystem(),
                new EmptySystem(),
                new EmptySystem()
            )
        );
        _systems.Init();
    }

    public void Update()
    {
        Systems.BeforeUpdate(0);
        Systems.Update(1);
        Systems.AfterUpdate(0);
    }
}

public class IncrementTheNumberSystem
    : BaseSystem<int>
{
    private readonly World _world;

    public IncrementTheNumberSystem(World world)
    {
        _world = world;
    }

    public override void Update(int data)
    {
        foreach (var (_, demo) in _world.Query<DemoComponent>())
            demo.Ref.Value += data;
    }
}

public class GenericOuterSystem<T>
    : BaseSystem<int>, ISystemDeclare<int>
{
    private readonly World _world;

    public GenericOuterSystem(World world)
    {
        _world = world;
    }

    public override void Update(int data)
    {
        foreach (var (_, demo) in _world.Query<OuterGenericClass<T>.InnerDemoComponent>())
            demo.Ref.Value = (T)(object)(data / 2f);
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
                : BaseSystem<int>, ISystemDeclare<int>
            {
                private readonly World _world;

                public GenericOuterInnerSystem(World world)
                {
                    _world = world;
                }

                public override void Update(int data)
                {
                    foreach (var (_, demo) in _world.Query<OuterGenericClass<T>.InnerGenericDemoComponent<U>>())
                    {
                        demo.Ref.ValueT = (T)(object)(byte)(data / 2f);
                        demo.Ref.ValueU = (U)(object)(int)(data / 3f);
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
    : BaseSystem<int>, ISystemDeclare<int>
{
    private readonly Random _random = new();
    private int _milliseconds;

    public override void Update(int data)
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
    : BaseSystem<int>, ISystemDeclare<int>
{
    public override void Update(int data)
    {
    }

    public void Declare(ref SystemDeclaration declaration)
    {
        declaration.Read<DemoComponent>();
    }
}
