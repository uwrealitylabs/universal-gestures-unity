using Meta.XR.MRUtilityKit;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;


// JSON File Writer
// JSON file will look like this:
// [
//    {"confidence":0, "handData":[...]},
//    {"confidence":1, "handData":[...]},
//    ...
// ]

// SHIFT to record positive data (confidence = 1)
// TAB to record negative data (confidence = 0)
// If SHIFT and TAB presseed at same time, no data will be recorded

public class JsonWriter : MonoBehaviour
{
    public string gestureName;
    class GestureData
    {
        public int confidence;
        public float[] handData;
    }
    // Start is called before the first frame update
    void JsonWrite(GestureData gestureData)
    {
        string prefix = ",\n    "; // Prefix & Suffix for each entry for proper json formatting
        string suffix = "\n]";
        string path = Application.dataPath + "/../JsonData/" + gestureName + ".json"; // Path to json file associated w gesture
        if (!File.Exists(path))
        {
            File.Create(path); // Create json if does not already exist
        }
        FileStream stream = new FileStream(path, FileMode.Open);
        if (stream.Length == 0)
        {
            prefix = "[\n    "; // Prefix for first entry
        }
        stream.Position = Math.Max(stream.Length - 2, 0);
        string jsonString = prefix + JsonUtility.ToJson(gestureData) + suffix;
        byte[] insertBytes = Encoding.ASCII.GetBytes(jsonString);
        stream.Write(insertBytes);
        Debug.Log("Writing to " + gestureName + ".json: '" + jsonString + "'");
        stream.Close();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.Tab))
        {
            GestureData gestureData = new GestureData();
            gestureData.confidence = 1;
            gestureData.handData = new float[10];
            for (int i = 0; i < 10; i++)
            {
                gestureData.handData[i] = UnityEngine.Random.Range(-10.0f, 10.0f); // Currently random data for testing purposes
            }
            JsonWrite(gestureData);
        } 
        else if (Input.GetKey(KeyCode.Tab) && !Input.GetKey(KeyCode.LeftShift))
        {
            GestureData gestureData = new GestureData();
            gestureData.confidence = 0;
            gestureData.handData = new float[10];
            for (int i = 0; i < 10; i++)
            {
                gestureData.handData[i] = UnityEngine.Random.Range(-10.0f, 10.0f);
            }
            JsonWrite(gestureData);
        }
    }
}
