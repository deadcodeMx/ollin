namespace Ollin.Scripting;

/// <summary>API de red para scripts.</summary>
public static class Network
{
    public static bool IsRunning => Ollin.Core.Networking.NetworkManager.IsRunning;
    public static bool IsHost => Ollin.Core.Networking.NetworkManager.IsHost;
    public static int PeerId => Ollin.Core.Networking.NetworkManager.PeerId;

    public static bool StartHost(int port = 7777) => Ollin.Core.Networking.NetworkManager.StartHost(port);
    public static bool Connect(string host, int port = 7777) => Ollin.Core.Networking.NetworkManager.Connect(host, port);
    public static void Stop() => Ollin.Core.Networking.NetworkManager.Stop();
}
