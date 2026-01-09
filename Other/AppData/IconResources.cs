using System.Drawing;

namespace AISFixer.AppData
{
    class IconResources
    {
        public static Icon AISFixerIcon => AppResource.GetIcon("Nalog.ico");
        public static Bitmap UnarchiveIcon => AppResource.GetBitmap("Unarchive.ico");
        public static Bitmap SettingsIcon => AppResource.GetBitmap("Settings.ico");
        public static Bitmap OpenFolderIcon => AppResource.GetBitmap("OpenFolderIcon.ico"); 
        public static Bitmap AboutIcon => AppResource.GetBitmap("About.ico");
        public static Bitmap ExitIcon => AppResource.GetBitmap("Exit.ico");
    }
}


