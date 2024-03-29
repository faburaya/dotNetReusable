﻿using System;

using System.IO;

using Xunit;

namespace Reusable.Utils.UnitTests
{
    public class SecretLoaderTest
    {
        private string TestFilePath => "test_SecretLoader.xml";

        private string SchemaDeploymentFilePath => "secrets.xsd";

        private string DeploymentXmlNamespace => "http://utils.reusable.faburaya.com/secrets";

        [Fact]
        public void Instantiate_WhenXmlFileUnavailable_ThenThrow()
        {
            Assert.Throws<FileNotFoundException>(() => new SecretLoader(
                new XmlMetadata(DeploymentXmlNamespace, "nicht vorhandene Datei", SchemaDeploymentFilePath)));
        }

        private static string CreateValidXml(string[] xmlElementsForConnStrings,
                                             string[] xmlElementsForCredentials)
        {
            var allConnStrings = new System.Text.StringBuilder();
            if (xmlElementsForConnStrings != null)
            {
                foreach (string entry in xmlElementsForConnStrings)
                    allConnStrings.AppendLine(entry);
            }

            var allCredentials = new System.Text.StringBuilder();
            if (xmlElementsForCredentials != null)
            {
                foreach (string entry in xmlElementsForCredentials)
                    allCredentials.AppendLine(entry);
            }

            return string.Format(@"<?xml version=""1.0"" encoding=""utf-8"" ?><secrets xmlns=""http://utils.reusable.faburaya.com/secrets""><connectionStrings>{0}</connectionStrings><credentials>{1}</credentials></secrets>",
                allConnStrings.ToString(),
                allCredentials.ToString());
        }

        [Theory]
        [InlineData("<ill></formed>", typeof(System.Xml.XmlException))]
        [InlineData("<root></root>", typeof(System.Xml.Schema.XmlSchemaException))]
        public void Instantiate_WhenInvalidXml_ThenThrow(string xmlContent, Type exceptionType)
        {
            File.WriteAllText(TestFilePath, xmlContent);

            try
            {
                Assert.Throws(exceptionType, () => new SecretLoader(
                    new XmlMetadata("http://nichts.de", TestFilePath, SchemaDeploymentFilePath)));
            }
            finally
            {
                File.Delete(TestFilePath);
            }
        }

        [Fact]
        public void Instantiate_WhenXmlEmpty_ThenDoNotThrow()
        {
            File.WriteAllText(TestFilePath, CreateValidXml(null, null));

            try
            {
                new SecretLoader(
                    new XmlMetadata(DeploymentXmlNamespace, TestFilePath, SchemaDeploymentFilePath));
            }
            finally
            {
                File.Delete(TestFilePath);
            }
        }

        private static string MakeXmlElementForConnection(string connName, string connString)
        {
            return $@"<connection name=""{connName}"" string=""{connString}"" />";
        }

        [Fact]
        public void GetConnectionString_WhenNameUnavailable_ThenNull()
        {
            File.WriteAllText(TestFilePath, CreateValidXml(new string[] {
                MakeXmlElementForConnection("eins", "Server=Server1;Database=Datenbank1;User ID=Benutzer1;Password=Passwort1;")
            }, null));

            try
            {
                var secretLoader = new SecretLoader(
                    new XmlMetadata(DeploymentXmlNamespace, TestFilePath, SchemaDeploymentFilePath));

                Assert.Null(secretLoader.GetConnectionString("zwei"));
            }
            finally
            {
                File.Delete(TestFilePath);
            }
        }

        [Fact]
        public void GetConnectionString_WhenOneAvailable_ThenGiveIt()
        {
            string expectedConnectionString = "Server=Server1;Database=Datenbank1;User ID=Benutzer1;Password=Passwort1;";

            File.WriteAllText(TestFilePath, CreateValidXml(new string[] {
                MakeXmlElementForConnection("eins", expectedConnectionString)
            }, null));

            try
            {
                var secretLoader = new SecretLoader(
                    new XmlMetadata(DeploymentXmlNamespace, TestFilePath, SchemaDeploymentFilePath));

                Assert.Equal(expectedConnectionString, secretLoader.GetConnectionString("eins"));
            }
            finally
            {
                File.Delete(TestFilePath);
            }
        }

