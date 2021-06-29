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
}
