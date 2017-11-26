using HoloToolkit.Unity;
using UnityEngine;
using System;
using System.Collections.Generic;
using UniRx.Triggers;
using UniRx;
using System.Linq;
#if WINDOWS_UWP
using System.Text;
using Windows.Networking;
using System.Collections.Concurrent;
using System.IO;
using Windows.Networking.Sockets;
#endif

public class WorldManager : Singleton<WorldManager>
{
    private GameObject applePrefab;
    private GameObject anchor;
    private GameObject headPrefab;
#if WINDOWS_UWP
    private GameObject apple;
    private ConcurrentQueue<Command> CommandQueue { get; } = new ConcurrentQueue<Command>();
    private DatagramSocket Socket { get; set; }

    private List<Command> HeadPositions { get; } = new List<Command>();

    // Use this for initialization
    private async void Start()
    {
        Debug.Log("WorldManager#Start called.");
        this.anchor = GameObject.Find("Anchor");
        this.applePrefab = Resources.Load<GameObject>("Prefabs/ApplePrefab");
        this.headPrefab = Resources.Load<GameObject>("Prefabs/HeadPrefab");
        this.Socket = new DatagramSocket();
        this.Socket.MessageReceived += this.Socket_MessageReceived;
        this.Socket.Control.MulticastOnly = true;
        await this.Socket.BindServiceNameAsync("10000");
        this.Socket.JoinMulticastGroup(new HostName("224.3.0.5"));
        Debug.Log("WorldManager#Start ended.");
    }

    private void Socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        using (var s = args.GetDataStream().AsStreamForRead())
        using (var ms = new MemoryStream())
        {
            s.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var json = Encoding.UTF8.GetString(ms.ToArray());
            Debug.Log($"Received Json: {json}");
            this.CommandQueue.Enqueue(JsonUtility.FromJson<Command>(json));
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (this.CommandQueue.TryDequeue(out var command))
        {
            Debug.Log($"Command execute: {command.command}");
            switch (command.command)
            {
                case Command.CreateAppleCommandName:
                    this.apple = Instantiate(this.applePrefab, this.anchor.transform);
                    this.apple.transform.localPosition = command.position;
                    break;
                case Command.StartRecordingCommand:
                    this.HeadPositions.Clear();
                    break;
                case Command.StopRecordingCommand:
                    break;
                case Command.AddRecordingCommand:
                    this.HeadPositions.Add(command);
                    break;
                case Command.PlayRecordingCommand:
                    var head = Instantiate(this.headPrefab, this.anchor.transform);
                    this.FixedUpdateAsObservable()
                        .Zip(this.HeadPositions.OrderBy(x => x.seq).ToObservable(), (_, x) => x)
                        .Subscribe(x =>
                        {
                            head.transform.localPosition = x.position;
                            head.transform.localEulerAngles = x.eularAngle;
                            head.transform.localPosition = head.transform.localPosition - head.transform.InverseTransformDirection(head.transform.forward) * 0.1f;
                        },
                        () =>
                        {
                            Destroy(head);
                        });
                    break;
            }
        }
    }
#endif
}

[Serializable]
public class Command
{
    public const string CreateAppleCommandName = "CreateApple";
    public const string StartRecordingCommand = "StartRecording";
    public const string StopRecordingCommand = "StopRecording";
    public const string AddRecordingCommand = "AddRecording";
    public const string PlayRecordingCommand = "PlayRecording";

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