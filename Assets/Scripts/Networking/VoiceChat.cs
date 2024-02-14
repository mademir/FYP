using ClientCOM;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceChat : MonoBehaviour
{
    public AudioSource audioSource;
    public Client client;
    internal Stack<string> VoiceData = new Stack<string>();
    int lastSamplePosition;
    AudioClip clip;
    public int Frequency = 44100;
    float timer;
    public float Delay = .2f;
    public bool Talking = false;
    public KeyCode PushToTalkKey = KeyCode.V;

    void Start()
    {
        clip = Microphone.Start(null, true, 150, Frequency);
        while (Microphone.GetPosition(null) < 0) { }
        int min, max;
        Microphone.GetDeviceCaps(null, out min, out max);
        Debug.LogError($"Mic frequency min: {min}, max: {max}");
    }

    void Update()
    {
        Talking = Input.GetKey(PushToTalkKey);

        if (Talking)
        {
            timer += Time.deltaTime;
            if (timer > Delay)
            {
                timer = 0;
                int audioPosition = Microphone.GetPosition(null);
                int difference = audioPosition - lastSamplePosition;
                if (difference > 0)
                {
                    float[] samples = new float[difference * clip.channels];
                    clip.GetData(samples, lastSamplePosition);
                    byte[] data = ToByteArray(samples);
                    SendVoice(data, clip.channels);
                }
                lastSamplePosition = audioPosition;
            }
        }

        /*int pos = Microphone.GetPosition(null);
        int diff = pos - lastSample;
        if (diff > 0)
        {
            float[] samples = new float[diff * c.channels];
            c.GetData(samples, lastSample);
            byte[] ba = ToByteArray(samples);
            SendVoice(ba, c.channels);
        }
        lastSample = pos;*/

        if (VoiceData.Count > 0) ReceiveVoice(VoiceData.Pop());
    }

    private void SendVoice(byte[] ba, int channels)
    {
        var voiceInfo = JsonConvert.SerializeObject(new VoiceInfo(ba, channels));
        client.SendUDPMessage(Values.VoiceTag + voiceInfo);
    }

    public void ReceiveVoice(string data)
    {
        VoiceInfo voiceInfo = JsonConvert.DeserializeObject<VoiceInfo>(data);
        var ba = voiceInfo.Data;
        var chan = voiceInfo.Channels;
        float[] f = ToFloatArray(ba);
        audioSource.clip = AudioClip.Create("test", f.Length, chan, Frequency, false);
        audioSource.clip.SetData(f, 0);
        if (!audioSource.isPlaying) audioSource.Play();
    }

    public byte[] ToByteArray(float[] floatData)
    {
        /*int len = floatArray.Length;
        byte[] byteArray = new byte[len];
        int pos = 0;
        foreach (float f in floatArray)
        {
            byteArray[pos++] = (byte)f;
        }
        return byteArray;*/
        
        /*int len = floatArray.Length * 2;
        byte[] byteArray = new byte[len];
        int pos = 0;
        foreach (float f in floatArray)
        {
            byte[] data = System.BitConverter.GetBytes((short)f);
            System.Array.Copy(data, 0, byteArray, pos, 2);
            pos += 2;
        }
        return byteArray;*/

        
        int pos = 0;
        byte[] byteData = new byte[4 * floatData.Length];
        foreach (float f in floatData)
        {
            byte[] data = BitConverter.GetBytes(f);
            Array.Copy(data, 0, byteData, pos, 4);
            pos += 4;
        }
        return byteData;
    }

    public float[] ToFloatArray(byte[] byteData)
    {
        /*int len = byteArray.Length;
        float[] floatArray = new float[len];
        for (int i = 0; i < byteArray.Length; i++)
        {
            floatArray[i] = byteArray[i];
        }
        return floatArray;*/

        /*int len = byteArray.Length / 2;
        float[] floatArray = new float[len];
        for (int i = 0; i < byteArray.Length; i += 2)
        {
            floatArray[i / 2] = System.BitConverter.ToInt16(byteArray, i);
        }
        return floatArray;*/

        float[] floatData = new float[byteData.Length / 4];
        for (int i = 0; i < byteData.Length; i += 4) floatData[i / 4] = BitConverter.ToSingle(byteData, i);
        return floatData;
    }
}
