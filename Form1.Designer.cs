namespace Resizer3
{

    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            tabControl = new TabControl();
            tabPageAllFiles = new TabPage();
            tabPageFailedFiles = new TabPage();
            dataGridView1 = new DataGridView();
            dataGridViewFailed = new DataGridView();
            InputFiles = new DataGridViewTextBoxColumn();
            FailedFilePath = new DataGridViewTextBoxColumn();
            FailedErrorMessage = new DataGridViewTextBoxColumn();
            buttonResize = new Button();
            textBox1 = new TextBox();
            label1 = new Label();
            numericUpDownQuality = new NumericUpDown();
            label2 = new Label();
            update = new Button();
            Label_3 = new Label();
            label3 = new Label();
            numFiles = new Label();
            completedFiles = new Label();
            numericUpDownThreads = new NumericUpDown();
            label4 = new Label();
            cancelButton = new Button();
            button1 = new Button();
            label5 = new Label();
            splitContainer1 = new SplitContainer();
            label10 = new Label();
            label8 = new Label();
            afterSizeLabel = new Label();
            label11 = new Label();
            mbPerSecLabel = new Label();
            percentSavedLabel = new Label();
            elapsedLabel = new Label();
            filesPerSecLabel = new Label();
            beforeSizeLabel = new Label();
            label9 = new Label();
            label7 = new Label();
            comboBox1 = new ComboBox();
            numericUpDownRes = new NumericUpDown();
            label6 = new Label();
            checkBox1 = new CheckBox();
            maxResComboBox = new ComboBox();
            labelMaxRes = new Label();
            labelWebpEffort = new Label();
            numericUpDownWebpEffort = new NumericUpDown();
            optimizedButton = new Button();
            processingProgress = new ProgressBar();
            clearListButton = new Button();
            // tabControl doesn't implement ISupportInitialize, no BeginInit needed
            tabPageAllFiles.SuspendLayout();
            tabPageFailedFiles.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewFailed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownQuality).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownThreads).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownRes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownWebpEffort).BeginInit();
            SuspendLayout();
            // 
            // tabControl
            // 
            tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl.Controls.AddRange(new Control[] { tabPageAllFiles, tabPageFailedFiles });
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(560, 490);
            tabControl.TabIndex = 0;
            // 
            // tabPageAllFiles
            // 
            tabPageAllFiles.Controls.Add(dataGridView1);
            tabPageAllFiles.Location = new Point(4, 22);
            tabPageAllFiles.Name = "tabPageAllFiles";
            tabPageAllFiles.Padding = new Padding(3);
            tabPageAllFiles.Size = new Size(552, 464);
            tabPageAllFiles.TabIndex = 0;
            tabPageAllFiles.Text = "All Files";
            tabPageAllFiles.UseVisualStyleBackColor = true;
            // 
            // tabPageFailedFiles
            // 
            tabPageFailedFiles.Controls.Add(dataGridViewFailed);
            tabPageFailedFiles.Location = new Point(4, 22);
            tabPageFailedFiles.Name = "tabPageFailedFiles";
            tabPageFailedFiles.Padding = new Padding(3);
            tabPageFailedFiles.Size = new Size(552, 464);
            tabPageFailedFiles.TabIndex = 1;
            tabPageFailedFiles.Text = "Failed Files";
            tabPageFailedFiles.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToOrderColumns = true;
            dataGridView1.AllowDrop = true;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { InputFiles });
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(0, 0);
            dataGridView1.Margin = new Padding(0);
            dataGridView1.MinimumSize = new Size(65, 48);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 25;
            dataGridView1.Size = new Size(546, 458);
            dataGridView1.TabIndex = 0;
            dataGridView1.DragDrop += dataGridView1_DragDropAsync;
            dataGridView1.DragEnter += dataGridView1_DragEnter;
            dataGridView1.MouseLeave += dataGridView1_MouseLeave;
            // 
            // dataGridViewFailed
            // 
            dataGridViewFailed.AllowUserToAddRows = false;
            dataGridViewFailed.AllowUserToDeleteRows = false;
            dataGridViewFailed.AllowUserToOrderColumns = true;
            dataGridViewFailed.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewFailed.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewFailed.Columns.AddRange(new DataGridViewColumn[] { FailedFilePath, FailedErrorMessage });
            dataGridViewFailed.Dock = DockStyle.Fill;
            dataGridViewFailed.Location = new Point(0, 0);
            dataGridViewFailed.Margin = new Padding(0);
            dataGridViewFailed.Name = "dataGridViewFailed";
            dataGridViewFailed.ReadOnly = true;
            dataGridViewFailed.RowHeadersWidth = 25;
            dataGridViewFailed.Size = new Size(546, 458);
            dataGridViewFailed.TabIndex = 1;
            dataGridViewFailed.DoubleClick += dataGridViewFailed_DoubleClick;
            // 
            // FailedFilePath
            // 
            FailedFilePath.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            FailedFilePath.DataPropertyName = "FilePath";
            FailedFilePath.HeaderText = "File Path";
            FailedFilePath.Name = "FailedFilePath";
            FailedFilePath.ReadOnly = true;
            // 
            // FailedErrorMessage
            // 
            FailedErrorMessage.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            FailedErrorMessage.DataPropertyName = "ErrorMessage";
            FailedErrorMessage.HeaderText = "Error Message";
            FailedErrorMessage.Name = "FailedErrorMessage";
            FailedErrorMessage.ReadOnly = true;
            // 
            // InputFiles
            // 
            InputFiles.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            InputFiles.DataPropertyName = "InputFiles";
            InputFiles.HeaderText = "InputFiles               (Drag and Drop)";
            InputFiles.Name = "InputFiles";
            // 
            // clearListButton
            // 
            clearListButton.Anchor = AnchorStyles.Top;
            clearListButton.Location = new Point(197, 51);
            clearListButton.Margin = new Padding(4, 3, 4, 3);
            clearListButton.Name = "clearListButton";
            clearListButton.Size = new Size(64, 26);
            clearListButton.TabIndex = 14;
            clearListButton.Text = "Clear List";
            clearListButton.UseVisualStyleBackColor = true;
            clearListButton.Click += clearListButton_Click;
            // 
            // buttonResize
            // 
            buttonResize.Anchor = AnchorStyles.Bottom;
            buttonResize.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            buttonResize.Font = new Font("Arial", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            buttonResize.Location = new Point(62, 384);
            buttonResize.Margin = new Padding(4, 3, 4, 3);
            buttonResize.MaximumSize = new Size(1120, 588);
            buttonResize.MinimumSize = new Size(112, 59);
            buttonResize.Name = "buttonResize";
            buttonResize.Size = new Size(199, 80);
            buttonResize.TabIndex = 1;
            buttonResize.Text = "Convert";
            buttonResize.UseVisualStyleBackColor = true;
            buttonResize.Click += buttonResize_Click;
            // 
            // textBox1
            // 
            textBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBox1.Location = new Point(6, 22);
            textBox1.Margin = new Padding(4, 3, 4, 3);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(190, 23);
            textBox1.TabIndex = 2;
            textBox1.DoubleClick += textBox1_DoubleClick;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(4, 0);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(90, 16);
            label1.TabIndex = 3;
            label1.Text = "Output Path:";
            // 
            // numericUpDownQuality
            // 
            numericUpDownQuality.Anchor = AnchorStyles.Bottom;
            numericUpDownQuality.Location = new Point(78, 253);
            numericUpDownQuality.Margin = new Padding(4, 3, 4, 3);
            numericUpDownQuality.MaximumSize = new Size(59, 0);
            numericUpDownQuality.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDownQuality.MinimumSize = new Size(59, 0);
            numericUpDownQuality.Name = "numericUpDownQuality";
            numericUpDownQuality.Size = new Size(59, 23);
            numericUpDownQuality.TabIndex = 4;
            numericUpDownQuality.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Bottom;
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9F);
            label2.Location = new Point(4, 255);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(48, 15);
            label2.TabIndex = 5;
            label2.Text = "Quality:";
            // 
            // update
            // 
            update.Anchor = AnchorStyles.Bottom;
            update.Enabled = false;
            update.Location = new Point(4, 437);
            update.Margin = new Padding(4, 3, 4, 3);
            update.Name = "update";
            update.Size = new Size(58, 27);
            update.TabIndex = 6;
            update.Text = "Update";
            update.UseVisualStyleBackColor = true;
            update.Click += update_Click;
            // 
            // Label_3
            // 
            Label_3.Anchor = AnchorStyles.Top;
            Label_3.AutoSize = true;
            Label_3.Location = new Point(4, 48);
            Label_3.Margin = new Padding(4, 0, 4, 0);
            Label_3.Name = "Label_3";
            Label_3.Size = new Size(94, 15);
            Label_3.TabIndex = 7;
            Label_3.Text = "Number of Files:";
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Top;
            label3.AutoSize = true;
            label3.Location = new Point(4, 63);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(95, 15);
            label3.TabIndex = 8;
            label3.Text = "Completed Files:";
            // 
            // numFiles
            // 
            numFiles.Anchor = AnchorStyles.Top;
            numFiles.AutoSize = true;
            numFiles.Location = new Point(101, 48);
            numFiles.Margin = new Padding(4, 0, 4, 0);
            numFiles.Name = "numFiles";
            numFiles.Size = new Size(13, 15);
            numFiles.TabIndex = 9;
            numFiles.Text = "0";
            // 
            // completedFiles
            // 
            completedFiles.Anchor = AnchorStyles.Top;
            completedFiles.AutoSize = true;
            completedFiles.Location = new Point(101, 63);
            completedFiles.Margin = new Padding(4, 0, 4, 0);
            completedFiles.Name = "completedFiles";
            completedFiles.Size = new Size(13, 15);
            completedFiles.TabIndex = 10;
            completedFiles.Text = "0";
            // 
            // numericUpDownThreads
            // 
            numericUpDownThreads.Anchor = AnchorStyles.Bottom;
            numericUpDownThreads.Location = new Point(78, 309);
            numericUpDownThreads.Margin = new Padding(4, 3, 4, 3);
            numericUpDownThreads.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            numericUpDownThreads.MaximumSize = new Size(59, 0);
            numericUpDownThreads.MinimumSize = new Size(59, 0);
            numericUpDownThreads.Name = "numericUpDownThreads";
            numericUpDownThreads.Size = new Size(59, 23);
            numericUpDownThreads.TabIndex = 11;
            numericUpDownThreads.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label4
            // 
            label4.Anchor = AnchorStyles.Bottom;
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 9F);
            label4.Location = new Point(4, 311);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(49, 15);
            label4.TabIndex = 12;
            label4.Text = "Threads";
            // 
            // cancelButton
            // 
            cancelButton.Anchor = AnchorStyles.Bottom;
            cancelButton.Location = new Point(4, 384);
            cancelButton.Margin = new Padding(4, 3, 4, 3);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(58, 47);
            cancelButton.TabIndex = 13;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += cancelButton_Click;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.Location = new Point(197, 21);
            button1.Margin = new Padding(4, 3, 4, 3);
            button1.Name = "button1";
            button1.Size = new Size(64, 26);
            button1.TabIndex = 15;
            button1.Text = "Open";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            label5.AutoSize = true;
            label5.Font = new Font("Microsoft Sans Serif", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label5.Location = new Point(114, 3);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.MaximumSize = new Size(118, 14);
            label5.MinimumSize = new Size(118, 14);
            label5.Name = "label5";
            label5.Size = new Size(118, 14);
            label5.TabIndex = 15;
            label5.Text = "Double Click to Change";
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.FixedPanel = FixedPanel.Panel2;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Location = new Point(14, 14);
            splitContainer1.Margin = new Padding(4, 3, 4, 3);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(tabControl);
            splitContainer1.Panel1MinSize = 400;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(label10);
            splitContainer1.Panel2.Controls.Add(label8);
            splitContainer1.Panel2.Controls.Add(afterSizeLabel);
            splitContainer1.Panel2.Controls.Add(label11);
            splitContainer1.Panel2.Controls.Add(mbPerSecLabel);
            splitContainer1.Panel2.Controls.Add(percentSavedLabel);
            splitContainer1.Panel2.Controls.Add(elapsedLabel);
            splitContainer1.Panel2.Controls.Add(filesPerSecLabel);
            splitContainer1.Panel2.Controls.Add(beforeSizeLabel);
            splitContainer1.Panel2.Controls.Add(label9);
            splitContainer1.Panel2.Controls.Add(label7);
            splitContainer1.Panel2.Controls.Add(comboBox1);
            splitContainer1.Panel2.Controls.Add(numericUpDownRes);
            splitContainer1.Panel2.Controls.Add(label6);
            splitContainer1.Panel2.Controls.Add(checkBox1);
            splitContainer1.Panel2.Controls.Add(maxResComboBox);
            splitContainer1.Panel2.Controls.Add(labelMaxRes);
            splitContainer1.Panel2.Controls.Add(labelWebpEffort);
            splitContainer1.Panel2.Controls.Add(numericUpDownWebpEffort);
            splitContainer1.Panel2.Controls.Add(optimizedButton);
            splitContainer1.Panel2.Controls.Add(numericUpDownThreads);
            splitContainer1.Panel2.Controls.Add(label4);
            splitContainer1.Panel2.Controls.Add(clearListButton);
            splitContainer1.Panel2.Controls.Add(button1);
            splitContainer1.Panel2.Controls.Add(label5);
            splitContainer1.Panel2.Controls.Add(numericUpDownQuality);
            splitContainer1.Panel2.Controls.Add(label2);
            splitContainer1.Panel2.Controls.Add(cancelButton);
            splitContainer1.Panel2.Controls.Add(textBox1);
            splitContainer1.Panel2.Controls.Add(buttonResize);
            splitContainer1.Panel2.Controls.Add(completedFiles);
            splitContainer1.Panel2.Controls.Add(label1);
            splitContainer1.Panel2.Controls.Add(label3);
            splitContainer1.Panel2.Controls.Add(numFiles);
            splitContainer1.Panel2.Controls.Add(update);
            splitContainer1.Panel2.Controls.Add(Label_3);
            splitContainer1.Panel2MinSize = 100;
            splitContainer1.Size = new Size(836, 493);
            splitContainer1.SplitterDistance = 560;
            splitContainer1.SplitterIncrement = 4;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 16;
            // 
            // label10
            // 
            label10.Anchor = AnchorStyles.Top;
            label10.AutoSize = true;
            label10.Location = new Point(4, 123);
            label10.Name = "label10";
            label10.Size = new Size(77, 15);
            label10.TabIndex = 31;
            label10.Text = "Elapsed Time";
            // 
            // label8
            // 
            label8.Anchor = AnchorStyles.Top;
            label8.AutoSize = true;
            label8.Location = new Point(4, 108);
            label8.Name = "label8";
            label8.Size = new Size(41, 15);
            label8.TabIndex = 30;
            label8.Text = "Saved:";
            // 
            // afterSizeLabel
            // 
            afterSizeLabel.Anchor = AnchorStyles.Top;
            afterSizeLabel.AutoSize = true;
            afterSizeLabel.Location = new Point(101, 93);
            afterSizeLabel.Margin = new Padding(4, 0, 4, 0);
            afterSizeLabel.Name = "afterSizeLabel";
            afterSizeLabel.Size = new Size(13, 15);
            afterSizeLabel.TabIndex = 24;
            afterSizeLabel.Text = "0";
            // 
            // label11
            // 
            label11.Anchor = AnchorStyles.Top;
            label11.AutoSize = true;
            label11.Location = new Point(4, 93);
            label11.Margin = new Padding(4, 0, 4, 0);
            label11.Name = "label11";
            label11.Size = new Size(59, 15);
            label11.TabIndex = 23;
            label11.Text = "After Size:";
            // 
            // mbPerSecLabel
            // 
            mbPerSecLabel.Anchor = AnchorStyles.Top;
            mbPerSecLabel.AutoSize = true;
            mbPerSecLabel.Location = new Point(101, 153);
            mbPerSecLabel.Name = "mbPerSecLabel";
            mbPerSecLabel.Size = new Size(59, 15);
            mbPerSecLabel.TabIndex = 26;
            mbPerSecLabel.Text = "0.00 MB/s";
            // 
            // percentSavedLabel
            // 
            percentSavedLabel.Anchor = AnchorStyles.Top;
            percentSavedLabel.AutoSize = true;
            percentSavedLabel.Location = new Point(101, 108);
            percentSavedLabel.Name = "percentSavedLabel";
            percentSavedLabel.Size = new Size(38, 15);
            percentSavedLabel.TabIndex = 27;
            percentSavedLabel.Text = "0.00%";
            // 
            // elapsedLabel
            // 
            elapsedLabel.Anchor = AnchorStyles.Top;
            elapsedLabel.AutoSize = true;
            elapsedLabel.Location = new Point(101, 123);
            elapsedLabel.Name = "elapsedLabel";
            elapsedLabel.Size = new Size(49, 15);
            elapsedLabel.TabIndex = 28;
            elapsedLabel.Text = "00:00:00";
            // 
            // filesPerSecLabel
            // 
            filesPerSecLabel.Anchor = AnchorStyles.Top;
            filesPerSecLabel.AutoSize = true;
            filesPerSecLabel.Location = new Point(101, 138);
            filesPerSecLabel.Name = "filesPerSecLabel";
            filesPerSecLabel.Size = new Size(62, 15);
            filesPerSecLabel.TabIndex = 29;
            filesPerSecLabel.Text = "0.00 files/s";
            // 
            // beforeSizeLabel
            // 
            beforeSizeLabel.Anchor = AnchorStyles.Top;
            beforeSizeLabel.AutoSize = true;
            beforeSizeLabel.Location = new Point(101, 78);
            beforeSizeLabel.Margin = new Padding(4, 0, 4, 0);
            beforeSizeLabel.Name = "beforeSizeLabel";
            beforeSizeLabel.Size = new Size(13, 15);
            beforeSizeLabel.TabIndex = 22;
            beforeSizeLabel.Text = "0";
            // 
            // label9
            // 
            label9.Anchor = AnchorStyles.Top;
            label9.AutoSize = true;
            label9.Location = new Point(4, 78);
            label9.Margin = new Padding(4, 0, 4, 0);
            label9.Name = "label9";
            label9.Size = new Size(67, 15);
            label9.TabIndex = 21;
            label9.Text = "Before Size:";
            // 
            // label7
            // 
            label7.Anchor = AnchorStyles.Bottom;
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 9F);
            label7.Location = new Point(4, 227);
            label7.Margin = new Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new Size(48, 15);
            label7.TabIndex = 20;
            label7.Text = "Format:";
            // 
            // comboBox1
            // 
            comboBox1.Anchor = AnchorStyles.Bottom;
            comboBox1.AutoCompleteCustomSource.AddRange(new string[] { "jpg", "png", "webp" });
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "jpg", "png", "webp" });
            comboBox1.Location = new Point(78, 224);
            comboBox1.Margin = new Padding(4, 3, 4, 3);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(59, 23);
            comboBox1.TabIndex = 19;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // numericUpDownRes
            // 
            numericUpDownRes.Anchor = AnchorStyles.Bottom;
            numericUpDownRes.Location = new Point(78, 282);
            numericUpDownRes.Margin = new Padding(4, 3, 4, 3);
            numericUpDownRes.MaximumSize = new Size(59, 0);
            numericUpDownRes.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDownRes.MinimumSize = new Size(59, 0);
            numericUpDownRes.Name = "numericUpDownRes";
            numericUpDownRes.Size = new Size(59, 23);
            numericUpDownRes.TabIndex = 17;
            numericUpDownRes.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label6
            // 
            label6.Anchor = AnchorStyles.Bottom;
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI", 9F);
            label6.Location = new Point(4, 284);
            label6.Margin = new Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new Size(66, 15);
            label6.TabIndex = 18;
            label6.Text = "Resolution:";
            // 
            // checkBox1
            // 
            checkBox1.Anchor = AnchorStyles.Bottom;
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(4, 171);
            checkBox1.Margin = new Padding(4, 3, 4, 3);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(173, 19);
            checkBox1.TabIndex = 16;
            checkBox1.Text = "Output Full Folder Structure";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // maxResComboBox
            // 
            maxResComboBox.Anchor = AnchorStyles.Bottom;
            maxResComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            maxResComboBox.FormattingEnabled = true;
            maxResComboBox.Items.AddRange(new object[] { "Off", "4K (2160p)", "1440p", "1080p" });
            maxResComboBox.Location = new Point(78, 337);
            maxResComboBox.Margin = new Padding(4, 3, 4, 3);
            maxResComboBox.Name = "maxResComboBox";
            maxResComboBox.Size = new Size(108, 23);
            maxResComboBox.TabIndex = 26;
            maxResComboBox.SelectedIndexChanged += maxResComboBox_SelectedIndexChanged;
            // 
            // labelMaxRes
            // 
            labelMaxRes.Anchor = AnchorStyles.Bottom;
            labelMaxRes.AutoSize = true;
            labelMaxRes.Font = new Font("Segoe UI", 9F);
            labelMaxRes.Location = new Point(4, 340);
            labelMaxRes.Margin = new Padding(4, 0, 4, 0);
            labelMaxRes.Name = "labelMaxRes";
            labelMaxRes.Size = new Size(53, 15);
            labelMaxRes.TabIndex = 27;
            labelMaxRes.Text = "Max Res:";
            // 
            // labelWebpEffort
            // 
            labelWebpEffort.Anchor = AnchorStyles.Bottom;
            labelWebpEffort.AutoSize = true;
            labelWebpEffort.Font = new Font("Segoe UI", 9F);
            labelWebpEffort.Location = new Point(4, 200);
            labelWebpEffort.Name = "labelWebpEffort";
            labelWebpEffort.Size = new Size(109, 15);
            labelWebpEffort.TabIndex = 34;
            labelWebpEffort.Text = "WebP Comp Effort:";
            // 
            // numericUpDownWebpEffort
            // 
            numericUpDownWebpEffort.Anchor = AnchorStyles.Bottom;
            numericUpDownWebpEffort.Location = new Point(114, 198);
            numericUpDownWebpEffort.Margin = new Padding(4, 3, 4, 3);
            numericUpDownWebpEffort.Maximum = new decimal(new int[] { 6, 0, 0, 0 });
            numericUpDownWebpEffort.MaximumSize = new Size(48, 0);
            numericUpDownWebpEffort.Name = "numericUpDownWebpEffort";
            numericUpDownWebpEffort.Size = new Size(32, 23);
            numericUpDownWebpEffort.TabIndex = 35;
            numericUpDownWebpEffort.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // optimizedButton
            // 
            optimizedButton.Anchor = AnchorStyles.Bottom;
            optimizedButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            optimizedButton.Font = new Font("Arial", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            optimizedButton.Location = new Point(153, 224);
            optimizedButton.Margin = new Padding(4, 3, 4, 3);
            optimizedButton.Name = "optimizedButton";
            optimizedButton.Size = new Size(108, 54);
            optimizedButton.TabIndex = 29;
            optimizedButton.Text = "Optimized Settings";
            optimizedButton.UseVisualStyleBackColor = true;
            optimizedButton.Click += optimizedButton_Click;
            // 
            // processingProgress
            // 
            processingProgress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            processingProgress.Location = new Point(0, 484);
            processingProgress.Name = "processingProgress";
            processingProgress.Size = new Size(860, 37);
            processingProgress.Style = ProgressBarStyle.Continuous;
            processingProgress.TabIndex = 25;
            // 
            // Form1
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(860, 520);
            Controls.Add(processingProgress);
            Controls.Add(splitContainer1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MinimumSize = new Size(876, 559);
            Name = "Form1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            // tabControl doesn't implement ISupportInitialize, no EndInit needed
            tabPageAllFiles.ResumeLayout(false);
            tabPageFailedFiles.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewFailed).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownQuality).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownThreads).EndInit();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numericUpDownRes).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownWebpEffort).EndInit();
            ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button buttonResize;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.NumericUpDown numericUpDownQuality;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button update;
        private System.Windows.Forms.Label Label_3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label numFiles;
        private System.Windows.Forms.Label completedFiles;
        private System.Windows.Forms.NumericUpDown numericUpDownThreads;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.NumericUpDown numericUpDownRes;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox comboBox1;
        private Label afterSizeLabel;
        private Label label11;
        private Label beforeSizeLabel;
        private Label label9;
        private System.Windows.Forms.Label mbPerSecLabel;
        private System.Windows.Forms.Label percentSavedLabel;
        private System.Windows.Forms.Label elapsedLabel;
        private System.Windows.Forms.Label filesPerSecLabel;
        private DataGridViewTextBoxColumn InputFiles;
        private System.Windows.Forms.ComboBox maxResComboBox;
        private System.Windows.Forms.Label labelMaxRes;
        private System.Windows.Forms.Label labelWebpEffort;
        private System.Windows.Forms.NumericUpDown numericUpDownWebpEffort;
        private System.Windows.Forms.Button optimizedButton;
        private Label label10;
        private Label label8;
        private ProgressBar processingProgress;
        private Button clearListButton;
        private TabControl tabControl;
        private TabPage tabPageAllFiles;
        private TabPage tabPageFailedFiles;
        private DataGridView dataGridViewFailed;
        private DataGridViewTextBoxColumn FailedFilePath;
        private DataGridViewTextBoxColumn FailedErrorMessage;
    }
}

