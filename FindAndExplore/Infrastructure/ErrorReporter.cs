using System;
using System.Collections.Generic;

namespace FindAndExplore.Infrastructure
{
    public class ErrorReporter : IErrorReporter
    {
        public void TrackError(Exception exception)
        {
            
        }

        public void TrackError(Exception exception, Dictionary<string, string> properties)
        {
            
        }

        public void TrackError(Exception exception, string propertyName, string propertyValue)
        {
            
        }
    }
}