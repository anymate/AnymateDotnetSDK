using System;
using System.Collections.Generic;
using System.Text;

namespace Anymate
{
    public class AnymateCredentials
    {

        public AnymateCredentials(string customerKey, string secret, string username, string password)
        {
            CustomerKey = customerKey;
            Secret = secret;
            Username = username;
            Password = password;
        }


        public AnymateCredentials()
        {

        }
        public string CustomerKey { get; set; }
        public string Secret { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AnymateCredentialsOnPremisesMode : AnymateCredentials
    {
        public AnymateCredentialsOnPremisesMode(string customerKey, string secret, string username, string password, string clientUri, string authUri)
        {
            CustomerKey = customerKey;
            Secret = secret;
            Username = username;
            Password = password;

            clientUri = clientUri.Trim();
            if (clientUri.EndsWith("/"))
                clientUri = clientUri.Substring(0, clientUri.Length - 1);

            authUri = authUri.Trim();
            if (authUri.EndsWith("/"))
                authUri = authUri.Substring(0, authUri.Length - 1);

            ClientUri = clientUri;
            AuthUri = authUri;
        }


        public AnymateCredentialsOnPremisesMode()
        {

        }
        public string ClientUri { get; set; }
        public string AuthUri { get; set; }
    }
}
