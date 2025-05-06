using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;
using Unity.NetCode;
using UnityEngine.SceneManagement;
using Unity.Entities;
#if UNITY_SERVER
using Unity.Services.Multiplay;
#endif
using Unity.Networking.Transport;
using Unity.Entities.UniversalDelegates;
using System;
using System.Data.Common;
using System.Linq;
using System.IO;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using Unity.Collections;


public class MultiplayerManager : MonoBehaviour
{

#if UNITY_SERVER 
    ServerConfig serverConfig;
    private IServerQueryHandler serverQueryHandler;
    World serverWorld;
#endif

    private static bool isServerRunning = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
#if UNITY_SERVER

        //Only works server side
        if (Application.platform == RuntimePlatform.LinuxServer) {

            //Init server code
            await UnityServices.InitializeAsync();
            Debug.Log("Multiplayer Initialized!");

            Debug.Log(UnityServices.State);

            //Retrieves server IP and port
            serverConfig = MultiplayService.Instance.ServerConfig;
            Debug.Log("Server Config Retrieved!");
            
            Debug.Log("Server Port: " + serverConfig.Port);
            Debug.Log("Server ID: " + serverConfig.AllocationId);

            //Sets up server for game settings
            serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(10, "MyServer", "MyGameType", "0", "TestMap");

            //Checks to make sure a server exists
            if (serverConfig.AllocationId != string.Empty) {

                //Creates a server world
                serverWorld = ClientServerBootstrap.CreateServerWorld("serverWorld");

                //Destroys the base local world
                foreach (World world in World.All) {
                    if (world.Flags == WorldFlags.Game) {
                        world.Dispose();
                        break;
                    }
                }

                if (World.DefaultGameObjectInjectionWorld == null) {
                    World.DefaultGameObjectInjectionWorld = serverWorld;
                }

                //Loads current scene in server
                await SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single); 

                //Set connection data and start the server in Unity
                RefRW<NetworkStreamDriver> networkStreamDriver = serverWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
                networkStreamDriver.ValueRW.Listen(NetworkEndpoint.AnyIpv4.WithPort(serverConfig.Port));
                Debug.Log("Server connected!");

                await MultiplayService.Instance.ReadyServerForPlayersAsync();

                //JoinToServer();
            }

        } else {

        }
#endif
    }                

    // Update is called once per frame
    async void Update()
    {

#if UNITY_SERVER
        if (Application.platform == RuntimePlatform.LinuxServer)
        {

            //UpdatePlayerList();
            if (serverQueryHandler != null) {
                try
                {
                    serverQueryHandler?.UpdateServerCheck();
                }
                catch (Exception ex)
                {
                    Debug.LogError("ServerQueryHandler failed: " + ex.Message);

                    serverQueryHandler?.Dispose();
                    serverQueryHandler = null;
                }

                await Task.Delay(1000);
            }

    }
#endif
    }


//My initial join code - not what I ended up using, but a concept that led me in the right direction
#if UNITY_SERVER
    public void JoinToServer() {

        World clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");

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

        //SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

        ushort port = serverConfig.Port;

        //IP has to be 0.0.0.0 according to 
        string ip = serverConfig.IpAddress;
        Debug.Log("Port: " + port);

        NetworkEndpoint connectNetworkEndpoint = NetworkEndpoint.Parse(ip, port);
        RefRW<NetworkStreamDriver> networkStreamDriver = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
        networkStreamDriver.ValueRW.Connect(clientWorld.EntityManager, connectNetworkEndpoint);

        Debug.Log("Client Connected!");

    }

    //Dispose of query handler - removes memory leaks
    private void OnApplicationQuit() {
        serverQueryHandler?.Dispose();
    }

    private void OnDestroy()
    {
        serverQueryHandler?.Dispose();
    }

#endif

} 



/* ORIGINAL START - TO SHOW WORK


private async void Start()
    {

        Debug.Log("hello world");
        if (!isServerRunning) {
            Debug.Log("Hello World 2");
            if (!isServerRunning) { //Application.platform == RuntimePlatform.LinuxServer
                Debug.Log("Server world!");

                //SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().buildIndex);

                //StartCoroutine(LoadSceneAndStartServer("SampleScene", serverWorld, clientWorld));

                //SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

                //Connects to Multiplay Service
                
                //await UnityServices.InitializeAsync();
                //Debug.Log("Multiplayer Initialized!");

                try
                    {
                        // Initialize Unity Services
                        await UnityServices.InitializeAsync();
                        Debug.Log("Multiplayer Initialized!");

                        // Ensure that MultiplayService is initialized and can be accessed
                        if (MultiplayService.Instance != null)
                        {
                            // Access the serverConfig after confirming initialization
                            serverConfig = MultiplayService.Instance.ServerConfig;
                            Debug.Log("Server Config Retrieved!");
                        }
                        else
                        {
                            Debug.LogError("MultiplayService is not initialized properly.");
                        }
                    }
                catch (Exception ex)
                    {
                        Debug.LogError($"Failed to initialize Unity Services: {ex.Message}");
                    }
    

                //Allows us to access variables like the IP and port of the server
                //serverConfig = MultiplayService.Instance.ServerConfig;

                Debug.Log("Server Port: " + serverConfig.Port);
                Debug.Log("Server ID: " + serverConfig.AllocationId);

                serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(10, "MyServer", "MyGameType", "0", "TestMap");

                //If a server has been found
                if (serverConfig.AllocationId != string.Empty) {

                    World serverWorld = ClientServerBootstrap.CreateServerWorld("serverWorld");
                    //World clientWorld = ClientServerBootstrap.CreateClientWorld("clientWorld");

                    //Destroys the base local world
                    foreach (World world in World.All) {
                        if (world.Flags == WorldFlags.Game) {
                            world.Dispose();
                            break;
                        }
                    }

                    if (World.DefaultGameObjectInjectionWorld == null) {
                        World.DefaultGameObjectInjectionWorld = serverWorld;
                    }

                    //Set connection data and start the server in Unity
                    RefRW<NetworkStreamDriver> networkStreamDriver = serverWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
                    networkStreamDriver.ValueRW.Listen(NetworkEndpoint.AnyIpv4.WithPort(serverConfig.Port));
                    NetworkEndpoint connectNetworkEndpoint = NetworkEndpoint.LoopbackIpv4.WithPort(serverConfig.Port);

                    await MultiplayService.Instance.ReadyServerForPlayersAsync();

                    Debug.Log("Server connected!");

                    isServerRunning = true;
                    
                    //Connects client
                    //networkStreamDriver = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
                    //networkStreamDriver.ValueRW.Connect(clientWorld.EntityManager, connectNetworkEndpoint);
                }

                //Tells the server someone is playing; keeps server from shutting down

            }

            //JoinToServer();
            
            
        }

        if (isServerRunning) {
            JoinToServer();
        } 
        
    } */