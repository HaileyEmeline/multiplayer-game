using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

//Stores additional variables for the player, such as movement and jump height
public partial class PlayerAspect : IAspect {

    public readonly RefRW<LocalTransform> localTransform;
    public readonly RefRW<PhysicsVelocity> physicsVelocity;
    public readonly RefRO<NetcodePlayerInput> netcodePlayerInput;

    public float MoveDirection {
        get => netcodePlayerInput.ValueRO.inputVector.x;
        //set => 
    }

    public float jumpForce { get; set; } = 9.8f;

    public float MoveSpeed { get; set; } = 10f;

}