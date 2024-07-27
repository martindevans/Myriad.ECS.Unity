using Myriad.ECS;
using Myriad.ECS.Command;
using Myriad.ECS.Components;

namespace Assets.Scenes
{
    public struct DemoComponent
        : IComponent
    {
        public int Value;
    }

    public struct GenericDemoComponent<T>
        : IComponent
    {
        public T Value;
    }

    public class OuterGenericClass<T>
    {
        public struct InnerDemoComponent
            : IComponent
        {
            public T Value;
        }

        public struct InnerGenericDemoComponent<U>
            : IComponent
        {
            public T ValueT;
            public U ValueU;
        }
    }

    public readonly struct PhantomComponent
        : IPhantomComponent
    {
    }

    public readonly struct DisposableComponent
        : IDisposableComponent
    {
        public void Dispose(ref LazyCommandBuffer lazy)
        {
        }
    }
}
