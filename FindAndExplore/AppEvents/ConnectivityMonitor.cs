using System;
using System.Reactive.Subjects;
using ReactiveUI;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace FindAndExplore.AppEvents
{
    public class ConnectivityMonitor : ReactiveObject, IConnectivityMonitor
    {
        readonly IConnectivity _connectivity;
        //readonly ISnackbar _snackbar;
        readonly Subject<NetworkAccess> _connectivityNotifications;

        public IObservable<NetworkAccess> ConnectivityNotifications => _connectivityNotifications;

        public ConnectivityMonitor(
            IConnectivity connectivity/*,
            ISnackbar snackbar*/)
        {
            _connectivity = connectivity;
            //_snackbar = snackbar;

            _connectivityNotifications = new Subject<NetworkAccess>();
        }

        public void Start()
        {
            _connectivity.ConnectivityChanged += OnConnectivityChanged;

            OnConnectivityChanged(this, new ConnectivityChangedEventArgs(_connectivity.NetworkAccess, _connectivity.ConnectionProfiles));
        }

        void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            _connectivityNotifications.OnNext(e.NetworkAccess);

            var access = e.NetworkAccess;
            if (access != NetworkAccess.Internet)
            {
                ShowNoInternet();
            }
        }

        void ShowNoInternet()
        {
            //_snackbar.ShowMessage("No Internet, please check your connection", 5);
        }
    }
}
