using System.Data;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.NetCode;
using JetBrains.Annotations;

//Lobby code - functional but not used in final project
public class TestLobby : MonoBehaviour
{


    private Lobby hostLobby;
    private float heartbeatTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        //Async lets you run code asynchronously - without await, the game would freeze until receives reply from server
        await UnityServices.InitializeAsync();

        //When receives response, keep initializing. 

        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        //No need for a username or password - in future can link to Steam account
        //but here automatically logs player in.
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    // Update is called once per frame
    void Update()
    {
        HandleLobbyHeartbeat();
    }

    //Lobbys become inactive after 30 seconds
    private async void HandleLobbyHeartbeat() {
        if(hostLobby != null) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f) {
                float heartbeatTimerMax = 20;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    //Creates a lobby
    
    private async void CreateLobby() {


        try {
            string lobbyName = "myLobby";
            int maxPlayers = 4;

            //Makes the lobby private
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions { IsPrivate = true };
            
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);

            hostLobby = lobby;

            Debug.Log("Created lobby! " + lobby.Name + " " + lobby.MaxPlayers);

        } catch (LobbyServiceException e) {

            Debug.Log(e);

        }
    }








    //Add a create lobby button and a join lobby button

    private async void JoinLobby() {
        try {

            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync("lobbyCode");

        } catch (LobbyServiceException e) {

            Debug.Log(e);

        }
    }

}
