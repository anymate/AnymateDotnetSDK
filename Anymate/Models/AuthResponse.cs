namespace Anymate
{
    public class AuthResponse
    {
        public bool Succeeded { get; set; } = true;

        public string HttpMessage { get; set; }
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
        public string refresh_token { get; set; }
        public static AuthResponse Failed { get => new AuthResponse() { Succeeded = false }; }
    }
}
