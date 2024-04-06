using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class LobbyController : MonoBehaviour
{
    internal Lobby MyLobby;

    public Client client;
    public TMP_InputField Name;
    public Transform PlayerListGO;
    public GameObject StartBtn;
    public GameObject PlayerEntry;


    public void UpdateLobby()
    {
        Name.text = MyLobby.Name;
        ClearAllPlayers();
        if (MyLobby.PlayerA != null) AddPlayer(MyLobby.PlayerA);
        if (MyLobby.PlayerB != null) AddPlayer(MyLobby.PlayerB);

        string ownerID = (new List<COM.Client>() { MyLobby.PlayerA, MyLobby.PlayerB }).Where(c => c.LobbyLeader).First().ID;
        //if (client.MyClientID == ownerID) StartBtn.SetActive(true);
        StartBtn.SetActive(client.MyClientID == ownerID && MyLobby.PlayerA != null && MyLobby.PlayerB != null);
        Name.readOnly = client.MyClientID != ownerID;
    }

    public void ClearAllPlayers()
    {
        //Clear players
        foreach (Transform child in PlayerListGO)
        {
            Destroy(child.gameObject);
        }
    }

    public void AddPlayer(COM.Client player)
    {
        GameObject playerEntry = Instantiate(PlayerEntry, PlayerListGO);
        playerEntry.transform.Find("name").GetComponent<TextMeshProUGUI>().text = player.Name;
        if (player.LobbyLeader) playerEntry.transform.Find("owner").gameObject.SetActive(true);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
