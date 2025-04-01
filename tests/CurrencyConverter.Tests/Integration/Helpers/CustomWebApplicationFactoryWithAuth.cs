public class CustomWebApplicationFactoryWithAuth : CustomWebApplicationFactory
{
    public CustomWebApplicationFactoryWithAuth() : base(enableAuth: true) { }
}

[CollectionDefinition("CurrencyApiAuthTests")]
public class CurrencyApiAuthTestCollection : ICollectionFixture<CustomWebApplicationFactoryWithAuth> { }