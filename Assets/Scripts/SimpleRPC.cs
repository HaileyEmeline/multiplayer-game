using Unity.NetCode;
using UnityEngine;

//Test to send RPCs
public struct SimpleRPC : IRpcCommand{

    public int value;
}
