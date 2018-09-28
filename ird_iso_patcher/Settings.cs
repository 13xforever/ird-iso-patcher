using System.Configuration;

namespace test_patcher
{
    internal sealed class Settings: ApplicationSettingsBase
    {
        [UserScopedSetting, DefaultSettingValue("")]
        public string IsoDir { get => (string)this[nameof(IsoDir)]; set => this[nameof(IsoDir)] = value; }

        [UserScopedSetting, DefaultSettingValue("")]
        public string IrdDir { get => (string)this[nameof(IrdDir)]; set => this[nameof(IrdDir)] = value; }
    }
}
