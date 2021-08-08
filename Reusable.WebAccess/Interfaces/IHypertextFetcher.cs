using System;
using System.Threading.Tasks;

namespace Reusable.WebAccess
{
    /// <summary>
    /// Schnittstelle für das Herunterladen von Hypertext.
    /// </summary>
    public interface IHypertextFetcher
    {
        Task<string> DownloadFrom(Uri url);
    }
}