        [Fact]
        public void GetConnectionString_WhenManyAvailable_ThenGiveThem()
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
            }, null));

            try
            {
                var secretLoader = new SecretLoader(
                    new XmlMetadata(DeploymentXmlNamespace, TestFilePath, SchemaDeploymentFilePath));

                Assert.Equal(expectedConnectionString[0], secretLoader.GetConnectionString("eins"));
                Assert.Equal(expectedConnectionString[1], secretLoader.GetConnectionString("zwei"));
                Assert.Equal(expectedConnectionString[2], secretLoader.GetConnectionString("drei"));
            }
            finally
            {
                File.Delete(TestFilePath);
            }
        }

        private static string MakeXmlElementForCredential(string credentialName,
                                                          string userId,
                                                          string password)
        {
            return $@"<credential name=""{credentialName}"" userid=""{userId}"" password=""{password}"" />";
        }

        [Fact]
        public void GetCredential_WhenNameUnavailable_ThenNull()
        {
            File.WriteAllText(TestFilePath, CreateValidXml(null, new string[] {
                MakeXmlElementForCredential("eins", "paloma", "Kennwort1")
            }));

            try
            {
                var secretLoader = new SecretLoader(
                    new XmlMetadata(DeploymentXmlNamespace, TestFilePath, SchemaDeploymentFilePath));

                Assert.Null(secretLoader.GetCredential("zwei"));
            }
            finally
            {
                File.Delete(TestFilePath);
            }
        }

        [Fact]
        public void GetCredential_WhenOneAvailable_ThenGiveIt()
        {
            var expectedCredential = new PasswordBasedCredential
            {
                UserId = "paloma",
                Password = "Kennwort1"
            };

            File.WriteAllText(TestFilePath, CreateValidXml(null, new string[] {
                MakeXmlElementForCredential("eins", expectedCredential.UserId, expectedCredential.Password)
            }));

            try
            {
                var secretLoader = new SecretLoader(
                    new XmlMetadata(DeploymentXmlNamespace, TestFilePath, SchemaDeploymentFilePath));

                Assert.Equal(expectedCredential, secretLoader.GetCredential("eins"));
            }
            finally
            {
                File.Delete(TestFilePath);
            }
        }

        [Fact]
        public void GetCredential_WhenManyAvailable_ThenGiveThem()
        {
            var expectedCredential = new PasswordBasedCredential[]
            {
                new PasswordBasedCredential { UserId = "paloma", Password = "Kennwort1" },
                new PasswordBasedCredential { UserId = "andressa", Password = "Kennwort2" },
                new PasswordBasedCredential { UserId = "liane", Password = "Kennwort3" }
            };

            File.WriteAllText(TestFilePath, CreateValidXml(null, new string[] {
                MakeXmlElementForCredential("eins",
                    expectedCredential[0].UserId, expectedCredential[0].Password),
                MakeXmlElementForCredential("zwei",
                    expectedCredential[1].UserId, expectedCredential[1].Password),
                MakeXmlElementForCredential("drei",
                    expectedCredential[2].UserId, expectedCredential[2].Password),
            }));

            try
            {
                var secretLoader = new SecretLoader(
                    new XmlMetadata(DeploymentXmlNamespace, TestFilePath, SchemaDeploymentFilePath));

                Assert.Equal(expectedCredential[0], secretLoader.GetCredential("eins"));
                Assert.Equal(expectedCredential[1], secretLoader.GetCredential("zwei"));
                Assert.Equal(expectedCredential[2], secretLoader.GetCredential("drei"));
            }
            finally
            {
                File.Delete(TestFilePath);
            }
        }

    }// end of class SecretLoaderTest

}// end of namespace Reusable.DataAccess.UnitTests
