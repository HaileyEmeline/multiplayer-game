using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

public readonly partial struct CharacterAspect : IAspect
{
    public readonly Entity Self;
    public readonly RefRW<LocalTransform> Transform;

    readonly RefRW<Player> m_Character;
    readonly RefRW<PhysicsVelocity> m_Velocity;
    //readonly RefRO<PlayerInput> m_Input; 
    readonly RefRO<GhostOwner> m_Owner;
    readonly RefRO<AutoCommandTarget> m_AutoCommandTarget;

    public int OwnerNetworkId => m_Owner.ValueRO.NetworkId;
    public ref Player Config => ref m_Character.ValueRW;
    public ref PhysicsVelocity Velocity => ref m_Velocity.ValueRW;

    public AutoCommandTarget AutoCommandTarget => m_AutoCommandTarget.ValueRO;

}
