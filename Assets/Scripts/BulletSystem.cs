using JetBrains.Annotations;
using Unity.Burst;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;


[UpdateInGroup(typeof(PredictedSimulationSystemGroup))] 
partial struct BulletSystem : ISystem
{

    public Entity playerEntity;
    public Entity mouseEntity;

    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<NetcodePlayerInputSystem>();
        //state.RequireForUpdate<MousePosition>();
        
    }
    
    //[BurstCompile]
    public void OnUpdate(ref SystemState state) {

        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        if (playerEntity == Entity.Null) {
            playerEntity = SystemAPI.GetSingletonEntity<PlayerPosition>();
        }

        //Finds the local player ID
        int localPlayerNetworkId = -1;
        foreach (var ghostOwner in SystemAPI.Query<RefRO<GhostOwner>>().WithAll<GhostOwnerIsLocal>()) {
            localPlayerNetworkId = ghostOwner.ValueRO.NetworkId;
        }


        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRW<Bullet> bullet,
            Entity entity)
            in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRW<Bullet>>().WithEntityAccess().WithAll<Simulate>()) {

            float moveSpeed = 30f;

            bool foundLocalInput = false;


            foreach (var (inputState, ghostOwner) in SystemAPI.Query<RefRO<PlayerInputStateGhost>, RefRO<GhostOwner>>()) { //used to have with all ghost owner
                
                //Only applies to local client
                if (ghostOwner.ValueRO.NetworkId == localPlayerNetworkId) {
                    foundLocalInput = true;

                    //Read in the aim direction
                    float3 aimDirection = inputState.ValueRO.aimDirection;

                    Debug.Log("Aim Direction: " + aimDirection + ghostOwner.ValueRO.NetworkId);


                    //Move the bullet forwards
                    localTransform.ValueRW.Position += aimDirection * moveSpeed * SystemAPI.Time.DeltaTime;

                    //To destroy
                    //Running client on server
                    if (state.World.IsServer()) {
                        bullet.ValueRW.timer -= SystemAPI.Time.DeltaTime;
                        if (bullet.ValueRW.timer <= 0f) {
                            entityCommandBuffer.DestroyEntity(entity);
                        }
                    }

                } 
            }

            if (foundLocalInput == false) {
                Debug.Log("No local input!");
                //break;
            }

        }

        entityCommandBuffer.Playback(state.EntityManager);
    }
}
