using Glide.Core.Models;

namespace Glide.Core.Security;

public interface INavigationPolicy
{
    bool IsAllowed(ServiceDescriptor service, Uri uri);
    bool IsExternalLinkBlocked(Uri targetUri);
    bool IsStrictEnabled { get; set; }
}

public class StrictNavigationPolicy : INavigationPolicy
{
    public static bool GlobalStrictSecurityMode { get; set; } = true;

    public bool IsStrictEnabled
    {
        get => GlobalStrictSecurityMode;
        set => GlobalStrictSecurityMode = value;
    }

    public bool IsAllowed(ServiceDescriptor service, Uri uri)
    {
        if (uri == null) return false;

        // Apenas esquemas seguros (HTTPS ou HTTP)
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            return false;
        }

        // Se o usuário desativou explicitamente o isolamento estrito de segurança
        if (!IsStrictEnabled)
        {
            return true;
        }

        string host = uri.Host.ToLowerInvariant();

        foreach (var allowed in service.AllowedDomains)
        {
            string cleanAllowed = allowed.ToLowerInvariant();
            if (host == cleanAllowed || host.EndsWith("." + cleanAllowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsExternalLinkBlocked(Uri targetUri)
    {
        if (!IsStrictEnabled) return false;
        return true;
    }
}
