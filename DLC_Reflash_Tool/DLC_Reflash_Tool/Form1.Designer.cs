namespace DLC_Reflash_Tool
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.GroupBox_SerialCOM = new System.Windows.Forms.GroupBox();
            this.cbxModeloFonte = new System.Windows.Forms.ComboBox();
            this.btnConectarPortaSerialCOM = new System.Windows.Forms.Button();
            this.cbxPortaSerialCOM = new System.Windows.Forms.ComboBox();
            this.btnRefreshPortaSerialCOM = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.txt_Info = new System.Windows.Forms.TextBox();
            this.btn_Iniciar_Gravação = new System.Windows.Forms.Button();
            this.txt_Caminho_ST_SW = new System.Windows.Forms.TextBox();
            this.btn_Carregar_ST_SW = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_RecarregarArquivosSW = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.TimeoutSerialResponse = new System.Windows.Forms.Timer(this.components);
            this.btnLimparInfo = new System.Windows.Forms.Button();
            this.TimerVoltageAnimation = new System.Windows.Forms.Timer(this.components);
            this.btnOUT1_OFF = new System.Windows.Forms.Button();
            this.btnOUT1_ON = new System.Windows.Forms.Button();
            this.TimerCheckBootMode = new System.Windows.Forms.Timer(this.components);
            this.lblTimer = new System.Windows.Forms.Label();
            this.TimerContador = new System.Windows.Forms.Timer(this.components);
            this.TimerForceBootMode = new System.Windows.Forms.Timer(this.components);
            this.lblEtapa = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.btnCancelarProcesso = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnOUT3_OFF = new System.Windows.Forms.Button();
            this.btnOUT3_ON = new System.Windows.Forms.Button();
            this.btnOUT2_OFF = new System.Windows.Forms.Button();
            this.btnOUT2_ON = new System.Windows.Forms.Button();
            this.GroupBox_SerialCOM.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // GroupBox_SerialCOM
            // 
            this.GroupBox_SerialCOM.Controls.Add(this.cbxModeloFonte);
            this.GroupBox_SerialCOM.Controls.Add(this.btnConectarPortaSerialCOM);
            this.GroupBox_SerialCOM.Controls.Add(this.cbxPortaSerialCOM);
            this.GroupBox_SerialCOM.Controls.Add(this.btnRefreshPortaSerialCOM);
            this.GroupBox_SerialCOM.Controls.Add(this.label6);
            this.GroupBox_SerialCOM.Controls.Add(this.label7);
            this.GroupBox_SerialCOM.Location = new System.Drawing.Point(11, 27);
            this.GroupBox_SerialCOM.Name = "GroupBox_SerialCOM";
            this.GroupBox_SerialCOM.Size = new System.Drawing.Size(97, 224);
            this.GroupBox_SerialCOM.TabIndex = 20;
            this.GroupBox_SerialCOM.TabStop = false;
            this.GroupBox_SerialCOM.Text = "Serial COM";
            // 
            // cbxModeloFonte
            // 
            this.cbxModeloFonte.FormattingEnabled = true;
            this.cbxModeloFonte.Items.AddRange(new object[] {
            "TDK-Lambda",
            "KEITHLEY",
            "MPS",
            "AFR_FA3005P"});
            this.cbxModeloFonte.Location = new System.Drawing.Point(6, 32);
            this.cbxModeloFonte.Name = "cbxModeloFonte";
            this.cbxModeloFonte.Size = new System.Drawing.Size(84, 21);
            this.cbxModeloFonte.TabIndex = 43;
            this.cbxModeloFonte.SelectedIndexChanged += new System.EventHandler(this.cbxModeloFonte_SelectedIndexChanged);
            // 
            // btnConectarPortaSerialCOM
            // 
            this.btnConectarPortaSerialCOM.Location = new System.Drawing.Point(6, 178);
            this.btnConectarPortaSerialCOM.Name = "btnConectarPortaSerialCOM";
            this.btnConectarPortaSerialCOM.Size = new System.Drawing.Size(85, 34);
            this.btnConectarPortaSerialCOM.TabIndex = 18;
            this.btnConectarPortaSerialCOM.Text = "Abrir Porta";
            this.btnConectarPortaSerialCOM.UseVisualStyleBackColor = true;
            this.btnConectarPortaSerialCOM.Click += new System.EventHandler(this.btnConectarPortaSerialCOM_Click);
            // 
            // cbxPortaSerialCOM
            // 
            this.cbxPortaSerialCOM.FormattingEnabled = true;
            this.cbxPortaSerialCOM.Location = new System.Drawing.Point(15, 101);
            this.cbxPortaSerialCOM.Name = "cbxPortaSerialCOM";
            this.cbxPortaSerialCOM.Size = new System.Drawing.Size(72, 21);
            this.cbxPortaSerialCOM.TabIndex = 13;
            // 
            // btnRefreshPortaSerialCOM
            // 
            this.btnRefreshPortaSerialCOM.Location = new System.Drawing.Point(15, 128);
            this.btnRefreshPortaSerialCOM.Name = "btnRefreshPortaSerialCOM";
            this.btnRefreshPortaSerialCOM.Size = new System.Drawing.Size(72, 23);
            this.btnRefreshPortaSerialCOM.TabIndex = 17;
            this.btnRefreshPortaSerialCOM.Text = "Refresh";
            this.btnRefreshPortaSerialCOM.UseVisualStyleBackColor = true;
            this.btnRefreshPortaSerialCOM.Click += new System.EventHandler(this.btnRefreshPortaSerialCOM_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 16);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(61, 13);
            this.label6.TabIndex = 14;
            this.label6.Text = "Mod. Fonte";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 85);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(32, 13);
            this.label7.TabIndex = 14;
            this.label7.Text = "Porta";
            // 
            // txt_Info
            // 
            this.txt_Info.Location = new System.Drawing.Point(11, 302);
            this.txt_Info.Multiline = true;
            this.txt_Info.Name = "txt_Info";
            this.txt_Info.ReadOnly = true;
            this.txt_Info.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txt_Info.Size = new System.Drawing.Size(500, 129);
            this.txt_Info.TabIndex = 21;
            // 
            // btn_Iniciar_Gravação
            // 
            this.btn_Iniciar_Gravação.BackColor = System.Drawing.SystemColors.ButtonShadow;
            this.btn_Iniciar_Gravação.Enabled = false;
            this.btn_Iniciar_Gravação.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Iniciar_Gravação.Location = new System.Drawing.Point(114, 168);
            this.btn_Iniciar_Gravação.Name = "btn_Iniciar_Gravação";
            this.btn_Iniciar_Gravação.Size = new System.Drawing.Size(130, 83);
            this.btn_Iniciar_Gravação.TabIndex = 22;
            this.btn_Iniciar_Gravação.Text = "Iniciar Gravação";
            this.btn_Iniciar_Gravação.UseVisualStyleBackColor = false;
            this.btn_Iniciar_Gravação.Click += new System.EventHandler(this.btn_Iniciar_Gravação_Click);
            // 
            // txt_Caminho_ST_SW
            // 
            this.txt_Caminho_ST_SW.Location = new System.Drawing.Point(9, 33);
            this.txt_Caminho_ST_SW.Name = "txt_Caminho_ST_SW";
            this.txt_Caminho_ST_SW.Size = new System.Drawing.Size(346, 20);
            this.txt_Caminho_ST_SW.TabIndex = 23;
            // 
            // btn_Carregar_ST_SW
            // 
            this.btn_Carregar_ST_SW.Location = new System.Drawing.Point(361, 33);
            this.btn_Carregar_ST_SW.Name = "btn_Carregar_ST_SW";
            this.btn_Carregar_ST_SW.Size = new System.Drawing.Size(26, 23);
            this.btn_Carregar_ST_SW.TabIndex = 24;
            this.btn_Carregar_ST_SW.Text = "...";
            this.btn_Carregar_ST_SW.UseVisualStyleBackColor = true;
            this.btn_Carregar_ST_SW.Click += new System.EventHandler(this.btn_Carregar_ST_SW_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(221, 13);
            this.label1.TabIndex = 25;
            this.label1.Text = "Caminho para D4X_STxxx_SWyyyy_All (.hex)";
            // 
            // btn_RecarregarArquivosSW
            // 
            this.btn_RecarregarArquivosSW.Location = new System.Drawing.Point(312, 62);
            this.btn_RecarregarArquivosSW.Name = "btn_RecarregarArquivosSW";
            this.btn_RecarregarArquivosSW.Size = new System.Drawing.Size(75, 23);
            this.btn_RecarregarArquivosSW.TabIndex = 29;
            this.btn_RecarregarArquivosSW.Text = "Recarregar";
            this.btn_RecarregarArquivosSW.UseVisualStyleBackColor = true;
            this.btn_RecarregarArquivosSW.Click += new System.EventHandler(this.btn_RecarregarArquivosSW_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btn_RecarregarArquivosSW);
            this.groupBox1.Controls.Add(this.btn_Carregar_ST_SW);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txt_Caminho_ST_SW);
            this.groupBox1.Location = new System.Drawing.Point(114, 27);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(397, 98);
            this.groupBox1.TabIndex = 30;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Arquivo de Software Atualizado";
            // 
            // TimeoutSerialResponse
            // 
            this.TimeoutSerialResponse.Interval = 3000;
            this.TimeoutSerialResponse.Tick += new System.EventHandler(this.TimeoutSerialResponse_Tick);
            // 
            // btnLimparInfo
            // 
            this.btnLimparInfo.Location = new System.Drawing.Point(445, 437);
            this.btnLimparInfo.Name = "btnLimparInfo";
            this.btnLimparInfo.Size = new System.Drawing.Size(75, 23);
            this.btnLimparInfo.TabIndex = 31;
            this.btnLimparInfo.Text = "Limpar";
            this.btnLimparInfo.UseVisualStyleBackColor = true;
            this.btnLimparInfo.Click += new System.EventHandler(this.btnLimparInfo_Click);
            // 
            // TimerVoltageAnimation
            // 
            this.TimerVoltageAnimation.Tick += new System.EventHandler(this.TimerVoltageAnimation_Tick);
            // 
            // btnOUT1_OFF
            // 
            this.btnOUT1_OFF.Enabled = false;
            this.btnOUT1_OFF.Location = new System.Drawing.Point(84, 19);
            this.btnOUT1_OFF.Name = "btnOUT1_OFF";
            this.btnOUT1_OFF.Size = new System.Drawing.Size(75, 23);
            this.btnOUT1_OFF.TabIndex = 35;
            this.btnOUT1_OFF.Text = "OUT1 OFF";
            this.btnOUT1_OFF.UseVisualStyleBackColor = true;
            this.btnOUT1_OFF.Click += new System.EventHandler(this.btnOUT_OFF_Click);
            // 
            // btnOUT1_ON
            // 
            this.btnOUT1_ON.Enabled = false;
            this.btnOUT1_ON.Location = new System.Drawing.Point(6, 19);
            this.btnOUT1_ON.Name = "btnOUT1_ON";
            this.btnOUT1_ON.Size = new System.Drawing.Size(75, 23);
            this.btnOUT1_ON.TabIndex = 35;
            this.btnOUT1_ON.Text = "OUT1 ON";
            this.btnOUT1_ON.UseVisualStyleBackColor = true;
            this.btnOUT1_ON.Click += new System.EventHandler(this.btnOUT_ON_Click);
            // 
            // TimerCheckBootMode
            // 
            this.TimerCheckBootMode.Interval = 500;
            this.TimerCheckBootMode.Tick += new System.EventHandler(this.TimerCheckBootMode_Tick);
            // 
            // lblTimer
            // 
            this.lblTimer.AutoSize = true;
            this.lblTimer.Location = new System.Drawing.Point(438, 284);
            this.lblTimer.Name = "lblTimer";
            this.lblTimer.Size = new System.Drawing.Size(73, 13);
            this.lblTimer.TabIndex = 37;
            this.lblTimer.Text = "Tempo: 00:00";
            // 
            // TimerContador
            // 
            this.TimerContador.Interval = 1000;
            this.TimerContador.Tick += new System.EventHandler(this.TimerContador_Tick);
            // 
            // TimerForceBootMode
            // 
            this.TimerForceBootMode.Interval = 10000;
            this.TimerForceBootMode.Tick += new System.EventHandler(this.TimerForceBootMode_Tick);
            // 
            // lblEtapa
            // 
            this.lblEtapa.AutoSize = true;
            this.lblEtapa.Location = new System.Drawing.Point(14, 284);
            this.lblEtapa.Name = "lblEtapa";
            this.lblEtapa.Size = new System.Drawing.Size(41, 13);
            this.lblEtapa.TabIndex = 40;
            this.lblEtapa.Text = "Etapa: ";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.Red;
            this.label4.Location = new System.Drawing.Point(12, 450);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(427, 13);
            this.label4.TabIndex = 41;
            this.label4.Text = "Uma janela do CMD será aberta automaticamente. Ao final do processo, ela também s" +
    "erá";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.Color.Red;
            this.label5.Location = new System.Drawing.Point(12, 466);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(474, 13);
            this.label5.TabIndex = 41;
            this.label5.Text = "fechada automaticamente. Caso o fechamento não ocorra após o processo, feche-a ma" +
    "nualmente.";
            // 
            // btnCancelarProcesso
            // 
            this.btnCancelarProcesso.Location = new System.Drawing.Point(142, 257);
            this.btnCancelarProcesso.Name = "btnCancelarProcesso";
            this.btnCancelarProcesso.Size = new System.Drawing.Size(75, 24);
            this.btnCancelarProcesso.TabIndex = 42;
            this.btnCancelarProcesso.Text = "Cancelar";
            this.btnCancelarProcesso.UseVisualStyleBackColor = true;
            this.btnCancelarProcesso.Click += new System.EventHandler(this.btnCancelarProcesso_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnOUT3_OFF);
            this.groupBox3.Controls.Add(this.btnOUT3_ON);
            this.groupBox3.Controls.Add(this.btnOUT2_OFF);
            this.groupBox3.Controls.Add(this.btnOUT2_ON);
            this.groupBox3.Controls.Add(this.btnOUT1_OFF);
            this.groupBox3.Controls.Add(this.btnOUT1_ON);
            this.groupBox3.Location = new System.Drawing.Point(250, 168);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(165, 113);
            this.groupBox3.TabIndex = 43;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Controle Manual";
            // 
            // btnOUT3_OFF
            // 
            this.btnOUT3_OFF.Enabled = false;
            this.btnOUT3_OFF.Location = new System.Drawing.Point(84, 77);
            this.btnOUT3_OFF.Name = "btnOUT3_OFF";
            this.btnOUT3_OFF.Size = new System.Drawing.Size(75, 23);
            this.btnOUT3_OFF.TabIndex = 35;
            this.btnOUT3_OFF.Text = "OUT3 OFF";
            this.btnOUT3_OFF.UseVisualStyleBackColor = true;
            this.btnOUT3_OFF.Click += new System.EventHandler(this.btnOUT3_OFF_Click);
            // 
            // btnOUT3_ON
            // 
            this.btnOUT3_ON.Enabled = false;
            this.btnOUT3_ON.Location = new System.Drawing.Point(6, 77);
            this.btnOUT3_ON.Name = "btnOUT3_ON";
            this.btnOUT3_ON.Size = new System.Drawing.Size(75, 23);
            this.btnOUT3_ON.TabIndex = 35;
            this.btnOUT3_ON.Text = "OUT3 ON";
            this.btnOUT3_ON.UseVisualStyleBackColor = true;
            this.btnOUT3_ON.Click += new System.EventHandler(this.btnOUT3_ON_Click);
            // 
            // btnOUT2_OFF
            // 
            this.btnOUT2_OFF.Enabled = false;
            this.btnOUT2_OFF.Location = new System.Drawing.Point(84, 48);
            this.btnOUT2_OFF.Name = "btnOUT2_OFF";
            this.btnOUT2_OFF.Size = new System.Drawing.Size(75, 23);
            this.btnOUT2_OFF.TabIndex = 35;
            this.btnOUT2_OFF.Text = "OUT2 OFF";
            this.btnOUT2_OFF.UseVisualStyleBackColor = true;
            this.btnOUT2_OFF.Click += new System.EventHandler(this.btnOUT2_OFF_Click);
            // 
            // btnOUT2_ON
            // 
            this.btnOUT2_ON.Enabled = false;
            this.btnOUT2_ON.Location = new System.Drawing.Point(6, 48);
            this.btnOUT2_ON.Name = "btnOUT2_ON";
            this.btnOUT2_ON.Size = new System.Drawing.Size(75, 23);
            this.btnOUT2_ON.TabIndex = 35;
            this.btnOUT2_ON.Text = "OUT2 ON";
            this.btnOUT2_ON.UseVisualStyleBackColor = true;
            this.btnOUT2_ON.Click += new System.EventHandler(this.btnOUT2_ON_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(523, 488);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.btnCancelarProcesso);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lblEtapa);
            this.Controls.Add(this.lblTimer);
            this.Controls.Add(this.btnLimparInfo);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btn_Iniciar_Gravação);
            this.Controls.Add(this.txt_Info);
            this.Controls.Add(this.GroupBox_SerialCOM);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DLC Reflash Tool";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.GroupBox_SerialCOM.ResumeLayout(false);
            this.GroupBox_SerialCOM.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox GroupBox_SerialCOM;
        private System.Windows.Forms.Button btnConectarPortaSerialCOM;
        private System.Windows.Forms.ComboBox cbxPortaSerialCOM;
        private System.Windows.Forms.Button btnRefreshPortaSerialCOM;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txt_Info;
        private System.Windows.Forms.Button btn_Iniciar_Gravação;
        private System.Windows.Forms.TextBox txt_Caminho_ST_SW;
        private System.Windows.Forms.Button btn_Carregar_ST_SW;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn_RecarregarArquivosSW;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Timer TimeoutSerialResponse;
        private System.Windows.Forms.Button btnLimparInfo;
        private System.Windows.Forms.Timer TimerVoltageAnimation;
        private System.Windows.Forms.Button btnOUT1_OFF;
        private System.Windows.Forms.Button btnOUT1_ON;
        private System.Windows.Forms.Timer TimerCheckBootMode;
        private System.Windows.Forms.Label lblTimer;
        private System.Windows.Forms.Timer TimerContador;
        private System.Windows.Forms.Timer TimerForceBootMode;
        private System.Windows.Forms.Label lblEtapa;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnCancelarProcesso;
        private System.Windows.Forms.ComboBox cbxModeloFonte;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button btnOUT3_OFF;
        private System.Windows.Forms.Button btnOUT3_ON;
        private System.Windows.Forms.Button btnOUT2_OFF;
        private System.Windows.Forms.Button btnOUT2_ON;
    }
}

