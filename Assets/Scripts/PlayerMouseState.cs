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

//Gets the aim direction by calculating angle between player and mouse cursor
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct PlayerMouseState : ISystem
{
    //[BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {

        //Check if player is clicking mouse
        if (Input.GetMouseButtonDown(0)) {

            PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

            //Iterate over the ghosts and transforms for the local client
            foreach (var (inputState, characterAspect) in SystemAPI.Query<RefRW<PlayerInputStateGhost>, RefRO<LocalTransform>>().WithAll<GhostOwnerIsLocal>()) //.WithAll<GhostOwnerIsLocal>()
            {
                // Get the mouse position
                UnityEngine.Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPosition.z = 0f;

                //Get the player's position from character aspect 
                UnityEngine.Vector3 playerPosition = characterAspect.ValueRO.Position;
                playerPosition.z = 0f;

                // Calculate the aim direction
                //Normalized stores it in vectors with each number between 0 and 1
                UnityEngine.Vector3 aimDirection = (mouseWorldPosition - playerPosition).normalized;

                Debug.Log("Player position: " + playerPosition);

                // Update PlayerInputStateGhost component with aim direction
                inputState.ValueRW.aimDirection = new float3(aimDirection.x, aimDirection.y, 0f);
                
                Debug.Log("Value: " + inputState.ValueRO.aimDirection);
            }

        } 

    }
}

//Store the component data so that it can be accessed elsewhere
[GhostComponent]
public struct PlayerInputStateGhost : IInputComponentData {
    public float3 aimDirection;
}


