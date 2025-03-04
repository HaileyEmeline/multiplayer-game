using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

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

    
    /* public float moveSpeed;
    public float3 moveDirection;

    public Entity playerEntity;

    public PlayerAspect(Entity playerEntity, float moveSpeed, float3 moveDirection) {
        this.playerEntity = playerEntity;
        this.moveSpeed = moveSpeed;
        this.moveDirection = moveDirection;
    }

    public void SetMovementSpeed(float speed) => moveSpeed = speed;
    public void SetMovementDirection(float3 direction) => moveDirection = direction; */
}