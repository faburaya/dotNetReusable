using System;
using System.IO;

using Xunit;

namespace Reusable.DataAccess.UnitTests
{
    public class SecretLoaderTest
    {
        private string TestFilePath => "test_SecretLoader.xml";

        private string SchemaDeploymentFilePath => Path.Combine("Schema", "secrets.xsd");

        private string DeploymentXmlNamespace => "http://dataaccess.reusable.faburaya.com/secrets";

        [Fact]
        public void GetDatabaseConnString_WhenXmlFileUnavailable_ThenThrow()
        {
            Assert.Throws<System.IO.FileNotFoundException>(() => new SecretLoader(
                new XmlMetadata(DeploymentXmlNamespace, "nicht vorhandene Datei", SchemaDeploymentFilePath)));
        }

        private static string CreateValidXml(string[] innerXmlElements)
        {
            var buffer = new System.Text.StringBuilder();
            foreach (string entry in innerXmlElements)
                buffer.AppendLine(entry);

            return string.Format(@"<?xml version=""1.0"" encoding=""utf-8"" ?><secrets xmlns=""http://dataaccess.reusable.faburaya.com/secrets""><database>{0}</database></secrets>", buffer.ToString());
        }

        [Theory]
        [InlineData("<ill></formed>", typeof(System.Xml.XmlException))]
        [InlineData("<root></root>", typeof(System.Xml.Schema.XmlSchemaException))]
        public void GetDatabaseConnString_WhenInvalidXml_ThenThrow(string xmlContent, Type exceptionType)
        {
            File.WriteAllText(TestFilePath, xmlContent);

            Assert.Throws(exceptionType, () => new SecretLoader(
                new XmlMetadata("http://nichts.de", TestFilePath, SchemaDeploymentFilePath)));

            File.Delete(TestFilePath);
        }

        [Fact]
        public void GetDatabaseConnString_WhenXmlEmpty_ThenDoNotThrow()
        {
            File.WriteAllText(TestFilePath, CreateValidXml(new string[] { "" }));

            new SecretLoader(
                new XmlMetadata(DeploymentXmlNamespace, TestFilePath, SchemaDeploymentFilePath));

            File.Delete(TestFilePath);
        }

        private static string MakeXmlElementForConnection(string connName, string connString)
        {
            return $@"<connection name=""{connName}"" string=""{connString}"" />";
        }

        [Fact]
        public void GetDatabaseConnString_WhenNameUnavailable_ThenNull()
        {
            File.WriteAllText(TestFilePath, CreateValidXml(new string[] {
                MakeXmlElementForConnection("eins", "Server=Server1;Database=Datenbank1;User ID=Benutzer1;Password=Passwort1;")
            }));

            var secretLoader = new SecretLoader(
                new XmlMetadata(DeploymentXmlNamespace, TestFilePath, SchemaDeploymentFilePath));

            Assert.Null(secretLoader.GetDatabaseConnString("zwei"));

            File.Delete(TestFilePath);
        }

        [Fact]
        public void GetDatabaseConnString_WhenOneAvailable_ThenGiveIt()
        {
            string expectedConnectionString = "Server=Server1;Database=Datenbank1;User ID=Benutzer1;Password=Passwort1;";

            File.WriteAllText(TestFilePath, CreateValidXml(new string[] {
                MakeXmlElementForConnection("eins", expectedConnectionString)
            }));

            var secretLoader = new SecretLoader(
                new XmlMetadata(DeploymentXmlNamespace, TestFilePath, SchemaDeploymentFilePath));

            Assert.Equal(expectedConnectionString, secretLoader.GetDatabaseConnString("eins"));

            File.Delete(TestFilePath);
        }

        [Fact]
        public void GetDatabaseConnString_WhenManyAvailable_ThenGiveThem()
        {
            var expectedConnectionString = new string[] {
                "Server=Server1;Database=Datenbank1;User ID=Benutzer1;Password=Passwort1;",
                "Server=Server2;Database=Datenbank2;User ID=Benutzer2;Password=Passwort2;",
                "Server=Server3;Database=Datenbank3;User ID=Benutzer3;Password=Passwort3;"
            };

            File.WriteAllText(TestFilePath, CreateValidXml(new string[] {
                MakeXmlElementForConnection("eins", expectedConnectionString[0]),
                MakeXmlElementForConnection("zwei", expectedConnectionString[1]),
                MakeXmlElementForConnection("drei", expectedConnectionString[2])
            }));

            var secretLoader = new SecretLoader(
                new XmlMetadata(DeploymentXmlNamespace, TestFilePath, SchemaDeploymentFilePath));

            Assert.Equal(expectedConnectionString[0], secretLoader.GetDatabaseConnString("eins"));
            Assert.Equal(expectedConnectionString[1], secretLoader.GetDatabaseConnString("zwei"));
            Assert.Equal(expectedConnectionString[2], secretLoader.GetDatabaseConnString("drei"));

            File.Delete(TestFilePath);
        }

    }// end of class SecretLoaderTest

}// end of namespace Reusable.DataAccess.UnitTests
