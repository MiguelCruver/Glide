using Glide.Core.Enums;
using Glide.Core.Services;
using Xunit;

namespace Glide.UnitTests;

public class ServiceCatalogTests
{
    private readonly DefaultServiceCatalog _catalog = new();

    [Fact]
    public void GetAllServices_ShouldReturnRegisteredServices()
    {
        var services = _catalog.GetAllServices();
        Assert.NotNull(services);
        Assert.True(services.Count >= 7);

        // Verifica serviços essenciais
        Assert.Contains(services, s => s.Id == "ytmusic");
        Assert.Contains(services, s => s.Id == "spotify");
        Assert.Contains(services, s => s.Id == "youtube");
        Assert.Contains(services, s => s.Id == "gmail");
        Assert.Contains(services, s => s.Id == "whatsapp");
        Assert.Contains(services, s => s.Id == "discord");
        Assert.Contains(services, s => s.Id == "instagram");
        Assert.Contains(services, s => s.Id == "twitter");
    }

    [Theory]
    [InlineData(ServiceCategory.Audio, 2)]
    [InlineData(ServiceCategory.Video, 1)]
    [InlineData(ServiceCategory.Communication, 3)]
    [InlineData(ServiceCategory.Notification, 1)]
    public void GetServicesByCategory_ShouldFilterCorrectly(ServiceCategory category, int minimumExpected)
    {
        var services = _catalog.GetServicesByCategory(category);
        Assert.NotNull(services);
        Assert.True(services.Count >= minimumExpected);
        Assert.All(services, s => Assert.Equal(category, s.Category));
    }

    [Fact]
    public void Services_MustHaveAllowedDomainsConfigured()
    {
        var services = _catalog.GetAllServices();
        foreach (var service in services)
        {
            Assert.False(string.IsNullOrWhiteSpace(service.Id));
            Assert.False(string.IsNullOrWhiteSpace(service.BaseUrl));
            Assert.NotEmpty(service.AllowedDomains);
            Assert.All(service.AllowedDomains, domain => Assert.False(string.IsNullOrWhiteSpace(domain)));
        }
    }
}

