using UnityEngine;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using System.Collections.Generic;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.NetCode;
using UnityEngine.SceneManagement;
using Unity.Entities;
using TMPro;
using Unity.Services.Core;
using Unity.Collections;
using Unity.VisualScripting;
using System.Linq;
using System.Linq.Expressions;
using Unity.Services.Multiplayer;



#if UNITY_SERVER
using Unity.Services.Multiplay;
#endif
using System;



public class MatchmakingManager : MonoBehaviour
{

    private string currentTicket;
    private string backfillTicketId;
    private EntityManager entityManager;
    private IMatchmakerService matchmakerService;
    private PayloadAllocation payloadAllocation;
    bool isDeallocating = false;
    bool deallocatingCancellationToken = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {

        //entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (Application.platform != RuntimePlatform.LinuxServer) {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            string playerId = AuthenticationService.Instance.PlayerId;
        } else {

#if UNITY_SERVER
            while (UnityServices.State == ServicesInitializationState.Uninitialized || UnityServices.State == ServicesInitializationState.Initializing) {
                await Task.Yield();
            }

            matchmakerService = MatchmakerService.Instance;
            payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<PayloadAllocation>();
            backfillTicketId = payloadAllocation.BackfillTicketId;

#endif
        }    

        //Probably should be through button; for now, just straight up

        CreateTicketOptions createTicketOptions = new CreateTicketOptions("test");

        //Should only be one player currently; list after lobby is set up
        List<Unity.Services.Matchmaker.Models.Player> players = new List<Unity.Services.Matchmaker.Models.Player> { new Unity.Services.Matchmaker.Models.Player(AuthenticationService.Instance.PlayerId)};
        Debug.Log($"Player ID in Matchmaker Manager: {AuthenticationService.Instance.PlayerId}");

        CreateTicketResponse createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);
        currentTicket = createTicketResponse.Id;
        Debug.Log("Ticket Created!");

        while (true) {
            TicketStatusResponse ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(createTicketResponse.Id);

            if (ticketStatusResponse.Type == typeof(MultiplayAssignment)) {
                MultiplayAssignment multiplayAssignment = (MultiplayAssignment)ticketStatusResponse.Value;

                if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Found) {

                    //await SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().buildIndex);

                    World clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                    entityManager = clientWorld.EntityManager;
                    //World serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");

                    //Destroys the base local world
                    foreach (World world in World.All) {
                        if (world.Flags == WorldFlags.Game) {
                            world.Dispose();
                            break;
                        }
                    }

                    if (World.DefaultGameObjectInjectionWorld == null) {
                        World.DefaultGameObjectInjectionWorld = clientWorld;
                    }

                    //Loads the game scene
                    await SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);


                    //Join server
                    int? port = multiplayAssignment.Port;

                    string ip = multiplayAssignment.Ip;
                    Debug.Log("Port: " + port);
                    Debug.Log("Ip:" + multiplayAssignment.Ip);

                    NetworkEndpoint connectNetworkEndpoint = NetworkEndpoint.Parse(ip, ushort.Parse(port.ToString()));
                    RefRW<NetworkStreamDriver> networkStreamDriver = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
                    networkStreamDriver.ValueRW.Connect(clientWorld.EntityManager, connectNetworkEndpoint);

/* #if UNITY_SERVER
                    if (Application.platform == RuntimePlatform.LinuxServer) {
                        foreach (var player in players) {
                            if (!ServerPlayerTracker.ConnectedClientIds.Contains(player.Id)) {
                                ServerPlayerTracker.ConnectedClientIds.Add(player.Id);

                                //Test if it was added
                                Debug.Log($"Connected Players: {ServerPlayerTracker.ConnectedClientIds.Count}");
                            }
                        }
                    }
#endif */
                    //Inform the server a client is connected:
                    

                    //RefRW<NetworkStreamDriver> networkStreamDriver = serverWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
                    //networkStreamDriver.ValueRW.Listen(NetworkEndpoint.AnyIpv4.WithPort((ushort)port));

                    //NetworkEndpoint connectNetworkEndpoint = NetworkEndpoint.LoopbackIpv4.WithPort((ushort)port);
                    //networkStreamDriver = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
                    //networkStreamDriver.ValueRW.Connect(clientWorld.EntityManager, connectNetworkEndpoint);

                    return;
                }
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Timeout) {
                    Debug.Log("Timeout!");
                    return;
                }
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Failed) {
                    Debug.Log("Failed!" + multiplayAssignment.Status + " " + multiplayAssignment.Message);
                    return;
                } 
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.InProgress) {
                    Debug.Log("In progress");
                }
            }

            //Wait one second
            await Task.Delay(1000);
        }
    }

