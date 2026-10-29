using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Glide.App.Services;
using Glide.Core.Enums;
using Glide.Core.Models;
using Glide.Core.Services;

namespace Glide.App.ViewModels;

public class NotificationsViewModel : ViewModelBase
{
    private readonly MainViewModel _mainVm;
    private readonly NotificationService _notificationService;
    private string _activeFilter = "Todas";
    private bool _doNotDisturb;

    public NotificationsViewModel(MainViewModel mainVm, IServiceCatalog? catalog = null)
    {
        _mainVm = mainVm;
        _notificationService = NotificationService.Instance;

        FilteredNotifications = new ObservableCollection<UnifiedNotification>();
        RefreshFilteredList();

        _notificationService.NotificationsChanged += () =>
        {
            Application.Current?.Dispatcher?.Invoke(RefreshFilteredList);
        };

        ToggleDoNotDisturbCommand = new RelayCommand(() =>
        {
            DoNotDisturb = !DoNotDisturb;
            OnPropertyChanged(nameof(DndButtonText));
        });

        MarkAllAsReadCommand = new RelayCommand(() =>
        {
            _notificationService.MarkAllAsRead();
            RefreshFilteredList();
        });

        FilterCommand = new RelayCommand(param =>
        {
            if (param is string filter)
            {
                ActiveFilter = filter;
            }
        });

        ClearAllCommand = new RelayCommand(() =>
        {
            _notificationService.ClearAll();
            RefreshFilteredList();
        });

        DeleteNotificationCommand = new RelayCommand(param =>
        {
            if (param is string id)
            {
                _notificationService.DeleteNotification(id);
                RefreshFilteredList();
            }
        });

        MarkAsReadCommand = new RelayCommand(param =>
        {
            if (param is string id)
            {
                _notificationService.MarkAsRead(id);
                RefreshFilteredList();
            }
        });
    }

    public ObservableCollection<UnifiedNotification> FilteredNotifications { get; }

    public string ActiveFilter
    {
        get => _activeFilter;
        set
        {
            if (SetProperty(ref _activeFilter, value))
            {
                RefreshFilteredList();
                OnPropertyChanged(nameof(IsFilterAll));
                OnPropertyChanged(nameof(IsFilterGames));
                OnPropertyChanged(nameof(IsFilterComm));
                OnPropertyChanged(nameof(IsFilterSystem));
            }
        }
    }

    public bool IsFilterAll => ActiveFilter == "Todas";
    public bool IsFilterGames => ActiveFilter == "Jogos";
    public bool IsFilterComm => ActiveFilter == "Comunicação";
    public bool IsFilterSystem => ActiveFilter == "Sistema";

    public bool DoNotDisturb
    {
        get => _doNotDisturb;
        set => SetProperty(ref _doNotDisturb, value);
    }

    public string DndButtonText => DoNotDisturb ? "🔔 Modo Não Perturbe (Ativo)" : "🔕 Modo Gaming (Silenciar)";

    public int UnreadCount => _notificationService.GetAll().Count(n => !n.IsRead);

    private void RefreshFilteredList()
    {
        FilteredNotifications.Clear();
        var all = _notificationService.GetAll();
        var filtered = ActiveFilter == "Todas" 
            ? all 
            : all.Where(n => string.Equals(n.Category, ActiveFilter, StringComparison.OrdinalIgnoreCase));

        foreach (var n in filtered)
        {
            FilteredNotifications.Add(n);
        }

        OnPropertyChanged(nameof(UnreadCount));
        OnPropertyChanged(nameof(HasNoNotifications));
        OnPropertyChanged(nameof(HasNotifications));
    }

    public bool HasNoNotifications => FilteredNotifications.Count == 0;
    public bool HasNotifications => FilteredNotifications.Count > 0;

    public ICommand ToggleDoNotDisturbCommand { get; }
    public ICommand MarkAllAsReadCommand { get; }
    public ICommand FilterCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand DeleteNotificationCommand { get; }
    public ICommand MarkAsReadCommand { get; }
}

