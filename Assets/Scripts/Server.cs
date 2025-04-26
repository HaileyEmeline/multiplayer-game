/*using UnityEngine;
using Unity.NetCode;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using Unity.Services.Multiplayer;
using Unity.Services.Matchmaker;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using System.Threading.Tasks;



public class Server : MonoBehaviour
{

    private IServerQueryHandler serverQueryHandler;
    private NetworkDriver driver;
    private NativeList<NetworkConnection> connections;
    private bool serverStarted = false;
    private string ipAddress = "0.0.0.0";
    private ushort port;
    private int currentPlayerCount = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        if (Application.platform == RuntimePlatform.LinuxServer) {
            Application.targetFrameRate = 60;

            await UnityServices.InitializeAsync();

            ServerConfig serverConfig = MultiplayService.Instance.ServerConfig;

            serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(10, "MyServer", "MyGameType", "0", "TestMap");

            //ServerConfig gives us the server ID, port, address, etc
            if (serverConfig.AllocationId != string.Empty) {
                //NetworkManager - sets connection data to ip 0.0.0.0, port to serverConfig.Port,  same ip again
                //NetworkManager - startserver
                ipAddress = serverConfig.IpAddress;
                port = serverConfig.Port;
                Debug.Log("Port! Server code:" + port);

                await MultiplayService.Instance.ReadyServerForPlayersAsync();

                StartServer();
            }
        }
    }

    // Update is called once per frame
    private async void Update()
    {
        /*if (Application.platform == RuntimePlatform.LinuxServer) {
            if (serverQueryHandler != null) {
                serverQueryHandler.CurrentPlayers = 0; //NetworkManager ConnectedClientID Count
                serverQueryHandler.UpdateServerCheck();
                await Task.Delay(100);
            }
        } */ /*

        if (driver.IsCreated)
        {
            driver.ScheduleUpdate().Complete();

            NetworkEvent.Type eventType;
            while ((eventType = driver.PopEventForConnection(connections[0], out DataStreamReader stream)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    case NetworkEvent.Type.Connect:
                        Debug.Log("Player connected to the server!");
                        currentPlayerCount++;  // Increment the player count
                        UpdatePlayerCount();
                        break;

                    case NetworkEvent.Type.Disconnect:
                        Debug.Log("Player disconnected from the server.");
                        currentPlayerCount--;  // Decrement the player count
                        UpdatePlayerCount();
                        break;

                    case NetworkEvent.Type.Data:
                        // Handle data messages if necessary
                        break;
                }
            }
        }
    }

    private void UpdatePlayerCount()
    {
        if (serverQueryHandler != null)
        {
            serverQueryHandler.CurrentPlayers = (ushort)currentPlayerCount;
            serverQueryHandler.UpdateServerCheck();
        }
    }

    public void StartServer() {
        //driver = NetworkDriver.Create(new NetworkStream;
        var endpoint = NetworkEndpoint.Parse(ipAddress, port);
        endpoint.Port = port;
        driver = NetworkDriver.Create();
        connections = new NativeList<NetworkConnection>(Allocator.Persistent);
        

    }

    private void OnApplicationQuit()
    {
        if (driver.IsCreated) {
            driver.Dispose();
        }
    }

}*/