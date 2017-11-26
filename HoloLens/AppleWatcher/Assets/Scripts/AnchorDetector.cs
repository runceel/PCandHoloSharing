using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class AnchorDetector : MonoBehaviour, ITrackableEventHandler
{
    private bool IsAnchorFound { get; set; }

    private WorldManager WorldManager { get; set; }

    private TrackableBehaviour.Status CurrentStatus { get; set; } = TrackableBehaviour.Status.UNKNOWN;

    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        this.CurrentStatus = newStatus;
    }

    // Use this for initialization
    private void Start()
    {
        this.WorldManager = WorldManager.Instance;
        this.GetComponent<TrackableBehaviour>().RegisterTrackableEventHandler(this);
        this.WorldManager.UI.SetAnchorSearchState();
    }

    // Update is called once per frame
    private void Update()
    {
        if (this.IsAnchorFound)
        {
            return;
        }

        if (this.CurrentStatus != TrackableBehaviour.Status.TRACKED)
        {
            return;
        }

        Debug.Log("Active");
        this.IsAnchorFound = true;
        this.WorldManager.UI.SetAppleState(this.transform.position, this.transform.rotation);
    }
}
