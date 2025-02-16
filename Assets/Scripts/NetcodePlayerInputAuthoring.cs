using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

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

    //Wrong way to handle one-off inputs
    //public bool shoot;

    //Correct way:
    public InputEvent shoot;
}