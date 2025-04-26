using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class PlayerTrackingSystem : SystemBase
{

    private int _lastPlayerCount = -1;
/*     protected override void OnUpdate()
    {
        var networkIds = EntityManager.CreateEntityQuery(typeof(NetworkId))
                                      .ToComponentDataArray<NetworkId>(Allocator.Temp);

        Debug.Log($"Length of Network IDs: {networkIds.Length}");
        ServerPlayerTracker.ConnectedClientIds.Clear();

        foreach (var id in networkIds)
        {
            ServerPlayerTracker.ConnectedClientIds.Add(id.Value.ToString());
        }

        Debug.Log($"[PlayerTrackingSystem] Connected Players: {ServerPlayerTracker.ConnectedClientIds.Count}");

        networkIds.Dispose();

        Task.Delay(1000);
    }  */

    protected override void OnUpdate()
    { /*
        var networkIdLookup = SystemAPI.QueryBuilder()
            .WithAll<NetworkId, Player>()
            .Build();

        //Debug.Log($"Network ID Lookup Length: {networkIdLookup.ToEntityArray(Allocator.Temp).Length}");

        ServerPlayerTracker.ConnectedClientIds.Clear();

        foreach (var entity in networkIdLookup.ToEntityArray(Allocator.Temp))
        {
            var networkId = EntityManager.GetComponentData<NetworkId>(entity);
            //Debug.Log(networkId);
            ServerPlayerTracker.ConnectedClientIds.Add(networkId.Value.ToString());
        }

        if (ServerPlayerTracker.ConnectedClientIds.Count != _lastPlayerCount)
        {
            Debug.Log($"[TrackConnectedPlayersSystem] Connected Players: {ServerPlayerTracker.ConnectedClientIds.Count}");
            _lastPlayerCount = ServerPlayerTracker.ConnectedClientIds.Count;
        } */

    } 
}