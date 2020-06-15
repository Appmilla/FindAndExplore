using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace FindAndExplore.Configuration
{
    /// <summary>
    /// This class supports config collection. It has methods to read the config values kept inside config.xml
    /// </summary>
    public static class ConfigCollection
    {
        static readonly IDictionary<string, string> _dictConfig = new Dictionary<string, string>();

        /// <summary>
        /// Public method. Reads the config key value from Dictionary collection.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetConfigValue(string key)
        {
            if (_dictConfig.Count == 0)
            {
                ReadAllConfig();
            }
            if (_dictConfig.TryGetValue(key, out var value))
            {
                return value;
            }
            else
            {
                _dictConfig.Add(key, ReadConfig(key));
                return _dictConfig[key];
            }
        }
        /// <summary>
        /// Private method. Reads given key value from config xml
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        static string ReadConfig(string key)
        {
            var objXml = LoadXmlDocument();

            //Read config.xml as build output
            //objXml.Load(@".\config\config.xml");
            var objNode = objXml.DocumentElement?.SelectSingleNode("/config/" + key);
            return objNode?.InnerText;
        }
        /// <summary>
        /// Private method. Reads all key values from config xml
        /// </summary>
        static void ReadAllConfig()
        {
            if (_dictConfig.Count > 0)
            {
                _dictConfig.Clear();
            }

            var objXml = LoadXmlDocument();
            foreach (var objNode in objXml.DocumentElement?.ChildNodes.Cast<XmlNode>() ?? Enumerable.Empty<XmlNode>())
            {
                if (!_dictConfig.ContainsKey(objNode.Name))
                    _dictConfig.Add(objNode.Name, objNode.InnerText);
            }
        }

        static XmlDocument LoadXmlDocument()
        {
            var objXml = new XmlDocument();

            //Read config.xml as embedded resource
            using var stream = typeof(ConfigCollection).Assembly.GetManifestResourceStream("Sse.Retail.Mobile.MySse.Services.Configuration.config.xml");

            objXml.Load(stream ?? throw new InvalidOperationException());
            return objXml;

        }
    }
}
