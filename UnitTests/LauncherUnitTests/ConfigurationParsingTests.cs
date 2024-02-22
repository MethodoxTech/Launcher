namespace LauncherUnitTests
{
    public class ConfigurationParsingTests
    {
        [Fact]
        public void ShouldBeAbleToRecognizeVariousPatterns()
        {
            Assert.Equal(Launcher.ShortcutType.Verbatim, Launcher.Launcher.ParseShortcut(@"workspace: !code ""C:\My Folder\My Subfolder""").Type);
            Assert.Equal(Launcher.ShortcutType.DiskLocation, Launcher.Launcher.ParseShortcut(@"folder: C:\My Folder\My Subfolder").Type);
        }
    }
}