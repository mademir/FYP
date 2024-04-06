using ClientCOM;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VoiceChat : MonoBehaviour
{
    public AudioSource audioSource;
    public List<AudioSource> audioSources = new List<AudioSource>(2);
    public Client client;
    //internal Stack<string> VoiceData = new Stack<string>();
    internal List<string> VoiceData = new List<string>();
    int lastSamplePosition;
    AudioClip clip;
    public int Frequency = 44100;
    float timer;
    public float Delay = .2f;
    public bool Talking = false;
    public KeyCode PushToTalkKey = KeyCode.V;

    int audioSourceToggle = 0;
    double prevClipEndDSP = 0;
    List<AudioClip> clipPool = new List<AudioClip>();

    void Start()
    {
        int min, max;
        Microphone.GetDeviceCaps(null, out min, out max);
        Debug.Log($"Mic frequency min: {min}, max: {max}");
        //Frequency = max;
        clip = Microphone.Start(null, true, 1800, Frequency);
        while (Microphone.GetPosition(null) < 0) { }
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

        // Receive clips from voice data
        if (VoiceData.Count > 0)
        {
            ReceiveVoice(VoiceData[0]);
            VoiceData.RemoveAt(0);
        }

        // Schedule the next clip on the next audio source to play right after the previous clip that is playing on the other audio source
        for (int i = 0; i < 2; i++)
        {
            if (clipPool.Count > 0 && !audioSources[audioSourceToggle].isPlaying)   // If there are clips to play and the next audio source is free
            {
                audioSources[audioSourceToggle].clip = clipPool[0]; // Set the next clip to play
                if (prevClipEndDSP < AudioSettings.dspTime) prevClipEndDSP = AudioSettings.dspTime; // If the last clip already ended, set the last clip end time to now
                audioSources[audioSourceToggle].PlayScheduled(prevClipEndDSP);  // Schedule the next clip to play at the time when the last clip ends
                prevClipEndDSP = prevClipEndDSP + clipPool[0].length;   // Set the clip's end time to it's start time (previous clip's end time) + it's length
                audioSourceToggle = 1 - audioSourceToggle;  // Toggle the audio source

                clipPool.RemoveAt(0);
            }
        }
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
        
        /*audioSource.clip = AudioClip.Create("test", f.Length, chan, Frequency, false);
        audioSource.clip.SetData(f, 0);
        if (!audioSource.isPlaying) audioSource.Play();*/

        var clip = AudioClip.Create("test", f.Length, chan, Frequency, false);
        clip.SetData(f, 0);
        clipPool.Add(clip);

        /*audioSources[audioSourceToggle].clip = clip;
        if (prevClipEndDSP < AudioSettings.dspTime) prevClipEndDSP = AudioSettings.dspTime + .2f;
        audioSources[audioSourceToggle].PlayScheduled(prevClipEndDSP);
        prevClipEndDSP = AudioSettings.dspTime + clip.length;
        audioSourceToggle = 1 - audioSourceToggle;*/
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
        byte[] byteData = new byte[2 * floatData.Length];
        foreach (float f in floatData)
        {
            byte[] data = BitConverter.GetBytes((short)(short.MaxValue * f));   // The float values are between 1.0f and -1.0f according to Unity
            Array.Copy(data, 0, byteData, pos, 2);
            pos += 2;
        }
        return byteData;

        /*int pos = 0;
        byte[] byteData = new byte[4 * floatData.Length];
        foreach (float f in floatData)
        {
            byte[] data = BitConverter.GetBytes(f);
            Array.Copy(data, 0, byteData, pos, 4);
            pos += 4;
        }
        return byteData;*/
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

        float[] floatData = new float[byteData.Length / 2];
        for (int i = 0; i < byteData.Length; i += 2) floatData[i / 2] = BitConverter.ToInt16(byteData, i) / (float)(short.MaxValue);
        return floatData;

        /*float[] floatData = new float[byteData.Length / 4];
        for (int i = 0; i < byteData.Length; i += 4) floatData[i / 4] = BitConverter.ToSingle(byteData, i);
        return floatData;*/
    }
}
