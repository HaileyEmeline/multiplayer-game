using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class MyValueAuthoring : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public class Baker : Baker<MyValueAuthoring> {

        public override void Bake(MyValueAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MyValue());
        }
    }
}

//For more advanced use cases, the [GhostComponent] field marks entire components as a ghost with a bunch of options.
public struct MyValue : IComponentData {
    //Ghostfield attribute makes it synchronizable 
    [GhostField] public int value;
}
