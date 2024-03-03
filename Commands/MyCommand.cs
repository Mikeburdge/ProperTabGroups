namespace ProperTabGroups
{
    [Command(PackageIds.MyCommand)]
    internal sealed class MyCommand : BaseCommand<MyCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var variables = await VS.Windows.GetAllWindowsAsync();

            if (variables != null)
            {
                foreach (var window in variables)
                {
                }
            }

        }
    }
}
