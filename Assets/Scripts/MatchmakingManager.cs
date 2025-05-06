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

        //Ensures only for client
        if (Application.platform != RuntimePlatform.LinuxServer) {

            //Initialize Unity Services - for Multiplayer hosting 
            await UnityServices.InitializeAsync();

            //Gets the random player ID 
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            string playerId = AuthenticationService.Instance.PlayerId;

        } else {
//Ensures this code will not try to compile in editor even when set to Linux Server
#if UNITY_SERVER

            //Initializes Multiplay Services on the server side
            while (UnityServices.State == ServicesInitializationState.Uninitialized || UnityServices.State == ServicesInitializationState.Initializing) {
                await Task.Yield();
            }

            //Create a backfill ticket ID
            matchmakerService = MatchmakerService.Instance;
            payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<PayloadAllocation>();
            backfillTicketId = payloadAllocation.BackfillTicketId;

#endif
        }    

        //Eventually this code would be through a join button in lobby
        //Creates a ticket using the current queue
        CreateTicketOptions createTicketOptions = new CreateTicketOptions("test");

        //Should only be one player currently; list is for multiple clients joining after Lobby is set up
        List<Unity.Services.Matchmaker.Models.Player> players = new List<Unity.Services.Matchmaker.Models.Player> { new Unity.Services.Matchmaker.Models.Player(AuthenticationService.Instance.PlayerId)};
        Debug.Log($"Player ID in Matchmaker Manager: {AuthenticationService.Instance.PlayerId}");

        //Creates a ticket with the players joining and the ticket options
        CreateTicketResponse createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);
        currentTicket = createTicketResponse.Id;
        Debug.Log("Ticket Created!");

        //Forever loop while trying to connect
        //We can use this because matchmaker is set to time out tickets after 40 seconds, which ends this loop
        while (true) {

            //Wait to hear from Matchmaker what happened to the ticket
            TicketStatusResponse ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(createTicketResponse.Id);

            //If it gets a response
            if (ticketStatusResponse.Type == typeof(MultiplayAssignment)) {

                //Stores the response
                MultiplayAssignment multiplayAssignment = (MultiplayAssignment)ticketStatusResponse.Value;

                //If a server is found
                if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Found) {

                    //Creates the client world in Unity
                    World clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
                    entityManager = clientWorld.EntityManager;

                    //Destroys the base local world
                    foreach (World world in World.All) {
                        if (world.Flags == WorldFlags.Game) {
                            world.Dispose();
                            break;
                        }
                    }

                    //Sets client code to apply to client world
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

                    //Creates an endpoint; place to connect to, using the IP and Port
                    NetworkEndpoint connectNetworkEndpoint = NetworkEndpoint.Parse(ip, ushort.Parse(port.ToString()));
                    RefRW<NetworkStreamDriver> networkStreamDriver = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();

                    //Sets the client world to be into the server
                    networkStreamDriver.ValueRW.Connect(clientWorld.EntityManager, connectNetworkEndpoint);

                    return;

                }

                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Timeout) {

                    //Server could not be found in time
                    Debug.Log("Timeout!");
                    return;

                }

                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.Failed) {

                    //All servers are full, or other issue occurs
                    Debug.Log("Failed!" + multiplayAssignment.Status + " " + multiplayAssignment.Message);
                    return;

                } 
                else if (multiplayAssignment.Status == MultiplayAssignment.StatusOptions.InProgress) {

                    //Sends every second to alert that is still running
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

        //Sends out a backfill ticket upon client connecting
        Debug.Log("Player connected!");
        if (Application.platform == RuntimePlatform.LinuxServer) {
            UpdateBackfillTicket();
        }

    }

    private void OnPlayerDisconnected()
    {

        //Sends out an updated backfill ticket with one less client connected on disconnect
        if (Application.platform == RuntimePlatform.LinuxServer) {
            UpdateBackfillTicket();
        }  

    }

#endif

    //Updates backfill ticket
    private async void UpdateBackfillTicket() {
#if UNITY_SERVER

        //Creates a list of players
        List<Unity.Services.Matchmaker.Models.Player> players = new List<Unity.Services.Matchmaker.Models.Player>();

        //Adds all players connected to server to list
        foreach (var playerId in ServerPlayerTracker.ConnectedClientIds) {
            players.Add(new Unity.Services.Matchmaker.Models.Player(playerId.ToString()));
        }

        //States the settings of this server
        MatchProperties matchProperties = new MatchProperties(null, players, null, backfillTicketId);

        //Update the backfill ticket with the list of players and match settings
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
    private async void Update()
    {
#if UNITY_SERVER

        if (Application.platform == RuntimePlatform.LinuxServer) {
            
            //Gets the number of players connected
            int connectedClientCount = ServerPlayerTracker.ConnectedClientIds.Count;
            
            //If nobody is connected, deallocate the server
            if (connectedClientCount == 0 && !isDeallocating) {
                isDeallocating = true;
                deallocatingCancellationToken = false;
                Deallocate();
            }

            //If clients are connected, stop deallocating
            if (connectedClientCount != 0) {
                isDeallocating = false;
                deallocatingCancellationToken = true;
            }

            //Approves backfill ticket if clients are connected
            if (backfillTicketId != null && connectedClientCount < 4) {
                //Debug.Log("Backfill ticket exists !");
                BackfillTicket backfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(backfillTicketId);
                backfillTicketId = backfillTicket.Id;
            }

            UpdateBackfillTicket();

            //Wait one second
            await Task.Delay(1000);
        }
#endif
    }

    //Deallocates unused servers
    private async void Deallocate() {

        Debug.Log("Deallocating");

        //Waits one minute
        await Task.Delay(60 * 1000);

        //Checks again if nobody is connected
        if (ServerPlayerTracker.ConnectedClientIds.Count == 0) {

            Debug.Log("FINAL COUNT: " + ServerPlayerTracker.ConnectedClientIds.Count);

            //Disconnects server
            Application.Quit();

        } else {

            Debug.Log("Server NOT deallocated!");

        }
    }

    //Payload allocation; for backfill ticket id and queue/match information
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

