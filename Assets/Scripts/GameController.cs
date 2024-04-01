using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static NetworkNode;

public class GameController : MonoBehaviour
{
    public Client client;
    public Player MyPlayer;
    public GameObject PlayerGO;
    public GameObject PeerGO;
    public string playerTag = "A";
    public List<GameObject> ASpawns = new List<GameObject>();
    public List<GameObject> BSpawns = new List<GameObject>();
    public Transform CurrentSpawn;

    public GameObject UI;
    public GameObject MainMenu;
    public GameObject LobbiesView;
    public GameObject LobbyView;

    public GameObject ConnectionLostMsg;

    public GameObject MuteToggle;
    public GameObject NameField;

    public LobbyController lobbyController;

    public PressurePlatePuzzleController PressurePlatePuzzle;

    public AudioSource AudioPlayer;
    public string MyName { get { return NameField.GetComponentInChildren<TMP_InputField>().text; } }

    public Material PlayerAMaterial;
    public Material PlayerBMaterial;

    List<GameObject> AllItems = new List<GameObject>();
    List<GameObject> MainMenuItems = new List<GameObject>();
    List<GameObject> LobbiesItems = new List<GameObject>();
    List<GameObject> LobbyItems = new List<GameObject>();

    public List<NetworkNode> Checkpoint1Doors = new List<NetworkNode>();
    public List<NetworkNode> Checkpoint2Doors = new List<NetworkNode>();
    public List<NetworkNode> Swings = new List<NetworkNode>();

    public enum AppState
    {
        MainMenu,
        Lobbies,
        Lobby
    }

    internal AppState CurrentAppState;

    public GameObject LobbyEntryPrefab;
    public Transform LobbyListGO;

    public bool ShowConnectionLost = false;
    bool tmpShowConnectionLost;

    internal List<Action> ExecuteOnMainThread = new List<Action>();

    int currentCheckpointIndex = 0;

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
        
        foreach (var action in ExecuteOnMainThread) {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        ExecuteOnMainThread.Clear();
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
        MyPlayer = new Player(MyName, client.MyClientID, client.LocalUdpPort);
        SetupView(LobbiesItems);
        CurrentAppState = AppState.Lobbies;
        //client.CheckConnection();
        client.ReqListLobbies();
    }

    public void SwitchToLobby()
    {
        SetupView(LobbyItems);
        CurrentAppState = AppState.Lobby;
    }

    internal void SwitchToGame()
    {
        UI.SetActive(false);
        PlayerGO.GetComponent<PlayerController>().canMove = true;
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
                entry.transform.Find("capacity").GetComponent<TextMeshProUGUI>().color = lobby.Full ? Color.red : Color.green;

                entry.GetComponentInChildren<Button>().onClick.AddListener(() => client.ReqJoinLobby(lobby.ID, MyPlayer));//.clicked += (() => client.ReqJoinLobby(lobby.ID, MyPlayer));
            }
        }
    }

    public void JoinLobby(Lobby lobby)
    {
        lobbyController.MyLobby = lobby;
        SwitchToLobby();
        lobbyController.UpdateLobby();
    }

    public void Teleport(GameObject gameObject, Transform target)
    {
        gameObject.transform.position = target.position;
        gameObject.transform.rotation = target.rotation;
    }

    public void OnCheckpointReached(int c)
    {
        if (currentCheckpointIndex >= c) return;
        currentCheckpointIndex = c;
        client.nodeTcpMessagePool.Add("FORW" + client.MyClientID + VariableUpdateCodes.VariableUpdateSpecialID + VariableUpdateCodes.SetCurrentCheckpointIndex + c.ToString()); //Update Peer Checkpoint
        UpdateSpawn();
        switch (c)
        {
            case 1:
                Debug.Log("Checkpoint 1 Reached");
                foreach (NetworkNode doors in Checkpoint1Doors) doors.SetAnimationTrigger("TrReached", client, true);
                foreach (NetworkNode swing in Swings) swing.SetAnimationTrigger("TrSwing", client, true);
                break;
            case 2:
                Debug.Log("Checkpoint 2 Reached");
                foreach (NetworkNode doors in Checkpoint2Doors) doors.SetAnimationTrigger("TrReached", client, true);
                break;
            default: break;
        }
    }

    private void UpdateSpawn() => CurrentSpawn = ((playerTag == "A") ? ASpawns[currentCheckpointIndex] : BSpawns[currentCheckpointIndex]).transform;

    public class VariableUpdateCodes
    {
        public const int VariableUpdateCodeLength = 5;
        public const string VariableUpdateSpecialID = "11111111";
        public const string SetCurrentCheckpointIndex = "CURCP";
    }

    public void ParseVariableUpdate(string message)
    {
        string msg = message.Substring(ClientCOM.Values.NodeIDLength);
        if (msg.Length < VariableUpdateCodes.VariableUpdateCodeLength) return;
        string variableUpdateCode = msg.Substring(0, VariableUpdateCodes.VariableUpdateCodeLength);
        string value = msg.Substring(VariableUpdateCodes.VariableUpdateCodeLength);

        switch (variableUpdateCode)
        {
            case VariableUpdateCodes.SetCurrentCheckpointIndex:
                currentCheckpointIndex = int.Parse(value);
                ExecuteOnMainThread.Add(() => UpdateSpawn());  //Peer reached checkpoint
                //If first checkpoint, make the swings invisible for player B
                if (currentCheckpointIndex == 1 && playerTag == "B")
                    ExecuteOnMainThread.Add(() => {foreach (NetworkNode swing in Swings) swing.GetComponent<Swingy>().MakeInvisible();});
                break;
            default:
                Console.WriteLine($"Unknown Variable Update Code: {variableUpdateCode}");
                break;
        }
    }
}
