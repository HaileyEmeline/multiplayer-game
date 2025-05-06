using System.Diagnostics;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]

partial struct ShootSystem : ISystem
{

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        //Ensure that EntitiesReferences is available before accessing it
        if (!SystemAPI.TryGetSingleton<EntitiesReferences>(out var entitiesRef))
        {
            return;
        }

        // Ensure that NetworkTime is available before accessing it
        if (!SystemAPI.TryGetSingleton<NetworkTime>(out var networkTime))
        {
            return;
        }

        //Allows us to grab reference, and later delete it, for the bullet entity
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach ((
        RefRO<NetcodePlayerInput> netcodePlayerInput,
        RefRO<LocalTransform> localTransform,
        RefRO<GhostOwner> ghostOwner) 
        in SystemAPI.Query<
        RefRO<NetcodePlayerInput>,
        RefRO<LocalTransform>,
        RefRO<GhostOwner>>().WithAll<Simulate>()) {

            //Only tests if this is the first time a tick is predicted - if tick is wrong it resets and reruns predictions
            if (networkTime.IsFirstTimeFullyPredictingTick) {

                //If the button has been pressed
                if (netcodePlayerInput.ValueRO.shoot.IsSet) {

                    UnityEngine.Debug.Log("Shoot true!" + state.World);

                    //Verify it exists (bug fixing)
                    if (entitiesReferences.bulletPrefabEntity == Entity.Null) {
                        UnityEngine.Debug.LogError("Bullet prefab entity is null :(");
                    }

                    Entity bulletEntity = entityCommandBuffer.Instantiate(entitiesReferences.bulletPrefabEntity);

                    //Spawn it onto player
                    entityCommandBuffer.SetComponent(bulletEntity, LocalTransform.FromPosition(localTransform.ValueRO.Position));
                    entityCommandBuffer.SetComponent(bulletEntity, new GhostOwner { NetworkId = ghostOwner.ValueRO.NetworkId });
                }
            }
            
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }

}
