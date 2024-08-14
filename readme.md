## Myriad.Unity.ECS

Integration for [Myriad ECS](https://github.com/martindevans/Myriad.ECS) into Unity.

## Installation

Install in Unity package manager, git url: `git@github.com:martindevans/Myriad.ECS.Unity.git?path=/Packages/me.martindevans.myriad-unity-integration`

## Scene Driven Usage

1. Create a `WorldHost<TData>` in the scene (most likely a `GameTimeWorldHost`)
2. Create a new `MonoBehaviour`, extending `WorldSystemGroup<TData>`
3. When the system group is enabled/disable in the scene, it will be added/remove to the Myriad system list

## Advanced Usage

1. Create a new MonoBehaviour, extending `BaseSimulationHost`. This runs your Myriad simulation.
2. Add `MyriadEntityBindingSystem<TData>` somewhere into your system schedule.
3. Create a new simulation host editor:

```csharp
[CustomEditor(typeof(SimulationHost))]
public class SimulationHostEditor
    : BaseSimulationHostEditor<SimulationHost, TData>
{
}
```

4. When you create an `Entity` which you want to bind to a GameObject, instantiate the GameObject with a `MyriadEntity` behaviour attached. Attach this behaviour to the `Entity`.
5. For every system you want to inspect, create a new system editor:

```csharp
[MyriadSystemEditor(typeof(YourSystem))]
public class YourSystemEditor
    : IMyriadSystemEditor
{
    public void Draw<T>(ISystem<T> sys)
    {
        var system = (sys as YourSystem)!;
        EditorGUILayout.LabelField($"Myriad Is Cool");
    }
}
```

For every component you want to inspect, create a new component editor:

```csharp
[MyriadComponentEditor(typeof(YourComponent))]
public class PagedRailEditor
    : IMyriadComponentEditor
{
    public void Draw(MyriadEntity entity)
    {
        var rail = entity.GetMyriadComponent<YourComponent>();
        EditorGUILayout.LabelField("Myriad Is Great");
    }
}
```