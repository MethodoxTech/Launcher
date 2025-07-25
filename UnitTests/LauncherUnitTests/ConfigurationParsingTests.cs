using Launcher.Shared;

namespace LauncherUnitTests
{
    public class ConfigurationParsingTests
    {
        [Fact]
        public void ShouldBeAbleToRecognizeVariousPatterns()
        {
            Assert.Equal(ShortcutType.Verbatim, LauncherCore.ParseShortcut(@"workspace: !code ""C:\My Folder\My Subfolder""").Type);
            Assert.Equal(ShortcutType.DiskLocation, LauncherCore.ParseShortcut(@"folder: C:\My Folder\My Subfolder").Type);
        }
    }
}