using System;
using Xamarin.Essentials;

namespace FindAndExplore.AppEvents
{
    public interface IConnectivityMonitor
    {
        void Start();

        IObservable<NetworkAccess> ConnectivityNotifications { get; }
    }
}
