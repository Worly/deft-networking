using System;
using System.Linq;

namespace Deft.Utils
{
    internal static class IdTokenManager
    {
        private const string KEY_SUFIX = "_Deft_NETWORK_ID_TOKEN";

        public static string GetIdToken(string connectionIdentifier)
        {
            var idToken = "";
            if (DeftConfig.Settings.HasKey(connectionIdentifier + KEY_SUFIX))
                idToken = DeftConfig.Settings.GetValue(connectionIdentifier + KEY_SUFIX);

            return idToken;
        }

        public static void SaveIdToken(string connectionIdentifier, string idToken)
        {
            DeftConfig.Settings.SetValue(connectionIdentifier + KEY_SUFIX, idToken);
        }

        public static string GenerateIdToken()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_.~$%^&*(){}[]";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 32)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
