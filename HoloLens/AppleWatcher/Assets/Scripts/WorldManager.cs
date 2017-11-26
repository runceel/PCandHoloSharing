using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using UniRx.Operators;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;

public class WorldManager : Singleton<WorldManager>, IInputClickHandler
{
    public UI UI { get; } = new UI();

    private Status AppStatus { get; set; } = WorldManager.Status.Init;

    private GameObject apple;

    private SharingManager SharingManager { get; set; }

    private List<HeadPosition> RecordingData { get; } = new List<HeadPosition>();

    public async void OnInputClicked(InputClickedEventData eventData)
    {
        if (this.AppStatus == Status.Init)
        {
            this.apple = this.UI.CreateApple(Camera.main.transform.position + Camera.main.transform.forward * 2.0f);
            this.UI.Console.RecordingButton.GetComponent<Button>().onClick.AddListener(this.RecordingButtonClick);
            this.UI.Console.PlayButton.GetComponent<Button>().onClick.AddListener(this.PlayButtonClick);
            await this.SharingManager.SendCommandAsync(Command.CreateCreateAppleCommand(this.apple.transform.localPosition));
            this.AppStatus = Status.Watch;
        }
    }

    private async void PlayButtonClick()
    {
        Debug.Log("PlayButtonClick");
        if (!this.RecordingData.Any())
        {
            return;
        }

        var text = this.UI.Console.PlayButton.GetComponentInChildren<Text>();
        text.text = "Playing";
        await this.SharingManager.SendCommandAsync(Command.CreatePlayRecordingCommand());
        var head = this.UI.CreateHead();
        this.FixedUpdateAsObservable()
            .Zip(this.RecordingData.ToObservable(), (x, y) => y)
            .Subscribe(x =>
            {
                head.transform.localPosition = x.position;
                head.transform.localEulerAngles = x.eularAngle;
            },
            () =>
            {
                text.text = "Play";
                Destroy(head);
            });
    }

    private async void RecordingButtonClick()
    {
        var text = this.UI.Console.RecordingButton.GetComponentInChildren<Text>();
        if (text.text == "Stop recording") { return; }

        Debug.Log("RecordingButtonClick");
        this.RecordingData.Clear();
        text.text = "Stop recording";
        await this.SharingManager.SendCommandAsync(Command.CreateStartRecordingCommand());
        var headTrackObject = new GameObject();
        headTrackObject.transform.parent = this.UI.Anchor.transform;
        this.FixedUpdateAsObservable()
            .TakeUntil(this.UI.Console.RecordingButton.GetComponent<Button>().OnClickAsObservable())
            .Select((_, i) =>
            {
                headTrackObject.transform.SetPositionAndRotation(
                    Camera.main.transform.position,
                    Camera.main.transform.rotation);
                var localPos = headTrackObject.transform.localPosition;
                var localEularAngules = headTrackObject.transform.localEulerAngles;
                return System.Tuple.Create(i, new HeadPosition { position = localPos, eularAngle = localEularAngules });
            })
            .Buffer(50)
            .Subscribe(async data =>
            {
                this.RecordingData.AddRange(data.Select(x => x.Item2));
                foreach (var pos in data)
                {
                    await this.SharingManager.SendCommandAsync(Command.CreateAddRecordingCommand(pos.Item1, pos.Item2)).ConfigureAwait(false);
                }
            }, 
            async () =>
            {
                text.text = "Record";
                Destroy(headTrackObject);
                await this.SharingManager.SendCommandAsync(Command.CreateStopRecordingCommand(this.RecordingData));
            });
    }

    // Use this for initialization
    void Start()
    {
        this.UI.Initialize();
        this.UI.SetAnchorSearchState();
        this.SharingManager = SharingManager.Instance;
        InputManager.Instance.AddGlobalListener(this.gameObject);
    }

    public enum Status
    {
        Init,
        Watch,
    }
}

[Serializable]
public class UI
{
    public GameObject Anchor { get; private set; }
    public GameObject Information { get; private set; }
    public ConsolePane Console { get; private set; }
    private GameObject ApplePrefab { get; set; }
    private GameObject HeadPrefab { get; set; }

    public void Initialize()
    {
        this.Anchor = GameObject.Find(nameof(this.Anchor));
        this.Information = GameObject.Find(nameof(this.Information));
        this.Console = new ConsolePane(GameObject.Find(nameof(this.Console)));
        this.ApplePrefab = Resources.Load<GameObject>("Prefabs/ApplePrefab");
        this.HeadPrefab = Resources.Load<GameObject>("Prefabs/HeadPrefab");
    }

    public void SetAnchorSearchState()
    {
        this.Anchor.SetActive(false);
        this.Information.SetActive(true);
        this.Console.Console.SetActive(false);
    }

    public void SetAppleState(Vector3 anchorPosition, Quaternion anchorRotation)
    {
        this.Anchor.transform.SetPositionAndRotation(anchorPosition, anchorRotation);
        this.Anchor.SetActive(true);
        this.Information.SetActive(false);
        this.Console.Console.SetActive(true);
        this.Console.RecordingButton.GetComponentInChildren<Text>().text = "Record";
    }

    public GameObject CreateApple(Vector3 position)
    {
        var apple = GameObject.Instantiate(this.ApplePrefab, this.Anchor.transform);
        apple.transform.position = position;
        return apple;
    }

    public GameObject CreateHead()
    {
        return GameObject.Instantiate(this.HeadPrefab, this.Anchor.transform);
    }
}

public class ConsolePane
{
    public ConsolePane(GameObject console)
    {
        Debug.Log($"ConsolePane create: {console}");
        this.Console = console;
        this.RecordingButton = this.Console.transform.Find("Panel").Find("RecordingButton").gameObject;
        this.PlayButton = this.Console.transform.Find("Panel").Find("PlayButton").gameObject;
        Debug.Log($"{this.Console}, {this.RecordingButton}, {this.PlayButton}");
    }

    public GameObject Console { get; private set; }
    public GameObject RecordingButton { get; private set; }
    public GameObject PlayButton { get; private set; } 
}

[Serializable]
public class Command
{
    public static string CreateAppleCommandName { get; } = "CreateApple";
    public static string StartRecordingCommand { get; } = "StartRecording";
    public static string StopRecordingCommand { get; } = "StopRecording";
    public static string AddRecordingCommand { get; } = "AddRecording";
    public static string PlayRecordingCommand { get; } = "PlayRecording";

    public static Command CreateCreateAppleCommand(Vector3 localPosition)
    {
        return new Command
        {
            command = CreateAppleCommandName,
            position = localPosition,
        };
    }

    public static Command CreateStartRecordingCommand()
    {
        return new Command
        {
            command = StartRecordingCommand,
        };
    }

    public static Command CreateAddRecordingCommand(int seq, HeadPosition pos)
    {
        return new Command
        {
            seq = seq,
            command = AddRecordingCommand,
            position = pos.position,
            eularAngle = pos.eularAngle,
        };
    }

    public static Command CreateStopRecordingCommand(IEnumerable<HeadPosition> headPositions)
    {
        return new Command
        {
            command = StopRecordingCommand,
        };
    }
    public static Command CreatePlayRecordingCommand()
    {
        return new Command
        {
            command = PlayRecordingCommand,
        };
    }

    public int seq;
    public string command;
    public Vector3 position;
    public Vector3 eularAngle;
}

[Serializable]
public class HeadPosition
{
    public Vector3 position;
    public Vector3 eularAngle;
}