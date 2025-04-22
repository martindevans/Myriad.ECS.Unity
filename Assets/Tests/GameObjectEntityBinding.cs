using Myriad.ECS.Command;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using NUnit.Framework;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Components;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Extensions;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Systems;
using UnityEngine;

namespace Tests
{
    public class GameObjectEntityBinding
    {
        [Test]
        public void GameObjectEntityBindingSimple()
        {
            var world = new WorldBuilder().Build();
            var systems = new SystemGroup<GameTime>("systems", new MyriadEntityBindingSystem<GameTime>(world));
            systems.Init();

            // Create a gameobject
            var go = new GameObject();

            // Create an entity, bound to GO
            var c = new CommandBuffer(world);
            var eb = c.Create().SetupGameObjectBinding(go, DestructMode.EntityDestroysGameObject);
            var r = c.Playback();
            var e = eb.Resolve();
            r.Dispose();
            
            // Check state of entity
            Assert.IsTrue(go.GetComponent<MyriadEntity>().Entity.HasValue);
            Assert.AreEqual(e, go.GetComponent<MyriadEntity>().Entity);
        }

        [Test]
        public void GameObjectEntityBindingSimple_DeadGameObject_Slave()
        {
            var world = new WorldBuilder().Build();
            var systems = new SystemGroup<GameTime>("systems", new MyriadEntityBindingSystem<GameTime>(world));
            systems.Init();

            // Create a gameobject
            var go = new GameObject();

            // Create an entity, bound to GO
            var c = new CommandBuffer(world);
            var eb = c.Create().SetupGameObjectBinding(go, DestructMode.EntityDestroysGameObject);

            // Destroy GO after it's added to entity, but before entity even exists
            var me = go.GetComponent<MyriadEntity>();
            Object.DestroyImmediate(go);

            // Now create entity
            var r = c.Playback();
            var e = eb.Resolve();
            r.Dispose();

            // Check state of entity
            Assert.IsTrue(me.Entity.HasValue);
            Assert.AreEqual(e, me.Entity);
            Assert.IsTrue(e.IsAlive());

            // Run systems
            var gt = new GameTime();
            systems.BeforeUpdate(gt);
            systems.Update(gt);
            systems.AfterUpdate(gt);

            // Check entity is not dead
            Assert.IsTrue(e.IsAlive());
        }

        [Test]
        public void GameObjectEntityBindingSimple_DeadGameObject_Master()
        {
            var world = new WorldBuilder().Build();
            var systems = new SystemGroup<GameTime>("systems", new MyriadEntityBindingSystem<GameTime>(world));
            systems.Init();

            // Create a gameobject
            var go = new GameObject();

            // Create an entity, bound to GO
            var c = new CommandBuffer(world);
            var eb = c.Create().SetupGameObjectBinding(go, DestructMode.GameObjectDestroysEntity);

            // Destroy GO after it's added to entity, but before entity even exists
            var me = go.GetComponent<MyriadEntity>();
            Object.DestroyImmediate(go);

            // Now create entity
            var r = c.Playback();
            var e = eb.Resolve();
            r.Dispose();

            // Check state of entity
            Assert.IsTrue(me.Entity.HasValue);
            Assert.AreEqual(e, me.Entity);
            Assert.IsTrue(e.IsAlive());

            // Run systems
            var gt = new GameTime();
            systems.BeforeUpdate(gt);
            systems.Update(gt);
            systems.AfterUpdate(gt);

            // Check entity is not dead
            Assert.IsFalse(e.IsAlive());
        }

        [Test] public void GameObjectEntityBindingSimple_DeadEntityObject_Slave()
        {
            var world = new WorldBuilder().Build();
            var systems = new SystemGroup<GameTime>("systems", new MyriadEntityBindingSystem<GameTime>(world));
            systems.Init();

            // Create a gameobject
            var go = new GameObject();

            // Create an entity, bound to GO
            var c = new CommandBuffer(world);
            var eb = c.Create().SetupGameObjectBinding(go, DestructMode.GameObjectDestroysEntity);
            var me = go.GetComponent<MyriadEntity>();

            // Now create entity
            var r = c.Playback();
            var e = eb.Resolve();
            r.Dispose();

            // Check state of entity
            Assert.IsTrue(me.Entity.HasValue);
            Assert.AreEqual(e, me.Entity);
            Assert.IsTrue(e.IsAlive());

            // Run systems
            var gt = new GameTime();
            systems.BeforeUpdate(gt);
            systems.Update(gt);
            systems.AfterUpdate(gt);

            // Destroy entity
            c.Delete(e);
            c.Playback().Dispose();

            // Check entity is dead and GO is not
            Assert.IsFalse(e.IsAlive());
            Assert.IsTrue(go);
        }

        [Test]
        public void GameObjectEntityBindingSimple_DeadEntityObject_Master()
        {
            var world = new WorldBuilder().Build();
            var systems = new SystemGroup<GameTime>("systems", new MyriadEntityBindingSystem<GameTime>(world));
            systems.Init();

            // Create a gameobject
            var go = new GameObject();

            // Create an entity, bound to GO
            var c = new CommandBuffer(world);
            var eb = c.Create().SetupGameObjectBinding(go, DestructMode.EntityDestroysGameObject);
            var me = go.GetComponent<MyriadEntity>();

            // Now create entity
            var r = c.Playback();
            var e = eb.Resolve();
            r.Dispose();

            // Check state of entity
            Assert.IsTrue(me.Entity.HasValue);
            Assert.AreEqual(e, me.Entity);
            Assert.IsTrue(e.IsAlive());

            // Run systems
            var gt = new GameTime();
            systems.BeforeUpdate(gt);
            systems.Update(gt);
            systems.AfterUpdate(gt);

            // Destroy entity
            c.Delete(e);
            c.Playback().Dispose();

            // Run systems to apply destroy
            systems.BeforeUpdate(gt);
            systems.Update(gt);
            systems.AfterUpdate(gt);

            // Check entity is dead and GO is too
            Assert.IsFalse(e.IsAlive());
            Assert.IsFalse(go);
        }
    }
}
