namespace AISManager
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            _downloadButton = new Button();
            checkVersionButton = new Button();
            _progressBar = new ProgressBar();
            checkBox = new CheckBox();
            menuStrip1 = new MenuStrip();
            toolStripMenuItem1 = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripMenuItem();
            toolStripMenuItem6 = new ToolStripMenuItem();
            toolStripMenuItem7 = new ToolStripMenuItem();
            toolStripMenuItem3 = new ToolStripMenuItem();
            toolStripMenuItem4 = new ToolStripMenuItem();
            panel3 = new Panel();
            label2 = new Label();
            label1 = new Label();
            versionLabel = new Label();
            panel1 = new Panel();
            panel7 = new Panel();
            label4 = new Label();
            clearLogsButton = new Button();
            panel2 = new Panel();
            splitContainer1 = new SplitContainer();
            _hotfixesListView = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            panel4 = new Panel();
            _logTextBox = new TextBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            panel5 = new Panel();
            label3 = new Label();
            panel6 = new Panel();
            checkBoxAutoScan = new CheckBox();
            checkBox1 = new CheckBox();
            label5 = new Label();
            menuStrip1.SuspendLayout();
            panel3.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            panel4.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            panel5.SuspendLayout();
            panel6.SuspendLayout();
            SuspendLayout();
            // 
            // _downloadButton
            // 
            _downloadButton.Location = new Point(418, 9);
            _downloadButton.Margin = new Padding(0);
            _downloadButton.Name = "_downloadButton";
            _downloadButton.Size = new Size(247, 33);
            _downloadButton.TabIndex = 3;
            _downloadButton.Text = "Скачать выбранные обновления";
            _downloadButton.UseVisualStyleBackColor = true;
            // 
            // checkVersionButton
            // 
            checkVersionButton.Location = new Point(267, 9);
            checkVersionButton.Margin = new Padding(0);
            checkVersionButton.Name = "checkVersionButton";
            checkVersionButton.Size = new Size(151, 33);
            checkVersionButton.TabIndex = 2;
            checkVersionButton.Text = "Поиск обновлений";
            checkVersionButton.UseVisualStyleBackColor = true;
            // 
            // _progressBar
            // 
            _progressBar.Dock = DockStyle.Bottom;
            _progressBar.Location = new Point(3, 86);
            _progressBar.Name = "_progressBar";
            _progressBar.Size = new Size(665, 2);
            _progressBar.TabIndex = 12;
            // 
            // checkBox
            // 
            checkBox.AutoSize = true;
            checkBox.Location = new Point(3, 14);
            checkBox.Name = "checkBox";
            checkBox.Size = new Size(125, 24);
            checkBox.TabIndex = 9;
            checkBox.Text = "Выделить всё";
            checkBox.UseVisualStyleBackColor = true;
            checkBox.CheckedChanged += checkBox_CheckedChanged;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1, toolStripMenuItem3 });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(6, 3, 0, 3);
            menuStrip1.Size = new Size(859, 30);
            menuStrip1.TabIndex = 10;
            menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItem2, toolStripMenuItem6, toolStripMenuItem7 });
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(117, 24);
            toolStripMenuItem1.Text = "Инструменты";
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(270, 26);
            toolStripMenuItem2.Text = "Данные программы";
            toolStripMenuItem2.Click += OpenApplicationFolder_Click;
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            toolStripMenuItem6.Size = new Size(270, 26);
            toolStripMenuItem6.Text = "Папка для сохранения";
            toolStripMenuItem6.Click += SaveAsToolStripMenuItem_Click;
            // 
            // toolStripMenuItem7
            // 
            toolStripMenuItem7.Name = "toolStripMenuItem7";
            toolStripMenuItem7.Size = new Size(270, 26);
            toolStripMenuItem7.Text = "Открыть папку с фиксами";
            toolStripMenuItem7.Click += OpenDownloadFolder_Click;
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItem4 });
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new Size(81, 24);
            toolStripMenuItem3.Text = "Справка";
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new Size(187, 26);
            toolStripMenuItem4.Text = "О программе";
            // 
            // panel3
            // 
            panel3.Controls.Add(label2);
            panel3.Controls.Add(label1);
            panel3.Dock = DockStyle.Top;
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(187, 70);
            panel3.TabIndex = 2;
            panel3.Paint += panel3_Paint;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 204);
            label2.Location = new Point(12, 14);
            label2.Name = "label2";
            label2.Size = new Size(113, 23);
            label2.TabIndex = 2;
            label2.Text = "AIS Manager";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = SystemColors.Control;
            label1.Font = new Font("Segoe UI", 7.8F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label1.Location = new Point(12, 38);
            label1.Name = "label1";
            label1.Size = new Size(164, 17);
            label1.TabIndex = 1;
            label1.Text = "Automated Update System";
            // 
            // versionLabel
            // 
            versionLabel.AutoSize = true;
            versionLabel.Location = new Point(270, 9);
            versionLabel.Name = "versionLabel";
            versionLabel.Size = new Size(0, 20);
            versionLabel.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.Controls.Add(panel7);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(panel3);
            panel1.Dock = DockStyle.Left;
            panel1.Location = new Point(0, 30);
            panel1.Name = "panel1";
            panel1.Size = new Size(187, 629);
            panel1.TabIndex = 13;
            panel1.Paint += panel1_Paint;
            // 
            // panel7
            // 
            panel7.BackColor = SystemColors.ActiveBorder;
            panel7.Dock = DockStyle.Top;
            panel7.Location = new Point(0, 70);
            panel7.Name = "panel7";
            panel7.Size = new Size(187, 1);
            panel7.TabIndex = 4;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(27, 586);
            label4.Name = "label4";
            label4.Size = new Size(149, 20);
            label4.TabIndex = 3;
            label4.Text = "Prototype Version 0.5";
            // 
            // clearLogsButton
            // 
            clearLogsButton.Location = new Point(149, 9);
            clearLogsButton.Margin = new Padding(0);
            clearLogsButton.Name = "clearLogsButton";
            clearLogsButton.Size = new Size(118, 33);
            clearLogsButton.TabIndex = 4;
            clearLogsButton.Text = "Очистить логи";
            clearLogsButton.UseVisualStyleBackColor = true;
            clearLogsButton.Click += ButtonClearLogs_Click;
            // 
            // panel2
            // 
            panel2.BackColor = SystemColors.ActiveBorder;
            panel2.Dock = DockStyle.Left;
            panel2.Location = new Point(187, 30);
            panel2.Name = "panel2";
            panel2.Size = new Size(1, 629);
            panel2.TabIndex = 14;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(3, 94);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(_hotfixesListView);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(panel4);
            splitContainer1.Size = new Size(665, 532);
            splitContainer1.SplitterDistance = 293;
            splitContainer1.SplitterWidth = 1;
            splitContainer1.TabIndex = 0;
            // 
            // _hotfixesListView
            // 
            _hotfixesListView.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2 });
            _hotfixesListView.Dock = DockStyle.Fill;
            _hotfixesListView.FullRowSelect = true;
            _hotfixesListView.Location = new Point(0, 0);
            _hotfixesListView.Name = "_hotfixesListView";
            _hotfixesListView.Size = new Size(665, 293);
            _hotfixesListView.TabIndex = 11;
            _hotfixesListView.UseCompatibleStateImageBehavior = false;
            _hotfixesListView.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Номер фикса";
            columnHeader1.Width = 150;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Размер";
            columnHeader2.Width = 140;
            // 
            // panel4
            // 
            panel4.Controls.Add(clearLogsButton);
            panel4.Controls.Add(_logTextBox);
            panel4.Controls.Add(_downloadButton);
            panel4.Controls.Add(checkBox);
            panel4.Controls.Add(checkVersionButton);
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(0, 0);
            panel4.Name = "panel4";
            panel4.Size = new Size(665, 238);
            panel4.TabIndex = 13;
            // 
            // _logTextBox
            // 
            _logTextBox.Dock = DockStyle.Bottom;
            _logTextBox.Location = new Point(0, 47);
            _logTextBox.Margin = new Padding(5);
            _logTextBox.Multiline = true;
            _logTextBox.Name = "_logTextBox";
            _logTextBox.ReadOnly = true;
            _logTextBox.ScrollBars = ScrollBars.Vertical;
            _logTextBox.Size = new Size(665, 191);
            _logTextBox.TabIndex = 6;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(splitContainer1, 0, 3);
            tableLayoutPanel1.Controls.Add(_progressBar, 0, 2);
            tableLayoutPanel1.Controls.Add(panel5, 0, 0);
            tableLayoutPanel1.Controls.Add(panel6, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(188, 30);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 55.8139534F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 44.1860466F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 9F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 537F));
            tableLayoutPanel1.Size = new Size(671, 629);
            tableLayoutPanel1.TabIndex = 15;
            // 
            // panel5
            // 
            panel5.Controls.Add(label3);
            panel5.Controls.Add(versionLabel);
            panel5.Dock = DockStyle.Fill;
            panel5.Location = new Point(3, 3);
            panel5.Name = "panel5";
            panel5.Size = new Size(665, 40);
            panel5.TabIndex = 13;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
            label3.Location = new Point(3, 9);
            label3.Name = "label3";
            label3.Size = new Size(267, 20);
            label3.TabIndex = 0;
            label3.Text = "Текущая версия АИС Налог 3 Пром: ";
            // 
            // panel6
            // 
            panel6.Controls.Add(checkBoxAutoScan);
            panel6.Controls.Add(checkBox1);
            panel6.Controls.Add(label5);
            panel6.Dock = DockStyle.Fill;
            panel6.Location = new Point(3, 49);
            panel6.Name = "panel6";
            panel6.Size = new Size(665, 30);
            panel6.TabIndex = 14;
            // 
            // checkBoxAutoScan
            // 
            checkBoxAutoScan.AutoSize = true;
            checkBoxAutoScan.Checked = true;
            checkBoxAutoScan.CheckState = CheckState.Checked;
            checkBoxAutoScan.Location = new Point(377, 3);
            checkBoxAutoScan.Name = "checkBoxAutoScan";
            checkBoxAutoScan.Size = new Size(131, 24);
            checkBoxAutoScan.TabIndex = 1;
            checkBoxAutoScan.Text = "Автопроверка";
            checkBoxAutoScan.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Checked = true;
            checkBox1.CheckState = CheckState.Checked;
            checkBox1.Location = new Point(162, 3);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(219, 24);
            checkBox1.TabIndex = 1;
            checkBox1.Text = "Авто-создание SFX (.Fix.rar)";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 204);
            label5.Location = new Point(3, 4);
            label5.Name = "label5";
            label5.Size = new Size(153, 20);
            label5.TabIndex = 0;
            label5.Text = "Список обновлений";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(859, 659);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            KeyPreview = true;
            MainMenuStrip = menuStrip1;
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Прототип AIS Manager";
            Load += MainForm_Load;
            Shown += MainForm_Shown;
            KeyDown += ScanOnF5Key;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            panel6.ResumeLayout(false);
            panel6.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button checkVersionButton;
        private Button _downloadButton;
        private CheckBox checkBox;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem toolStripMenuItem2;
        private ToolStripMenuItem toolStripMenuItem3;
        private ToolStripMenuItem toolStripMenuItem4;
        private ToolStripMenuItem toolStripMenuItem6;
        private ToolStripMenuItem toolStripMenuItem7;
        private ProgressBar _progressBar;
        private Panel panel3;
        private Label label1;
        private Label versionLabel;
        private Panel panel1;
        private Label label2;
        private Panel panel2;
        private SplitContainer splitContainer1;
        private ListView _hotfixesListView;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private TextBox _logTextBox;
        private TableLayoutPanel tableLayoutPanel1;
        private Panel panel4;
        private Panel panel5;
        private Label label3;
        private Button clearLogsButton;
        private Panel panel6;
        private Label label5;
        private CheckBox checkBox1;
        private CheckBox checkBoxAutoScan;
        private Label label4;
        private Panel panel7;
    }
}
