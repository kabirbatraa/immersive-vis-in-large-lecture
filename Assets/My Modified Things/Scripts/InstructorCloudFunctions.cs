using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PhotonPun = Photon.Pun;
using PhotonRealtime = Photon.Realtime;

public class InstructorCloudFunctions : MonoBehaviour
{

    public static int defaultGroupNumber = 1;

    public static InstructorCloudFunctions Instance;

    private void Awake() {

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }




    [SerializeField]
    private GameObject mainObjectContainerPrefab;



    // get player group number from player custom properties
    public int GetPlayerGroupNumber(PhotonRealtime.Player player) {

        bool groupNumberExists = player.CustomProperties.ContainsKey("groupNumber");
        int groupNumber = groupNumberExists ? (int)player.CustomProperties["groupNumber"] : defaultGroupNumber;

        return groupNumber;

    }


    public int GetMaxGroupNumber() {

        var players = PhotonPun.PhotonNetwork.CurrentRoom.Players.Values;

        // get max group number
        int maxGroupNumber = 0; 

        foreach (PhotonRealtime.Player player in players) {
            int groupNumber = GetPlayerGroupNumber(player);
            if (maxGroupNumber < groupNumber) maxGroupNumber = groupNumber;
        }

        return maxGroupNumber;
    }
    
    public void CreateMainObjectContainerPerGroup() {


        // before creating new main objects, delete any preexisting objects
        DeleteAllMainObjects();


        // get all players:
        // value collection (basically list) of PhotonRealtime.Player objects
        // values because players are like a dictionary (we dont want the keys)
        var players = PhotonPun.PhotonNetwork.CurrentRoom.Players.Values;

        // get max group number
        int maxGroupNumber = GetMaxGroupNumber(); 

        // create headPositionsPerGroup array: stores list of player positions for each group
        // array of list ints
        List<Vector3>[] headPositionsPerGroup = new List<Vector3>[maxGroupNumber+1];
            // +1 because we should be able to access the array at the max group number index
        
        // initialize all of the lists to empty lists
        for (int i = 1; i <= maxGroupNumber; i++) {
            headPositionsPerGroup[i] = new List<Vector3>();
        }

        // get every MyPhotonUserHeadTracker 
        var allHeadTrackerObjects = FindObjectsByType<PhotonUserHeadTrackerCommunication>(FindObjectsSortMode.None);

        // populate the headPositionsPerGroup array
        foreach (PhotonUserHeadTrackerCommunication headTrackerScript in allHeadTrackerObjects) {
            // get the head tracker object
            GameObject headTrackerObject = headTrackerScript.gameObject;

            // get photon view
            var photonView = headTrackerObject.GetComponent<PhotonPun.PhotonView>();

            // get player owner 
            PhotonRealtime.Player player = photonView.Owner;

            // get player's group number
            int groupNumber = GetPlayerGroupNumber(player);
            // skip if group number is 0 (admin)
            if (groupNumber == 0) {
                continue;
            }

            // get the vector 3 associated with this player's head
            Vector3 position;
            // if local head: (this only happens if this function was called by a admin headset and not a laptop)
            if (photonView.IsMine) {
                position = UserHeadPositionTrackerManager.Instance.localHeadTransform.position;
            }
            else {
                // not local head
                position = headTrackerObject.transform.position;
            }

            // append to array
            headPositionsPerGroup[groupNumber].Add(position);
        }

        // now we have all of the vector3 in a list for each group (done)

        // for each group, get the average position of the group members
        // and instantiate a Main Object Container at that position
        for (int i = 1; i <= maxGroupNumber; i++) {
            List<Vector3> headPositions = headPositionsPerGroup[i];

            // if list is empty, then skip
            if (headPositions.Count == 0) {
                continue;
            }

            // get the average of all transforms of this group

            Vector3 averageVector = Vector3.zero;
            foreach(Vector3 position in headPositions) {
                averageVector += position;
            }
            averageVector /= headPositions.Count;




            // adjust this averageVector spawn point by an offset: instantiate the object in front of the group
            averageVector += new Vector3(0, 0, 1);



            // now, instantiate the Main Object container at this position and for this group

            // instantiate
            var mainObjectContainerInstance = PhotonPun.PhotonNetwork.Instantiate(mainObjectContainerPrefab.name, averageVector, mainObjectContainerPrefab.transform.rotation);
            // set group number
            SetPhotonObjectGroupNumber(mainObjectContainerInstance, i);

        }



    }




    public void DeleteAllMainObjects() {
        // get all main objects
        GameObject[] mainObjects = GameObject.FindGameObjectsWithTag("MainObjectContainer");
        int count = mainObjects.Length;
        foreach (var mainObject in mainObjects) {

            // will fail to destroy object if not the owner of the object

            // therefore, transfer ownership to local player (the instructor), and then destroy
            PhotonPun.PhotonView photonView = mainObject.GetComponent<PhotonPun.PhotonView>();
            photonView.TransferOwnership(Photon.Pun.PhotonNetwork.LocalPlayer); 
            PhotonPun.PhotonNetwork.Destroy(mainObject);
        }
        Debug.Log("removed " + count + " main objects");
    }



    private bool MainObjectsExist() {
        GameObject[] mainObjects = GameObject.FindGameObjectsWithTag("MainObjectContainer");
        return mainObjects.Length > 0;
    }


    private void RecreateMainObjectsIfTheyExist() {

        if (MainObjectsExist()) {
            CreateMainObjectContainerPerGroup();
        }
    }







