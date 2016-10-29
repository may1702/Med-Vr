using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Handles saving/loading of replay data.
/// </summary>
public static class ReplayDataHandler {

    static public string path = Application.dataPath + Path.AltDirectorySeparatorChar + "Recordings" + Path.AltDirectorySeparatorChar;
    static StringBuilder sb;

    /// <summary>
    /// Write data for all frames
    /// </summary>
    /// <param name="timeline">Constructed timeline</param>
    public static void WriteReplayObjectData(Dictionary<int, List<ObjectFrame>> objectTimeline, string savename) {
        sb = new StringBuilder();
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    
        foreach (int key in objectTimeline.Keys) {
            sb.AppendLine("*FRAME" + key);
            foreach (ObjectFrame frame in objectTimeline[key]) {
                //Object frames
                sb.AppendLine(">OBJECTFRAME");

                sb.AppendLine(frame.FrameIndex.ToString());
                sb.AppendLine(frame.ObjectName);

                sb.AppendLine(frame.Options.RecordPosition.ToString());
                sb.AppendLine(frame.Options.RecordLocalPosition.ToString());
                sb.AppendLine(frame.Options.RecordEulerAngles.ToString());
                sb.AppendLine(frame.Options.RecordLocalEulerAngles.ToString());
                sb.AppendLine(frame.Options.RecordLocalScale.ToString());
                sb.AppendLine(frame.Options.RecordRotation.ToString());
                sb.AppendLine(frame.Options.RecordLocalRotation.ToString());

                if (frame.Options.RecordPosition) sb.AppendLine(Vector3String(frame.Position));
                if (frame.Options.RecordLocalPosition) sb.AppendLine(Vector3String(frame.LocalPosition));
                if (frame.Options.RecordEulerAngles) sb.AppendLine(Vector3String(frame.EulerAngles));
                if (frame.Options.RecordLocalEulerAngles) sb.AppendLine(Vector3String(frame.LocalEulerAngles));
                if (frame.Options.RecordLocalScale) sb.AppendLine(Vector3String(frame.LocalScale));
                if (frame.Options.RecordRotation) sb.AppendLine(QuaternionString(frame.Rotation));
                if (frame.Options.RecordLocalRotation) sb.AppendLine(QuaternionString(frame.LocalRotation));
            }
        }

        Debug.Log("Saving to " + path + savename + ".dat");
        System.IO.File.WriteAllText(path + savename + ".dat", sb.ToString());
    }

