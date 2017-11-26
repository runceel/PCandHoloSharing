using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class AnchorManager : MonoBehaviour, ITrackableEventHandler
{
    public GameObject anchor;
    public GameObject information;

    private TrackableBehaviour.Status CurrentStatus { get; set; } = TrackableBehaviour.Status.UNKNOWN;

    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        this.CurrentStatus = newStatus;
    }

    private void Start()
    {
        this.GetComponent<TrackableBehaviour>().RegisterTrackableEventHandler(this);
    }

    private void Update()
    {
        if (this.anchor == null)
        {
            return;
        }

        if (this.CurrentStatus != TrackableBehaviour.Status.TRACKED)
        {
            this.information?.SetActive(true);
            this.anchor.SetActive(false);
            return;
        }

        this.information?.SetActive(false);
        this.anchor.SetActive(true);
        this.anchor.transform.position = this.transform.position;
        this.anchor.transform.rotation= this.transform.rotation;
    }
}
