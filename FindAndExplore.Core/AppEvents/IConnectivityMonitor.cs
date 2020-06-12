using System;
using Xamarin.Essentials;

namespace FindAndExplore.Core.AppEvents
{
    public interface IConnectivityMonitor
    {
        void Start();

        IObservable<NetworkAccess> ConnectivityNotifications { get; }
    }
}