    /// <summary> 
    /// Read data for frame timeline from a save in default dir
    /// </summary>
    /// <param name="savename">Name of the saved replay to load</param>
    /// <returns>The saved frame timeline</returns>
    public static Dictionary<int, List<ObjectFrame>> ReadReplayObjectData(string savename, out List<int> activeFrames) {
        activeFrames = new List<int>();

        if (!File.Exists(path + savename + ".dat")) { 
            Debug.Log("Could not find save at " + (path + savename + ".dat"));
            return null;
        }

        FileStream stream = new FileStream(path + savename + ".dat", FileMode.Open);
        StreamReader reader = new StreamReader(stream);

        Dictionary<int, List<ObjectFrame>> timeline = new Dictionary<int, List<ObjectFrame>>();

        string loadedData = reader.ReadToEnd();
        string[] loadedFrames = loadedData.Split('*');

        List<ObjectFrame> objFrames = new List<ObjectFrame>();
        int frameIndex = 0;

        //Skip string 0 - always blank
        for (int i = 1; i < loadedFrames.Length; i++) {       
            string[] frameData = loadedFrames[i].Split('>');

            for (int j = 0; j < frameData.Length; j++) {
                if (frameData[j].Trim().StartsWith("FRAME")) {
                    if (!(objFrames.Count == 0)) {
                        timeline.Add(frameIndex, objFrames);
                        activeFrames.Add(frameIndex);
                        objFrames = new List<ObjectFrame>(objFrames);
                    }
                    int.TryParse(frameData[j].Replace("FRAME", ""), out frameIndex);
                } else {
                    //Frame parameters
                    string objectName;
                    bool RecordPosition, RecordLocalPosition,
                    RecordEulerAngles, RecordLocalEulerAngles,
                    RecordRotation, RecordLocalRotation,
                    RecordLocalScale;
                    Vector3 pos = Vector3.zero,
                            localpos = Vector3.zero,
                            euler = Vector3.zero,
                            localeuler = Vector3.zero,
                            localscale = Vector3.zero;
                    Quaternion rotation = new Quaternion(),
                               localrotation = new Quaternion();

                    string[] frameVars = frameData[j].Split('\n');
                    int.TryParse(frameVars[1], out frameIndex);
                    objectName = frameVars[2].Trim();
                    bool.TryParse(frameVars[3], out RecordPosition);
                    bool.TryParse(frameVars[4], out RecordLocalPosition);
                    bool.TryParse(frameVars[5], out RecordEulerAngles);
                    bool.TryParse(frameVars[6], out RecordLocalEulerAngles);
                    bool.TryParse(frameVars[7], out RecordLocalScale);
                    bool.TryParse(frameVars[8], out RecordRotation);
                    bool.TryParse(frameVars[9], out RecordLocalRotation);

                    int dataIndex = 10;

                    if (RecordPosition) pos = ReadVector3String(frameVars[dataIndex++]);
                    if (RecordLocalPosition) localpos = ReadVector3String(frameVars[dataIndex++]);
                    if (RecordEulerAngles) euler = ReadVector3String(frameVars[dataIndex++]);
                    if (RecordLocalEulerAngles) localeuler = ReadVector3String(frameVars[dataIndex++]);
                    if (RecordLocalScale) localscale = ReadVector3String(frameVars[dataIndex++]);
                    if (RecordRotation) rotation = ReadQuaternionString(frameVars[dataIndex++]);
                    if (RecordLocalRotation) localrotation = ReadQuaternionString(frameVars[dataIndex++]);

                    ObjectFrame frame = new ObjectFrame(objectName, frameIndex, new ObjectTrackerOptions(RecordPosition, RecordLocalPosition, RecordEulerAngles, RecordLocalEulerAngles, RecordLocalScale, RecordRotation, RecordLocalRotation),
                        pos, localpos, euler, localeuler, localscale, rotation, localrotation);
                    objFrames.Add(frame);
                }
            }
        }
        return timeline;
    }