#if UNITY_SERVER
    private void OnPlayerConnected()
    {
        Debug.Log("Player connected!");
        if (Application.platform == RuntimePlatform.LinuxServer) {
            UpdateBackfillTicket();
        }
    }

    private void OnPlayerDisconnected()
    {
        if (Application.platform == RuntimePlatform.LinuxServer) {
            UpdateBackfillTicket();
        }   
    }

#endif

    private async void UpdateBackfillTicket() {
#if UNITY_SERVER
        List<Unity.Services.Matchmaker.Models.Player> players = new List<Unity.Services.Matchmaker.Models.Player>();

        foreach (var playerId in ServerPlayerTracker.ConnectedClientIds) {
            players.Add(new Unity.Services.Matchmaker.Models.Player(playerId.ToString()));
        }

        MatchProperties matchProperties = new MatchProperties(null, players, null, backfillTicketId);
        await MatchmakerService.Instance.UpdateBackfillTicketAsync(payloadAllocation.BackfillTicketId, 
            new BackfillTicket(backfillTicketId, properties: new BackfillTicketProperties(matchProperties)));

        /* //Search for serverWorld
        World serverWorld = World.All.FirstOrDefault(w => w.Name.Contains("server") && w.Flags == WorldFlags.Game);

        if (serverWorld != null) {
            //for each playerid in connected clients, add the player id to the list.
            var query = serverWorld.EntityManager.CreateEntityQuery(typeof(NetworkId));
            var networkIds = query.ToComponentDataArray<NetworkId>(Allocator.Temp);

            foreach (var networkId in networkIds)
            {
                players.Add(new Unity.Services.Matchmaker.Models.Player(networkId.Value.ToString()));
            }

            networkIds.Dispose();

        
    
        } else {
            Debug.Log("Server World could not be found");
        } */
#endif
    }

    // Update is called once per frame
    //deallocates the servers
    private async void Update()
    {
#if UNITY_SERVER
        //Counts the number of clients connected
/*         var query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(NetworkId));
        var networkIds = query.ToComponentDataArray<NetworkId>(Allocator.Temp);

        foreach (var networkId in networkIds)
        {
            connectedClientCount++;
        } */

        //networkIds.Dispose();

        if (Application.platform == RuntimePlatform.LinuxServer) {
            
            int connectedClientCount = ServerPlayerTracker.ConnectedClientIds.Count;
            
            if (connectedClientCount == 0 && !isDeallocating) {
                isDeallocating = true;
                deallocatingCancellationToken = false;
                Deallocate();
            }

            if (connectedClientCount != 0) {
                isDeallocating = false;
                deallocatingCancellationToken = true;
            }

            if (backfillTicketId != null && connectedClientCount < 4) {
                //Debug.Log("Backfill ticket exists !");
                BackfillTicket backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillTicketId);
                backfillTicketId = backfillTicket.Id;
            }

            UpdateBackfillTicket();

            await Task.Delay(1000);
        }
#endif
    }

    private async void Deallocate() {
        Debug.Log("Deallocating");
        await Task.Delay(60 * 1000);

        if (ServerPlayerTracker.ConnectedClientIds.Count == 0) {
            Debug.Log("FINAL COUNT: " + ServerPlayerTracker.ConnectedClientIds.Count);
            Application.Quit();
        } else {
            Debug.Log("Ruh roh raggy !");
        }
    }

    [System.Serializable]
    public class PayloadAllocation {
        public MatchProperties matchProperties;
        public string GeneratorName;
        public string QueueName;
        public string PoolName;
        public string EnvironmentId;
        public string BackfillTicketId;
        public string MatchId;
        public string PoolId;
    }

}

