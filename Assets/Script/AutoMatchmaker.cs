using Mirror;
using Mirror.Discovery;
using UnityEngine;

public class AutoMatchmaker : NetworkManager
{
    #region Public

    public NetworkDiscovery m_Discovery;
   
    #endregion

    #region Unity API

   
    #endregion

    #region Main Methods

    [ContextMenu("Start Matchmaking")]
    public void StartMatchmaking()
    {
        m_Discovery.OnServerFound.AddListener(NetworkDiscovery_OnServerFound);
        Debug.Log( "Searching host" );
        m_Discovery.StartDiscovery();
        Invoke(nameof(BecomeHost), 5f);
    }
   
    #endregion

    #region Utils

    private void NetworkDiscovery_OnServerFound(ServerResponse response)
    {
        if (_foundServer) return;
            
        _foundServer = true;
        Debug.Log( $"Server found at {response.EndPoint.Address} : {response.EndPoint.Port}");

        NetworkManager.singleton.networkAddress = response.uri.Host;
        NetworkManager.singleton.StartClient();
    }

    private void BecomeHost()
    {
        if (_foundServer) return;
            
        Debug.Log( "Becoming host" );
        NetworkManager.singleton.StartHost();
        m_Discovery.AdvertiseServer();
    }
   
    #endregion

    #region Private & Protected

    private bool _foundServer;

    #endregion
}
