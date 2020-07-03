using System;
using System.Collections.Generic;

namespace FindAndExplore.Infrastructure
{
    public interface IErrorReporter
    {
        void TrackError(Exception exception);

        void TrackError(Exception exception, Dictionary<string, string> properties);

        void TrackError(Exception exception, string propertyName, string propertyValue);
    }
}