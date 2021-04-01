using System.Xml;
using System.Collections.Generic;

namespace Reusable.Utils
{
    /// <summary>
    /// Lädt geheime Daten, die außerhalb der appsettings.json bleiben sollen.
    /// (Zum Beispiel, die Verbindungszeichenkette der Datenbank.)
    /// </summary>
    public class SecretLoader
    {
        /// <summary>
        /// Lädt die Datenquelle mit den Geheimnissen.
        /// </summary>
        public SecretLoader(XmlMetadata metadata)
        {
            var dom = new XmlDocument();
            dom.Load(metadata.FilePath);
            dom.Schemas.Add(metadata.XmlNamespace, metadata.SchemaFilePath);
            dom.Validate(null);

            _connStringsByName = LoadConnectionStrings(dom, metadata.XmlNamespace);
            _credentialsByName = LoadCredentials(dom, metadata.XmlNamespace);
        }

        private readonly Dictionary<string, string> _connStringsByName;

        private readonly Dictionary<string, PasswordBasedCredential> _credentialsByName;

        /// <summary>
        /// Bietet eine Verbindugszeichenkette.
        /// </summary>
        /// <param name="name">Der Name der gewünschten Verbindung.</param>
        /// <returns>
        /// Der geheime Teil der Verbindungszeichenkette, wenn es vorhanden ist.
        /// (Zum Beispiel: "Server;Database;User ID;Password;")
        /// </returns>
        public string GetConnectionString(string name)
        {
            if (_connStringsByName.TryGetValue(name, out string connectionString))
            {
                return connectionString;
            }

            return null;
        }

        private static Dictionary<string, string> LoadConnectionStrings(XmlDocument dom,
                                                                        string targetNamespace)
        {
            var dbConnStringsByName = new Dictionary<string, string>();

            XmlNamespaceManager nsManager = new XmlNamespaceManager(dom.NameTable);
            nsManager.AddNamespace("tns", targetNamespace);

            const string xpath = "/tns:secrets/tns:connectionStrings/tns:connection";
            foreach (XmlNode node in dom.SelectNodes(xpath, nsManager))
            {
                var entry = node as XmlElement;
                string connectionName = entry.GetAttribute("name");
                string connectionString = entry.GetAttribute("string");

                dbConnStringsByName.Add(connectionName, connectionString);
            }

            return dbConnStringsByName;
        }

        /// <summary>
        /// Bietet Anmeldeinformationen.
        /// </summary>
        /// <param name="name">Der Name der Anmeldung.</param>
        /// <returns>Die Anmeldeinformationen: Benutzer & Kennwort.</returns>
        public PasswordBasedCredential GetCredential(string name)
        {
            if (_credentialsByName.TryGetValue(name, out PasswordBasedCredential credential))
            {
                return credential;
            }

            return null;
        }

        private static Dictionary<string, PasswordBasedCredential> LoadCredentials(XmlDocument dom,
                                                                                   string targetNamespace)
        {
            var credentialsByName = new Dictionary<string, PasswordBasedCredential>();

            XmlNamespaceManager nsManager = new XmlNamespaceManager(dom.NameTable);
            nsManager.AddNamespace("tns", targetNamespace);

            const string xpath = "/tns:secrets/tns:credentials/tns:credential";
            foreach (XmlNode node in dom.SelectNodes(xpath, nsManager))
            {
                var entry = node as XmlElement;
                string credentialName = entry.GetAttribute("name");
                string userId = entry.GetAttribute("userid");
                string password = entry.GetAttribute("password");

                credentialsByName.Add(credentialName, new PasswordBasedCredential {
                    UserId = userId,
                    Password = password
                });
            }

            return credentialsByName;
        }

    }// end of class SecretLoader

}// end of namespace Reusable.Utils
