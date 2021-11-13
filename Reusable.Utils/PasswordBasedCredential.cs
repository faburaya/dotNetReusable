using System;

namespace Reusable.Utils
{
    /// <summary>
    /// Enthält eine auf Kennwort basierte Anmeldeinformation.
    /// </summary>
    public class PasswordBasedCredential : IEquatable<PasswordBasedCredential>
    {
        /// <summary>
        /// Die Benutzerkennung.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Das Kennwort.
        /// </summary>
        public string Password { get; set; }

        /// <inheritdoc/>
        public bool Equals(PasswordBasedCredential other)
        {
            return this.UserId == other.UserId
                && this.Password == other.Password;
        }
    }
}
