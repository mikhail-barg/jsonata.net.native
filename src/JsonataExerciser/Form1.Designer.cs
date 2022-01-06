
namespace JsonataExerciser
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.DatasetFctb = new FastColoredTextBoxNS.FastColoredTextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.formatDatasetJsonButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.sampleComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.BindingsFctb = new FastColoredTextBoxNS.FastColoredTextBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.QueryFctb = new FastColoredTextBoxNS.FastColoredTextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.ResultFctb = new FastColoredTextBoxNS.FastColoredTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.DatasetFctb)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BindingsFctb)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.QueryFctb)).BeginInit();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ResultFctb)).BeginInit();
            this.SuspendLayout();
            // 
            // DatasetFctb
            // 
            this.DatasetFctb.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.DatasetFctb.AutoIndentCharsPatterns = "\r\n^\\s*[\\w\\.]+(\\s\\w+)?\\s*(?<range>=)\\s*(?<range>[^;]+);\r\n";
            this.DatasetFctb.AutoScrollMinSize = new System.Drawing.Size(43, 14);
            this.DatasetFctb.BackBrush = null;
            this.DatasetFctb.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy2;
            this.DatasetFctb.CharHeight = 14;
            this.DatasetFctb.CharWidth = 8;
            this.DatasetFctb.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.DatasetFctb.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.DatasetFctb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DatasetFctb.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.DatasetFctb.IsReplaceMode = false;
            this.DatasetFctb.Language = FastColoredTextBoxNS.Language.JS;
            this.DatasetFctb.LeftBracket = '(';
            this.DatasetFctb.LeftBracket2 = '{';
            this.DatasetFctb.Location = new System.Drawing.Point(3, 44);
            this.DatasetFctb.Name = "DatasetFctb";
            this.DatasetFctb.Paddings = new System.Windows.Forms.Padding(0);
            this.DatasetFctb.RightBracket = ')';
            this.DatasetFctb.RightBracket2 = '}';
            this.DatasetFctb.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.DatasetFctb.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("DatasetFctb.ServiceColors")));
            this.DatasetFctb.Size = new System.Drawing.Size(339, 230);
            this.DatasetFctb.TabIndex = 0;
            this.DatasetFctb.Text = "{}";
            this.DatasetFctb.Zoom = 100;
            this.DatasetFctb.TextChangedDelayed += new System.EventHandler<FastColoredTextBoxNS.TextChangedEventArgs>(this.DatasetFctb_TextChangedDelayed);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Cursor = System.Windows.Forms.Cursors.VSplit;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer3);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(800, 450);
            this.splitContainer1.SplitterDistance = 345;
            this.splitContainer1.TabIndex = 1;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.groupBox2);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer3.Size = new System.Drawing.Size(345, 450);
            this.splitContainer3.SplitterDistance = 277;
            this.splitContainer3.TabIndex = 4;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.DatasetFctb);
            this.groupBox2.Controls.Add(this.toolStrip1);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Location = new System.Drawing.Point(0, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(345, 277);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Data";
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.formatDatasetJsonButton,
            this.toolStripSeparator1,
            this.sampleComboBox,
            this.toolStripLabel1});
            this.toolStrip1.Location = new System.Drawing.Point(3, 19);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(339, 25);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // formatDatasetJsonButton
            // 
            this.formatDatasetJsonButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.formatDatasetJsonButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.formatDatasetJsonButton.Image = ((System.Drawing.Image)(resources.GetObject("formatDatasetJsonButton.Image")));
            this.formatDatasetJsonButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.formatDatasetJsonButton.Name = "formatDatasetJsonButton";
            this.formatDatasetJsonButton.Size = new System.Drawing.Size(23, 22);
            this.formatDatasetJsonButton.ToolTipText = "Format JSON";
            this.formatDatasetJsonButton.Click += new System.EventHandler(this.formatDatasetJsonButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // sampleComboBox
            // 
            this.sampleComboBox.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.sampleComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.sampleComboBox.FlatStyle = System.Windows.Forms.FlatStyle.Standard;
            this.sampleComboBox.Name = "sampleComboBox";
            this.sampleComboBox.Size = new System.Drawing.Size(121, 25);
            this.sampleComboBox.SelectedIndexChanged += new System.EventHandler(this.sampleComboBox_SelectedIndexChanged);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(49, 22);
            this.toolStripLabel1.Text = "Sample:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.BindingsFctb);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(345, 169);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Bindings";
            // 
            // BindingsFctb
            // 
            this.BindingsFctb.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.BindingsFctb.AutoIndentCharsPatterns = "\r\n^\\s*[\\w\\.]+(\\s\\w+)?\\s*(?<range>=)\\s*(?<range>[^;]+);\r\n";
            this.BindingsFctb.AutoScrollMinSize = new System.Drawing.Size(43, 14);
            this.BindingsFctb.BackBrush = null;
            this.BindingsFctb.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy2;
            this.BindingsFctb.CharHeight = 14;
            this.BindingsFctb.CharWidth = 8;
            this.BindingsFctb.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.BindingsFctb.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.BindingsFctb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.BindingsFctb.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.BindingsFctb.IsReplaceMode = false;
            this.BindingsFctb.Language = FastColoredTextBoxNS.Language.JS;
            this.BindingsFctb.LeftBracket = '(';
            this.BindingsFctb.LeftBracket2 = '{';
            this.BindingsFctb.Location = new System.Drawing.Point(3, 19);
            this.BindingsFctb.Name = "BindingsFctb";
            this.BindingsFctb.Paddings = new System.Windows.Forms.Padding(0);
            this.BindingsFctb.RightBracket = ')';
            this.BindingsFctb.RightBracket2 = '}';
            this.BindingsFctb.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.BindingsFctb.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("BindingsFctb.ServiceColors")));
            this.BindingsFctb.Size = new System.Drawing.Size(339, 147);
            this.BindingsFctb.TabIndex = 1;
            this.BindingsFctb.Text = "{}";
            this.BindingsFctb.Zoom = 100;
            this.BindingsFctb.TextChangedDelayed += new System.EventHandler<FastColoredTextBoxNS.TextChangedEventArgs>(this.BindingsFctb_TextChangedDelayed);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.groupBox3);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.groupBox4);
            this.splitContainer2.Size = new System.Drawing.Size(451, 450);
            this.splitContainer2.SplitterDistance = 195;
            this.splitContainer2.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.QueryFctb);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox3.Location = new System.Drawing.Point(0, 0);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(451, 195);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Query";
            // 
            // QueryFctb
            // 
            this.QueryFctb.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.QueryFctb.AutoIndentCharsPatterns = "\r\n^\\s*[\\w\\.]+(\\s\\w+)?\\s*(?<range>=)\\s*(?<range>[^;]+);\r\n";
            this.QueryFctb.AutoScrollMinSize = new System.Drawing.Size(35, 14);
            this.QueryFctb.BackBrush = null;
            this.QueryFctb.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy2;
            this.QueryFctb.CharHeight = 14;
            this.QueryFctb.CharWidth = 8;
            this.QueryFctb.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.QueryFctb.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.QueryFctb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.QueryFctb.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.QueryFctb.IsReplaceMode = false;
            this.QueryFctb.Language = FastColoredTextBoxNS.Language.JS;
            this.QueryFctb.LeftBracket = '(';
            this.QueryFctb.LeftBracket2 = '{';
            this.QueryFctb.Location = new System.Drawing.Point(3, 19);
            this.QueryFctb.Name = "QueryFctb";
            this.QueryFctb.Paddings = new System.Windows.Forms.Padding(0);
            this.QueryFctb.RightBracket = ')';
            this.QueryFctb.RightBracket2 = '}';
            this.QueryFctb.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.QueryFctb.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("QueryFctb.ServiceColors")));
            this.QueryFctb.Size = new System.Drawing.Size(445, 173);
            this.QueryFctb.TabIndex = 1;
            this.QueryFctb.Text = "$";
            this.QueryFctb.Zoom = 100;
            this.QueryFctb.TextChangedDelayed += new System.EventHandler<FastColoredTextBoxNS.TextChangedEventArgs>(this.QueryFctb_TextChangedDelayed);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.ResultFctb);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox4.Location = new System.Drawing.Point(0, 0);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(451, 251);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Result";
            // 
            // ResultFctb
            // 
            this.ResultFctb.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.ResultFctb.AutoIndentCharsPatterns = "\r\n^\\s*[\\w\\.]+(\\s\\w+)?\\s*(?<range>=)\\s*(?<range>[^;]+);\r\n";
            this.ResultFctb.AutoScrollMinSize = new System.Drawing.Size(27, 14);
            this.ResultFctb.BackBrush = null;
            this.ResultFctb.BracketsHighlightStrategy = FastColoredTextBoxNS.BracketsHighlightStrategy.Strategy2;
            this.ResultFctb.CharHeight = 14;
            this.ResultFctb.CharWidth = 8;
            this.ResultFctb.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.ResultFctb.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.ResultFctb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ResultFctb.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.ResultFctb.IsReplaceMode = false;
            this.ResultFctb.Language = FastColoredTextBoxNS.Language.JS;
            this.ResultFctb.LeftBracket = '(';
            this.ResultFctb.LeftBracket2 = '{';
            this.ResultFctb.Location = new System.Drawing.Point(3, 19);
            this.ResultFctb.Name = "ResultFctb";
            this.ResultFctb.Paddings = new System.Windows.Forms.Padding(0);
            this.ResultFctb.ReadOnly = true;
            this.ResultFctb.RightBracket = ')';
            this.ResultFctb.RightBracket2 = '}';
            this.ResultFctb.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.ResultFctb.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("ResultFctb.ServiceColors")));
            this.ResultFctb.Size = new System.Drawing.Size(445, 229);
            this.ResultFctb.TabIndex = 2;
            this.ResultFctb.Zoom = 100;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "JSONata Exerciser";
            ((System.ComponentModel.ISupportInitialize)(this.DatasetFctb)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.BindingsFctb)).EndInit();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.QueryFctb)).EndInit();
            this.groupBox4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ResultFctb)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private FastColoredTextBoxNS.FastColoredTextBox DatasetFctb;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private FastColoredTextBoxNS.FastColoredTextBox QueryFctb;
        private FastColoredTextBoxNS.FastColoredTextBox ResultFctb;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton formatDatasetJsonButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripComboBox sampleComboBox;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private FastColoredTextBoxNS.FastColoredTextBox BindingsFctb;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
    }
}
