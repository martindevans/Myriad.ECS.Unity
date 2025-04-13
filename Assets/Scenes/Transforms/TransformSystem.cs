using System.Diagnostics;
using Myriad.ECS.Components;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Systems;
using UnityEngine;

namespace Assets.Scenes.Transforms
{
    public class TransformSystem
        : WorldSystemGroup<GameTime>
    {
        protected override ISystemGroup<GameTime> CreateGroup(World world)
        {
            return new SystemGroup<GameTime>("Transform",
                new UnityTransfomSystem(world)
            );
        }
    }

    public class UnityTransfomSystem
        : BaseUpdateTransformHierarchySystem<GameTime, TransformMatrix, LocalTransformMatrix, WorldTransformMatrix, TransformParent>
    {
        public UnityTransfomSystem(World world)
            : base(world)
        {
        }
    }

    [DebuggerDisplay("{Matrix}")]
    public struct TransformMatrix
        : ITransform<TransformMatrix>
    {
        public Matrix4x4 Matrix;

        public TransformMatrix Compose(TransformMatrix child)
        {
            return new TransformMatrix
            {
                Matrix = Matrix * child.Matrix
            };
        }
    }

    [DebuggerDisplay("{Transform}")]
    public struct LocalTransformMatrix
        : ILocalTransform<TransformMatrix>
    {
        public TransformMatrix Transform { get; set; }
    }

    [DebuggerDisplay("{Transform}")]
    public struct WorldTransformMatrix
        : IWorldTransform<TransformMatrix>
    {
        public TransformMatrix Transform { get; set; }
        public int Phase { get; set; }
    }
}
