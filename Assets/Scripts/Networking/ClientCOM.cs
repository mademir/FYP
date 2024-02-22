using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace ClientCOM
{
    public class Values
    {
        public const int NodeIDLength = 8;

        public const int TagLength = 5;
        public const string TransformTag = "TRANS";
        public const string VoiceTag = "VOICE";
    }
    public class TransformInfo
    {
        public Vector3 Position { get; set; } = Vector3.zero;
        public Quaternion Rotation { get; set; } = Quaternion.identity;

        public TransformInfo() { }

        public TransformInfo(Transform transform)
        {
            Position = transform.position;
            Rotation = transform.rotation;
        }
        public TransformInfo(string jsonData)
        {
            var info = JsonConvert.DeserializeObject<List<float[]>>(jsonData);
            var pos = info[0];
            Position = new Vector3(pos[0], pos[1], pos[2]);
            var rot = info[1];
            Rotation = new Quaternion(rot[0], rot[1], rot[2], rot[3]);
        }

        public string Serialise()
        {
            var jsonPosition = new float[3] {Position.x, Position.y, Position.z};
            var jsonRotation = new float[4] {Rotation.x, Rotation.y, Rotation.z, Rotation.w};

            var data = new List<float[]>() {jsonPosition, jsonRotation};

            return JsonConvert.SerializeObject(data);
        }

        public static bool Compare(TransformInfo info1, TransformInfo info2)
        {
            float minPosDiff = 0.001f;
            float minRotDiff = 0.001f;
            return (Mathf.Abs(Vector3.Distance(info1.Position, info2.Position)) < minPosDiff) && (Mathf.Abs(Quaternion.Angle(info1.Rotation, info2.Rotation)) < minRotDiff);
        }
    }

    public class VoiceInfo
    {
        public byte[] Data;
        public int Channels;

        public VoiceInfo(byte[] data, int channels)
        {
            Data = data;
            Channels = channels;
        }
    }
}