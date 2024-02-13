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
    int lastSample;
    AudioClip c;
    int FREQUENCY = 44100;
    private float timer;

    void Start()
    {
        c = Microphone.Start(null, true, 100, FREQUENCY);
        while (Microphone.GetPosition(null) < 0) { }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > .2f)
        {
            timer = 0;
            int pos = Microphone.GetPosition(null);
            int diff = pos - lastSample;
            if (diff > 0)
            {
                float[] samples = new float[diff * c.channels];
                c.GetData(samples, lastSample);
                byte[] ba = ToByteArray(samples);
                SendVoice(ba, c.channels);
            }
            lastSample = pos;
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
        audioSource.clip = AudioClip.Create("test", f.Length, chan, FREQUENCY, false);
        audioSource.clip.SetData(f, 0);
        if (!audioSource.isPlaying) audioSource.Play();
    }

    public byte[] ToByteArray(float[] floatArray)
    {
        int len = floatArray.Length * 4;
        byte[] byteArray = new byte[len];
        int pos = 0;
        foreach (float f in floatArray)
        {
            byte[] data = System.BitConverter.GetBytes(f);
            System.Array.Copy(data, 0, byteArray, pos, 4);
            pos += 4;
        }
        return byteArray;
    }

    public float[] ToFloatArray(byte[] byteArray)
    {
        int len = byteArray.Length / 4;
        float[] floatArray = new float[len];
        for (int i = 0; i < byteArray.Length; i += 4)
        {
            floatArray[i / 4] = System.BitConverter.ToSingle(byteArray, i);
        }
        return floatArray;
    }
}
