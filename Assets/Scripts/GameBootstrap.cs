using Unity.NetCode;
using Unity.Services.Core;
using UnityEngine;
#if UNITY_SERVER
using Unity.Services.Multiplay;
using Unity.Services.Multiplayer;
using Unity.Services.Matchmaker;
#endif
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
//using UnityEditor.Build.Pipeline;

[UnityEngine.Scripting.Preserve]
public class GameBootstrap : ClientServerBootstrap {

    /*

    private string serverIpAddress;
    private ushort serverPort;
    private NetworkDriver driver;
    private NetworkConnection connection;
    public override bool Initialize(string defaultWorldName)
    {

        UnityServices.InitializeAsync().ContinueWith(task => 
        {
            if (task.Exception != null)
            {
                Debug.LogError("Unity Services initialization failed");
                return;
            }

            // Retrieve the server configuration from Multiplay
            var serverConfig = MultiplayService.Instance.ServerConfig;
            serverIpAddress = serverConfig.IpAddress;
            serverPort = serverConfig.Port;

            Debug.Log("port! game bootstrap: " + serverPort);

            // Once services are initialized and we have server info, we can connect
            AutoConnectPort = serverPort; // Set the port for connection
            ConnectToServer(); // Proceed with connecting to the server
        });

        return base.Initialize(defaultWorldName);

    }

    private void ConnectToServer() {
        driver = new NetworkDriver();
        var endpoint = NetworkEndpoint.Parse(serverIpAddress, serverPort);

        connection = driver.Connect(endpoint);

        Debug.Log($"Connecting to server at {serverIpAddress}:{serverPort}");

        //The connection is attempted; we need to handle it in Update
    }

    private void Update() {
        if (driver.IsCreated) {
            driver.ScheduleUpdate().Complete();

            NetworkEvent.Type eventType;
            while ((eventType = driver.PopEventForConnection(connection, out DataStreamReader stream)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    case NetworkEvent.Type.Connect:
                        Debug.Log("Successfully connected to the server!");
                        break;

                    case NetworkEvent.Type.Disconnect:
                        Debug.Log("Disconnected from the server.");
                        // Handle disconnection logic here, e.g., retry connection or notify the player
                        break;

                    case NetworkEvent.Type.Data:
                        // Handle data messages from the server (if needed)
                        break;
                }
            }
        }
    }

    private void OnApplicationQuit() {
        if (driver.IsCreated) {
            driver.Dispose();
        }
    }

    /* ORIGINAL 
    
    public override bool Initialize(string defaultWorldName)
    {

        AutoConnectPort = 7979;
        return base.Initialize(defaultWorldName);
    }
    
    

    async Awaitable StartServer() {
        await UnityServices.InitializeAsync();
        var server = MultiplayService.Instance.ServerConfig;

        //World world = World.DefaultGameObjectInjectionWorld;
        //var transport = world.GetOrCreateSystem<NetworkStreamConnectSystem>();
        //transport.SetConnectionData("0.0.0.0", server.Port);


    }

    */

    //For testing - removes this while we try to connect.
    public override bool Initialize(string defaultWorldName)
    {
        return false;
    }
}

/*
async Awaitable StartServer() {
    await UnityServices.InitializeAsync();
    var server = MultiplayService.Instance.ServerConfig;
    var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    transport.SetConnectionData("0.0.0.0", server.Port);

    var callbacks = new MultiplayEventCallbacks();
    callbacks.Allocate += OnAllocate;
    callbacks.Deallocate += OnDeallocate;
    callbacks.Error += OnError;
    callbacks.SubscriptionStateChanged += OnSubscriptionStateChanged;

    while (MultiplayService.Instance == null) {
        await Awaitable.NextFrameAsync();
    }

    var events = await MultiplayService.Instance.SubscribeToServerEventsAsync(callbacks);
    await CreateBackfillTicket();
} */