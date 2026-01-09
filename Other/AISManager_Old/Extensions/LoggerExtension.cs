using Serilog;

namespace AISManager.Extensions
{
    public static class LoggerExtension
    {
        public static DialogResult VerboseAndShow(this ILogger logger,
            string text,
            string caption = null,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.None)
        {
            logger.Verbose("{Text}", text);
            return MessageBox.Show(text, caption, buttons, icon);
        }

        public static DialogResult VerboseAndShow(this ILogger logger,
            Control owner,
            string text,
            string caption = null,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.None)
        {
            logger.Verbose("{Text}", text);
            return owner.MessageBoxShow(text, caption, buttons, icon);
        }

        public static DialogResult InformationAndShow(this ILogger logger,
            string text,
            string caption = "Информация",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            logger.Information("{Text}", text);
            return MessageBox.Show(text, caption, buttons, icon);
        }

        public static DialogResult InformationAndShow(this ILogger logger,
            Control owner,
            string text,
            string caption = "Информация",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            logger.Information("{Text}", text);
            return owner.MessageBoxShow(text, caption, buttons, icon);
        }

        public static DialogResult DebugAndShow(this ILogger logger,
            string text,
            string caption = "Отладка",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.None)
        {
            logger.Debug("{Text}", text);
            return MessageBox.Show(text, caption, buttons, icon);
        }

        public static DialogResult DebugAndShow(this ILogger logger,
            Control owner,
            string text,
            string caption = "Отладка",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.None)
        {
            logger.Debug("{Text}", text);
            return owner.MessageBoxShow(text, caption, buttons, icon);
        }

        public static DialogResult WarningAndShow(this ILogger logger,
            string text,
            string caption = "Предупреждение",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Warning)
        {
            logger.Warning("{Text}", text);
            return MessageBox.Show(text, caption, buttons, icon);
        }

        public static DialogResult WarningAndShow(this ILogger logger,
            Control owner,
            string text,
            string caption = "Предупреждение",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Warning)
        {
            logger.Warning("{Text}", text);
            return owner.MessageBoxShow(text, caption, buttons, icon);
        }

        public static DialogResult ErrorAndShow(this ILogger logger,
            string text,
            string caption = "Ошибка",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            logger.Error("{Text}", text);
            return MessageBox.Show(text, caption, buttons, icon);
        }

        public static DialogResult ErrorAndShow(this ILogger logger,
            Control owner,
            string text,
            string caption = "Ошибка",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            logger.Error("{Text}", text);
            return owner.MessageBoxShow(text, caption, buttons, icon);
        }

        public static DialogResult ErrorAndShow(this ILogger logger,
            Exception exception,
            string text,
            string caption = "Ошибка",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            logger.Error(exception, "{Text}", text);
            return MessageBox.Show(text, caption, buttons, icon);
        }

        public static DialogResult ErrorAndShow(this ILogger logger,
            Control owner,
            Exception exception,
            string text,
            string caption = "Ошибка",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            logger.Error(exception, "{Text}", text);
            return owner.MessageBoxShow(text, caption, buttons, icon);
        }

        public static DialogResult FatalAndShow(this ILogger logger,
            string text,
            string caption = "Фатальная ошибка",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            logger.Fatal("{Text}", text);
            return MessageBox.Show(text, caption, buttons, icon);
        }

        public static DialogResult FatalAndShow(this ILogger logger,
            Control owner,
            string text,
            string caption = "Фатальная ошибка",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            logger.Fatal("{Text}", text);
            return owner.MessageBoxShow(text, caption, buttons, icon);
        }


        public static DialogResult FatalAndShow(this ILogger logger,
            Exception exception,
            string text,
            string caption = "Фатальная ошибка",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            logger.Fatal(exception, "{Text}", text);
            return MessageBox.Show(text, caption, buttons, icon);
        }

        public static DialogResult FatalAndShow(this ILogger logger,
            Control owner,
            Exception exception,
            string text,
            string caption = "Фатальная ошибка",
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            logger.Fatal(exception, "{Text}", text);
            return owner.MessageBoxShow(text, caption, buttons, icon);
        }
    }
}
