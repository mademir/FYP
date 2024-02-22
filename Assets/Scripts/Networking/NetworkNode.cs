using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class NetworkNode : MonoBehaviour
{
    public class ActionCodes
    {
        public const int ActionCodeLength = 5;
        public const string SetMaterial = "MATER";
    }

    public string ID = new string('0', ClientCOM.Values.NodeIDLength);

    // When receiving the message to run this, set isLocal to false
    public void SetMaterial(string materialName, Client client, bool isLocal = true)
    {
        gameObject.GetComponent<Renderer>().material = Resources.Load(materialName, typeof(Material)) as Material;

        //if (isLocal) new Thread(() => client.SendTCPMessage("FORW" + client.MyClientID + ID.ToString() + ActionCodes.SetMaterial + materialName)).Start();
        if (isLocal) client.nodeTcpMessagePool.Add("FORW" + client.MyClientID + ID.ToString() + ActionCodes.SetMaterial + materialName);
    }
}
