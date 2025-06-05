namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Settings;

    /// <summary>
    /// Custom settings related to backward compatibility.
    /// </summary>
    public partial class CompatibilitySettings : ExposeSettings
    {
        internal CompatibilitySettings(SettingsHolder settings) : base(settings)
        {
        }
    }
}
