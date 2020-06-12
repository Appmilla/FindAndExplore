using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CommonServiceLocator;
using ReactiveUI;

namespace FindAndExplore.Core.Extensions
{
    public static class SubscribeSafeExtensions
    {
        public static IDisposable SubscribeSafeErrorReporting<T>(
        this IObservable<T> @this,
        [CallerMemberName] string callerMemberName = null,
        [CallerFilePath] string callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
        {
            return @this
                .Subscribe(
                    _ => { },
                    ex =>
                    {
                        //TODO implement this
                        /*
                        var errorReporter = ServiceLocator.Current.GetInstance<IErrorReporter>();

                        var properties = new Dictionary<string, string>
                        {
                            { "Caller member name:", callerMemberName },
                            { "Caller file path:", callerFilePath },
                            { "caller line number:", callerLineNumber.ToString() }
                          };

                        errorReporter.TrackError(ex, properties);
                        */

                        RxApp.DefaultExceptionHandler.OnNext(ex);
                    });
        }


        public static IDisposable SubscribeSafeErrorReporting<T>(
        this IObservable<T> @this,
        Action<T> onNext,
        [CallerMemberName] string callerMemberName = null,
        [CallerFilePath] string callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
        {
            return @this
                .Subscribe(
                    onNext,
                    ex =>
                    {
                        //TODO implement this
                        /*
                        var errorReporter = ServiceLocator.Current.GetInstance<IErrorReporter>();

                        var properties = new Dictionary<string, string>
                        {
                            { "Caller member name:", callerMemberName },
                            { "Caller file path:", callerFilePath },
                            { "caller line number:", callerLineNumber.ToString() }
                          };

                        errorReporter.TrackError(ex, properties);
                        */

                        RxApp.DefaultExceptionHandler.OnNext(ex);
                    });
        }
    }
}
