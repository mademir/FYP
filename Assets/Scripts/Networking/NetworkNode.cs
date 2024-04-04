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
        public const string SetAnimationTrigger = "ANIMT";
        public const string SetIsKinematic = "ISKNM";
        public const string ResetLocalTransform = "RSTLT";
    }

    public string ID = new string('0', ClientCOM.Values.NodeIDLength);

    // When receiving the message to run this, set isLocal to false
    public void SetMaterial(string materialName, Client client, bool isLocal = true)
    {
        gameObject.GetComponent<Renderer>().material = Resources.Load(materialName, typeof(Material)) as Material;

        //if (isLocal) new Thread(() => client.SendTCPMessage("FORW" + client.MyClientID + ID.ToString() + ActionCodes.SetMaterial + materialName)).Start();
        if (isLocal) client.nodeTcpMessagePool.Add("FORW" + client.MyClientID + ID.ToString() + ActionCodes.SetMaterial + materialName);
    }

    public void SetAnimationTrigger(string trigger, Client client, bool isLocal = true)
    {
        gameObject.GetComponent<Animator>().SetTrigger(trigger);

        if (isLocal) client.nodeTcpMessagePool.Add("FORW" + client.MyClientID + ID.ToString() + ActionCodes.SetAnimationTrigger + trigger);
    }

    public void SetIsKinematic(string value, Client client, bool isLocal = true)
    {
        GetComponent<Rigidbody>().isKinematic = value == "true";

        if (isLocal) client.nodeTcpMessagePool.Add("FORW" + client.MyClientID + ID.ToString() + ActionCodes.SetIsKinematic + value);
    }

    public void ResetLocalTransform(Client client, bool isLocal = true)
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (isLocal) client.nodeTcpMessagePool.Add("FORW" + client.MyClientID + ID.ToString() + ActionCodes.ResetLocalTransform);
    }
}
