using System;

namespace Reusable.DataAccess
{
    /// <summary>
    /// Metadaten über die zu ladende XML-Datei.
    /// </summary>
    public class XmlMetadata
    {
        /// <summary>
        /// Namespace, wo in der XML-Datei die Namen deklariert werden.
        /// </summary>
        public string XmlNamespace { get; }

        /// <summary>
        /// Pfad der XML-Datei.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Pfad der XSD-Datei.
        /// </summary>
        public string SchemaFilePath { get; }

        public XmlMetadata(string xmlns, string filePath, string schemaFilePath)
        {
            this.XmlNamespace = xmlns;
            this.FilePath = filePath;
            this.SchemaFilePath = schemaFilePath;
        }
    }

}
