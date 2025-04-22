using ipvcr.Logic.Api;
using System.IO.Abstractions;

namespace ipvcr.Logic.Settings;

public abstract class BaseSettingsManager<T> where T : class, new()
{
    public event EventHandler<SettingsChangedEventArgs<T>>? SettingsChanged;

    protected readonly IFileSystem _filesystem;
    protected T _settings;
    protected readonly object _lock = new();
    protected readonly string _settingsFilename;
    protected readonly string _settingsPath;

    protected BaseSettingsManager(IFileSystem filesystem, string settingsFilename, string settingsPath)
    {
        _filesystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
        _settingsFilename = settingsFilename;
        _settingsPath = settingsPath;
        _settings = LoadSettings();
    }

    protected virtual T LoadSettings()
    {
        lock (_lock)
        {
            var fullSettingsPath = Path.Combine(_settingsPath, _settingsFilename);

            if (!_filesystem.File.Exists(fullSettingsPath))
            {
                var defaultSettings = CreateDefaultSettings();
                _settings = defaultSettings;
                SaveSettingsToFile(System.Text.Json.JsonSerializer.Serialize(defaultSettings));
                return defaultSettings;
            }

            try
            {
                var json = _filesystem.File.ReadAllText(fullSettingsPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new T();
                }

                var deserialized = System.Text.Json.JsonSerializer.Deserialize<T>(json) ?? new T();
                return deserialized;
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException("The settings file is not readable.");
            }
            catch (System.Text.Json.JsonException)
            {
                throw new InvalidOperationException("The settings file contains invalid JSON.");
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("An error occurred while reading the settings file.", ex);
            }
        }
    }

    public virtual T Settings
    {
        get
        {
            lock (_lock)
            {
                return _settings;
            }
        }
        set
        {
            lock (_lock)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _settings = value;
                SaveSettings(_settings);
                RaiseSettingsChanged(_settings);
            }
        }
    }

    protected virtual T CreateDefaultSettings()
    {
        return new T();
    }

    protected void SaveSettings(T settings)
    {
        lock (_lock)
        {
            _settings = settings;
            SaveSettingsToFile(System.Text.Json.JsonSerializer.Serialize(settings));
        }
    }

    protected void SaveSettingsToFile(string jsonSettings)
    {
        var fullSettingsPath = Path.Combine(_settingsPath, _settingsFilename);
        try
        {
            if (!_filesystem.File.Exists(fullSettingsPath))
            {
                using var fileStream = _filesystem.File.Create(fullSettingsPath);
            }

            _filesystem.File.WriteAllText(fullSettingsPath, jsonSettings);
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException("The settings file is not writable.");
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("An error occurred while writing to the settings file.", ex);
        }
    }

    protected void RaiseSettingsChanged(T settings)
    {
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs<T>(settings));
    }
}
