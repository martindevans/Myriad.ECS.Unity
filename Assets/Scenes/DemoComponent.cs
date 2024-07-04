using Myriad.ECS;

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
}
