using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    public List<GameObject> DefaultSpawns = new List<GameObject>();
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
    public FallingTilesPuzzle fallingTilesPuzzle;

    public AudioSource AudioPlayer;
    public string MyName { get { return NameField.GetComponentInChildren<TMP_InputField>().text; } }

    public Material PlayerAMaterial;
    public Material PlayerBMaterial;

    List<GameObject> AllItems = new List<GameObject>();
    List<GameObject> MainMenuItems = new List<GameObject>();
    List<GameObject> LobbiesItems = new List<GameObject>();
    List<GameObject> LobbyItems = new List<GameObject>();

    public List<NetworkNode> EntranceDoors = new List<NetworkNode>();
    public List<NetworkNode> Checkpoint1Doors = new List<NetworkNode>();
    public List<NetworkNode> Checkpoint2Doors = new List<NetworkNode>();
    public List<NetworkNode> Checkpoint3Doors = new List<NetworkNode>();
    public List<NetworkNode> Checkpoint4Doors = new List<NetworkNode>();
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
    public bool PlayerTagsAssigned = false;

    // Start is called before the first frame update
    void Start()
    {
        AllItems = new List<GameObject> { MainMenu, NameField, LobbiesView, LobbyView, ConnectionLostMsg};
        MainMenuItems = new List<GameObject> { MainMenu, NameField };
        LobbiesItems = new List<GameObject> { LobbiesView };
        LobbyItems = new List<GameObject> { LobbyView };

        tmpShowConnectionLost = ShowConnectionLost;

        SwitchToMainMenu();

        //Auto switch to lobbies view if player name is saved (it is only saved when reloading the game)
        if (PlayerPrefs.HasKey("MyName"))
        {
            NameField.GetComponentInChildren<TMP_InputField>().text = PlayerPrefs.GetString("MyName");
            PlayerPrefs.DeleteKey("MyName");
            ExecuteOnMainThread.Add(SwitchToLobbies);   //To wait before executing
        }
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

        if (!PlayerTagsAssigned)
        {
            if (PlayerGO.transform.position.x < 35 && Math.Abs(PlayerGO.transform.position.z + 5.5f) > 4f
                && PeerGO.transform.position.x < 35 && Math.Abs(PeerGO.transform.position.z + 5.5f) > 4f) //Check if they passed entrance
            {
                // Assign player tags
                playerTag = PlayerGO.transform.position.z > -5.5f ? "A" : "B";
                PlayerTagsAssigned = true;
                Debug.Log("Player tags are assigned");
                if (playerTag == "A") PressurePlatePuzzle.SetupPuzzle(); // Only let Player A setup the puzzle
                UpdateSpawn();
                foreach (NetworkNode doors in EntranceDoors) doors.SetAnimationTrigger("TrOpen", client, true);
            }
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
        if (PlayerPrefs.HasKey("MyName")) PlayerPrefs.DeleteKey("MyName");
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
            case 3:
                Debug.Log("Checkpoint 3 Reached");
                foreach (NetworkNode doors in Checkpoint3Doors) doors.SetAnimationTrigger("TrReached", client, true);
                fallingTilesPuzzle.SetTileColours();
                break;
            case 4:
                Debug.Log("Checkpoint 4 Reached");
                foreach (NetworkNode doors in Checkpoint4Doors) doors.SetAnimationTrigger("TrReached", client, true);
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

    public void OnGameFinished() => client.ReqEndGame();

    public void ResetGame()
    {
        PlayerPrefs.SetString("MyName", MyName);
        SceneManager.LoadScene("SampleScene");
    }

    public void OnLobbyNameChange() => client.ReqLobbyNameUpdate(lobbyController.Name.text);
}
