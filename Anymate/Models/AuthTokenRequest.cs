namespace Anymate.Models
{
    public class AuthTokenRequest
    {
        public AuthTokenRequest(string customerKey, string secret, string username, string password)
        {
            client_id = customerKey;
            client_secret = secret;
            this.username = username;
            this.password = password;
        }
        
        public AuthTokenRequest()
        {
        }
        public string access_token { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string username { get; set; }
        public string password { get; set; }

    }
}
