namespace auth.Models;

    public class JwtValidationResult
    {
        public bool IsValid { get; set; }
        public string UserId { get; set; }
        public string[] Scopes { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class VerifyTokenRequest
{
    public string Token { get; set; }
}