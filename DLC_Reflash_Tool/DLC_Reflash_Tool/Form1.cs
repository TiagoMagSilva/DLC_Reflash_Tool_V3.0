using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using static System.Net.Mime.MediaTypeNames;

/* 
 * POS  -  14V(430mA); 15V(410mA); 19.8V(320mA) -> Entra em boot normalmente! Corrente cai para ~55mA
 * DRL  -  14V(1.91A); 15V(1.79A); 19.8V(1.28A)
 * TURN -  14V(1.44A); 15V(1.35A); 19.8V(1.07A) 
 * 
 * DLC leva 6 segundos para entrar em boot após aplicados os 14.00V;
 * Após entrar em boot, a DLC permanece em modo boot por 10 segundos;
 * 
 * Arquivos obrigatórios: ALFlasherAll.exe; msvcp140d.dll; ucrtbased.dll; vcruntime140d.dll
 */

namespace DLC_Reflash_Tool
{
    public partial class MainForm : Form
    {
        String path_SERIAL = System.AppDomain.CurrentDomain.BaseDirectory + "/SERIAL/";
        String path_LOG = System.AppDomain.CurrentDomain.BaseDirectory + "/LOG/";
        String path_CONFIG = System.AppDomain.CurrentDomain.BaseDirectory + "/CONFIG/";

        private CancellationTokenSource beepCancellationToken;

        private SerialPort PortaSerialCOM = new SerialPort();

        UInt16 RequestIndex = 0, aux_count = 0, TempoDecorrido = 0, QT_Timeout = 0;

        double[,] VinProfile = new double[5, 2]; //Tensão(V) ; Tempo(ms)
        UInt16 VinProfileIndex = 0;
        int ProcessoID_Channel0 = 0, ProcessoID_Channel1 = 0;

        Boolean GravaçãoON = false, DLC_Modo_Boot = false, ConexãoAutomática = false, FonteConectada = false;
        Boolean ForçarBootMode = false, DLC_WRITING_PROCESS = false;

        Double output_Current = 0.0;

        UInt16 IndexPortaSerialConexaoAutomatica = 0;
        
        enum portaSerialCOM_Request
        {
            IDNRequest = 1, //Identificação da fonte
            REVRequest,     //Revisão de firmware da fonte
            SNRequest,      //Número de série da fonte
            DATERequest,    //Data da última calibração da fonte
            
            AddressDefinition,  //Definição de endereço para comunicação
            CLSCommand,         //Comando para limpar FEVE e SEVE da fonte
            RSTCommand,         //Comando para resetar os registros da fonte
            RMTCommand,         //Comando para definir o modo de operação da fonte (Remote ou Local)
            RMTRequest,         //Comando para solicitar o modo de operação atual da fonte

            PVCommand,     //Comando para definir a tensão de saída da fonte
            PVRequest,     //Comando para solicitar a tensão de saída para qual a fonte está configurada
            MVRequest,     //Comando para retornar a tensão de saída medida pela fonte
            PCCommand,     //Comando para definir a corrente de saída da fonte
            PCRequest,     //Comando para solicitar a corrente de saída para qual a fonte está configurada
            MCRequest,     //Comando para retornar a corrente de saída medida pela fonte
            DVCRequest,    //Retorna a tensão e corrente da fonte em um único comando

            OUTCommand,    //Comando para ligar ou desligar a saída da fonte
            OUTRequest,    //Comando para solicitar o estado da saída da fonte (ligada ou desligada)
            FLDCommand,    //Comando para definir o estado do foldback protection da fonte
            FLDRequest,    //Comando para solicitar o estado do foldback protection da fonte (ativado ou desativado)    
            FBDCommand,    //Comando para adicionar nn*0.1 seconds de delay após a detecção de uma condição de foldback
            FBDRequest,    //Comando para solicitar o valor do foldback delay configurado na fonte
            FBDRSTCommand, //Comando para resetar foldback delay da fonte
            OVPCommand,    //Comando para definir o valor do OVP da fonte
            OPVRequest,    //Comando para solicitar o valor do OVP configurado na fonte
            OVMCommand,    //Comando para definir o o OVP para o máximo valor possível
            UVRequest,     //Comando para solicitar o modo Under Voltage da fonte UVP ou UVL
            UVLCommand,    //Comando para definir o valor do Under Voltage da fonte
            UVLRequest,    //Comando para solicitar o valor do Under Voltage configurado na fonte
            UVPCommand,    //Comando para definir o valor do Under Voltage Protection da fonte
            UVPRequest,    //Comando para solicitar o valor do Under Voltage Protection configurado na fonte
            ASTCommand,    //Comando para definir o estado do Auto Restart da fonte
            ASTRequest,    //Comando para solicitar o estado do Auto Restart da fonte (ativado ou desativado)
            SAVCommand,    //Comando para salvar as configurações atuais da fonte na memória não volátil
            RCLCommand,    //Comando para recarregar as configurações salvas na memória não volátil da fonte
            MODERequest,    //Comando para solicitar o modo de operação atual da fonte (CV ou CC)
            PMSCommand,     //Comando para definir Master/Slave em modo de operação paralela ou série
            PMSRequest,     //Comando para solicitar o estado de Master/Slave em modo de operação paralela ou série
            
            MPS_FUNC2Command,   //Comando para definir o estado da saída 2 da fonte Marelli
            MPS_FUNC3Command,   //Comando para definir o estado da saída 3 da fonte Marelli       
        };

        // Helpers to find a window for a given process and bring it to foreground
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MINIMIZE = 6;

