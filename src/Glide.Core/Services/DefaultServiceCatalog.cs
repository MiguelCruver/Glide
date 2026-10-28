using Glide.Core.Enums;
using Glide.Core.Models;

namespace Glide.Core.Services;

public interface IServiceCatalog
{
    IReadOnlyList<ServiceDescriptor> GetAllServices();
    IReadOnlyList<ServiceDescriptor> GetServicesByCategory(ServiceCategory category);
    ServiceDescriptor? GetServiceById(string id);
}

public class DefaultServiceCatalog : IServiceCatalog
{
    private readonly List<ServiceDescriptor> _services = new()
    {
        // Áudio
        new ServiceDescriptor(
            Id: "ytmusic",
            Name: "YouTube Music",
            Description: "Streaming de música contínuo com suporte a reprodução leve e controles no miniplayer.",
            Category: ServiceCategory.Audio,
            Icon: "🎵",
            BaseUrl: "https://music.youtube.com",
            AllowedDomains: new[]
            {
                "music.youtube.com",
                "youtube.com",
                "www.youtube.com",
                "accounts.google.com",
                "googlevideo.com",
                "ytimg.com",
                "google.com",
                "gstatic.com"
            }
        ),
        new ServiceDescriptor(
            Id: "spotify",
            Name: "Spotify",
            Description: "Catálogo de podcasts e músicas com integração via Web Player.",
            Category: ServiceCategory.Audio,
            Icon: "🎧",
            BaseUrl: "https://open.spotify.com",
            AllowedDomains: new[]
            {
                "open.spotify.com",
                "spotify.com",
                "accounts.spotify.com",
                "scdn.co",
                "spotifycdn.com",
                "akamaized.net",
                "spclient.wg.spotify.com"
            }
        ),

        // Vídeo & Redes
        new ServiceDescriptor(
            Id: "youtube",
            Name: "YouTube",
            Description: "Assista a vídeos, transmissões ao vivo e tutoriais no segundo monitor ou no hub.",
            Category: ServiceCategory.Video,
            Icon: "▶️",
            BaseUrl: "https://www.youtube.com",
            AllowedDomains: new[]
            {
                "youtube.com",
                "www.youtube.com",
                "accounts.google.com",
                "googlevideo.com",
                "ytimg.com",
                "google.com",
                "gstatic.com"
            }
        ),
        new ServiceDescriptor(
            Id: "twitter",
            Name: "X (Twitter)",
            Description: "Acompanhe vídeos, clipes de esports e timelines do X sem sair do app.",
            Category: ServiceCategory.Video,
            Icon: "𝕏",
            BaseUrl: "https://x.com",
            AllowedDomains: new[]
            {
                "x.com",
                "twitter.com",
                "twimg.com",
                "abs.twimg.com",
                "ton.twitter.com"
            }
        ),

        // Central de Notificações / Comunicação
        new ServiceDescriptor(
            Id: "steam",
            Name: "Steam Community",
            Description: "Comunidade Steam, amigos, perfis e mercado do CS2.",
            Category: ServiceCategory.Notification,
            Icon: "🎮",
            BaseUrl: "https://steamcommunity.com",
            AllowedDomains: new[]
            {
                "steamcommunity.com",
                "steampowered.com",
                "store.steampowered.com",
                "help.steampowered.com",
                "steamstatic.com"
            }
        ),
        new ServiceDescriptor(
            Id: "gmail",
            Name: "Gmail",
            Description: "Caixa postal unificada com monitoramento de mensagens não lidas em background ultraleve.",
            Category: ServiceCategory.Notification,
            Icon: "✉️",
            BaseUrl: "https://mail.google.com",
            AllowedDomains: new[]
            {
                "mail.google.com",
                "accounts.google.com",
                "google.com",
                "gstatic.com",
                "googleusercontent.com"
            }
        ),
        new ServiceDescriptor(
            Id: "whatsapp",
            Name: "WhatsApp",
            Description: "Comunicação rápida em container restrito com bloqueio padrão de links externos arbitrários.",
            Category: ServiceCategory.Communication,
            Icon: "💬",
            BaseUrl: "https://web.whatsapp.com",
            AllowedDomains: new[]
            {
                "web.whatsapp.com",
                "whatsapp.com",
                "whatsapp.net",
                "fbcdn.net"
            }
        ),
        new ServiceDescriptor(
            Id: "discord",
            Name: "Discord",
            Description: "Visualização web dos canais de texto e mensagens diretas em sandbox isolada.",
            Category: ServiceCategory.Communication,
            Icon: "🎮",
            BaseUrl: "https://discord.com/app",
            AllowedDomains: new[]
            {
                "discord.com",
                "discordapp.com",
                "discord.gg",
                "discordapp.net"
            }
        ),
        new ServiceDescriptor(
            Id: "instagram",
            Name: "Instagram",
            Description: "Acesso a feed, reels, vídeos e conversas com economia de GPU sob demanda.",
            Category: ServiceCategory.Communication,
            Icon: "📸",
            BaseUrl: "https://www.instagram.com",
            AllowedDomains: new[]
            {
                "instagram.com",
                "www.instagram.com",
                "cdninstagram.com",
                "facebook.com",
                "fbcdn.net"
            }
        )
    };

    public IReadOnlyList<ServiceDescriptor> GetAllServices() => _services.AsReadOnly();

    public IReadOnlyList<ServiceDescriptor> GetServicesByCategory(ServiceCategory category) =>
        _services.Where(s => s.Category == category).ToList().AsReadOnly();

    public ServiceDescriptor? GetServiceById(string id) =>
        _services.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
}

