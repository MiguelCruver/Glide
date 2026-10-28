using System.Diagnostics;
using NAudio.Wave;

namespace Glide.App.Services;

public class MicrophoneService : IDisposable
{
    private WaveInEvent? _waveIn;
    private bool _isMuted = false;
    private bool _isDisposed = false;
    private bool _isSpeaking = false;
    private DateTime _lastSpeechTime = DateTime.MinValue;
    private readonly object _lock = new();

    public event Action<float>? AudioLevelChanged;
    public event Action<bool>? SpeakingChanged;

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            _isMuted = value;
            if (_isMuted && _isSpeaking)
            {
                _isSpeaking = false;
                SpeakingChanged?.Invoke(false);
            }
        }
    }

    public bool IsSpeaking => _isSpeaking;

    public void Start()
    {
        lock (_lock)
        {
            if (_waveIn != null) return;

            try
            {
                if (WaveInEvent.DeviceCount == 0) return;

                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(16000, 16, 1),
                    BufferMilliseconds = 80
                };

                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _waveIn.StartRecording();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Glide Microfone] Erro ao iniciar captura: {ex.Message}");
                _waveIn?.Dispose();
                _waveIn = null;
            }
        }
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_isMuted || e.BytesRecorded == 0)
        {
            AudioLevelChanged?.Invoke(0f);
            if (_isSpeaking)
            {
                _isSpeaking = false;
                SpeakingChanged?.Invoke(false);
            }
            return;
        }

        int sampleCount = e.BytesRecorded / 2;
        double sum = 0;

        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            short sample = (short)(e.Buffer[i] | (e.Buffer[i + 1] << 8));
            double normalized = sample / 32768.0;
            sum += normalized * normalized;
        }

        double rms = Math.Sqrt(sum / sampleCount);
        float level = (float)Math.Clamp(rms * 4.0, 0.0, 1.0);

        AudioLevelChanged?.Invoke(level);

        bool speechDetected = rms > 0.015;
        var now = DateTime.UtcNow;

        if (speechDetected)
        {
            _lastSpeechTime = now;
            if (!_isSpeaking)
            {
                _isSpeaking = true;
                SpeakingChanged?.Invoke(true);
            }
        }
        else
        {
            // Hangover de 300ms para evitar oscilações
            if (_isSpeaking && (now - _lastSpeechTime).TotalMilliseconds > 300)
            {
                _isSpeaking = false;
                SpeakingChanged?.Invoke(false);
            }
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            try
            {
                if (_waveIn != null)
                {
                    _waveIn.DataAvailable -= WaveIn_DataAvailable;
                    _waveIn.StopRecording();
                    _waveIn.Dispose();
                    _waveIn = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Glide Microfone] Erro ao parar captura: {ex.Message}");
            }
            finally
            {
                if (_isSpeaking)
                {
                    _isSpeaking = false;
                    SpeakingChanged?.Invoke(false);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Stop();
    }
}

