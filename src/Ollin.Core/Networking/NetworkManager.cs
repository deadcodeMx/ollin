using System.Net;
using System.Net.Sockets;

namespace Ollin.Core.Networking;

/// <summary>Transporte UDP simple de snapshots de transforms.</summary>
public static class NetworkManager
{
    private static readonly object Gate = new();
    private static readonly HashSet<IPEndPoint> Peers = new();
    private static UdpClient? _udp;
    private static IPEndPoint? _server;
    private static Thread? _thread;
    private static volatile bool _running;
    private static int _peerId;

    public static bool IsRunning => _running;
    public static bool IsHost { get; private set; }
    public static int PeerId => _peerId;
    public static event Action<NetworkSnapshot>? SnapshotReceived;

    public static bool StartHost(int port)
    {
        Stop();
        try
        {
            _udp = new UdpClient(port);
            _server = new IPEndPoint(IPAddress.Any, port);
            _peerId = 0;
            IsHost = true;
            _running = true;
            StartThread();
            return true;
        }
        catch { Stop(); return false; }
    }

    public static bool Connect(string host, int port)
    {
        Stop();
        try
        {
            _udp = new UdpClient(0);
            _server = new IPEndPoint(IPAddress.Parse(host), port);
            _peerId = Random.Shared.Next(1, int.MaxValue);
            IsHost = false;
            _running = true;
            StartThread();
            Send(new NetworkSnapshot { PeerId = _peerId });
            return true;
        }
        catch { Stop(); return false; }
    }

    public static void Send(NetworkSnapshot snapshot)
    {
        if (!_running || _udp is null) return;
        var bytes = snapshot.ToBytes();
        lock (Gate)
        {
            if (IsHost)
            {
                foreach (var peer in Peers)
                {
                    try { _udp.Send(bytes, bytes.Length, peer); } catch { }
                }
            }
            else if (_server is not null)
            {
                try { _udp.Send(bytes, bytes.Length, _server); } catch { }
            }
        }
    }

    public static void Stop()
    {
        _running = false;
        try { _udp?.Close(); } catch { }
        _udp = null;
        _server = null;
        _thread = null;
        lock (Gate) Peers.Clear();
        IsHost = false;
    }

    private static void StartThread()
    {
        _thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "Ollin.Network" };
        _thread.Start();
    }

    private static void ReceiveLoop()
    {
        var udp = _udp;
        if (udp is null) return;
        while (_running)
        {
            try
            {
                IPEndPoint? remote = null;
                var bytes = udp.Receive(ref remote);
                if (remote is null) continue;
                if (IsHost) lock (Gate) Peers.Add(remote);
                var snapshot = NetworkSnapshot.FromBytes(bytes);
                if (snapshot is not null) SnapshotReceived?.Invoke(snapshot);
            }
            catch (SocketException) { if (!_running) break; }
            catch (ObjectDisposedException) { break; }
            catch { }
        }
    }
}
