//using UnityEngine;
//using UnityEngine.UI;
//using System.IO;
//using System.Collections;
//using System.Collections.Generic;

///// <summary>
///// Populates the list of available recordings from the supplied list of directories
///// Automatically searches the default save directory
///// </summary>
//public class PopulateRecordingMenu : MonoBehaviour {

//    public GameObject RecordingInfoRectFab;
//    public GameObject ScrollContentRect;
//    public List<string> SearchDirs;

//    private float _currentYSpawn;

//    void Awake() {
//        SearchDirs = new List<string>();
//        SearchDirs.Add(ReplayDataHandler.path);
//        _currentYSpawn = 1400.0f; //TODO - do not hardcode
//    }

//	void Start () {
//	    foreach (string s in SearchDirs) {
//            AddMenuItemsFromDir(s);
//        }
//	}

//    private void AddMenuItemsFromDir(string dir) {
//        //Check if directory exists, scan for all replay files
//        if (Directory.Exists(dir)) {
//            string[] files = System.IO.Directory.GetFiles(dir, "*.dat");

//            //Create menu items for each replay
//            foreach(string file in files) {
//                GameObject newInfoRect = (GameObject)Instantiate(RecordingInfoRectFab, ScrollContentRect.transform, false);

//                //Set position
//                newInfoRect.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, _currentYSpawn);
//                _currentYSpawn -= RecordingInfoRectFab.GetComponent<RectTransform>().sizeDelta.y;

//                //Fill text fields
//                FileInfo fInfo = new FileInfo(file);
//                newInfoRect.transform.FindChild("RecordingInfoPanel/NameText").GetComponent<Text>().text = "Name: " + fInfo.Name;
//                newInfoRect.transform.FindChild("RecordingInfoPanel/OperationText").GetComponent<Text>().text = "Operation: " + "N/A"; //TODO
//                newInfoRect.transform.FindChild("RecordingInfoPanel/LengthText").GetComponent<Text>().text = "Length: " + "N/A"; //TODO

//                //Set up load button
//                newInfoRect.transform.FindChild("RecordingInfoPanel/LoadButton").GetComponent<TriggerRecordingLoad>().SetLoadTarget(fInfo);
//            }

//        }
//    }
//}
