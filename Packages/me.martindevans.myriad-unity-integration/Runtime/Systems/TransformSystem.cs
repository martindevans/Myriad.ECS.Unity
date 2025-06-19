using System.Diagnostics;
using Myriad.ECS.Components;
using Myriad.ECS.Worlds;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Systems
{
    /// <summary>
    /// Updates matrices down hierarchy of <see cref="TransformParent"/> links.
    /// </summary>
    public class MyriadTransformSystem<TData>
        : BaseUpdateTransformHierarchySystem<
            TData,
            MyriadTransformSystem<TData>.TransformMatrix,
            MyriadTransformSystem<TData>.LocalTransformMatrix,
            MyriadTransformSystem<TData>.WorldTransformMatrix,
            TransformParent
        >
    {
        public MyriadTransformSystem(World world)
            : base(world)
        {
        }

        /// <summary>
        /// Transform matrix
        /// </summary>
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

        /// <summary>
        /// Transform from parent to child (see <see cref="TransformParent"/>).
        /// </summary>
        [DebuggerDisplay("{Transform}")]
        public struct LocalTransformMatrix
            : ILocalTransform<TransformMatrix>
        {
            public TransformMatrix Transform { get; set; }
        }

        /// <summary>
        /// World transform, composed from all <see cref="LocalTransformMatrix"/> in hierarchy.
        /// </summary>
        [DebuggerDisplay("{Transform}")]
        public struct WorldTransformMatrix
            : IWorldTransform<TransformMatrix>
        {
            public TransformMatrix Transform { get; set; }
            public int Phase { get; set; }
        }
    }
}
