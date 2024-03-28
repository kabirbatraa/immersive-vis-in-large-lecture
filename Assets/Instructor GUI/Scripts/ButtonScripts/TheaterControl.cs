using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheaterControl : MonoBehaviour
{
    private bool theaterStarted = false;
    private bool theaterGoing = false;
    public void toggleTheater()
    {
        if (!theaterStarted)
        {
            StreamTheaterModeData.Instance.TriggerTheaterMode();
            theaterStarted = true;
        }
        else
        {
            if (theaterGoing)
            {
                StreamTheaterModeData.Instance.PauseTheaterMode();
                theaterGoing = false;
            }
            else
            {
                StreamTheaterModeData.Instance.ContinueTheaterMode();
                theaterGoing = true;
            }
        }
    }

    public void resetTheaterMode()
    {
        StreamTheaterModeData.Instance.ResetTheaterMode();
        theaterStarted = false;
        theaterGoing = false;
    }
}