        IntPtr FindWindowForProcess(int pid)
        {
            IntPtr found = IntPtr.Zero;
            try
            {
                EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        if (!IsWindowVisible(hWnd)) return true;
                        uint windowPid;
                        GetWindowThreadProcessId(hWnd, out windowPid);
                        if ((int)windowPid == pid)
                        {
                            found = hWnd;
                            return false; // stop enumeration
                        }
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
            return found;
        }

        void SendKeysToProcessWindow(Process proc, string keys)
        {
            try
            {
                IntPtr h = proc.MainWindowHandle;
                if (h == IntPtr.Zero)
                {
                    h = FindWindowForProcess(proc.Id);
                }

                if (h != IntPtr.Zero)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        try { SetForegroundWindow(h); } catch { }
                        try { SendKeys.SendWait(keys); } catch { }
                    }));
                }
            }
            catch { }
        }

        public MainForm()
        {
            InitializeComponent();

            // Habilita captura de teclas do Form
            this.KeyPreview = true;

            // Associa o evento (para teclas que não são de navegação)
            this.KeyDown += MainForm_KeyDown;

            this.ActiveControl = btnConectarPortaSerialCOM;
        }

        // Teclas de seta são interceptadas pelo Windows Forms antes do KeyDown.
        // ProcessCmdKey captura essas teclas em um nível mais baixo.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Right = Iniciar Gravação
            if (keyData == Keys.Right && btn_Iniciar_Gravação.Enabled)
            {
                Console.WriteLine("Right arrow pressed - Starting recording...");
                btn_Iniciar_Gravação.PerformClick();
                return true;
            }

            // Up = Ligar Saída
            if (keyData == Keys.Up && btnOUT1_ON.Enabled)
            {
                Console.WriteLine("Up arrow pressed - Turning output ON...");
                btnOUT1_ON.PerformClick();
                return true;
            }

            // Down = Desligar Saída
            if (keyData == Keys.Down && btnOUT1_OFF.Enabled)
            {
                Console.WriteLine("Down arrow pressed - Turning output OFF...");
                btnOUT1_OFF.PerformClick();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+L = Limpar Info
            if (e.KeyCode == Keys.L)
            {
                Console.WriteLine("L pressed - Clearing info...");
                btnLimparInfo.PerformClick();
                e.Handled = true;
            }

            if(e.KeyCode == Keys.A)
            {
                Console.WriteLine("A pressed - opening COM port...");
                btnConectarPortaSerialCOM.PerformClick();
                e.Handled = true;
            }

            // Esc = Cancelar Processo
            if (e.KeyCode == Keys.Escape)
            {
                Console.WriteLine("Escape pressed - Canceling process...");
                btnCancelarProcesso.PerformClick();
                e.Handled = true;
            }
        }

        void CheckUP_Inicial()
        {
            AtualizarPortasSeriais();
            LerInformacoesPortaSerialSalva();
            LerInformacoesSoftwareSalvas();            
                      
            
            cbxPortaSerialCOM.Enabled = true;
            btnRefreshPortaSerialCOM.Enabled = true;

            this.ActiveControl = btnConectarPortaSerialCOM;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            CheckUP_Inicial();
        }

        void AtualizarPortasSeriais()
        {
            string[] NomesDasPortas = SerialPort.GetPortNames();

            cbxPortaSerialCOM.Items.Clear();

            cbxPortaSerialCOM.Text = string.Empty;

            foreach (string porta in NomesDasPortas)
            {
                cbxPortaSerialCOM.Items.Add(porta);
            }

            if (cbxPortaSerialCOM.Items.Count > 0)
            {
                cbxPortaSerialCOM.SelectedIndex = 0;
            }                   
        }

        void LerInformacoesPortaSerialSalva()
        {
            if (File.Exists(path_SERIAL + "SERIAL" + ".txt"))
            {
                if (File.ReadLines(path_SERIAL + "SERIAL" + ".txt").Count() > 0)
                {
                    string[] LinhasDoTXT;
                    LinhasDoTXT = File.ReadAllLines(path_SERIAL + "SERIAL" + ".txt");

                    foreach (string line in LinhasDoTXT)
                    {
                        string[] partes = line.Split(';');
                        if (partes.Length == 6)
                        {
                            for (int i = 0; i < cbxPortaSerialCOM.Items.Count; i++)
                            {
                                string NomePorta = cbxPortaSerialCOM.GetItemText(cbxPortaSerialCOM.Items[i]);

                                if (partes[0] == NomePorta)
                                {
                                    cbxPortaSerialCOM.SelectedIndex = i;
                                    break;
                                }
                            }   

                            for (int i = 0; i < cbxModeloFonte.Items.Count; i++)
                            {
                                string modelo = cbxModeloFonte.GetItemText(cbxModeloFonte.Items[i]);

                                if (partes[4] == modelo)
                                {
                                    cbxModeloFonte.SelectedIndex = i;
                                    break;
                                }
                            }

                            cbkGravaçãoDupla.Checked = Boolean.Parse(partes[5]);
                        }
                    }
                }
            }
        }

        void LerInformacoesSoftwareSalvas()
        {
            /*
            if (File.Exists(path_CONFIG + "D4X_DL_SWSB" + ".txt"))
            {
                if (File.ReadLines(path_CONFIG + "D4X_DL_SWSB" + ".txt").Count() > 0)
                {
                    string[] LinhasDoTXT;
                    LinhasDoTXT = File.ReadAllLines(path_CONFIG + "D4X_DL_SWSB" + ".txt");

                    //Com isto, apenas a última linha será considerada!
                    foreach (string line in LinhasDoTXT)
                    {
                        txt_Caminho_DL_SWSB.Text = line;
                    }
                }
            }*/

            if (File.Exists(path_CONFIG + "D4X_ST_SW" + ".txt"))
            {
                if (File.ReadLines(path_CONFIG + "D4X_ST_SW" + ".txt").Count() > 0)
                {
                    string[] LinhasDoTXT;
                    LinhasDoTXT = File.ReadAllLines(path_CONFIG + "D4X_ST_SW" + ".txt");

                    //Com isto, apenas a última linha será considerada!
                    foreach (string line in LinhasDoTXT)
                    {
                        //txt_Caminho_ST_SW.Text = line;
                        lblSWName.Text = line;
                    }
                }
            }
        }

        void Salvar_Dados_Serial()
        {
            try
            {
                if (File.Exists(path_SERIAL + "SERIAL" + ".txt"))
                {
                    File.WriteAllText(path_SERIAL + "SERIAL" + ".txt", String.Empty);

                    using (var tw = new StreamWriter(path_SERIAL + "SERIAL" + ".txt", true))
                    {
                        tw.WriteLine(cbxPortaSerialCOM.SelectedItem.ToString() + ";" + 9600 + ";" + 6 + ";" + "false" + ";" + cbxModeloFonte.SelectedItem.ToString() + ";" + cbkGravaçãoDupla.Checked);
                    }
                }
                else
                {
                    if (!File.Exists(path_SERIAL + "SERIAL" + ".txt"))
                    {
                        File.Create(path_SERIAL + "SERIAL" + ".txt").Dispose();

                        File.WriteAllText(path_SERIAL + "SERIAL" + ".txt", String.Empty);

                        using (TextWriter tw = new StreamWriter(path_SERIAL + "SERIAL" + ".txt"))
                        {
                            tw.WriteLine(cbxPortaSerialCOM.SelectedItem.ToString() + ";" + 9600 + ";" + 6 + ";" + "false" + ";" + cbxModeloFonte.SelectedItem.ToString() + ";" + cbkGravaçãoDupla.Checked);
                        }
                    }
                }
                LOG_TXT("Salvando informações da porta serial - Porta: " + cbxPortaSerialCOM.SelectedItem.ToString() + " - Velocidade: " + 9600 + " - Endereço: " + 6 + " Modelo Fonte: " + cbxModeloFonte.SelectedItem.ToString() + " Gravação Dupla: " + cbkGravaçãoDupla.Checked);
            }
            catch (Exception)
            {
                MessageBox.Show("Não foi possível criar o arquivo de dados da porta serial!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /*
        void Salvar_Dados_D4X_DL_SWSB()
        {
            try
            {
                if (File.Exists(path_CONFIG + "D4X_DL_SWSB" + ".txt"))
                {
                    File.WriteAllText(path_CONFIG + "D4X_DL_SWSB" + ".txt", String.Empty);

                    using (var tw = new StreamWriter(path_CONFIG + "D4X_DL_SWSB" + ".txt", true))
                    {
                        tw.WriteLine(txt_Caminho_DL_SWSB.Text);
                    }
                }
                else
                {
                    if (!File.Exists(path_CONFIG + "D4X_DL_SWSB" + ".txt"))
                    {
                        File.Create(path_CONFIG + "D4X_DL_SWSB" + ".txt").Dispose();

                        File.WriteAllText(path_CONFIG + "D4X_DL_SWSB" + ".txt", String.Empty);

                        using (TextWriter tw = new StreamWriter(path_CONFIG + "D4X_DL_SWSB" + ".txt"))
                        {
                            tw.WriteLine(txt_Caminho_DL_SWSB.Text);
                        }
                    }
                }
                LOG_TXT("Salvando D4X_DL_SWSB file path:  " + txt_Caminho_DL_SWSB.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Não foi possível salver o caminho para arquivo D4X_DL_SWSB!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }*/

        /*
        void Salvar_Dados_D4X_ST_SW()
        {
            try
            {
                if (File.Exists(path_CONFIG + "D4X_ST_SW" + ".txt"))
                {
                    File.WriteAllText(path_CONFIG + "D4X_ST_SW" + ".txt", String.Empty);

                    using (var tw = new StreamWriter(path_CONFIG + "D4X_ST_SW" + ".txt", true))
                    {
                        tw.WriteLine(txt_Caminho_ST_SW.Text);
                    }
                }
                else
                {
                    if (!File.Exists(path_CONFIG + "D4X_ST_SW" + ".txt"))
                    {
                        File.Create(path_CONFIG + "D4X_ST_SW" + ".txt").Dispose();

                        File.WriteAllText(path_CONFIG + "D4X_ST_SW" + ".txt", String.Empty);

                        using (TextWriter tw = new StreamWriter(path_CONFIG + "D4X_ST_SW" + ".txt"))
                        {
                            tw.WriteLine(txt_Caminho_ST_SW.Text);
                        }
                    }
                }
                LOG_TXT("Salvando D4X_ST_SW file path:  " + txt_Caminho_ST_SW.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Não foi possível salvar o caminho para arquivo D4X_ST_SW!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }*/

        void LOG_TXT(String MSG)
        {
            String NOME_LOG = System.DateTime.Today.ToString("yyMMdd");

            try
            {
                if (File.Exists(path_LOG + NOME_LOG + ".txt"))
                {
                    using (var tw = new StreamWriter(path_LOG + NOME_LOG + ".txt", true))
                    {
                        tw.WriteLine(System.DateTime.Now.ToString() + "  -  " + MSG);
                    }
                }
                else
                {
                    if (!File.Exists(path_LOG + NOME_LOG + ".txt"))
                    {
                        File.Create(path_LOG + NOME_LOG + ".txt").Dispose();

                        using (TextWriter tw = new StreamWriter(path_LOG + NOME_LOG + ".txt"))
                        {
                            tw.WriteLine(System.DateTime.Now.ToString() + "  -  " + MSG);
                        }
                    }
                }
            }
            catch (Exception)
            {
                //MessageBox.Show("Não foi possível criar o arquivo de LOG!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
       
        private string SendCommand(string command)
        {
            if (PortaSerialCOM == null || !PortaSerialCOM.IsOpen)
            {
                throw new Exception("Porta serial não está aberta.");
            }

            try
            {
                PortaSerialCOM.DiscardInBuffer();
                PortaSerialCOM.DiscardOutBuffer();

                if (cbxModeloFonte.SelectedIndex == 3)
                {
                    PortaSerialCOM.Write(command);
                }
                else
                {
                    PortaSerialCOM.WriteLine(command);
                }

                if (command.Contains("?"))
                {
                    Thread.Sleep(200);
                    string response = PortaSerialCOM.ReadLine().Trim();
                    return response;
                }

                return string.Empty;
            }
            catch(Exception ex)
            {
                return string.Empty;
            }            
        }

        private void btnConectarPortaSerialCOM_Click(object sender, EventArgs e)
        {
            QT_Timeout = 0;
            if(cbxModeloFonte.SelectedIndex == 1)//KEITHLEY
            {                
                FonteConectada = false;

                if (!PortaSerialCOM.IsOpen)
                {
                    if (cbxPortaSerialCOM.SelectedItem == null)
                    {
                        MessageBox.Show("Selecione uma porta COM.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    try
                    {
                        PortaSerialCOM = new SerialPort();
                        PortaSerialCOM.PortName = cbxPortaSerialCOM.SelectedItem.ToString();
                        PortaSerialCOM.BaudRate = 9600;
                        PortaSerialCOM.DataBits = 8;
                        PortaSerialCOM.Parity = Parity.None;
                        PortaSerialCOM.StopBits = StopBits.One;
                        PortaSerialCOM.Handshake = Handshake.None;
                        PortaSerialCOM.ReadTimeout = 2000;
                        PortaSerialCOM.WriteTimeout = 2000;
                        PortaSerialCOM.NewLine = "\n";

                        PortaSerialCOM.Open();

                        Thread.Sleep(100);

                        string response = SendCommand("*IDN?");

                        if (!string.IsNullOrEmpty(response))
                        {                            
                            FonteConectada = true;
                            btnConectarPortaSerialCOM.Text = "Desconectar";
                            btnOUT1_OFF.Enabled = true;
                            btnOUT1_ON.Enabled = true;
                            btnConectarPortaSerialCOM.Text = "Fechar Porta (A)";                            
                            cbxPortaSerialCOM.Enabled = false;
                            btnRefreshPortaSerialCOM.Enabled = false;                            
                            cbxModeloFonte.Enabled = false;
                            Salvar_Dados_Serial();

                            btn_Iniciar_Gravação.Invoke(new Action(() =>
                            {
                                btn_Iniciar_Gravação.Enabled = true;
                                btn_Iniciar_Gravação.UseVisualStyleBackColor = false;
                                btn_Iniciar_Gravação.BackColor = Color.YellowGreen;
                                if (ConexãoAutomática)
                                    cbxPortaSerialCOM.SelectedIndex = IndexPortaSerialConexaoAutomatica;
                                FonteConectada = true;

                                if (PortaSerialCOM.IsOpen)
                                {
                                    btnOUT1_OFF.Enabled = true;
                                    btnOUT1_ON.Enabled = true;
                                    btnConectarPortaSerialCOM.Text = "Fechar Porta (A)";                                    
                                    cbxPortaSerialCOM.Enabled = false;
                                    btnRefreshPortaSerialCOM.Enabled = false;
                                    Salvar_Dados_Serial();
                                }

                            }));

                            VinProfileIndex = 0;

                            AppendToTxtInfoSafe($"Fonte conectada:\n{response}");

                            //SendCommand("*RST");
                            SendCommand("VOLT:PROT 24.0");
                            SendCommand("CURR:PROT 10.0");
                            Thread.Sleep(500);
                        }
                        else
                        {
                            throw new Exception("Nenhuma resposta do dispositivo.");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (PortaSerialCOM != null && PortaSerialCOM.IsOpen)
                        {
                            PortaSerialCOM.Close();
                        }

                        
                        TimeoutSerialResponse.Stop();
                        TimerVoltageAnimation.Stop();
                        btnOUT1_OFF.Enabled = false;
                        btnOUT1_ON.Enabled = false;
                        PortaSerialCOM.Close();
                        btnConectarPortaSerialCOM.Text = "Abrir Porta (A)";                        
                        cbxPortaSerialCOM.Enabled = true;
                        btnRefreshPortaSerialCOM.Enabled = true;                        
                        cbxModeloFonte.Enabled = true;

                        MessageBox.Show($"Erro ao conectar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    {
                        TimeoutSerialResponse.Stop();
                        TimerVoltageAnimation.Stop();
                        btnOUT1_OFF.Enabled = false;
                        btnOUT1_ON.Enabled = false;
                        PortaSerialCOM.Close();
                        btnConectarPortaSerialCOM.Text = "Abrir Porta (A)";                        
                        cbxPortaSerialCOM.Enabled = true;
                        btnRefreshPortaSerialCOM.Enabled = true;                        
                        cbxModeloFonte.Enabled = true;

                        if (btn_Iniciar_Gravação.InvokeRequired)
                        {
                            btn_Iniciar_Gravação.Invoke(new Action(() =>
                            {
                                btn_Iniciar_Gravação.Enabled = false;
                                btn_Iniciar_Gravação.UseVisualStyleBackColor = true;
                                btn_Iniciar_Gravação.BackColor = SystemColors.ButtonShadow;
                            }));
                        }
                        else
                        {
                            btn_Iniciar_Gravação.Enabled = false;
                            btn_Iniciar_Gravação.UseVisualStyleBackColor = true;
                            btn_Iniciar_Gravação.BackColor = SystemColors.ButtonShadow;
                        }
                    }
                }
            }
            else if(cbxModeloFonte.SelectedIndex == 0 || cbxModeloFonte.SelectedIndex == 2)//TDK-Lambda <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            {
                IndexPortaSerialConexaoAutomatica = 0;
                FonteConectada = false;

                if (!PortaSerialCOM.IsOpen)
                {
                    if (cbxPortaSerialCOM.Items.Count > 0)
                    {
                        if (cbxPortaSerialCOM.SelectedIndex > -1)
                        {
                            try
                            {
                                PortaSerialCOM = new SerialPort(cbxPortaSerialCOM.SelectedItem.ToString(),
                                9600, Parity.None, 8, StopBits.One);

                                PortaSerialCOM.Open();
                                PortaSerialCOM.NewLine = "\r"; //Nova linha não será mais \n e sim \r. Com isto, se a fonte não mandar \n, já vai funcionar!

                                PortaSerialCOM.DataReceived += new SerialDataReceivedEventHandler(PortaSerial_DadoRecebido);

                                LOG_TXT("Porta serial aberta com sucesso!");

                                //MessageBox.Show("Tudo certo com a porta serial selecionada!", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            }
                            catch (Exception ex)
                            {
                                LOG_TXT("Erro ao abrir porta serial!");
                                MessageBox.Show("Erro ao abrir porta serial!\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Você precisa preencher os campos de Nome da Serial e Velocidade!", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Selecione uma porta serial válida!", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    if (PortaSerialCOM.IsOpen)
                    {
                        btnOUT1_OFF.Enabled = true;
                        btnOUT1_ON.Enabled = true;
                        cbxModeloFonte.Enabled = false;

                        if (cbxModeloFonte.SelectedIndex == 2)//MPS
                        {
                            btnOUT2_OFF.Enabled = true;
                            btnOUT2_ON.Enabled = true;
                            btnOUT3_OFF.Enabled = true;
                            btnOUT3_ON.Enabled = true;
                        }

                        btnConectarPortaSerialCOM.Text = "Fechar Porta (A)";
                        cbxPortaSerialCOM.Enabled = false;
                        btnRefreshPortaSerialCOM.Enabled = false;
                        Salvar_Dados_Serial();

                        VinProfileIndex = 0;
                        MontarPacoteSerial((UInt16)portaSerialCOM_Request.AddressDefinition, 0);//desconsiderar o 0                  
                    }

                }
                else
                {
                    {
                        TimeoutSerialResponse.Stop();
                        TimerVoltageAnimation.Stop();
                        cbxModeloFonte.Enabled = false;
                        btnOUT1_OFF.Enabled = false;
                        btnOUT1_ON.Enabled = false;
                        btnOUT2_OFF.Enabled = false;
                        btnOUT2_ON.Enabled = false;
                        btnOUT3_OFF.Enabled = false;
                        btnOUT3_ON.Enabled = false;
                        PortaSerialCOM.Close();
                        btnConectarPortaSerialCOM.Text = "Abrir Porta (A)";
                        cbxPortaSerialCOM.Enabled = true;
                        btnRefreshPortaSerialCOM.Enabled = true;

                        if (btn_Iniciar_Gravação.InvokeRequired)
                        {
                            btn_Iniciar_Gravação.Invoke(new Action(() =>
                            {
                                btn_Iniciar_Gravação.Enabled = false;
                                btn_Iniciar_Gravação.UseVisualStyleBackColor = true;
                                btn_Iniciar_Gravação.BackColor = SystemColors.ButtonShadow;
                            }));
                        }
                        else
                        {
                            btn_Iniciar_Gravação.Enabled = false;
                            btn_Iniciar_Gravação.UseVisualStyleBackColor = true;
                            btn_Iniciar_Gravação.BackColor = SystemColors.ButtonShadow;
                        }
                    }
                }
            }            
            else if(cbxModeloFonte.SelectedIndex == 3) //AFR
            {                
                FonteConectada = false;

                if (!PortaSerialCOM.IsOpen)
                {
                    if (cbxPortaSerialCOM.SelectedItem == null)
                    {
                        MessageBox.Show("Selecione uma porta COM.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    try
                    {
                        Console.WriteLine("Tentando conexão com fonte AFR");
                        PortaSerialCOM = new SerialPort();
                        PortaSerialCOM.PortName = cbxPortaSerialCOM.SelectedItem.ToString();
                        PortaSerialCOM.BaudRate = 9600;
                        PortaSerialCOM.DataBits = 8;
                        PortaSerialCOM.Parity = Parity.None;
                        PortaSerialCOM.StopBits = StopBits.One;
                        PortaSerialCOM.Handshake = Handshake.None;
                        PortaSerialCOM.ReadTimeout = 2000;
                        PortaSerialCOM.WriteTimeout = 2000;
                        PortaSerialCOM.NewLine = "\n";

                        PortaSerialCOM.Open();

                        Thread.Sleep(100);

                        string response = SendCommand("STATUS?\\n");

                        if (!string.IsNullOrEmpty(response))
                        {
                            FonteConectada = true;
                            btnConectarPortaSerialCOM.Text = "Desconectar";
                            btnOUT1_OFF.Enabled = true;
                            btnOUT1_ON.Enabled = true;
                            btnConectarPortaSerialCOM.Text = "Fechar Porta (A)";
                            cbxPortaSerialCOM.Enabled = false;
                            btnRefreshPortaSerialCOM.Enabled = false;
                            cbxModeloFonte.Enabled = false;
                            Salvar_Dados_Serial();

                            btn_Iniciar_Gravação.Invoke(new Action(() =>
                            {
                                btn_Iniciar_Gravação.Enabled = true;
                                btn_Iniciar_Gravação.UseVisualStyleBackColor = false;
                                btn_Iniciar_Gravação.BackColor = Color.YellowGreen;
                                if (ConexãoAutomática)
                                    cbxPortaSerialCOM.SelectedIndex = IndexPortaSerialConexaoAutomatica;
                                FonteConectada = true;

                                if (PortaSerialCOM.IsOpen)
                                {
                                    btnOUT1_OFF.Enabled = true;
                                    btnOUT1_ON.Enabled = true;
                                    btnConectarPortaSerialCOM.Text = "Fechar Porta (A)";
                                    cbxPortaSerialCOM.Enabled = false;
                                    btnRefreshPortaSerialCOM.Enabled = false;
                                    Salvar_Dados_Serial();
                                }

                            }));

                            VinProfileIndex = 0;

                            AppendToTxtInfoSafe($"Fonte conectada!");

                            SendCommand("OUTPUT0\\n");
                            Thread.Sleep(200);
                            SendCommand("ISET1:5,000\\n");
                            Thread.Sleep(200);
                            SendCommand("VSET1:0,000\\n");

                            Thread.Sleep(500);
                        }
                        else
                        {
                            throw new Exception("Nenhuma resposta do dispositivo.");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (PortaSerialCOM != null && PortaSerialCOM.IsOpen)
                        {
                            PortaSerialCOM.Close();
                        }


                        TimeoutSerialResponse.Stop();
                        TimerVoltageAnimation.Stop();
                        btnOUT1_OFF.Enabled = false;
                        btnOUT1_ON.Enabled = false;
                        PortaSerialCOM.Close();
                        btnConectarPortaSerialCOM.Text = "Abrir Porta (A)";
                        cbxPortaSerialCOM.Enabled = true;
                        btnRefreshPortaSerialCOM.Enabled = true;
                        cbxModeloFonte.Enabled = true;

                        MessageBox.Show($"Erro ao conectar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    {
                        TimeoutSerialResponse.Stop();
                        TimerVoltageAnimation.Stop();
                        btnOUT1_OFF.Enabled = false;
                        btnOUT1_ON.Enabled = false;
                        PortaSerialCOM.Close();
                        btnConectarPortaSerialCOM.Text = "Abrir Porta (A)";
                        cbxPortaSerialCOM.Enabled = true;
                        btnRefreshPortaSerialCOM.Enabled = true;
                        cbxModeloFonte.Enabled = true;

                        if (btn_Iniciar_Gravação.InvokeRequired)
                        {
                            btn_Iniciar_Gravação.Invoke(new Action(() =>
                            {
                                btn_Iniciar_Gravação.Enabled = false;
                                btn_Iniciar_Gravação.UseVisualStyleBackColor = true;
                                btn_Iniciar_Gravação.BackColor = SystemColors.ButtonShadow;
                            }));
                        }
                        else
                        {
                            btn_Iniciar_Gravação.Enabled = false;
                            btn_Iniciar_Gravação.UseVisualStyleBackColor = true;
                            btn_Iniciar_Gravação.BackColor = SystemColors.ButtonShadow;
                        }
                    }
                }
            }
            /*
            if (!PortaSerialCOM.IsOpen)
            {
                
            }
            else
            {
                TimeoutSerialResponse.Stop();
                TimerVoltageAnimation.Stop();
                btnOUT_OFF.Enabled = false;
                btnOUT_ON.Enabled = false;
                PortaSerialCOM.Close();
                btnConectarPortaSerialCOM.Text = "Abrir Porta";
                cbxBaudRatePortaSerialCOM.Enabled = true;
                cbxPortaSerialCOM.Enabled = true;
                btnRefreshPortaSerialCOM.Enabled = true;                

                if (btn_Iniciar_Gravação.InvokeRequired)
                {
                    btn_Iniciar_Gravação.Invoke(new Action(() =>
                    {
                        btn_Iniciar_Gravação.Enabled = false;
                        btn_Iniciar_Gravação.UseVisualStyleBackColor = true;
                        btn_Iniciar_Gravação.BackColor = SystemColors.ButtonShadow;
                    }));
                }
                else
                {
                    btn_Iniciar_Gravação.Enabled = false;
                    btn_Iniciar_Gravação.UseVisualStyleBackColor = true;
                    btn_Iniciar_Gravação.BackColor = SystemColors.ButtonShadow;
                }
            }*/
        }
        void MontarPacoteSerial(UInt16 TipoPacote, double value)
        {
            RequestIndex = TipoPacote;            

            //Implementar a lógica para montar o pacote serial a ser enviado, de acordo com o protocolo definido para comunicação com a fonte.
            switch (TipoPacote)
            {
                case (UInt16)portaSerialCOM_Request.AddressDefinition:
                    //Montar pacote de Definição de endereço
                    EnviarDadosSerial("ADR " + 6);
                    Console.WriteLine("ADR " + 6 + " --->");
                    break;
                case (UInt16)portaSerialCOM_Request.PVCommand:
                    //Montar pacote de definição de tensão de saída
                    EnviarDadosSerial("PV " + value.ToString("F2").Replace(",", "."));
                    Console.WriteLine("PV " + value.ToString("F2").Replace(",", ".") + " --->");
                    break;
                case (UInt16)portaSerialCOM_Request.OUTCommand:
                    EnviarDadosSerial("OUT " + (uint)value);
                    Console.WriteLine("OUT " + (uint)value + " --->");
                    break;
                case (UInt16)portaSerialCOM_Request.MCRequest:
                    EnviarDadosSerial("MC?");
                    Console.WriteLine("MC? --->");
                    break;
                case (UInt16)portaSerialCOM_Request.MPS_FUNC2Command:
                    EnviarDadosSerial("FUNC_2 " + value.ToString("F2").Replace(",", "."));
                    Console.WriteLine("FUNC_2 " + value.ToString("F2").Replace(",", "."));
                    break;
                case (UInt16)portaSerialCOM_Request.MPS_FUNC3Command:
                    EnviarDadosSerial("FUNC_3 " + value.ToString("F2").Replace(",", "."));
                    Console.WriteLine("FUNC_3 " + value.ToString("F2").Replace(",", "."));
                    break;
            }

            TimeoutSerialResponse.Start();
        }

        void EnviarDadosSerial(string Dados)
        {
            try
            {
                if (PortaSerialCOM.IsOpen)
                {
                    PortaSerialCOM.WriteLine(Dados);
                    LOG_TXT("Dado enviado: " + Dados);
                }
                else
                {
                    MessageBox.Show("A porta serial não está aberta!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LOG_TXT("Erro ao enviar dados pela porta serial!");
                MessageBox.Show("Erro ao enviar dados pela porta serial!\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // >>>>>>>>>>> DADOS RECEBIDOS PELA PORTA SERIAL SÃO TRATADOS AQUI <<<<<<<<<<
        private void PortaSerial_DadoRecebido(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string DadoRecebido = PortaSerialCOM.ReadLine();
                Console.WriteLine("Dado recebido: " + DadoRecebido);
                LOG_TXT("Dado Recebido: " + DadoRecebido);

                PortaSerialCOM.DiscardInBuffer();

                if(DadoRecebido == "OK")
                {
                    TimeoutSerialResponse.Stop();

                    switch (RequestIndex)
                    {
                        case (UInt16)portaSerialCOM_Request.AddressDefinition:

                            if (btn_Iniciar_Gravação.InvokeRequired)
                            {
                                btn_Iniciar_Gravação.Invoke(new Action(() =>
                                {
                                    btn_Iniciar_Gravação.Enabled = true;
                                    btn_Iniciar_Gravação.UseVisualStyleBackColor = false;
                                    btn_Iniciar_Gravação.BackColor = Color.YellowGreen;
                                    if(ConexãoAutomática)
                                        cbxPortaSerialCOM.SelectedIndex = IndexPortaSerialConexaoAutomatica;
                                    FonteConectada = true;

                                    if (PortaSerialCOM.IsOpen)
                                    {
                                        btnOUT1_OFF.Enabled = true;
                                        btnOUT1_ON.Enabled = true;
                                        btnConectarPortaSerialCOM.Text = "Fechar Porta (A)";                                        
                                        cbxPortaSerialCOM.Enabled = false;
                                        btnRefreshPortaSerialCOM.Enabled = false;
                                        Salvar_Dados_Serial();
                                    }

                                }));
                            }
                            else
                            {
                                btn_Iniciar_Gravação.Enabled = true;
                                btn_Iniciar_Gravação.UseVisualStyleBackColor = false;
                                btn_Iniciar_Gravação.BackColor = Color.YellowGreen;
                                cbxPortaSerialCOM.SelectedIndex = IndexPortaSerialConexaoAutomatica;
                                FonteConectada = true;

                                if (PortaSerialCOM.IsOpen)
                                {
                                    btnOUT1_OFF.Enabled = true;
                                    btnOUT1_ON.Enabled = true;
                                    btnConectarPortaSerialCOM.Text = "Fechar Porta (A)";                                    
                                    cbxPortaSerialCOM.Enabled = false;
                                    btnRefreshPortaSerialCOM.Enabled = false;
                                    Salvar_Dados_Serial();
                                }
                            }                            

                            txt_Info_AppendText("Fonte conectada com sucesso!");                            

                            break;
                        case (UInt16)portaSerialCOM_Request.PVCommand:
                            if (GravaçãoON)
                            {                                
                                StartTimerVoltageAnimationSafe();
                            }
                                break;
                        case (UInt16)portaSerialCOM_Request.OUTCommand:
                            Console.WriteLine("OUT n Command received");
                            if(GravaçãoON)
                            {                                
                                StartTimerVoltageAnimationSafe();
                            }
                            break;                        
                    }                    
                }
                else
                {
                    TimeoutSerialResponse.Stop();

                    //Verificando se é erro:
                    if(DadoRecebido == "E01")
                    {
                        MessageBox.Show("TDK Erro: Valor de tensão acima do range máximo da fonte (OVP)!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else if(DadoRecebido == "E02")
                    {
                        MessageBox.Show("TDK Erro: Valor de tensão abaixo do range mínimo da fonte (UVL)!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else if (DadoRecebido == "E04")
                    {
                        MessageBox.Show("TDK Erro: OVP está acima do limite máximo permitido!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else if (DadoRecebido == "E06")
                    {
                        MessageBox.Show("TDK Erro: Valor de UVL menor que a tensão de saída ajustada!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else if (DadoRecebido == "E07")
                    {
                        MessageBox.Show("TDK Erro: Output ligada durante latched fault shut down!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else if (DadoRecebido == "E08")
                    {
                        MessageBox.Show("TDK Erro: Comando não pode ser executado via Modo Escravo Paralelo Avançado", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }

                    else if (DadoRecebido == "C01")
                    {
                        MessageBox.Show("TDK Erro: Comando ou solicitação invalida!", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else if (DadoRecebido == "C02")
                    {
                        MessageBox.Show("TDK Erro: Parâmetro faltante", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else if (DadoRecebido == "C03")
                    {
                        MessageBox.Show("TDK Erro: Parâmetro ilegal", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else if (DadoRecebido == "C04")
                    {
                        MessageBox.Show("TDK Erro: Erro de checksum", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else if (DadoRecebido == "C05")
                    {
                        MessageBox.Show("TDK Erro: Parâmetro fora do range", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    ////////////////////////

                    switch (RequestIndex)
                    {
                        case (UInt16)portaSerialCOM_Request.MCRequest:
                            if (double.TryParse(DadoRecebido, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double currentValue))
                            {
                                // Conversão bem-sucedida
                                Console.WriteLine("Valor: " + currentValue);
                                output_Current = currentValue;
                            }
                            else
                            {
                                // Falha na conversão
                                Console.WriteLine("Não foi possível converter");
                            }
                            break;
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        // Thread-safe helpers
        void StartTimerVoltageAnimationSafe()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(StartTimerVoltageAnimationSafe));                
                return;
            }
            TimerVoltageAnimation.Start();
        }

        /*
        private void btn_Carregar_ST_SW_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Selecione um arquivo";
                ofd.Filter = "(*.hex)|*.hex"; // ajuste o filtro se quiser
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txt_Caminho_ST_SW.Text = ofd.FileName;
                    lblSWName.Text = Path.GetFileName(ofd.FileName);
                    Salvar_Dados_D4X_ST_SW();
                    // Se quiser também copiar para a área de transferência:
                    // Clipboard.SetText(ofd.FileName);
                }
            }
        }*/

        /*
        private void btn_Carregar_DL_SWSB_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Selecione um arquivo";
                ofd.Filter = "(*.hex)|*.hex"; // ajuste o filtro se quiser
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txt_Caminho_DL_SWSB.Text = ofd.FileName;
                    Salvar_Dados_D4X_DL_SWSB();
                    // Se quiser também copiar para a área de transferência:
                    // Clipboard.SetText(ofd.FileName);
                }
            }
        }*/

        private void btn_Iniciar_Gravação_Click(object sender, EventArgs e)
        {
            if (PortaSerialCOM.IsOpen)
            {
                if (lblSWName.Text != string.Empty)
                {
                    //Aqui deve ser implementada a lógica para iniciar o processo de gravação, utilizando os arquivos e a porta serial selecionados.
                    //Checar_Status_Inicial();
                    IniciarProcessoGravacao();
                }
                else
                {
                    MessageBox.Show("Selecione os arquivos D4X_DL_SWSB e D4X_ST_SW antes de iniciar a gravação!", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Conecte a porta serial antes de iniciar a gravação!", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void IniciarProcessoGravacao()
        {
            VinProfileIndex = 0;
            GravaçãoON = true;
            TimerVoltageAnimation.Interval = 100;
            btn_Iniciar_Gravação.Enabled = false;
            btnOUT1_OFF.Enabled = false;
            btnOUT1_ON.Enabled = false;
            ForçarBootMode = false;
            TimerForceBootMode.Stop();
            DLC_Modo_Boot = false;
            DLC_WRITING_PROCESS = false;

            //Implementar a lógica para iniciar o processo de gravação, utilizando os arquivos e a porta serial selecionados.
            CarregarPerfilAnimação();

            txt_Info_AppendText("Animação de Vin iniciada.");

            if (cbxModeloFonte.SelectedIndex == 0 || cbxModeloFonte.SelectedIndex == 2)//TDK-Lambda
                MontarPacoteSerial((UInt16)portaSerialCOM_Request.OUTCommand, 1);
            else if (cbxModeloFonte.SelectedIndex == 1)//KEITHLEY
            {
                SendCommand("OUTP ON");
                string outputState = SendCommand("OUTP?").Trim();
                if (outputState == "1")
                {
                    TimerVoltageAnimation.Start();
                }
                else
                {
                    AppendToTxtInfoSafe("Falha ao ligar a saída da fonte.");
                }
            } 
            
            else if(cbxModeloFonte.SelectedIndex == 3) //AFR
            {
                Console.WriteLine("Iniciando para AFR");
                SendCommand("OUTPUT1\\n");

                Thread.Sleep(200);
                string outputState = SendCommand("STATUS?\\n").Trim();
                Console.WriteLine(outputState);
                if (outputState == "110")
                {                    
                    TimerVoltageAnimation.Start();
                }
                else
                {
                    AppendToTxtInfoSafe("Falha ao ligar a saída da fonte.");
                }
            }

            TempoDecorrido = 0;
            TimerContador.Start();

            //MontarPacoteSerial((UInt16)portaSerialCOM_Request.PVCommand, VinProfile[0,0]);
            //TimerVoltageAnimation.Interval = (Int32)VinProfile[0, 1];            
        }
               
        //CMD COMMAND <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
        Task<int> RunCmdAndStreamOutput(string command, int channel)
        {
            var psi = new ProcessStartInfo("cmd.exe", "/k " + command)
            {
                CreateNoWindow = false,
                UseShellExecute = false, //Se for true, nem abre o CMD!! TEM QUE SER FALSE
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,//Se for true, não digita o número do canal!
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var tcs = new TaskCompletionSource<int>();

            try
            {
                if (!proc.Start())
                {
                    tcs.TrySetException(new InvalidOperationException("Failed to start process. Canal " + channel.ToString()));
                    return tcs.Task;
                }

                LOG_TXT("Processo iniciado - PID: " + proc.Id + "Canal " + channel.ToString());
                
                if(channel == 0)
                    ProcessoID_Channel0 = proc.Id;
                else if (channel == 1)
                    ProcessoID_Channel1 = proc.Id;

                // Minimiza a janela do CMD
                Task.Run(async () =>
                {
                    await Task.Delay(100);
                    try
                    {
                        IntPtr h = proc.MainWindowHandle;
                        if (h == IntPtr.Zero)
                        {
                            h = FindWindowForProcess(proc.Id);
                        }
                        if (h != IntPtr.Zero)
                        {
                            ShowWindow(h, SW_MINIMIZE);
                            LOG_TXT("Canal " + channel.ToString() + "Janela do processo minimizada");
                        }
                    }
                    catch (Exception ex)
                    {
                        LOG_TXT("Canal " + channel.ToString() + ". Erro ao minimizar janela: " + ex.Message);
                    }
                });

                // Captura e analisa StandardError
                Task.Run(async () =>
                {
                    try
                    {
                        while (!proc.StandardOutput.EndOfStream)
                        {
                            string outputline = await proc.StandardOutput.ReadLineAsync();
                            if (!string.IsNullOrEmpty(outputline))
                            {
                                Console.WriteLine("Canal " + channel.ToString() + ". Standard Output: " + outputline);
                                LOG_TXT("Canal " + channel.ToString() + ". Standard Output: " + outputline);
                                // ANÁLISE DE STRINGS AQUI:

                                if (outputline.Contains("Login failed (KO) !!!"))
                                {
                                    Task.Run(() => Console.Beep(800, 3000));
                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + " >>> DLC não respondeu <<<");
                                    LOG_TXT("Canal " + channel.ToString() + " >>> DLC não respondeu <<<");
                                    proc.Kill();
                                    proc.Dispose();

                                    if(channel == 0)
                                        FecharProcessoPorId(ProcessoID_Channel0);
                                    else if (channel == 1)
                                        FecharProcessoPorId(ProcessoID_Channel1);

                                    if (btn_Iniciar_Gravação.InvokeRequired)
                                    {
                                        btn_Iniciar_Gravação.Invoke(new Action(() =>
                                        {
                                            btn_Iniciar_Gravação.Enabled = true;
                                            btnOUT1_OFF.Enabled = true;
                                            btnOUT1_ON.Enabled = true;
                                            lblEtapa.Text = "Finalizado com falha";
                                        }));
                                    }
                                    else
                                    {
                                        btn_Iniciar_Gravação.Enabled = true;
                                        btnOUT1_OFF.Enabled = true;
                                        btnOUT1_ON.Enabled = true;
                                        lblEtapa.Text = "Finalizado com falha";
                                    }
                                }                                

                                if (outputline.Contains("Flashing failed (KO) !!!"))
                                {
                                    Task.Run(() => Console.Beep(800, 3000));
                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + " >>> Falha na gravação. <<<");
                                    LOG_TXT("Canal " + channel.ToString() + " >>> Falha na gravação. <<<");
                                    if (btn_Iniciar_Gravação.InvokeRequired)
                                    {
                                        btn_Iniciar_Gravação.Invoke(new Action(() =>
                                        {
                                            btn_Iniciar_Gravação.Enabled = true;
                                            btnOUT1_OFF.Enabled = true;
                                            btnOUT1_ON.Enabled = true;
                                            lblEtapa.Text = "Finalizado com falha";
                                        }));
                                    }
                                    else
                                    {
                                        btn_Iniciar_Gravação.Enabled = true;
                                        btnOUT1_OFF.Enabled = true;
                                        btnOUT1_ON.Enabled = true;
                                        lblEtapa.Text = "Finalizado com falha";
                                    }
                                    proc.Kill();
                                    proc.Dispose();

                                    if (channel == 0)
                                        FecharProcessoPorId(ProcessoID_Channel0);
                                    else if (channel == 1)
                                        FecharProcessoPorId(ProcessoID_Channel1);
                                }

                                if (outputline.Contains("ECU is not programmed successfully (KO)"))
                                {
                                    Task.Run(() => Console.Beep(800, 3000));
                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + " >>> DLC não foi regravada! Reinicie o processo. <<<");
                                    LOG_TXT("Canal " + channel.ToString() + " >>> DLC não foi regravada! Reinicie o processo. <<<<<<");
                                    if (btn_Iniciar_Gravação.InvokeRequired)
                                    {
                                        btn_Iniciar_Gravação.Invoke(new Action(() =>
                                        {
                                            btn_Iniciar_Gravação.Enabled = true;
                                            btnOUT1_OFF.Enabled = true;
                                            btnOUT1_ON.Enabled = true;
                                            lblEtapa.Text = "Finalizado com falha";
                                        }));
                                    }
                                    else
                                    {
                                        btn_Iniciar_Gravação.Enabled = true;
                                        btnOUT1_OFF.Enabled = true;
                                        btnOUT1_ON.Enabled = true;
                                        lblEtapa.Text = "Finalizado com falha";
                                    }
                                    proc.Kill();
                                    proc.Dispose();

                                    if (channel == 0)
                                        FecharProcessoPorId(ProcessoID_Channel0);
                                    else if (channel == 1)
                                        FecharProcessoPorId(ProcessoID_Channel1);
                                }

                                if (outputline.Contains("Upload RAM application failed (KO) !!!"))
                                {
                                    Task.Run(() => Console.Beep(800, 3000));
                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + " >>> Falha no upload <<<");
                                    LOG_TXT("Canal " + channel.ToString() + " >>> Falha no upload <<<");
                                    if (btn_Iniciar_Gravação.InvokeRequired)
                                    {
                                        btn_Iniciar_Gravação.Invoke(new Action(() =>
                                        {
                                            btn_Iniciar_Gravação.Enabled = true;
                                            btnOUT1_OFF.Enabled = true;
                                            btnOUT1_ON.Enabled = true;
                                            lblEtapa.Text = "Finalizado com falha";
                                        }));
                                    }
                                    else
                                    {
                                        btn_Iniciar_Gravação.Enabled = true;
                                        btnOUT1_OFF.Enabled = true;
                                        btnOUT1_ON.Enabled = true;
                                        lblEtapa.Text = "Finalizado com falha";
                                    }
                                    proc.Kill();
                                    proc.Dispose();

                                    if (channel == 0)
                                        FecharProcessoPorId(ProcessoID_Channel0);
                                    else if (channel == 1)
                                        FecharProcessoPorId(ProcessoID_Channel1);
                                }

                                if (outputline.Contains("No CAN hardware found!"))
                                {
                                    Task.Run(() => Console.Beep(800, 3000));
                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + " >>> VECTOR não encontrado <<<");
                                    LOG_TXT("Canal " + channel.ToString() + " >>> VECTOR não encontrado <<<");
                                    if (btn_Iniciar_Gravação.InvokeRequired)
                                    {
                                        btn_Iniciar_Gravação.Invoke(new Action(() =>
                                        {
                                            btn_Iniciar_Gravação.Enabled = true;
                                            btnOUT1_OFF.Enabled = true;
                                            btnOUT1_ON.Enabled = true;
                                            lblEtapa.Text = "Finalizado com falha";
                                        }));
                                    }
                                    else
                                    {
                                        btn_Iniciar_Gravação.Enabled = true;
                                        btnOUT1_OFF.Enabled = true;
                                        btnOUT1_ON.Enabled = true;
                                        lblEtapa.Text = "Finalizado com falha";
                                    }
                                    proc.Kill();
                                    proc.Dispose();

                                    if (channel == 0)
                                        FecharProcessoPorId(ProcessoID_Channel0);
                                    else if (channel == 1)
                                        FecharProcessoPorId(ProcessoID_Channel1);
                                }

                                if (outputline.Contains("ECU responding (OK)"))
                                {
                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + ". Etapa: 1/6 Conectando à DLC");
                                    LOG_TXT("Canal " + channel.ToString() + ". Etapa: 1/6 Conectando à DLC");

                                    if (lblEtapa.InvokeRequired)
                                    {
                                        lblEtapa.Invoke(new Action(() =>
                                        {
                                            lblEtapa.Text = "Etapa: 1/6 Conectando à DLC";
                                        }));
                                    }
                                    else
                                    {
                                        lblEtapa.Text = "Etapa: 1/6 Conectando à DLC";
                                    }
                                }

                                if (outputline.Contains("Upload: ["))
                                {
                                    if (lblEtapa.InvokeRequired)
                                    {
                                        lblEtapa.Invoke(new Action(() =>
                                        {
                                            lblEtapa.Text = "Etapa: 2/6 Upload";
                                        }));
                                    }
                                    else
                                    {
                                        lblEtapa.Text = "Etapa: 2/6 Upload";
                                    }

                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + ". Etapa: 2/6 Upload");
                                    LOG_TXT("Canal " + channel.ToString() + ". Etapa: 2/6 Upload");
                                }

                                if (outputline.Contains("CRC check passed"))
                                {
                                    String frase;
                                    if(DLC_WRITING_PROCESS)
                                        frase = "Etapa: 6/6 CRC OK";
                                    else
                                        frase = "Etapa: 3/6 CRC OK";

                                    if (lblEtapa.InvokeRequired)
                                    {
                                        lblEtapa.Invoke(new Action(() =>
                                        {
                                            lblEtapa.Text = frase;
                                        }));
                                    }
                                    else
                                    {
                                        lblEtapa.Text = frase;
                                    }

                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + ". Etapa: 3/6 CRC OK");
                                    LOG_TXT("Canal " + channel.ToString() + ". Etapa: 3/6 CRC OK");
                                }
                                                                
                                if (outputline.Contains("Write: ["))
                                {
                                    if (lblEtapa.InvokeRequired)
                                    {
                                        lblEtapa.Invoke(new Action(() =>
                                        {
                                            lblEtapa.Text = "Etapa: 5/6 Gravando novo SW na DLC";
                                        }));
                                    }
                                    else
                                    {
                                        lblEtapa.Text = "Etapa: 5/6 Gravando novo SW na DLC";
                                    }

                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + ". Etapa: 5/6 Gravando novo SW na DLC");
                                    LOG_TXT("Canal " + channel.ToString() + ". Etapa: 5/6 Gravando novo SW na DLC");
                                    DLC_WRITING_PROCESS = true;
                                }

                                if (outputline.Contains("Erase: ["))
                                {
                                    if (lblEtapa.InvokeRequired)
                                    {
                                        lblEtapa.Invoke(new Action(() =>
                                        {
                                            lblEtapa.Text = "Etapa: 4/6 Limpando memória da DLC";
                                        }));
                                    }
                                    else
                                    {
                                        lblEtapa.Text = "Etapa: 4/6 Limpando memória da DLC";
                                    }

                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + ". Etapa: 4/6 Limpando memória da DLC");
                                    LOG_TXT("Canal " + channel.ToString() + ". Etapa: 4/6 Limpando memória da DLC");
                                }

                                if (outputline.Contains("ECU is programmed successfully (OK)"))
                                {
                                    if (lblEtapa.InvokeRequired)
                                    {
                                        lblEtapa.Invoke(new Action(() =>
                                        {
                                            lblEtapa.Text = "Etapa: 6/6 Finalizado com sucesso";
                                        }));
                                    }
                                    else
                                    {
                                        lblEtapa.Text = "Etapa: 6/6 Finalizado com sucesso";
                                    }

                                    Task.Run(() => Console.Beep(800, 3000));
                                    
                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + ". DLC programada com sucesso!");
                                    LOG_TXT("Canal " + channel.ToString() + ". DLC programada com sucesso!");
                                    TimerContador.Stop();
                                    
                                    proc.Kill();
                                    proc.Dispose();

                                    if (channel == 0)
                                        FecharProcessoPorId(ProcessoID_Channel0);
                                    else if (channel == 1)
                                        FecharProcessoPorId(ProcessoID_Channel1);
                                }

                                if (outputline.Contains("ECU is in application mode now (OK)"))
                                {
                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + ". DLC em modo de aplicação");
                                    LOG_TXT("Canal " + channel.ToString() + ". DLC em modo de aplicação");
                                }

                                if (outputline.Contains("Time elapsed:"))
                                {
                                    AppendToTxtInfoSafe("Canal " + channel.ToString() + " ." + outputline);
                                    LOG_TXT("Canal " + channel.ToString() + ". Tempo total de gravação: " + outputline);
                                    if (btn_Iniciar_Gravação.InvokeRequired)
                                    {
                                        btn_Iniciar_Gravação.Invoke(new Action(() =>
                                        {
                                            btn_Iniciar_Gravação.Enabled = true;
                                            btnOUT1_OFF.Enabled = true;
                                            btnOUT1_ON.Enabled = true;
                                        }));
                                    }
                                    else
                                    {
                                        btn_Iniciar_Gravação.Enabled = true;
                                        btnOUT1_OFF.Enabled = true;
                                        btnOUT1_ON.Enabled = true;
                                    }
                                }
                                
                                if (outputline.Contains("=>"))
                                {
                                    if(DLC_WRITING_PROCESS)
                                    {
                                        if (lblEtapa.InvokeRequired)
                                        {
                                            lblEtapa.Invoke(new Action(() => lblEtapa.Text = "Etapa: 5/6 Gravando novo SW na DLC (" + outputline.Substring(outputline.Length - 4) + ")"));
                                        }
                                        else
                                        {
                                            lblEtapa.Text = "Etapa: 5/6 Gravando novo SW na DLC (" + outputline.Substring(outputline.Length - 4) + ")";
                                        }
                                    }
                                   
                                    //AppendToTxtInfoSafe(outputline);
                                    //LOG_TXT("Tempo total de gravação: " + outputline.Substring(outputline.Length - 4));
                                }                                

                                /*
                                // 2. Procura por canais VN1640A
                                if (errorLine.Contains("VN1640A Channel"))
                                {
                                    // Extrai o número do canal
                                    if (errorLine.Contains("Channel 1"))
                                    {
                                        //AppendToTxtInfoSafe(">>> Canal 1 disponível");
                                        //LOG_TXT("Canal 1 detectado: " + errorLine);
                                    }
                                    else if (errorLine.Contains("Channel 2"))
                                    {
                                        //AppendToTxtInfoSafe(">>> Canal 2 disponível");
                                        //LOG_TXT("Canal 2 detectado: " + errorLine);
                                    }
                                }

                                // 3. Extrai número serial usando IndexOf e Substring
                                if (errorLine.Contains("serial number"))
                                {
                                    int startIndex = errorLine.IndexOf("serial number: ");
                                    if (startIndex >= 0)
                                    {
                                        startIndex += "serial number: ".Length;
                                        int endIndex = errorLine.IndexOf(")", startIndex);
                                        if (endIndex >= 0)
                                        {
                                            string serialNumber = errorLine.Substring(startIndex, endIndex - startIndex);
                                            //LOG_TXT("Serial number encontrado: " + serialNumber);
                                        }
                                    }
                                }

                                // 4. Procura por número no início da linha (opções do menu: 0:, 1:, 2:, etc)
                                if (System.Text.RegularExpressions.Regex.IsMatch(errorLine, @"^\s*\d+:"))
                                {
                                    //AppendToTxtInfoSafe(errorLine); // Mostra a opção do menu
                                }

                                // 5. Padrões de sucesso/erro
                                if (errorLine.Contains("ECU responding") || errorLine.Contains("OK"))
                                {
                                    //AppendToTxtInfoSafe(">>> " + errorLine);
                                    //LOG_TXT("Sucesso: " + errorLine);
                                }
                                else if (errorLine.Contains("error") || errorLine.Contains("fail"))
                                {
                                    //AppendToTxtInfoSafe(">>> ERRO: " + errorLine);
                                    //LOG_TXT("Erro: " + errorLine);
                                }
                                else
                                {
                                    // Outras mensagens
                                    //AppendToTxtInfoSafe(errorLine);
                                    //LOG_TXT("Info: " + errorLine);
                                }*/
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LOG_TXT("Canal " + channel.ToString() + ". Erro ao ler StdOut: " + ex.Message);
                    }
                });

                // Envia "0" + ENTER após 2 segundos
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(2000);
                        if (proc != null && !proc.HasExited)
                        {
                            try
                            {
                                SendKeysToProcessWindow(proc, channel.ToString() +"{ENTER}");
                                LOG_TXT("Attempted to send '" + channel.ToString() + "' ENTER to process window.");
                            }
                            catch (Exception ex)
                            {
                                LOG_TXT("Canal " + channel.ToString() + ". Failed to send keys to process window: " + ex.Message);
                            }
                        }
                    }
                    catch { }
                });

                // Wait for exit on a background thread and propagate exit code
                Task.Run(() =>
                {
                    try
                    {
                        proc.WaitForExit();
                        int code = -1;
                        try { code = proc.ExitCode; } catch { code = -1; }
                        tcs.TrySetResult(code);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                    finally
                    {
                        try { proc.Dispose(); } catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                try { proc.Dispose(); } catch { }
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        /*
        Task<int> RunCmdAndStreamOutput(string command)
        {
            var psi = new ProcessStartInfo("cmd.exe", "/k " + command)
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var tcs = new TaskCompletionSource<int>();

            try
            {
                if (!proc.Start())
                {
                    tcs.TrySetException(new InvalidOperationException("Failed to start process."));
                    return tcs.Task;
                }

                LOG_TXT("Processo iniciado - PID: " + proc.Id);

                // Minimiza a janela do CMD
                Task.Run(async () =>
                {
                    await Task.Delay(100);
                    try
                    {
                        IntPtr h = proc.MainWindowHandle;
                        if (h == IntPtr.Zero)
                        {
                            h = FindWindowForProcess(proc.Id);
                        }
                        if (h != IntPtr.Zero)
                        {
                            ShowWindow(h, SW_MINIMIZE);
                            LOG_TXT("Janela do processo minimizada");
                        }
                    }
                    catch (Exception ex)
                    {
                        LOG_TXT("Erro ao minimizar janela: " + ex.Message);
                    }
                });

                // After the cmd window opens, wait 2 seconds and force typing "0" + ENTER into it.
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(2000);
                        if (proc != null && !proc.HasExited)
                        {
                            try
                            {
                                SendKeysToProcessWindow(proc, "0{ENTER}");
                                LOG_TXT("Attempted to send '0' + ENTER to process window.");
                            }
                            catch (Exception ex)
                            {
                                LOG_TXT("Failed to send keys to process window: " + ex.Message);
                            }
                        }
                    }
                    catch { }
                });

                // Wait for exit on a background thread and propagate exit code
                Task.Run(() =>
                {
                    try
                    {
                        proc.WaitForExit();
                        int code = -1;
                        try { code = proc.ExitCode; } catch { code = -1; }
                        tcs.TrySetResult(code);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                    finally
                    {
                        try { proc.Dispose(); } catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                try { proc.Dispose(); } catch { }
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }*/

        // Asynchronous streaming: runs cmd hidden and appends each output line to txt_Info safely
        // Now returns a Task<int> so callers can await exit code if desired.
        /*Task<int> RunCmdAndStreamOutput(string command)
        {
            // Start a visible cmd window so the user can see the tool output directly.
            // Do not redirect output so the console shows all information.
            var psi = new ProcessStartInfo("cmd.exe", "/k " + command)
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var tcs = new TaskCompletionSource<int>();

            try
            {
                if (!proc.Start())
                {
                    tcs.TrySetException(new InvalidOperationException("Failed to start process."));
                    return tcs.Task;
                }

                // After the cmd window opens, wait 2 seconds and force typing "0" + ENTER into it.
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(2000);
                        if (proc != null && !proc.HasExited)
                        {
                            try
                            {
                                SendKeysToProcessWindow(proc, "0{ENTER}");
                                LOG_TXT("Attempted to send '0' + ENTER to process window.");
                            }
                            catch (Exception ex)
                            {
                                LOG_TXT("Failed to send keys to process window: " + ex.Message);
                            }
                        }
                    }
                    catch { }
                });

                // Wait for exit on a background thread and propagate exit code
                Task.Run(() =>
                {
                    try
                    {
                        proc.WaitForExit();
                        int code = -1;
                        try { code = proc.ExitCode; } catch { code = -1; }
                        tcs.TrySetResult(code);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                    finally
                    {
                        try { proc.Dispose(); } catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                try { proc.Dispose(); } catch { }
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }*/

        void CarregarPerfilAnimação()
        {
            //Implementar a lógica para carregar o perfil de animação, utilizando os arquivos e a porta serial selecionados.
            if (File.Exists(path_CONFIG + "ReflashModeVin_Profile" + ".txt"))
            {
                if (File.ReadLines(path_CONFIG + "ReflashModeVin_Profile" + ".txt").Count() > 0)
                {
                    string[] LinhasDoTXT;
                    LinhasDoTXT = File.ReadAllLines(path_CONFIG + "ReflashModeVin_Profile" + ".txt");

                    UInt16 IndexLine = 0;

                    LOG_TXT("Carregando perfil de Vin para animação:");
                    foreach (string line in LinhasDoTXT)
                    {
                        LOG_TXT(line);
                        string[] partes = line.Split(';');
                        if (partes.Length == 2 && IndexLine < VinProfile.Length)
                        {
                            if (double.TryParse(partes[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double tensao) &&
                                double.TryParse(partes[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double tempo))
                            {
                                if(tensao > 100)
                                {
                                    tensao /= 10;
                                }
                                if(tensao < 1)
                                {
                                    tensao *= 10;
                                }

                                VinProfile[IndexLine, 0] = tensao;
                                VinProfile[IndexLine, 1] = tempo;
                                IndexLine++;
                            }
                            else
                            {
                                LOG_TXT("Erro ao converter linha: " + line);
                            }
                        }
                    }
                    /*
                    for (int i = 0; i < VinProfile.GetLength(0); i++)
                    {
                        for (int j = 0; j < VinProfile.GetLength(1); j++)
                        {
                            Console.WriteLine($"[{i},{j}] = {VinProfile[i, j]}");
                        }
                    }

                    Console.WriteLine("Teste " + VinProfile[3,0] * 2);
                    */
                }
            }
        }

        private void btn_RecarregarArquivosSW_Click(object sender, EventArgs e)
        {
            LerInformacoesSoftwareSalvas();
        }

        private void TimeoutSerialResponse_Tick(object sender, EventArgs e)
        {
            TimeoutSerialResponse.Stop();

            QT_Timeout++;
            if(QT_Timeout > 1)
            {
                QT_Timeout = 0;
                if (!ConexãoAutomática || FonteConectada) //Conexão manual
                {
                    RequestIndex = 0;

                    if (btn_Iniciar_Gravação.InvokeRequired)
                    {
                        btn_Iniciar_Gravação.Invoke(new Action(() =>
                        {
                            btn_Iniciar_Gravação.Enabled = false;
                            btn_Iniciar_Gravação.UseVisualStyleBackColor = true;
                            btn_Iniciar_Gravação.BackColor = SystemColors.ButtonShadow;
                        }));
                    }
                    else
                    {
                        btn_Iniciar_Gravação.Enabled = false;
                        btn_Iniciar_Gravação.UseVisualStyleBackColor = true;
                        btn_Iniciar_Gravação.BackColor = SystemColors.ButtonShadow;
                    }

                    txt_Info_AppendText("Fonte não está respondendo! Verifique a conexão!\r\n");
                    LOG_TXT("Fonte não está respondendo! Verifique a conexão!\r\n");
                }
            }                               
        }

        void txt_Info_AppendText(string text)
        {
            // garante que cada nova entrada termine com uma nova linha
            string line = text + "\r\n";

            if (txt_Info.InvokeRequired)
            {
                txt_Info.Invoke(new Action<string>(txt_Info_AppendText), line);
            }
            else
            {
                txt_Info.AppendText(line);
            }
        }

        // Helper to append text to txt_Info from any thread
        void AppendToTxtInfoSafe(string text)
        {
            if (txt_Info.InvokeRequired)
                txt_Info.Invoke(new Action<string>(AppendToTxtInfoSafe), text + "\r\n");
            else
                txt_Info.AppendText(text + "\r\n");
        }

        private void btnRefreshPortaSerialCOM_Click(object sender, EventArgs e)
        {
            AtualizarPortasSeriais();
        }

        private void btnLimparInfo_Click(object sender, EventArgs e)
        {
            txt_Info.Clear();
        }

        private void TimerVoltageAnimation_Tick(object sender, EventArgs e)
        {
            if(cbxModeloFonte.SelectedIndex == 0 || cbxModeloFonte.SelectedIndex == 2) //TDK-Lambda
            {
                MontarPacoteSerial((UInt16)portaSerialCOM_Request.PVCommand, VinProfile[VinProfileIndex, 0]);
            }                
            else if(cbxModeloFonte.SelectedIndex == 1) //KEITHLEY
            {

                SendCommand("APPLy " + VinProfile[VinProfileIndex, 0].ToString("F2").Replace(",",".") + ",5.0");
                LOG_TXT("Enviando comando de tensão: " + "APPLy " + VinProfile[VinProfileIndex, 0].ToString("F2").Replace(",", ".") + ",5.0");
                Console.WriteLine("Enviando comando de tensão: " + "APPLy " + VinProfile[VinProfileIndex, 0].ToString("F2").Replace(",", ".") + ",5.0");
            } 
            else if(cbxModeloFonte.SelectedIndex == 3) //AFR 3005P
            {
                SendCommand("VSET1:" + VinProfile[VinProfileIndex, 0].ToString("F2").Replace(".", ",") + "\\n");
                LOG_TXT("Enviando comando de tensão: " + "VSET1:" + VinProfile[VinProfileIndex, 0].ToString("F2").Replace(".", ",") + "\\n");
                Console.WriteLine("Enviando comando de tensão: " + "VSET1:" + VinProfile[VinProfileIndex, 0].ToString("F2").Replace(".", ",") + "\\n");
            }

            if (VinProfile[VinProfileIndex, 1] != -1)
            {
                TimerVoltageAnimation.Interval = (Int32)VinProfile[VinProfileIndex, 1];
                VinProfileIndex++;
                if(cbxModeloFonte.SelectedIndex == 0 || cbxModeloFonte.SelectedIndex == 2)//TDK-Lambda aguarda ela responder para ligar timer
                    TimerVoltageAnimation.Stop();
            }
            else //Finalizou a animação para entrada em modo de boot
            {
                TimerVoltageAnimation.Stop();
                GravaçãoON = false;
                AppendToTxtInfoSafe("Aguardando modo BOOT.");
                TimerCheckBootMode.Start();
                TimerForceBootMode.Start();
            }            
        }
        
        private void btnOUT_ON_Click(object sender, EventArgs e)
        {
            if (cbxModeloFonte.SelectedIndex == 0 || cbxModeloFonte.SelectedIndex == 2) //TDK-Lambda e Keithley
            {
                MontarPacoteSerial((UInt16)portaSerialCOM_Request.OUTCommand, 1);
            }
            else if (cbxModeloFonte.SelectedIndex == 1)//KEITHLEY
            {
                try
                {
                    SendCommand("OUTP ON");
                    //MessageBox.Show("Saída ligada!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AppendToTxtInfoSafe("Saída ligada!");
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"Erro ao ligar saída: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    AppendToTxtInfoSafe($"Erro ao ligar saída: {ex.Message}");
                }
            }
            else if(cbxModeloFonte.SelectedIndex == 3) //AFR 3005P
            {
                SendCommand("OUTPUT1\\n");
                Thread.Sleep(200);
                SendCommand("VSET1:13,500\\n");
                AppendToTxtInfoSafe("Saída ligada!");
            }
        }

        private void btnOUT_OFF_Click(object sender, EventArgs e)
        {
            if (cbxModeloFonte.SelectedIndex == 0 || cbxModeloFonte.SelectedIndex == 2) //TDK-Lambda
                MontarPacoteSerial((UInt16)portaSerialCOM_Request.OUTCommand, 0);
            else if (cbxModeloFonte.SelectedIndex == 1)//KEITHLEY
            {
                try
                {
                    SendCommand("OUTP OFF");
                    //MessageBox.Show("Saída ligada!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AppendToTxtInfoSafe("Saída desligada!");
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"Erro ao ligar saída: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    AppendToTxtInfoSafe($"Erro ao desligar saída: {ex.Message}");
                }
            }
            else if(cbxModeloFonte.SelectedIndex == 3) //AFS
            {
                SendCommand("OUTPUT0\\n");
                AppendToTxtInfoSafe("Saída desligada!");
            }
        }

        private void btnCancelarProcesso_Click(object sender, EventArgs e)
        {
            FecharProcessoPorId(ProcessoID_Channel0);
            FecharProcessoPorId(ProcessoID_Channel1);
        }
        
        void FecharProcessoPorId(int processId)
        {
            btn_Iniciar_Gravação.Invoke(new Action(() =>
            {
                aux_count = 0;
                TempoDecorrido = 0;
                VinProfileIndex = 0;
                GravaçãoON = false;
                DLC_Modo_Boot = false;
                ConexãoAutomática = false;
                FonteConectada = true;
                ForçarBootMode = false;
                DLC_WRITING_PROCESS = false;
                output_Current = 0;
                IndexPortaSerialConexaoAutomatica = 0;
                TimerCheckBootMode.Stop();
                TimerContador.Stop();
                TimerForceBootMode.Stop();
                TimerVoltageAnimation.Stop();
                TimeoutSerialResponse.Stop();
                lblEtapa.Text = "Etapa:";
                btn_Iniciar_Gravação.Enabled = true;
                btnOUT1_OFF.Enabled = true;
                btnOUT1_ON.Enabled = true;
            }));

            try
            {
                Process processo = Process.GetProcessById(processId);
                if (!processo.HasExited)
                {
                    // Tenta fechar a janela normalmente primeiro
                    bool fechou = processo.CloseMainWindow();

                    if (!fechou || !processo.WaitForExit(2000))  // Aguarda 2 segundos
                    {
                        // Se não fechou, força o encerramento
                        processo.Kill();
                        LOG_TXT("Processo " + processId + " encerrado forçadamente.");
                    }
                    else
                    {
                        LOG_TXT("Processo " + processId + " encerrado normalmente.");
                    }                    
                }
                processo.Dispose();
            }
            catch (ArgumentException)
            {
                LOG_TXT("Processo " + processId + " já foi encerrado.");
            }
            catch (Exception ex)
            {
                LOG_TXT("Erro ao encerrar processo: " + ex.Message);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            FecharProcessoPorId(ProcessoID_Channel0);
            FecharProcessoPorId(ProcessoID_Channel1);
        }

        private void cbxModeloFonte_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cbxModeloFonte.SelectedIndex == 2) //Marelli Power Supply
            {
                               
            }            
        }

        private void btnOUT2_ON_Click(object sender, EventArgs e)
        {
            if (cbxModeloFonte.SelectedIndex == 2) //MPS
            {
                MontarPacoteSerial((UInt16)portaSerialCOM_Request.MPS_FUNC2Command, 1);
            }
        }

        private void btnOUT2_OFF_Click(object sender, EventArgs e)
        {
            if (cbxModeloFonte.SelectedIndex == 2) //MPS
            {
                MontarPacoteSerial((UInt16)portaSerialCOM_Request.MPS_FUNC2Command, 0);
            }
        }
        private void cbkGravaçãoDupla_CheckedChanged(object sender, EventArgs e)
        {
            Salvar_Dados_Serial();
        }
        private void btnOUT3_ON_Click(object sender, EventArgs e)
        {
            if (cbxModeloFonte.SelectedIndex == 2) //MPS
            {
                MontarPacoteSerial((UInt16)portaSerialCOM_Request.MPS_FUNC3Command, 1);
            }
        }

        private void btnOUT3_OFF_Click(object sender, EventArgs e)
        {
            if (cbxModeloFonte.SelectedIndex == 2) //MPS
            {
                MontarPacoteSerial((UInt16)portaSerialCOM_Request.MPS_FUNC3Command, 0);
            }
        }

        private void TimerForceBootMode_Tick(object sender, EventArgs e)
        {
            ForçarBootMode = true;
            TimerForceBootMode.Stop();
            AppendToTxtInfoSafe("BOOT não detectado. Forçando gravação.");
        }
            
        private void TimerContador_Tick(object sender, EventArgs e)
        {
            TempoDecorrido++;
            lblTimer.Text = "Tempo: " + FormatarTempoMMSS(TempoDecorrido);
        }

        string FormatarTempoMMSS(int segundos)
        {
            TimeSpan tempo = TimeSpan.FromSeconds(segundos);
            return tempo.ToString(@"mm\:ss");
        }

        private void Checar_Status_Inicial()
        {
            /*
            private SerialPort PortaSerialCOM = new SerialPort();

            UInt16 RequestIndex = 0, aux_count = 0, TempoDecorrido = 0;

            double[,] VinProfile = new double[5, 2]; //Tensão(V) ; Tempo(ms)
            UInt16 VinProfileIndex = 0;

            Boolean GravaçãoON = false, AguardandoBoot_DLC = false, ConexãoAutomática = false, FonteConectada = false;
            Boolean ForçarBootMode = false, DLC_WRITING_PROCESS = false;

            Double output_Current = 0.0;

            string[] SerialPorts;
            UInt16 IndexPortaSerialConexaoAutomatica = 0; */

            Console.WriteLine("PortaSeria: " + PortaSerialCOM.IsOpen);
            Console.WriteLine("RequestIndex: " + RequestIndex);
            Console.WriteLine("aux_count: " + aux_count);
            Console.WriteLine("TempoDecorrido: " + TempoDecorrido);
            Console.WriteLine("VinProfileIndex: " + VinProfileIndex);
            Console.WriteLine("GravaçãoON: " + GravaçãoON);
            Console.WriteLine("AguardandoBoot_DLC: " + DLC_Modo_Boot);
            Console.WriteLine("ConexãoAutomática: " + ConexãoAutomática);
            Console.WriteLine("FonteConectada: " + FonteConectada);
            Console.WriteLine("ForçarBootMode: " + ForçarBootMode);
            Console.WriteLine("DLC_WRITING_PROCESS: " + DLC_WRITING_PROCESS);
            Console.WriteLine("output_Current: " + output_Current);
            Console.WriteLine("IndexPortaSerialConexaoAutomatica: " + IndexPortaSerialConexaoAutomatica);
        }

        private void TimerCheckBootMode_Tick(object sender, EventArgs e)
        {
            if (cbxModeloFonte.SelectedIndex == 0 || cbxModeloFonte.SelectedIndex == 2)//TDK-Lambda
            {
                if (!DLC_Modo_Boot)
                {
                    //Solicitando corrente pra saber se entramos no modo BOOT;
                    MontarPacoteSerial((UInt16)portaSerialCOM_Request.MCRequest, 0); //Não possui valor como argumento!
                }

                //Aguardamos a corrente cair ou forçamos a gravação por não conseguir detectar modo BOOT
                if (((output_Current > 0.02 && output_Current < 0.1) || ForçarBootMode) && !DLC_Modo_Boot)
                {
                    AppendToTxtInfoSafe("Iniciando processo de gravação. Aguarde alguns segundos...");
                    output_Current = 0;
                    DLC_Modo_Boot = true;
                    TimerForceBootMode.Stop();

                    string D4X_DL_SWSB_File = "D4X_DL000_SWSB_0210.hex";
                    string D4X_ST_SW_File = lblSWName.Text;
                    string cmdCommand = ".\\ALFlasherAll.exe --ecuClass NXP --baud 19200 --txId 0x3C --rxId " +
                                        "0x3D --lin --ram " + D4X_DL_SWSB_File + " --jumpAddr 0x20000004 --writeCode " +
                                        D4X_ST_SW_File + " --crcCode " + D4X_ST_SW_File + " --ecuReset";

                    LOG_TXT("Running command: " + cmdCommand);

                    if (cbkGravaçãoDupla.Checked)
                    {
                        var Channel0_process_double = RunCmdAndStreamOutput(cmdCommand, 0);
                        var Channel1_process_double = RunCmdAndStreamOutput(cmdCommand, 1);
                    }
                    else
                    {
                        var Channel0_process_single = RunCmdAndStreamOutput(cmdCommand, 0);
                    }
                }
                else if (DLC_Modo_Boot && (output_Current <= 0.02 || output_Current >= 0.1) && !ForçarBootMode)
                {
                    AppendToTxtInfoSafe(">>> DLC saiu do modo BOOT <<<");
                    MontarPacoteSerial((UInt16)portaSerialCOM_Request.OUTCommand, 0);
                    DLC_Modo_Boot = false;
                    TimerCheckBootMode.Stop();
                    GravaçãoON = false;
                    TempoDecorrido = 0;
                    lblTimer.Text = "Tempo: 00:00";
                    VinProfileIndex = 0;
                    DLC_WRITING_PROCESS = false;
                    output_Current = 0;
                }
                else //Atualizar mais lentamente (5 segundos)
                {
                    aux_count++;
                    if (aux_count >= 6)
                    {
                        aux_count = 0;
                        MontarPacoteSerial((UInt16)portaSerialCOM_Request.MCRequest, 0); //Não possui valor como argumento!
                    }
                }
            }
            else if (cbxModeloFonte.SelectedIndex == 1) //KEITHLEY
            {
                string current = SendCommand("MEAS:CURR?").Trim();
                if (double.TryParse(current, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double currValue))
                {
                    output_Current = currValue;
                    Console.WriteLine("Corrente medida: " + output_Current.ToString("F4", CultureInfo.InvariantCulture) + " A");
                }
                if(((output_Current > 0.07 && output_Current < 0.15) || ForçarBootMode) && !DLC_Modo_Boot)
                //if (ForçarBootMode && !DLC_Modo_Boot)
                {
                    AppendToTxtInfoSafe("Iniciando processo de gravação. Aguarde alguns segundos...");
                    output_Current = 0;
                    DLC_Modo_Boot = true;
                    TimerForceBootMode.Stop();

                    string D4X_DL_SWSB_File = "D4X_DL000_SWSB_0210.hex";
                    string D4X_ST_SW_File = lblSWName.Text;
                    string cmdCommand = ".\\ALFlasherAll.exe --ecuClass NXP --baud 19200 --txId 0x3C --rxId " +
                                        "0x3D --lin --ram " + D4X_DL_SWSB_File + " --jumpAddr 0x20000004 --writeCode " +
                                        D4X_ST_SW_File + " --crcCode " + D4X_ST_SW_File + " --ecuReset";

                    LOG_TXT("Running command: " + cmdCommand);

                    if (cbkGravaçãoDupla.Checked)
                    {
                        var Channel0_process_double = RunCmdAndStreamOutput(cmdCommand, 0);
                        var Channel1_process_double = RunCmdAndStreamOutput(cmdCommand, 1);
                    }
                    else
                    {
                        var Channel0_process_single = RunCmdAndStreamOutput(cmdCommand, 0);
                    }
                }
            }            
            else if(cbxModeloFonte.SelectedIndex == 3)//AFR
            {
                string current = SendCommand("IOUT1?\\n").Trim();
                if (double.TryParse(current, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double currValue))
                {
                    output_Current = currValue;
                    Console.WriteLine("Corrente medida: " + output_Current.ToString("F3", CultureInfo.InvariantCulture) + " A");
                }
                if (((output_Current > 0.07 && output_Current < 0.15) || ForçarBootMode) && !DLC_Modo_Boot)
                {
                    AppendToTxtInfoSafe("Iniciando processo de gravação. Aguarde alguns segundos...");
                    output_Current = 0;
                    DLC_Modo_Boot = true;
                    TimerForceBootMode.Stop();

                    string D4X_DL_SWSB_File = "D4X_DL000_SWSB_0210.hex";
                    string D4X_ST_SW_File = lblSWName.Text;
                    string cmdCommand = ".\\ALFlasherAll.exe --ecuClass NXP --baud 19200 --txId 0x3C --rxId " +
                                        "0x3D --lin --ram " + D4X_DL_SWSB_File + " --jumpAddr 0x20000004 --writeCode " +
                                        D4X_ST_SW_File + " --crcCode " + D4X_ST_SW_File + " --ecuReset";

                    LOG_TXT("Running command: " + cmdCommand);

                    if (cbkGravaçãoDupla.Checked)
                    {
                        var Channel0_process_double = RunCmdAndStreamOutput(cmdCommand, 0);
                        var Channel1_process_double = RunCmdAndStreamOutput(cmdCommand, 1);
                    }
                    else
                    {
                        var Channel0_process_single = RunCmdAndStreamOutput(cmdCommand, 0);
                    }                    
                }
            }
        }

        async Task BeepIntervalado(int frequencia = 800, int duracao = 200, int intervalo = 500, int repeticoes = 5)
        {
            beepCancellationToken = new CancellationTokenSource();

            await Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < repeticoes; i++)
                    {
                        if (beepCancellationToken.Token.IsCancellationRequested)
                            break;

                        Console.Beep(frequencia, duracao);

                        if (i < repeticoes - 1) // Não aguarda após o último beep
                        {
                            Thread.Sleep(intervalo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LOG_TXT("Erro ao emitir beep: " + ex.Message);
                }
            }, beepCancellationToken.Token);
        }

        void PararBeep()
        {
            if (beepCancellationToken != null)
            {
                beepCancellationToken.Cancel();
            }
        }
    }
}
