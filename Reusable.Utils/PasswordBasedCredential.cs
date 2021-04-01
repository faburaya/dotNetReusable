using System;

namespace Reusable.Utils
{
    public class PasswordBasedCredential : IEquatable<PasswordBasedCredential>
    {
        public string UserId { get; set; }

        public string Password { get; set; }

        public bool Equals(PasswordBasedCredential other)
        {
            return this.UserId == other.UserId
                && this.Password == other.Password;
        }
    }
}