    private void SetPhotonObjectGroupNumber(GameObject photonObject, int groupNumber) {

        string key = "groupNum" + photonObject.GetComponent<PhotonPun.PhotonView>().ViewID;
        int value = groupNumber;
        var newCustomProperty = new ExitGames.Client.Photon.Hashtable { { key, value } };
        PhotonPun.PhotonNetwork.CurrentRoom.SetCustomProperties(newCustomProperty);

    }

    private bool PhotonObjectHasGroupNumber(GameObject photonObject) {

        string key = "groupNum" + photonObject.GetComponent<PhotonPun.PhotonView>().ViewID;
        return PhotonPun.PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key);
    }

    private int GetPhotonObjectGroupNumber(GameObject photonObject) {

        string key = "groupNum" + photonObject.GetComponent<PhotonPun.PhotonView>().ViewID;
        int objectGroupNumber = (int)PhotonPun.PhotonNetwork.CurrentRoom.CustomProperties[key];

        return objectGroupNumber;
    }






    public void SetStudentsIntoIndividualGroups() {

        // value collection (basically list) of PhotonRealtime.Player objects
        var players = PhotonPun.PhotonNetwork.CurrentRoom.Players.Values;

        int groupNumber = 1;
        foreach (PhotonRealtime.Player player in players) {
            
            // if the player is the current player, then skip
            if (player.Equals(Photon.Pun.PhotonNetwork.LocalPlayer)) {
                Debug.Log("(skipping current player)");
                continue;
            }
            // skip players of group number 0 (admins)
            if (GetPlayerGroupNumber(player) == 0) {
                Debug.Log("skipping player with group number 0: " + player.NickName);
                continue;
            }

            player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "groupNumber", groupNumber } });
            // also set the local cache so recreating main object uses correct group number even if cloud hasnt updated yet
            player.CustomProperties["groupNumber"] = groupNumber;
            Debug.Log("Set player group of nickname " + player.NickName + " to group " + groupNumber);
            groupNumber++;
        }

        RecreateMainObjectsIfTheyExist();

    }





    public void SetStudentsIntoGroupsOfTwo() {

        // value collection (basically list) of PhotonRealtime.Player objects
        // values because players are like a dictionary (we dont want the keys)
        var players = PhotonPun.PhotonNetwork.CurrentRoom.Players.Values;

        int groupNumber = 1;
        int counter = 0;
        foreach (PhotonRealtime.Player player in players) {
            
            // if the player is the current player, then skip
            if (player.Equals(Photon.Pun.PhotonNetwork.LocalPlayer)) {
                Debug.Log("(skipping current player)");
                continue;
            }
            // skip players of group number 0 (admins)
            if (GetPlayerGroupNumber(player) == 0) {
                Debug.Log("skipping player with group number 0: " + player.NickName);
                continue;
            }

            player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "groupNumber", groupNumber } });
            // also set the local cache so recreating main object uses correct group number even if cloud hasnt updated yet
            player.CustomProperties["groupNumber"] = groupNumber;
            SampleController.Instance.Log("Set player group of nickname " + player.NickName + " to group " + groupNumber);
            counter++;
            if (counter % 2 == 0) {
                groupNumber++;
            }
        }

        RecreateMainObjectsIfTheyExist();

    }





    public void SetAllStudentsGroupOne() {

        // value collection (basically list) of PhotonRealtime.Player objects
        var players = PhotonPun.PhotonNetwork.CurrentRoom.Players.Values;

        foreach (PhotonRealtime.Player player in players) {
            
            // if the player is the current player, then skip
            if (player.Equals(Photon.Pun.PhotonNetwork.LocalPlayer)) {
                Debug.Log("(skipping current player)");
                continue;
            }
            // skip players of group number 0 (admins)
            if (GetPlayerGroupNumber(player) == 0) {
                Debug.Log("skipping player with group number 0: " + player.NickName);
                continue;
            }

            player.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "groupNumber", 1 } });
            // also set the local cache so recreating main object uses correct group number even if cloud hasnt updated yet
            player.CustomProperties["groupNumber"] = 1;
            SampleController.Instance.Log("Set player group of nickname: " + player.NickName);
        }

        RecreateMainObjectsIfTheyExist();

    }





    // room has custom property?
    public bool RoomHasCustomProperty(string key) {
        return PhotonPun.PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key);
    }

    // get room custom property
    public object GetRoomCustomProperty(string key) {

        // string key = "groupNum" + photonObject.GetComponent<PhotonPun.PhotonView>().ViewID;
        // int objectGroupNumber = (int)PhotonPun.PhotonNetwork.CurrentRoom.CustomProperties[key];

        // return objectGroupNumber;
        return PhotonPun.PhotonNetwork.CurrentRoom.CustomProperties[key];
    }


    public void SetRoomCustomProperty(string key, object value) {

        var newCustomProperty = new ExitGames.Client.Photon.Hashtable { { key, value } };
        // update on server
        PhotonPun.PhotonNetwork.CurrentRoom.SetCustomProperties(newCustomProperty);

        // update locally because server will update local cached hashmap with delay
        PhotonPun.PhotonNetwork.CurrentRoom.CustomProperties[key] = value;

    }




    public void SetActiveModelNumber(int modelNumber) {
        // SetRoomCustomProperty("mainObjectCurrentModelName", "Model1");
        SetRoomCustomProperty("mainObjectCurrentModelName", "Model" + modelNumber);
    }

    public int getTotalNumberOfModels() {
        if (RoomHasCustomProperty("totalNumberOfModels")) {
            return (int)GetRoomCustomProperty("totalNumberOfModels");
        }
        else {
            return 2; // assume there are at least 2 models
        }
        
    }





}
