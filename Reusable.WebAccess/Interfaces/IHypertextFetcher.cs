using System;
using System.Threading.Tasks;

namespace Reusable.WebAccess
{
    /// <summary>
    /// Schnittstelle für das Herunterladen von Hypertext.
    /// </summary>
    public interface IHypertextFetcher
    {
        /// <summary>
        /// Ladet den Hypertext aus einem gegebenen URL herunter.
        /// </summary>
        /// <param name="url">Das gegebene URL.</param>
        /// <returns>Der Hypertext der Internetseite.</returns>
        Task<string> DownloadFrom(Uri url);
    }
}