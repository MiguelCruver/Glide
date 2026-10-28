using Glide.Core.Enums;
using Glide.Core.Models;
using Glide.Core.Security;
using Xunit;

namespace Glide.UnitTests;

public class NavigationPolicyTests
{
    private readonly StrictNavigationPolicy _policy = new();

    private readonly ServiceDescriptor _sampleService = new(
        Id: "ytmusic",
        Name: "YouTube Music",
        Description: "Teste",
        Category: ServiceCategory.Audio,
        Icon: "🎵",
        BaseUrl: "https://music.youtube.com",
        AllowedDomains: new[]
        {
            "music.youtube.com",
            "youtube.com",
            "accounts.google.com"
        }
    );

    [Theory]
    [InlineData("https://music.youtube.com")]
    [InlineData("https://music.youtube.com/watch?v=123")]
    [InlineData("https://accounts.google.com/signin/v2")]
    [InlineData("https://subdomain.youtube.com/api")]
    public void IsAllowed_ShouldPermitAuthorizedDomains(string url)
    {
        var uri = new Uri(url);
        bool allowed = _policy.IsAllowed(_sampleService, uri);
        Assert.True(allowed);
    }

    [Theory]
    [InlineData("https://malicious-external-site.com")]
    [InlineData("https://random-phishing.org/login")]
    [InlineData("https://facebook.com")]
    public void IsAllowed_ShouldBlockUnauthorizedDomains(string url)
    {
        var uri = new Uri(url);
        bool allowed = _policy.IsAllowed(_sampleService, uri);
        Assert.False(allowed);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    public void IsAllowed_ShouldRejectUnsafeSchemes(string url)
    {
        var uri = new Uri(url);
        bool allowed = _policy.IsAllowed(_sampleService, uri);
        Assert.False(allowed);
    }
}

