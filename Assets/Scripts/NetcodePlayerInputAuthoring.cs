using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

//Adds input to entities
public class NetcodePlayerInputAuthoring : MonoBehaviour
{
    public class Baker : Baker<NetcodePlayerInputAuthoring> {
        public override void Bake(NetcodePlayerInputAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity,new NetcodePlayerInput());
        }
    }
}

//Special type; icomponent data set up in a special way to handle inputs - has a buffer compatible with things like prediction
public struct NetcodePlayerInput : IInputComponentData {
    public float2 inputVector;
    public InputEvent jump;
    public InputEvent shoot;
}