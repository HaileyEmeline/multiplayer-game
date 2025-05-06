using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Networking.Transport;

//Code to connect to the local server with the play button
//Not functional in final project because the local server does not exist
//Here to show the different process
public class UINetcodeForEntities : MonoBehaviour
{

    [SerializeField] private Button playGameButton;
    

    private bool isServerRunning = false;
    private bool isClicked = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        playGameButton.onClick.AddListener(OnPlayClicked);
    }

    private void OnPlayClicked() {
        //playGameButton.gameObject.SetActive(false);
        playGameButton.interactable = false;

        if (isClicked == false) {
        if (!isServerRunning) {
            StartServer();
        }
        else {
            JoinGame();
        }

        isClicked = true;
        } else {

        }

        
    }

    private void StartServer() {

        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().buildIndex);

        Debug.Log("Server started!");
        World serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        World clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");

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

        //StartCoroutine(LoadSceneAndStartServer("SampleScene", serverWorld, clientWorld));

        SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

        ushort port = 7979;

        RefRW<NetworkStreamDriver> networkStreamDriver = serverWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
        networkStreamDriver.ValueRW.Listen(NetworkEndpoint.AnyIpv4.WithPort(port));

        NetworkEndpoint connectNetworkEndpoint = NetworkEndpoint.LoopbackIpv4.WithPort(port);
        networkStreamDriver = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
        networkStreamDriver.ValueRW.Connect(clientWorld.EntityManager, connectNetworkEndpoint);

        isServerRunning = true;

    }

    private void JoinGame() {
        Debug.Log("Client Joined!");
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

        SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

        ushort port = 7979;
        string ip = "127.0.0.1";

        NetworkEndpoint connectNetworkEndpoint = NetworkEndpoint.Parse(ip, port);
        RefRW<NetworkStreamDriver> networkStreamDriver = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
        networkStreamDriver.ValueRW.Connect(clientWorld.EntityManager, connectNetworkEndpoint);

    }
}
