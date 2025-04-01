public class CustomWebApplicationFactoryWithoutAuth : CustomWebApplicationFactory
{
    public CustomWebApplicationFactoryWithoutAuth() : base(enableAuth: false) { }
}

[CollectionDefinition("CurrencyApiNoAuthTests")]
public class CurrencyApiNoAuthTestCollection : ICollectionFixture<CustomWebApplicationFactoryWithoutAuth> { }