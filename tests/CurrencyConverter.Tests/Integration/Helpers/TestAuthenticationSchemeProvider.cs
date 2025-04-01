using Microsoft.AspNetCore.Authentication;

public class TestAuthenticationSchemeProvider : IAuthenticationSchemeProvider
{
    private readonly AuthenticationScheme _testScheme = new AuthenticationScheme(
        "Test",
        "Test",
        typeof(TestAuthHandler)  // This handler should be used when authentication is required
    );

    public Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync() =>
        Task.FromResult(new[] { _testScheme }.AsEnumerable());

    public Task<AuthenticationScheme?> GetSchemeAsync(string name) =>
        Task.FromResult(name == _testScheme.Name ? _testScheme : null);

    // Ensure the default challenge scheme is set for unauthorized requests
    public Task<AuthenticationScheme?> GetDefaultChallengeSchemeAsync() =>
        Task.FromResult<AuthenticationScheme?>(_testScheme);

    public Task<AuthenticationScheme?> GetDefaultAuthenticateSchemeAsync() =>
        Task.FromResult<AuthenticationScheme?>(null);

    public Task<AuthenticationScheme?> GetDefaultForbidSchemeAsync() =>
        Task.FromResult<AuthenticationScheme?>(null);

    public Task<AuthenticationScheme?> GetDefaultSignInSchemeAsync() =>
        Task.FromResult<AuthenticationScheme?>(null);

    public Task<AuthenticationScheme?> GetDefaultSignOutSchemeAsync() =>
        Task.FromResult<AuthenticationScheme?>(null);

    public void AddScheme(AuthenticationScheme scheme)
    {
        throw new NotImplementedException();
    }

    public void RemoveScheme(string name)
    {
        throw new NotImplementedException();
    }

    // This ensures that it returns the test scheme when checking for valid request handlers
    public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync() =>
        Task.FromResult(new[] { _testScheme }.AsEnumerable());
}