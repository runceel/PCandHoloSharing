using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Storage.Streams;
using Windows.Networking;
using Windows.Networking.Sockets;
#endif

public class SharingManager : Singleton<SharingManager>
{
#if WINDOWS_UWP
    private DatagramSocket Socket { get; set; }
#else
#endif

#if WINDOWS_UWP
    private async void Start()
    {
        this.Socket = new DatagramSocket();
        this.Socket.Control.MulticastOnly = true;
        await this.Socket.BindServiceNameAsync("10000");
        this.Socket.JoinMulticastGroup(new HostName("224.3.0.5"));
    }

    public async Task SendCommandAsync(Command command)
    {
        var json = JsonUtility.ToJson(command);
        var data = Encoding.UTF8.GetBytes(json);

        using (var s = await this.Socket.GetOutputStreamAsync(new HostName("224.3.0.5"), "10000"))
        using (var w = new DataWriter(s))
        {
            w.WriteBytes(data);
            await w.StoreAsync();
        }

        Debug.Log($"Sended: {json}");
    }

#else
    public Task SendCommandAsync(Command command)
    {
        return Task.FromResult(0);
    }
#endif
}

