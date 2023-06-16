namespace AWLabAutomation
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
            this.components = new System.ComponentModel.Container();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tvLabs = new System.Windows.Forms.TreeView();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.tbNotLike = new System.Windows.Forms.TextBox();
            this.tbEndDate = new System.Windows.Forms.TextBox();
            this.tbStartDate = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.listBox2 = new System.Windows.Forms.ListBox();
            this.lblStatus100Count = new System.Windows.Forms.Label();
            this.lblStatus5Count = new System.Windows.Forms.Label();
            this.lblStatus4Count = new System.Windows.Forms.Label();
            this.lblStatus3Count = new System.Windows.Forms.Label();
            this.lblStatus2Count = new System.Windows.Forms.Label();
            this.lblStatus1Count = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tvLabs);
            this.groupBox1.Location = new System.Drawing.Point(19, 20);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(732, 622);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Labs";
            // 
            // tvLabs
            // 
            this.tvLabs.Location = new System.Drawing.Point(23, 31);
            this.tvLabs.Margin = new System.Windows.Forms.Padding(4);
            this.tvLabs.Name = "tvLabs";
            this.tvLabs.Size = new System.Drawing.Size(692, 574);
            this.tvLabs.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.listBox1);
            this.groupBox2.Location = new System.Drawing.Point(19, 649);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox2.Size = new System.Drawing.Size(1359, 219);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Activity";
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 16;
            this.listBox1.Location = new System.Drawing.Point(23, 32);
            this.listBox1.Margin = new System.Windows.Forms.Padding(4);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(1327, 164);
            this.listBox1.TabIndex = 0;
            // 
            // timer1
            // 
            this.timer1.Interval = 10000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(41, 891);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(124, 31);
            this.button1.TabIndex = 2;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(211, 889);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(160, 32);
            this.button2.TabIndex = 3;
            this.button2.Text = "button2";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnUpdate);
            this.groupBox3.Controls.Add(this.tbNotLike);
            this.groupBox3.Controls.Add(this.tbEndDate);
            this.groupBox3.Controls.Add(this.tbStartDate);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.label8);
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Controls.Add(this.listBox2);
            this.groupBox3.Controls.Add(this.lblStatus100Count);
            this.groupBox3.Controls.Add(this.lblStatus5Count);
            this.groupBox3.Controls.Add(this.lblStatus4Count);
            this.groupBox3.Controls.Add(this.lblStatus3Count);
            this.groupBox3.Controls.Add(this.lblStatus2Count);
            this.groupBox3.Controls.Add(this.lblStatus1Count);
            this.groupBox3.Controls.Add(this.label6);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.label4);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Location = new System.Drawing.Point(759, 20);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox3.Size = new System.Drawing.Size(619, 622);
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Status";
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(430, 224);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(179, 77);
            this.btnUpdate.TabIndex = 19;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // tbNotLike
            // 
            this.tbNotLike.Location = new System.Drawing.Point(226, 280);
            this.tbNotLike.Name = "tbNotLike";
            this.tbNotLike.Size = new System.Drawing.Size(178, 22);
            this.tbNotLike.TabIndex = 18;
            // 
            // tbEndDate
            // 
            this.tbEndDate.Location = new System.Drawing.Point(226, 252);
            this.tbEndDate.Name = "tbEndDate";
            this.tbEndDate.Size = new System.Drawing.Size(178, 22);
            this.tbEndDate.TabIndex = 17;
            this.tbEndDate.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // tbStartDate
            // 
            this.tbStartDate.Location = new System.Drawing.Point(226, 224);
            this.tbStartDate.Name = "tbStartDate";
            this.tbStartDate.Size = new System.Drawing.Size(178, 22);
            this.tbStartDate.TabIndex = 16;
            // 
            // label9
            // 
            this.label9.Location = new System.Drawing.Point(21, 277);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(207, 28);
            this.label9.TabIndex = 15;
            this.label9.Text = "Not Like Email : ";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(21, 249);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(207, 28);
            this.label8.TabIndex = 14;
            this.label8.Text = "End Date : ";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(21, 221);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(207, 28);
            this.label7.TabIndex = 13;
            this.label7.Text = "Start Date : ";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // listBox2
            // 
            this.listBox2.FormattingEnabled = true;
            this.listBox2.ItemHeight = 16;
            this.listBox2.Location = new System.Drawing.Point(13, 309);
            this.listBox2.Name = "listBox2";
            this.listBox2.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBox2.Size = new System.Drawing.Size(597, 292);
            this.listBox2.TabIndex = 12;
            // 
            // lblStatus100Count
            // 
            this.lblStatus100Count.BackColor = System.Drawing.Color.Yellow;
            this.lblStatus100Count.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblStatus100Count.Location = new System.Drawing.Point(224, 172);
            this.lblStatus100Count.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus100Count.Name = "lblStatus100Count";
            this.lblStatus100Count.Size = new System.Drawing.Size(147, 22);
            this.lblStatus100Count.TabIndex = 11;
            // 
            // lblStatus5Count
            // 
            this.lblStatus5Count.BackColor = System.Drawing.Color.Yellow;
            this.lblStatus5Count.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblStatus5Count.Location = new System.Drawing.Point(224, 144);
            this.lblStatus5Count.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus5Count.Name = "lblStatus5Count";
            this.lblStatus5Count.Size = new System.Drawing.Size(147, 22);
            this.lblStatus5Count.TabIndex = 10;
            // 
            // lblStatus4Count
            // 
            this.lblStatus4Count.BackColor = System.Drawing.Color.Yellow;
            this.lblStatus4Count.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblStatus4Count.Location = new System.Drawing.Point(224, 116);
            this.lblStatus4Count.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus4Count.Name = "lblStatus4Count";
            this.lblStatus4Count.Size = new System.Drawing.Size(147, 22);
            this.lblStatus4Count.TabIndex = 9;
            // 
            // lblStatus3Count
            // 
            this.lblStatus3Count.BackColor = System.Drawing.Color.Yellow;
            this.lblStatus3Count.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblStatus3Count.Location = new System.Drawing.Point(224, 87);
            this.lblStatus3Count.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus3Count.Name = "lblStatus3Count";
            this.lblStatus3Count.Size = new System.Drawing.Size(147, 22);
            this.lblStatus3Count.TabIndex = 8;
            // 
            // lblStatus2Count
            // 
            this.lblStatus2Count.BackColor = System.Drawing.Color.Yellow;
            this.lblStatus2Count.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblStatus2Count.Location = new System.Drawing.Point(224, 59);
            this.lblStatus2Count.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus2Count.Name = "lblStatus2Count";
            this.lblStatus2Count.Size = new System.Drawing.Size(147, 22);
            this.lblStatus2Count.TabIndex = 7;
            // 
            // lblStatus1Count
            // 
            this.lblStatus1Count.BackColor = System.Drawing.Color.Yellow;
            this.lblStatus1Count.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblStatus1Count.Location = new System.Drawing.Point(224, 31);
            this.lblStatus1Count.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus1Count.Name = "lblStatus1Count";
            this.lblStatus1Count.Size = new System.Drawing.Size(147, 22);
            this.lblStatus1Count.TabIndex = 6;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(21, 167);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(207, 28);
            this.label6.TabIndex = 5;
            this.label6.Text = "Status 100 Count : ";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(21, 139);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(207, 28);
            this.label5.TabIndex = 4;
            this.label5.Text = "Status 5 Count : ";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(21, 111);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(207, 28);
            this.label4.TabIndex = 3;
            this.label4.Text = "Status 4 Count : ";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(21, 82);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(207, 28);
            this.label3.TabIndex = 2;
            this.label3.Text = "Status 3 Count : ";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(21, 54);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(207, 28);
            this.label2.TabIndex = 1;
            this.label2.Text = "Status 2 Count : ";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(21, 26);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(207, 28);
            this.label1.TabIndex = 0;
            this.label1.Text = "Status 1 Count : ";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1391, 937);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "AirWatch SE Lab Manager v1.10 03/09/2016 12:38 PM";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TreeView tvLabs;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label lblStatus100Count;
        private System.Windows.Forms.Label lblStatus5Count;
        private System.Windows.Forms.Label lblStatus4Count;
        private System.Windows.Forms.Label lblStatus3Count;
        private System.Windows.Forms.Label lblStatus2Count;
        private System.Windows.Forms.Label lblStatus1Count;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox listBox2;
        private System.Windows.Forms.TextBox tbEndDate;
        private System.Windows.Forms.TextBox tbStartDate;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.TextBox tbNotLike;
    }
}

