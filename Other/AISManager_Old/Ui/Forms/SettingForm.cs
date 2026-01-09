using AISManager.App.Configs;
using HardDev.CoreUtils.Config;

namespace AISManager.Ui.Forms
{
    public partial class SettingForm : Form
    {
        public SettingForm()
        {
            InitializeComponent();

            LoadSettings();
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
        }

        public static DialogResult ShowModal()
        {
            using var settingForm = new SettingForm();
            return settingForm.ShowDialog();
        }

        public void LoadSettings()
        {
            MainConfig mainCfg = AppConfig.GetOrLoad<MainConfig>();

            checkBoxAutoScan.Checked = mainCfg.AutoStartScan;
        }

        private void SaveSettings()
        {
            MainConfig mainCfg = AppConfig.GetOrLoad<MainConfig>();

            mainCfg.AutoStartScan = checkBoxAutoScan.Checked;
            mainCfg.Save();

            DialogResult = DialogResult.Yes;
            Close();
        }

        private void EnterAndEscapeKeys(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
            {
                Close();
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}