public class ConnectViewModel : ViewModelBase, IDisposable
{
    private readonly MainViewModel _mainVm;
    private readonly ConnectDbService _dbService;
    private readonly MicrophoneService _micService;
    private readonly UserProfileService _profileService;
    private readonly GlideOnlineP2PService _p2pService;
    private readonly string _localUserId = $"usr_{Guid.NewGuid():N}";
    private string _roomCode = "GLIDE-7782";
    private string _targetRoomCode = string.Empty;
    private bool _isInRoom = false;
    private bool _isMuted = false;
    private bool _isDeafened = false;
    private bool _isScreenSharing = false;
    private string _chatInput = string.Empty;
    private float _micLevel = 0f;
    private bool _disposed = false;

    public ConnectViewModel(MainViewModel mainVm)
    {
        _mainVm = mainVm;
        _dbService = new ConnectDbService();
        _micService = new MicrophoneService();
        _profileService = UserProfileService.Instance;
        _p2pService = GlideOnlineP2PService.Instance;

        var profile = _profileService.CurrentProfile;

        // Inicia somente com o usuário local (zero participantes falsos ou simulados)
        Participants = new ObservableCollection<ConnectParticipant>
        {
            new ConnectParticipant(Id: _localUserId, DisplayName: profile.Nickname, IsSpeaking: false, IsMuted: false, AvatarPath: profile.AvatarPath)
        };

        ChatMessages = new ObservableCollection<ConnectChatMessage>();

        // Escuta atualizações de perfil em tempo real
        _profileService.ProfileUpdated += updatedProfile =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (Participants.Count > 0)
                {
                    Participants[0] = Participants[0] with
                    {
                        DisplayName = updatedProfile.Nickname,
                        AvatarPath = updatedProfile.AvatarPath
                    };
                    _ = _p2pService.BroadcastPresenceAsync("ping");
                }
            });
        };

        // Carrega histórico do SQLite local
        LoadRoomHistory();

        // Integração em tempo real da rede P2P online (amigos conectados na mesma sala)
        _p2pService.MessageReceived += incomingMsg =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                _dbService.SaveMessage(RoomCode, incomingMsg);
                ChatMessages.Add(incomingMsg);
            });
        };

        _p2pService.ParticipantJoined += peer =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                int existingIdx = -1;
                for (int i = 0; i < Participants.Count; i++)
                {
                    if (Participants[i].Id == peer.Id)
                    {
                        existingIdx = i;
                        break;
                    }
                }

                if (existingIdx >= 0)
                {
                    Participants[existingIdx] = peer;
                }
                else
                {
                    Participants.Add(peer);
                    NotificationService.Instance.AddNotification(new UnifiedNotification(
                        Id: Guid.NewGuid().ToString(),
                        ServiceId: "connect",
                        Sender: peer.DisplayName,
                        Title: "Amigo Conectado",
                        Snippet: $"{peer.DisplayName} ingressou na sua sala do Glide Connect.",
                        Timestamp: DateTime.Now,
                        Category: "Comunicação"
                    ));
                }
            });
        };

        _p2pService.ParticipantLeft += peerId =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                for (int i = Participants.Count - 1; i >= 1; i--)
                {
                    if (Participants[i].Id == peerId)
                    {
                        string leftName = Participants[i].DisplayName;
                        Participants.RemoveAt(i);
                        NotificationService.Instance.AddNotification(new UnifiedNotification(
                            Id: Guid.NewGuid().ToString(),
                            ServiceId: "connect",
                            Sender: "Glide Connect",
                            Title: "Amigo Desconectou",
                            Snippet: $"{leftName} saiu da sala.",
                            Timestamp: DateTime.Now,
                            Category: "Comunicação"
                        ));
                        break;
                    }
                }
            });
        };

        _p2pService.ParticipantSpeakingChanged += (peerId, isSpeaking) =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                for (int i = 1; i < Participants.Count; i++)
                {
                    if (Participants[i].Id == peerId)
                    {
                        Participants[i] = Participants[i] with { IsSpeaking = isSpeaking };
                        break;
                    }
                }
            });
        };

        // Configuração da captura real de áudio do microfone do usuário
        _micService.SpeakingChanged += isSpeaking =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                if (Participants.Count > 0)
                {
                    Participants[0] = Participants[0] with { IsSpeaking = isSpeaking };
                }
            });

            if (IsInRoom)
            {
                _ = _p2pService.SendVoiceActivityAsync(isSpeaking);
            }
        };

        _micService.AudioLevelChanged += level =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                MicLevel = level;
            });
        };

        _micService.Start();

        ToggleMuteCommand = new RelayCommand(() =>
        {
            IsMuted = !IsMuted;
            _micService.IsMuted = IsMuted;
            if (Participants.Count > 0)
            {
                Participants[0] = Participants[0] with
                {
                    IsMuted = IsMuted,
                    IsSpeaking = IsMuted ? false : Participants[0].IsSpeaking
                };
            }
            if (IsInRoom)
            {
                _ = _p2pService.BroadcastPresenceAsync("ping");
            }
        });

        ToggleDeafenCommand = new RelayCommand(() =>
        {
            IsDeafened = !IsDeafened;
            if (Participants.Count > 0)
            {
                Participants[0] = Participants[0] with { IsDeafened = IsDeafened };
            }
            if (IsInRoom)
            {
                _ = _p2pService.BroadcastPresenceAsync("ping");
            }
        });

        ToggleScreenShareCommand = new RelayCommand(() => IsScreenSharing = !IsScreenSharing);

        SendMessageCommand = new RelayCommand(() =>
        {
            if (string.IsNullOrWhiteSpace(ChatInput)) return;
            string authorName = _profileService.CurrentProfile.Nickname;
            var msg = new ConnectChatMessage(
                Id: Guid.NewGuid().ToString(),
                SenderName: authorName,
                Content: ChatInput.Trim(),
                Timestamp: DateTime.Now,
                ImagePath: null,
                IsSelf: true
            );

            _dbService.SaveMessage(RoomCode, msg);
            ChatMessages.Add(msg);
            ChatInput = string.Empty;

            if (IsInRoom)
            {
                _ = _p2pService.SendChatMessageAsync(msg);
            }
        });

        ClearHistoryCommand = new RelayCommand(() =>
        {
            _dbService.ClearMessages(RoomCode);
            ChatMessages.Clear();

            // Limpa anexos da sala do disco
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roomAttachDir = Path.Combine(appData, "Glide", "Attachments", RoomCode);
            if (Directory.Exists(roomAttachDir))
            {
                try { Directory.Delete(roomAttachDir, true); } catch {}
            }
        });

        GenerateRoomCodeCommand = new RelayCommand(() =>
        {
            var rnd = new Random();
            RoomCode = $"GLIDE-{rnd.Next(1000, 9999)}";
            TargetRoomCode = RoomCode;
            LoadRoomHistory();
            if (IsInRoom && Participants.Count > 0)
            {
                _ = _p2pService.ConnectToRoomAsync(RoomCode, Participants[0]);
            }
        });

        CreateRoomCommand = new RelayCommand(async () =>
        {
            var rnd = new Random();
            RoomCode = $"GLIDE-{rnd.Next(1000, 9999)}";
            TargetRoomCode = RoomCode;
            IsInRoom = true;
            LoadRoomHistory();

            if (Participants.Count > 0)
            {
                await _p2pService.ConnectToRoomAsync(RoomCode, Participants[0]);
            }

            NotificationService.Instance.AddNotification(new UnifiedNotification(
                Id: Guid.NewGuid().ToString(),
                ServiceId: "connect",
                Sender: "Glide Connect",
                Title: "Sala Criada com Sucesso",
                Snippet: $"Você criou e entrou na sala {RoomCode}. Convide amigos compartilhando seu código.",
                Timestamp: DateTime.Now,
                Category: "Comunicação"
            ));
        });

        CopyRoomCodeCommand = new RelayCommand(() =>
        {
            try
            {
                Clipboard.SetText(RoomCode);
                NotificationService.Instance.AddNotification(new UnifiedNotification(
                    Id: Guid.NewGuid().ToString(),
                    ServiceId: "connect",
                    Sender: "Glide Connect",
                    Title: "Código Copiado",
                    Snippet: $"Código {RoomCode} copiado para a área de transferência.",
                    Timestamp: DateTime.Now,
                    Category: "Comunicação"
                ));
            }
            catch {}
        });

        JoinRoomCommand = new RelayCommand(async () =>
        {
            string codeToJoin = !string.IsNullOrWhiteSpace(TargetRoomCode) 
                ? TargetRoomCode.Trim().ToUpperInvariant() 
                : RoomCode.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(codeToJoin)) return;

            RoomCode = codeToJoin;
            IsInRoom = true;
            LoadRoomHistory();

            if (Participants.Count > 0)
            {
                await _p2pService.ConnectToRoomAsync(RoomCode, Participants[0]);
            }

            NotificationService.Instance.AddNotification(new UnifiedNotification(
                Id: Guid.NewGuid().ToString(),
                ServiceId: "connect",
                Sender: "Glide Connect",
                Title: "Entrou na Sala",
                Snippet: $"Você conectou-se com sucesso à sala {RoomCode}.",
                Timestamp: DateTime.Now,
                Category: "Comunicação"
            ));
        });

        LeaveRoomCommand = new RelayCommand(async () =>
        {
            string oldRoom = RoomCode;
            IsInRoom = false;
            await _p2pService.LeaveRoomAsync();

            // Remove todos os participantes remotos, preservando apenas o usuário local
            while (Participants.Count > 1)
            {
                Participants.RemoveAt(Participants.Count - 1);
            }

            NotificationService.Instance.AddNotification(new UnifiedNotification(
                Id: Guid.NewGuid().ToString(),
                ServiceId: "connect",
                Sender: "Glide Connect",
                Title: "Desconectado",
                Snippet: $"Você saiu da sala {oldRoom}.",
                Timestamp: DateTime.Now,
                Category: "Comunicação"
            ));
        });
    }

    public ObservableCollection<ConnectParticipant> Participants { get; }
    public ObservableCollection<ConnectChatMessage> ChatMessages { get; }

    public void AddAttachment(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath)) return;

        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roomAttachDir = Path.Combine(appData, "Glide", "Attachments", RoomCode);
            Directory.CreateDirectory(roomAttachDir);

            string ext = Path.GetExtension(sourceFilePath);
            string destFileName = $"{Guid.NewGuid()}{ext}";
            string destPath = Path.Combine(roomAttachDir, destFileName);
            File.Copy(sourceFilePath, destPath, true);

            string authorName = _profileService.CurrentProfile.Nickname;
            var msg = new ConnectChatMessage(
                Id: Guid.NewGuid().ToString(),
                SenderName: authorName,
                Content: $"[Imagem: {Path.GetFileName(sourceFilePath)}]",
                Timestamp: DateTime.Now,
                ImagePath: destPath,
                IsSelf: true
            );

            _dbService.SaveMessage(RoomCode, msg);
            ChatMessages.Add(msg);

            if (IsInRoom)
            {
                _ = _p2pService.SendChatMessageAsync(msg, destPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Glide Connect] Falha ao salvar anexo: {ex.Message}");
        }
    }

    private void LoadRoomHistory()
    {
        ChatMessages.Clear();
        var msgs = _dbService.GetMessages(RoomCode);
        if (msgs.Count == 0)
        {
            var welcome = new ConnectChatMessage(
                Id: Guid.NewGuid().ToString(),
                SenderName: "Glide Connect",
                Content: $"Sala {RoomCode} pronta. Compartilhe o código para conectar amigos via rede P2P criptografada.",
                Timestamp: DateTime.Now,
                ImagePath: null,
                IsSelf: false
            );
            _dbService.SaveMessage(RoomCode, welcome);
            ChatMessages.Add(welcome);
        }
        else
        {
            foreach (var m in msgs)
            {
                ChatMessages.Add(m);
            }
        }
    }

    public float MicLevel
    {
        get => _micLevel;
        set => SetProperty(ref _micLevel, value);
    }

    public string RoomCode
    {
        get => _roomCode;
        set => SetProperty(ref _roomCode, value);
    }

    public string TargetRoomCode
    {
        get => _targetRoomCode;
        set => SetProperty(ref _targetRoomCode, value);
    }

    public bool IsInRoom
    {
        get => _isInRoom;
        set => SetProperty(ref _isInRoom, value);
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (SetProperty(ref _isMuted, value))
            {
                OnPropertyChanged(nameof(MuteButtonText));
            }
        }
    }

    public bool IsDeafened
    {
        get => _isDeafened;
        set
        {
            if (SetProperty(ref _isDeafened, value))
            {
                OnPropertyChanged(nameof(DeafenButtonText));
            }
        }
    }

    public bool IsScreenSharing
    {
        get => _isScreenSharing;
        set
        {
            if (SetProperty(ref _isScreenSharing, value))
            {
                OnPropertyChanged(nameof(ScreenShareButtonText));
            }
        }
    }

    public string MuteButtonText => IsMuted ? "🎤 Desmutar" : "🎤 Mutar";
    public string DeafenButtonText => IsDeafened ? "🎧 Ouvir" : "🎧 Ensurdecer";
    public string ScreenShareButtonText => IsScreenSharing ? "⏹ Parar Transmissão" : "🖥️ Compartilhar Tela (AMD AMF)";

    public string ChatInput
    {
        get => _chatInput;
        set => SetProperty(ref _chatInput, value);
    }

    public ICommand ToggleMuteCommand { get; }
    public ICommand ToggleDeafenCommand { get; }
    public ICommand ToggleScreenShareCommand { get; }
    public ICommand SendMessageCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand GenerateRoomCodeCommand { get; }
    public ICommand CreateRoomCommand { get; }
    public ICommand CopyRoomCodeCommand { get; }
    public ICommand JoinRoomCommand { get; }
    public ICommand LeaveRoomCommand { get; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _micService.Dispose();
        _p2pService.Dispose();
    }
}

