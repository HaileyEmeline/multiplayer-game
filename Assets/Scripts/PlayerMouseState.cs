using System;
using System.Numerics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using Unity.Transforms;
using Unity.VisualScripting;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct PlayerMouseState : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {

        if (Input.GetMouseButtonDown(0)) {
            //Debug.Log("Is there anybody out there?");

            PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

            //UnityEngine.Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            //mouseWorldPosition.z = 0f;

            /*foreach (var (character, inputState) in SystemAPI.Query<CharacterAspect, PlayerInputStateGhost>().WithAll<GhostOwnerIsLocal>()) {
                UnityEngine.Vector3 playerPosition = character.Transform.ValueRO.Position;

                //Calculate difference in position
                UnityEngine.Vector3 aimDirection = (mouseWorldPosition - playerPosition).normalized;


            }*/


            foreach (var (inputState, characterAspect) in SystemAPI.Query<RefRW<PlayerInputStateGhost>, RefRO<LocalTransform>>().WithAll<GhostOwnerIsLocal>()) //.WithAll<GhostOwnerIsLocal>()
            {
                //Debug.Log("Found an entity");
                // Get the player's position from characterAspect (now a localtransform)
                UnityEngine.Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPosition.z = 0f;

                UnityEngine.Vector3 playerPosition = characterAspect.ValueRO.Position;
                playerPosition.z = 0f;

                // Calculate the aim direction
                UnityEngine.Vector3 aimDirection = (mouseWorldPosition - playerPosition).normalized;

                Debug.Log("Player position: " + playerPosition);

                //var entityCommandBuffer = commandBufferSystem.CreateCommandBuffer();
                //entityCommandBuffer.SetComponent(characterAspect.Entity, new PlayerInputStateGhost { aimDirection = new float3(aimDirection.x, aimDirection.y, 0f) });

                // Update PlayerInputStateGhost component with aim direction
                inputState.ValueRW.aimDirection = new float3(aimDirection.x, aimDirection.y, 0f);
                
                Debug.Log("Value: " + inputState.ValueRO.aimDirection);
            }

        

            //Update IComponent to be read

        } 

    }
}

[GhostComponent]
public struct PlayerInputStateGhost : IInputComponentData {
    public float3 aimDirection;
}


