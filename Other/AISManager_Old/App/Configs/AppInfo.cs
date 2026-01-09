namespace AISManager.App.Configs
{
    internal static class AppInfo
    {
        public static string GetBranchAndCommitInfo()
        {
            string branch = ThisAssembly.Git.Branch;
            string commit = ThisAssembly.Git.Commit;
            bool isDirty = ThisAssembly.Git.IsDirty;

            string separator = isDirty ? "-" : "+";

            return $"{branch}{separator}{commit}";
        }
    }
}