public class SettingsViewModel : ViewModelBase
{
    private readonly MainViewModel _mainVm;
    private readonly AppSettingsService _settingsService;
    private readonly UserProfileService _profileService;
    private readonly AppSettings _settings;

    private string _profileNick;
    private string _profileStatus;
    private string? _avatarPath;
    private string? _bannerPath;
    private string _saveProfileFeedback = string.Empty;

    // Atualizador Integrado (GitHub Releases)
    private string _updateStatusMessage = "Versão oficial instalada. Clique para verificar novas atualizações.";
    private bool _isCheckingUpdate = false;
    private bool _hasUpdateAvailable = false;
    private string? _latestVersionTag = null;
    private string? _updateDownloadUrl = null;
    private string? _releaseNotes = null;
    private int _downloadProgressPercentage = 0;
    private bool _isDownloadingUpdate = false;

    public SettingsViewModel(MainViewModel mainVm)
    {
        _mainVm = mainVm;
        _settingsService = AppSettingsService.Instance;
        _profileService = UserProfileService.Instance;
        _settings = _settingsService.Settings;

        var profile = _profileService.CurrentProfile;
        _profileNick = profile.Nickname;
        _profileStatus = profile.StatusMessage;
        _avatarPath = profile.AvatarPath;
        _bannerPath = profile.BannerPath;

        SaveProfileCommand = new RelayCommand(() =>
        {
            var updated = new UserProfile(
                Nickname: string.IsNullOrWhiteSpace(ProfileNick) ? "Raposo" : ProfileNick.Trim(),
                StatusMessage: string.IsNullOrWhiteSpace(ProfileStatus) ? "Online no Glide" : ProfileStatus.Trim(),
                AvatarPath: AvatarPath,
                BannerPath: BannerPath
            );
            _profileService.SaveProfile(updated);
            SaveProfileFeedback = "✓ Perfil salvo com sucesso!";
        });

        ClearCacheCommand = new RelayCommand(() =>
        {
            _settingsService.ClearWebViewCache();
            MessageBox.Show("O cache temporário do navegador WebView2 foi limpo com sucesso.", "Glide — Manutenção", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        ResetDefaultsCommand = new RelayCommand(() =>
        {
            var res = MessageBox.Show("Deseja restaurar todas as configurações para o padrão de fábrica?", "Glide — Redefinir", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                _settingsService.ResetToDefaults();
                OnPropertyChanged(string.Empty);
            }
        });

        CheckForUpdateCommand = new RelayCommand(async () =>
        {
            if (IsCheckingUpdate || IsDownloadingUpdate) return;
            IsCheckingUpdate = true;
            UpdateStatusMessage = "Consultando GitHub Releases...";

            try
            {
                var result = await AppUpdateService.Instance.CheckForUpdateAsync();
                UpdateStatusMessage = result.StatusMessage;
                HasUpdateAvailable = result.HasUpdate;
                LatestVersionTag = result.LatestVersion;
                UpdateDownloadUrl = result.DownloadUrl;
                ReleaseNotes = result.ReleaseNotes;

                if (result.HasUpdate)
                {
                    NotificationService.Instance.AddNotification(new UnifiedNotification(
                        Id: Guid.NewGuid().ToString(),
                        ServiceId: "system",
                        Sender: "Glide Updater",
                        Title: "Nova Atualização Disponível",
                        Snippet: $"A versão {result.LatestVersion} do Glide está pronta para download e instalação.",
                        Timestamp: DateTime.Now,
                        Category: "Sistema"
                    ));
                }
            }
            catch (Exception ex)
            {
                UpdateStatusMessage = $"Falha ao verificar atualizações: {ex.Message}";
                HasUpdateAvailable = false;
            }
            finally
            {
                IsCheckingUpdate = false;
            }
        });

        InstallUpdateCommand = new RelayCommand(async () =>
        {
            if (string.IsNullOrEmpty(UpdateDownloadUrl) || IsDownloadingUpdate) return;

            IsDownloadingUpdate = true;
            DownloadProgressPercentage = 0;
            UpdateStatusMessage = "Baixando instalador oficial...";

            var progress = new Progress<int>(pct =>
            {
                DownloadProgressPercentage = pct;
                UpdateStatusMessage = $"Baixando atualização: {pct}%...";
            });

            try
            {
                string installerPath = await AppUpdateService.Instance.DownloadUpdateAsync(UpdateDownloadUrl, progress);
                UpdateStatusMessage = "Download concluído! Iniciando instalação...";
                await Task.Delay(400);
                AppUpdateService.Instance.LaunchInstallerAndShutdown(installerPath);
            }
            catch (Exception ex)
            {
                UpdateStatusMessage = $"Erro no download: {ex.Message}";
                IsDownloadingUpdate = false;
            }
        });
    }

    // --- Perfil do Usuário ---
    public string ProfileNick
    {
        get => _profileNick;
        set
        {
            if (SetProperty(ref _profileNick, value))
            {
                SaveProfileFeedback = string.Empty;
            }
        }
    }

    public string ProfileStatus
    {
        get => _profileStatus;
        set
        {
            if (SetProperty(ref _profileStatus, value))
            {
                SaveProfileFeedback = string.Empty;
            }
        }
    }

    public string? AvatarPath
    {
        get => _avatarPath;
        set => SetProperty(ref _avatarPath, value);
    }

    public string? BannerPath
    {
        get => _bannerPath;
        set => SetProperty(ref _bannerPath, value);
    }

    public string SaveProfileFeedback
    {
        get => _saveProfileFeedback;
        set => SetProperty(ref _saveProfileFeedback, value);
    }

    public void UpdateAvatar(string filePath)
    {
        string dest = _profileService.SetAvatar(filePath);
        AvatarPath = dest;
    }

    public void UpdateBanner(string filePath)
    {
        string dest = _profileService.SetBanner(filePath);
        BannerPath = dest;
    }

    // --- Segurança & Isolamento Estrito (Com Bloqueio e Confirmação de Risco) ---
    public bool StrictSecurityMode
    {
        get => _settings.StrictSecurityMode;
        set
        {
            if (_settings.StrictSecurityMode == value) return;

            if (!value)
            {
                // Usuário tentando desativar a segurança: EXIGE confirmação explícita
                var result = MessageBox.Show(
                    "⚠️ ATENÇÃO: CONFIRMAÇÃO DE RISCO DE SEGURANÇA\n\n" +
                    "Desativar o Isolamento Estrito permitirá a abertura irrestrita de links externos, scripts não homologados e pop-ups durante o seu jogo.\n\n" +
                    "Isso anula o sandbox de navegação que mantém o Glide seguro e ultraleve.\n\n" +
                    "Deseja realmente assumir o risco e desativar o isolamento de segurança?",
                    "Glide — Confirmação de Risco de Isolamento",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    // Reverte para ligado
                    OnPropertyChanged(nameof(StrictSecurityMode));
                    return;
                }
            }

            _settings.StrictSecurityMode = value;
            _settingsService.SaveSettings(_settings);
            OnPropertyChanged();
        }
    }

    // --- Demais Configurações ---
    public bool CloseToTray
    {
        get => _settings.CloseToTray;
        set
        {
            if (_settings.CloseToTray != value)
            {
                _settings.CloseToTray = value;
                _settingsService.SaveSettings(_settings);
                OnPropertyChanged();
            }
        }
    }

    public bool StartMinimized
    {
        get => _settings.StartMinimized;
        set
        {
            if (_settings.StartMinimized != value)
            {
                _settings.StartMinimized = value;
                _settingsService.SaveSettings(_settings);
                OnPropertyChanged();
            }
        }
    }

    public bool GamingModeAuto
    {
        get => _settings.GamingMode;
        set
        {
            if (_settings.GamingMode != value)
            {
                _settings.GamingMode = value;
                _settingsService.SaveSettings(_settings);
                OnPropertyChanged();
                _mainVm.IsGamingMode = value;
            }
        }
    }

    public bool AutoEcoModeInBackground
    {
        get => _settings.AutoEcoModeInBackground;
        set
        {
            if (_settings.AutoEcoModeInBackground != value)
            {
                _settings.AutoEcoModeInBackground = value;
                _settingsService.SaveSettings(_settings);
                OnPropertyChanged();
            }
        }
    }

    public bool HardwareAcceleration
    {
        get => _settings.HardwareAcceleration;
        set
        {
            if (_settings.HardwareAcceleration != value)
            {
                _settings.HardwareAcceleration = value;
                _settingsService.SaveSettings(_settings);
                OnPropertyChanged();
            }
        }
    }

    public bool PushToTalkEnabled
    {
        get => _settings.PushToTalkEnabled;
        set
        {
            if (_settings.PushToTalkEnabled != value)
            {
                _settings.PushToTalkEnabled = value;
                _settingsService.SaveSettings(_settings);
                OnPropertyChanged();
            }
        }
    }

    public string PushToTalkKey
    {
        get => _settings.PushToTalkKey;
        set
        {
            if (_settings.PushToTalkKey != value)
            {
                _settings.PushToTalkKey = value;
                _settingsService.SaveSettings(_settings);
                OnPropertyChanged();
            }
        }
    }

    // --- Atualizações do Sistema (In-App Updater) ---
    public string CurrentVersionText => AppUpdateService.CurrentVersion;

    public string UpdateStatusMessage
    {
        get => _updateStatusMessage;
        set => SetProperty(ref _updateStatusMessage, value);
    }

    public bool IsCheckingUpdate
    {
        get => _isCheckingUpdate;
        set
        {
            if (SetProperty(ref _isCheckingUpdate, value))
            {
                OnPropertyChanged(nameof(CanCheckForUpdate));
            }
        }
    }

    public bool HasUpdateAvailable
    {
        get => _hasUpdateAvailable;
        set
        {
            if (SetProperty(ref _hasUpdateAvailable, value))
            {
                OnPropertyChanged(nameof(CanInstallUpdate));
            }
        }
    }

    public string? LatestVersionTag
    {
        get => _latestVersionTag;
        set => SetProperty(ref _latestVersionTag, value);
    }

    public string? UpdateDownloadUrl
    {
        get => _updateDownloadUrl;
        set => SetProperty(ref _updateDownloadUrl, value);
    }

    public string? ReleaseNotes
    {
        get => _releaseNotes;
        set => SetProperty(ref _releaseNotes, value);
    }

    public int DownloadProgressPercentage
    {
        get => _downloadProgressPercentage;
        set => SetProperty(ref _downloadProgressPercentage, value);
    }

    public bool IsDownloadingUpdate
    {
        get => _isDownloadingUpdate;
        set
        {
            if (SetProperty(ref _isDownloadingUpdate, value))
            {
                OnPropertyChanged(nameof(CanCheckForUpdate));
                OnPropertyChanged(nameof(CanInstallUpdate));
            }
        }
    }

    public bool CanCheckForUpdate => !IsCheckingUpdate && !IsDownloadingUpdate;
    public bool CanInstallUpdate => HasUpdateAvailable && !IsDownloadingUpdate;

    public ICommand SaveProfileCommand { get; }
    public ICommand ClearCacheCommand { get; }
    public ICommand ResetDefaultsCommand { get; }
    public ICommand CheckForUpdateCommand { get; }
    public ICommand InstallUpdateCommand { get; }
}