    public static Dictionary<int, List<ObjectFrame>> ReadReplayObjectDataFromPath(string fullpath, out List<int> activeFrames) {
        activeFrames = new List<int>();

        if (!File.Exists(fullpath)) {
            Debug.Log("Could not find save at " + (fullpath));
            return null;
        }

        FileStream stream = new FileStream(fullpath, FileMode.Open);
        StreamReader reader = new StreamReader(stream);

        Dictionary<int, List<ObjectFrame>> timeline = new Dictionary<int, List<ObjectFrame>>();

        string loadedData = reader.ReadToEnd();
        string[] loadedFrames = loadedData.Split('*');

        List<ObjectFrame> objFrames = new List<ObjectFrame>();
        int frameIndex = 0;

        //Skip string 0 - always blank
        for (int i = 1; i < loadedFrames.Length; i++) {
            string[] frameData = loadedFrames[i].Split('>');

            for (int j = 0; j < frameData.Length; j++) {
                if (frameData[j].Trim().StartsWith("FRAME")) {
                    if (!(objFrames.Count == 0)) {
                        timeline.Add(frameIndex, objFrames);
                        activeFrames.Add(frameIndex);
                        objFrames = new List<ObjectFrame>(objFrames);
                    }
                    int.TryParse(frameData[j].Replace("FRAME", ""), out frameIndex);
                }
                else {
                    //Frame parameters
                    string objectName;
                    bool RecordPosition, RecordLocalPosition,
                    RecordEulerAngles, RecordLocalEulerAngles,
                    RecordRotation, RecordLocalRotation,
                    RecordLocalScale;
                    Vector3 pos = Vector3.zero,
                            localpos = Vector3.zero,
                            euler = Vector3.zero,
                            localeuler = Vector3.zero,
                            localscale = Vector3.zero;
                    Quaternion rotation = new Quaternion(),
                               localrotation = new Quaternion();

                    string[] frameVars = frameData[j].Split('\n');
                    int.TryParse(frameVars[1], out frameIndex);
                    objectName = frameVars[2].Trim();
                    bool.TryParse(frameVars[3], out RecordPosition);
                    bool.TryParse(frameVars[4], out RecordLocalPosition);
                    bool.TryParse(frameVars[5], out RecordEulerAngles);
                    bool.TryParse(frameVars[6], out RecordLocalEulerAngles);
                    bool.TryParse(frameVars[7], out RecordLocalScale);
                    bool.TryParse(frameVars[8], out RecordRotation);
                    bool.TryParse(frameVars[9], out RecordLocalRotation);

                    int dataIndex = 10;

                    if (RecordPosition) pos = ReadVector3String(frameVars[dataIndex++]);
                    if (RecordLocalPosition) localpos = ReadVector3String(frameVars[dataIndex++]);
                    if (RecordEulerAngles) euler = ReadVector3String(frameVars[dataIndex++]);
                    if (RecordLocalEulerAngles) localeuler = ReadVector3String(frameVars[dataIndex++]);
                    if (RecordLocalScale) localscale = ReadVector3String(frameVars[dataIndex++]);
                    if (RecordRotation) rotation = ReadQuaternionString(frameVars[dataIndex++]);
                    if (RecordLocalRotation) localrotation = ReadQuaternionString(frameVars[dataIndex++]);

                    ObjectFrame frame = new ObjectFrame(objectName, frameIndex, new ObjectTrackerOptions(RecordPosition, RecordLocalPosition, RecordEulerAngles, RecordLocalEulerAngles, RecordLocalScale, RecordRotation, RecordLocalRotation),
                        pos, localpos, euler, localeuler, localscale, rotation, localrotation);
                    objFrames.Add(frame);
                }
            }
        }
        return timeline;
    }

    /// <summary>
    /// Write a vector3 to string while preserving precision and format
    /// </summary>
    /// <param name="vector">The vector to write as string</param>
    /// <returns>A vector3 written as a string</returns>
    public static string Vector3String(Vector3 vector) {
        string result = "";
        result += vector.x;
        result += "/";
        result += vector.y;
        result += "/";
        result += vector.z;
        return result;
    }

    /// <summary>
    /// Return a vector3 from the Vector3String() method result
    /// </summary>
    /// <param name="s">The vector3 string to read</param>
    /// <returns>Vector3 created from string</returns>
    public static Vector3 ReadVector3String(string s) {
        string[] vectordata = s.Split('/');
        Vector3 v = new Vector3();
        float.TryParse(vectordata[0], out v.x);
        float.TryParse(vectordata[1], out v.y);
        float.TryParse(vectordata[2], out v.z);
        return v;
    }

    /// <summary>
    /// Write a quaternion to string while preserving precision and format
    /// </summary>
    /// <param name="quat">The quaternion to write as string</param>
    /// <returns>A quaternion written as a string</returns>
    public static string QuaternionString(Quaternion quat) {
        string result = "";
        result += quat.x;
        result += "/";
        result += quat.y;
        result += "/";
        result += quat.z;
        result += "/";
        result += quat.w;
        return result;
    }

    /// <summary>
    /// Return a quaternion from the QuaternionString() method result
    /// </summary>
    /// <param name="s">The quaternion string to read</param>
    /// <returns>Quaternion created from string</returns>
    public static Quaternion ReadQuaternionString(string s) {
        string[] vectordata = s.Split('/');
        Quaternion q = new Quaternion();
        float.TryParse(vectordata[0], out q.x);
        float.TryParse(vectordata[1], out q.y);
        float.TryParse(vectordata[2], out q.z);
        float.TryParse(vectordata[3], out q.w);
        return q;
    }


}
