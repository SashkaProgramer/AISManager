using System.Reflection;

namespace AISManager.Extensions
{
    public static class ControlExtension
    {
        public static void RunOnUiThread(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }

        public static TResult RunOnUiThread<TResult>(this Control control, Func<TResult> func)
        {
            if (control.InvokeRequired)
            {
                return (TResult)control.Invoke(func);
            }

            return func();
        }

        public static void DoubleBuffered(this Control control, bool enabled)
        {
            PropertyInfo prop = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            prop?.SetValue(control, enabled, null);
        }

        public static DialogResult MessageBoxShow(this Control owner,
            string text,
            string caption = null,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.None)
        {
            return owner.RunOnUiThread(() => MessageBox.Show(owner, text, caption, buttons, icon));
        }
    }
}
