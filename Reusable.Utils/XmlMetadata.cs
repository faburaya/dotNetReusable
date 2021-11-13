
namespace Reusable.Utils
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

        /// <summary>
        /// Erstellt eine neue Instanz von <see cref="XmlMetadata"/>.
        /// </summary>
        /// <param name="xmlns">Das Namespace.</param>
        /// <param name="filePath">Das Pfad der XML-Datei.</param>
        /// <param name="schemaFilePath">Das Pfad der XSD-Datei.</param>
        public XmlMetadata(string xmlns, string filePath, string schemaFilePath)
        {
            this.XmlNamespace = xmlns;
            this.FilePath = filePath;
            this.SchemaFilePath = schemaFilePath;
        }
    }

}// end of namespace Reusable.Utils
