using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class GameController : MonoBehaviour
{
    public Client client;
    public Player MyPlayer;

    public GameObject MainMenu;
    public GameObject LobbiesView;
    public GameObject LobbyView;

    public GameObject ConnectionLostMsg;

    public GameObject MuteToggle;
    public GameObject NameField;

    public LobbyController lobbyController;

    public AudioSource AudioPlayer;
    public string MyName { get { return NameField.GetComponentInChildren<TMP_InputField>().text; } }

    public Lobby MyLobby { get; private set; }

    List<GameObject> AllItems = new List<GameObject>();
    List<GameObject> MainMenuItems = new List<GameObject>();
    List<GameObject> LobbiesItems = new List<GameObject>();
    List<GameObject> LobbyItems = new List<GameObject>();

    public enum AppState
    {
        MainMenu,
        Lobbies,
        Lobby
    }

    public AppState CurrentAppState;

    public GameObject LobbyEntryPrefab;
    public Transform LobbyListGO;

    public bool ShowConnectionLost = false;
    bool tmpShowConnectionLost;

    // Start is called before the first frame update
    void Start()
    {
        AllItems = new List<GameObject> { MainMenu, NameField, LobbiesView, LobbyView, ConnectionLostMsg};
        MainMenuItems = new List<GameObject> { MainMenu, NameField };
        LobbiesItems = new List<GameObject> { LobbiesView };
        LobbyItems = new List<GameObject> { LobbyView };

        tmpShowConnectionLost = ShowConnectionLost;

        SwitchToMainMenu();
    }

    // Update is called once per frame
    void Update()
    {
        if (ShowConnectionLost != tmpShowConnectionLost)
        {
            ConnectionLostMsg.SetActive(ShowConnectionLost);
            tmpShowConnectionLost = ShowConnectionLost;
        }
            
    }

    public void onMuteToggle()
    {
        bool muteState = MuteToggle.activeSelf;
        MuteToggle.SetActive(!muteState);
        if (muteState) AudioPlayer.UnPause();
        else AudioPlayer.Pause();
    }

    void SetupView(List<GameObject> items)
    {
        foreach (GameObject item in AllItems) { item.SetActive(false); }
        foreach (GameObject item in items) { item.SetActive(true); }
    }

    public void SwitchToMainMenu()
    {
        SetupView(MainMenuItems);
        CurrentAppState = AppState.MainMenu;
    }

    public void SwitchToLobbies()
    {
        if (LobbyView.activeSelf) client.ReqLeaveLobby(); //if exiting lobby, tell cc to call leave lobby
        if (MyName == "") return;
        MyPlayer = new Player(MyName, client.MyClientID);
        SetupView(LobbiesItems);
        CurrentAppState = AppState.Lobbies;
        client.CheckConnection();
        client.ReqListLobbies();
    }

    public void SwitchToLobby()
    {
        SetupView(LobbyItems);
        CurrentAppState = AppState.Lobby;
    }

    private void OnApplicationQuit()
    {
        CloseGame();
    }

    public void CloseGame()
    {
        client?.Disconnect();
        Application.Quit();
    }

    public void UpdateLobbyList(List<Lobby> result)
    {
        if (CurrentAppState != AppState.Lobbies) return;

        //Clear lobbies
        foreach (Transform child in LobbyListGO)
        {
            Destroy(child.gameObject);
        }

        if (LobbiesView.activeSelf) //if in lobbies view
        {
            foreach (Lobby lobby in result)
            {
                GameObject entry = Instantiate(LobbyEntryPrefab, LobbyListGO);

                entry.transform.Find("name").GetComponent<TextMeshProUGUI>().text = lobby.Name;

                entry.transform.Find("id").GetComponent<TextMeshProUGUI>().text = "ID: " + lobby.ID;

                entry.transform.Find("capacity").GetComponent<TextMeshProUGUI>().text = lobby.Full ? "2/2" : "1/2";
                entry.transform.Find("capacity").GetComponent<TextMeshProUGUI>().color = lobby.Full ? Color.green : Color.red;

                entry.GetComponentInChildren<Button>().clicked += (() => client.ReqJoinLobby(lobby.ID, MyPlayer));
            }
        }
    }

    public void JoinLobby(Lobby lobby)
    {
        MyLobby = lobby;
        SwitchToLobby();
        UpdateLobby();
    }

    public void UpdateLobby()
    {
        throw new NotImplementedException();
    }

    public void CreateLobby()
    {
        throw new NotImplementedException();
    }
}
