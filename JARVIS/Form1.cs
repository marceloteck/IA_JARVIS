﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks; // tasking
using System.Windows.Forms;
using Microsoft.Speech.Recognition; // namespace usado pra o reconhecimento de voz, Microsoft Speech Server 11 SDK pt-BR
using System.Threading; // namespace para threading
using System.IO; // namespace para E/S de arquivos
using System.Diagnostics; // diágnosticos
using System.Web;
using JARVIS.Listas;
using System.Web.UI;
using JARVIS.Class_Conversas.Listas;
using JARVIS.Class_Conversas;
using JARVIS.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using JARVIS.Reconhecimento;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;

namespace JARVIS
{
    public partial class Form1 : Form
    {
        // Forms
        public static WebLoader webLoader = null;
        public static ProcessList processList = null;
        public static AppsDialog appsDialog = null;
        public LoadingSystem loadingSystem = null;
        public SelectVoice selectVoice = null;
        public MotionDetection motionDetection = null;
        public PersonIdentifier personId = null;

        public SpeechRecognitionEngine sre; // declarando o reconhecedor

        // Aqui vão ficar alguns objetos básicos
        public string[] voices = GetVoices.GetVoicesFromCulture("pt-BR"); // pegar vozes em português

        public Dictionary<string, string> dictCmdSites = new Dictionary<string, string>(); // dicionário dos comandos

        public bool speechRecognitionActived = false; // booleana do reconhecimento

        public bool comboBoxVisible = true;
        public static Random rnd = new Random(); // rand

        // array de notícias
        public G1NewsItem[] newsFromG1;
        public int newsIndex = 0;


        // Método pra setar a voz
        public void SetVoice() // percorrer vozes e tentar setar a primeira
        {
            int counter = 0; // contador
            if (voices.Length == 0)
                MessageBox.Show("Desculpe, nenhuma voz em português SAPI  foi encontrada, tente instalar uma e tente novamente!");
            while (counter < voices.Length) // enquanto contador for menor que o array de vozes
            {
                try
                {
                    Speaker.SetVoice(voices[counter]); // tentar setar a voz pelo índice
                    break; // ocorreu bem, então vamos sair do loop
                }
                catch (Exception ex) // deu mal
                {
                    MessageBox.Show(ex.Message); // vamos mostrar o erro pelo menos né!
                    counter++; // incrementa o índice da voz
                }
            }
        }

   
        private void LoadSpeechRecognition() // fazer o que é preciso para o reconhecimento de voz
        {
            try
            {

                //Speaker.Speak("Estou carregando meu nucleo");
                sre = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("pt-BR")); // instanciando  o reconhecedor passando a cultura da engine
                sre.SetInputToDefaultAudioDevice(); // definindo o microfone como entrada de aúdio
            }
            catch (Exception ex) // deu mal
            {
                string es = ex.Message;
                msgErroRec(es);
            }


            Choices cControls = new Choices();
				cControls.Add(cControlsList.Controlls.ToArray());

				// Alarme
				Choices cAlarm = new Choices();
				for (int i = 1; i <= 12; i++)
					cAlarm.Add(i.ToString());

				// Criação das Gramáticas, Choices
				Choices cChats = new Choices(); // palavras ou frases de conversa
				cChats.Add(cChatsList.Conversa.ToArray());
				cChats.Add(InternoComands.ConversaSpeech.ToArray());
				


				Choices cDummes = new Choices(); // conversa mais desenrolada
				cDummes.Add(DummeIn.InStartingConversation.ToArray());
				cDummes.Add(DummeIn.InQuestionForDumme.ToArray());
				cDummes.Add(DummeIn.InDoWork.ToArray());
				cDummes.Add(DummeIn.InDummeStatus.ToArray());
				cDummes.Add(DummeIn.InJarvis.ToArray());

				Choices cCommands = new Choices(); // palavras ou frases que são comandos
				
				// informações de hora e data
				cCommands.Add(DataHora.QueHoras.ToArray());
				cCommands.Add(DataHora.QueData.ToArray());
				cCommands.Add(DataHora.QueDia.ToArray());
				cCommands.Add(DataHora.QueDiaSemana.ToArray());
				cCommands.Add(DataHora.QueMes.ToArray());
				cCommands.Add(DataHora.QueAno.ToArray());



                #region comandos
                /*cCommands.Add("que dia é hoje");
				cCommands.Add("data de hoje");
				cCommands.Add("em que mês estamos");
				cCommands.Add("em que ano estamos");*/

                // Comandos do programa
                cCommands.Add(InternoComands.LsdeComands.ToArray());

                // configurar o sintetizador
                cCommands.Add(InternoComands.PareFalar.ToArray());


                cCommands.Add(InternoComands.divers.ToArray());
                
                #endregion

                // Define as opções de reconhecimento de fala
                Choices opcoes = new Choices();
				opcoes.Add(ComandsLotesG.ListLote.ToArray());
				

				//Choices cNumbers = new Choices(File.ReadAllLines("n.txt")); // números
				#region Process Choices
				Choices cProcess = new Choices(); // lista de comandos
				cProcess.Add(cProcessWin.ProgramasWin.ToArray()) ;
				cProcess.Add(WebList.ComandsWEB.ToArray()) ;
				cProcess.Add(ShellList.ShellListComands.ToArray()) ;
				#endregion

				//cCalculations
			   /* Choices cCalculations = new Choices(); // lista de comandos
				cCalculations.Add(DiversosComands.Number.ToArray());*/


				Choices cCustomSites = new Choices(); // lista de comandos do usuário

				#region GrammarBuilders
				// Gramática do alarme
				GrammarBuilder gbAlarm = new GrammarBuilder();
				gbAlarm.Append(new Choices("defina alarme", "alarme às", "despertador às"));
				gbAlarm.Append(cAlarm);
				gbAlarm.Append(new Choices("horas da manhã", "horas da tarde", "horas da noite"));

				// GrammarsBuilders
				GrammarBuilder gbChats = new GrammarBuilder(); // vamos criar um grammaBuilder para as conversas
				gbChats.Append(cChats); // já foi feito

				GrammarBuilder gbDumme = new GrammarBuilder(); // conversa solta
				gbDumme.Append(cDummes); 

				GrammarBuilder gbCommands = new GrammarBuilder(); //para a lista de comandos
				gbCommands.Append(cCommands); // feito

				GrammarBuilder gbControls = new GrammarBuilder();
				gbControls.Append(cControls);

				GrammarBuilder gbLote = new GrammarBuilder();
				gbLote.Append(new Choices(ComandsLotesG.ChoicesArrList.ToArray())); // comando
				gbLote.Append(opcoes);

				GrammarBuilder gbProcess = new GrammarBuilder();
				//gbProcess.Append(new Choices("abrir", "abra", "abre", "fechar", "feche")); // comando
				gbProcess.Append(new Choices(InternoComands.Exec.ToArray())); // comando
				gbProcess.Append(cProcess);
				
				/*GrammarBuilder gbCalculations = new GrammarBuilder();
				gbCalculations.Append(new Choices("quanto é", "calcule", "matemática")); // comando
				gbCalculations.Append(cCalculations); // adicionar lista de processos*/

				#endregion
				// Grammars'

				#region Grammars
				Grammar gChats = new Grammar(gbChats); // gramática das conversas
				gChats.Name = "Chats"; // damos um nome para a gramática, pois vamos usa isso mais adiante

				Grammar gDumme = new Grammar(gbDumme);
				gDumme.Name = "Dumme"; // nome

				Grammar gCommands = new Grammar(gbCommands); // gramática dos comandos
				gCommands.Name = "Commands"; // nome 

				Grammar gProcess = new Grammar(gbProcess);
				gProcess.Name = "Process";
				// Agora vamos carregar as gramáticas

				
				Grammar gControls = new Grammar(gbControls);
				gControls.Name = "Control";

				Grammar LoteGramar = new Grammar(gbLote);
				LoteGramar.Name = "ComandsLote"; // nome

				/*Grammar gCalculations = new Grammar(gbCalculations);
				gCalculations.Name = "Calculations";*/

				#endregion
				// podemos fazer de várias maneiras, por enquanto vou fazer o seguinte
				// Lista de gramáticas 

				#region List of Grammars
				List<Grammar> grammars = new List<Grammar>();
				grammars.Add(gChats);
				grammars.Add(gDumme);
				grammars.Add(gCommands); // comandos
				grammars.Add(gControls);
				//grammars.Add(gCalculations);
				grammars.Add(gProcess);
				grammars.Add(LoteGramar);
				
				ParallelOptions op = new ParallelOptions() { MaxDegreeOfParallelism = 4 };
				Parallel.For(0, grammars.Count, op, i => // loop paralelo
					{
						sre.LoadGrammar(grammars[i]); // carregar gramática
					});

                #endregion
            try { 
				#region SpeechEngine Events
				speechRecognitionActived = true; // reconhecimento de voz ativo!
				sre.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(reconhecido); // evento do reconhecimento
				sre.AudioLevelUpdated += new EventHandler<AudioLevelUpdatedEventArgs>(audioElevou); // quando o aúdio é elevadosre
				sre.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(rejeitado); // quando o reconhecimento de voz falhou
				sre.SpeechDetected += new EventHandler<SpeechDetectedEventArgs>(vozDetectada); // alguma voz foi detectada
				sre.LoadGrammarCompleted += new EventHandler<LoadGrammarCompletedEventArgs>(loaded); // gramática carregada
				sre.RecognizeAsync(RecognizeMode.Multiple); // iniciar o reconhecimento async e múltiplo

				#endregion
			
			}catch (Exception ex)
            {
                string es = ex.Message;
                msgErroRec(es);
            }
			
        }

        public static void msgErroRec(string ex)
        {
            MessageBox.Show("Não consigo fazer reconhecimento de voz devido a esse erro:" + ex);
            MessageBox.Show("Estou abrindo o chat para que possamos conversar");
            chatBotMsg msgBot = new chatBotMsg();
            msgBot.Show();
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            // Seu código aqui
            //Speaker.Speak("Tudo pronto, já pode falar");
        }

        // Método chamado quando o Form for carregado
        private void Form1_Load(object sender, EventArgs e)
        {
            //AIML.ConfigAIMLFiles(); // Configura os arquivos AIML

            Thread.CurrentThread.Priority = ThreadPriority.Highest; // setamos o nível de prioridade da thread atual
            
            // trackBar
            trackBar1.Maximum = 20;
            trackBar1.Value = 10;
            Speaker.SetVolume(100);

            // detalhes do computador
            //lblInfo.Text += SystemInfo.GetUserNameString() + "\n";
            //lblInfo.Text += SystemInfo.GetOSArchString() + "\n";

            // Aqui vamos cuidar dos elementos visuais
            progressBar1.Maximum = 100; // valor máximo da progressBar
            pictureBox1.Visible = false; // não vai mostrar a pictureBox1, a que vai ser usada para mostrar o status
            progressBarCPUUsage.Maximum = 100;

            SetVoice(); // chamar o método para setar a voz

            LoadSpeechRecognition(); // chamar o método que vai fazer o reconhecimento

            // Saudação
            InitializingProgram.Start();

            //loadingSystem = new LoadingSystem();
            //loadingSystem.ShowDialog(); // mostrat diálogo
            //Speaker.Speak("olá senhor, em que posso ajudar?"); // falar algo

            timer2.Tick += timer2_Tick; // timer do evento das progressBar's 1,5 segundos de intervalo
        }

        // Método do evento do reconhecimento
        private void reconhecido(object s, SpeechRecognizedEventArgs e) // passamos a classe EventArgs SpeechRecognized
        {
            var EResult = e.Result;
            string speech = EResult.Text; // criamos uma variável que contêm a palavra ou frase reconhecida
            double confidence = EResult.Confidence; // criamos uma variável para a confiança
            string EGramar = e.Result.Grammar.Name;

            RcLabel.Text = "Rec:\n" + Math.Round(confidence, 2);

            if (confidence > 0.4)// pegar o resultado da confiança, se for maior que 40% faz algo
            {
                label1.Text = "Reconhecido:\n" +  speech; // mostrar o que foi reconhecido

                Rec rec = new Rec();
                rec.reconhecimento(speech, EGramar);

                #region Antigo switch
                /*
                 switch (e.Result.Grammar.Name) // vamos usar o nome da gramática para executar as ações
                 {
                     case "Chats": // caso for uma conversa
                         Conversation.SaySomethingFor(speech); // vamos usar a classe que faz algo sobre
                         break;

                     case "Dumme":
                         if (DummeIn.InStartingConversation.Any(x => x == speech))
                         {
                             int randIndex = rnd.Next(0, DummeOut.OutStartingConversation.Count);
                             Speaker.Speak(DummeOut.OutStartingConversation[randIndex]);
                         }
                         else if (DummeIn.InQuestionForDumme.Any(x => x == speech))
                         {
                             int randIndex = rnd.Next(0, DummeOut.OutQuestionForDumme.Count);
                             Speaker.Speak(DummeOut.OutQuestionForDumme[randIndex]);
                         }
                         else if(DummeIn.InDoWork.Any(x => x == speech))
                         {
                             int randIndex = rnd.Next(0, DummeOut.OutDoWork.Count);
                             Speaker.Speak(DummeOut.OutDoWork[randIndex]);
                         }
                         else if(DummeIn.InDummeStatus.Any(x => x == speech))
                         {
                             int randIndex = rnd.Next(0, DummeOut.OutDummeStatus.Count);
                             Speaker.Speak(DummeOut.OutDummeStatus[randIndex]);
                         }
                         else if (DummeIn.InJarvis.Any(x => x == speech))
                         {
                             int randIndex = rnd.Next(0, DummeOut.OutJarvis.Count);
                             Speaker.Speak(DummeOut.OutJarvis[randIndex]);
                         }
                         break;
                     case "Commands":
                         switch (speech)
                         {
                             case "quais são as notícias":
                                 newsFromG1 = G1FeedNews.GetNews();
                                 Speaker.Speak("Já carreguei as notícias");
                                 break;
                             case "próxima notícia":
                                 Speaker.Speak("Título da notícia.. " + newsFromG1[newsIndex].Title
                                     + " .. " + newsFromG1[newsIndex].Text);
                                 newsIndex++;
                                 break;
                         }
                         if (speech == "até mais jarvis")
                         {
                             ExitNow(); // chama oo método
                         }
                         else if (speech == "minimizar a janela principal")
                         {
                             MinimizeWindow(); // minimizar
                         }
                         else if (speech == "mostrar janela principal")
                         {
                             BackWindowToNormal(); // mostrar janela principal
                         }
                         else
                         {
                             Commands.Execute(speech);
                         }
                         break;
                     case "Jokes":
                         break;
                     case "Calculations":
                         Calculations.DoCalculation(speech);
                         break;
                     case "Process":
                         ProcessControl.OpenOrClose(speech);
                         break;
                     case "Control":
                         motionDetection = new MotionDetection();
                         motionDetection.Show();
                         break;
                     case "ComandsLote":
                         LoteComands.Executar(speech);
                         break;
                     default: // caso padrão
                         Speaker.Speak(AIML.GetOutputChat(speech)); // pegar resposta
                         break;
                 }
                 */
                #endregion

            }
        }






        public void loadPage()
        {
            if (webLoader == null)
            {
                webLoader = new WebLoader();
                webLoader.Show();
            }
            else
            {

            }
        }

        // Método do aúdio elevado
        private void audioElevou(object s, AudioLevelUpdatedEventArgs e) // passamos a classe
        {
            progressBar1.Value = e.AudioLevel; // setando o valor da progressBar igual ao valor do aúdio em percento

            if (speechRecognitionActived == false)
            {
                Speaker.SpeakRand("fale um comando", "Você já pode me pedir para fazer algo!",
                    "estou aqui!");
                speechRecognitionActived = true;
            }
        }

        // Método do erro no reconhecimento
        private void rejeitado(object s, SpeechRecognitionRejectedEventArgs e) // passamos a classe
        {
            pictureBox1.Image = (Bitmap)Bitmap.FromFile("Pictures\\fall.png"); // imagem do erro, carregamos ela
        }

        // Método que será chamado quando a voz for detectada
        private void vozDetectada(object s, SpeechDetectedEventArgs e)
        {
            pictureBox1.Visible = true; // mostrar a pictureBox1
            pictureBox1.Image = (Bitmap)Bitmap.FromFile("Pictures\\ok.png"); // carreganda a imagem
        }

        // Gramática carregada
        int grammars = 0;
        private void loaded(object s, LoadGrammarCompletedEventArgs e)
        {
            grammars++;
            //Speaker.Speak("já foram carregadas " + grammars + " gramáticas.");
        }

        // quando clicar na pictureBox1
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (speechRecognitionActived == true) // se o reconhecimento de voz estiver ativo
            {
                sre.RecognizeAsyncCancel(); // para o reconhecimento
                speechRecognitionActived = false; // altera o valor da booleana
                Speaker.Speak("reconhecimento de voz desativado"); // diz algo
            }
            else if (speechRecognitionActived == false)
            {
                sre.RecognizeAsync(RecognizeMode.Multiple);
                speechRecognitionActived = true;
                Speaker.Speak("reconhecimento de voz ativado");
            }
        }

        // Atualizar dados do sistema
        private bool showMemoryFree = false;
        private void timer1_Tick_1(object sender, EventArgs e)
        {
            if (showMemoryFree == false)
            {
                int memFree = (int)PCStats.GetFreeMemory();
                int memUsage = (int)PCStats.GetTotalMemory() - memFree;
                progressBarMemory.Maximum = (int)PCStats.GetTotalMemory();
                progressBarMemory.Value = memUsage;
                label3.Text = "Memória Usada: " + memUsage + " MB";
                showMemoryFree = true;

                double cpuUsage = PCStats.GetCPUUsage();
                label2.Text = "Uso de CPU: " + Math.Round(cpuUsage, 2) + "%";
                progressBarCPUUsage.Value = Convert.ToInt32(cpuUsage);
            }
            else
            {
                int memFree = (int)PCStats.GetFreeMemory();
                label3.Text = "Memória Livre: " + memFree + " MB";
                progressBarMemory.Maximum = (int)PCStats.GetTotalMemory();
                progressBarMemory.Value = memFree;
                showMemoryFree = false;

                double cpuUsage = PCStats.GetCPUUsage();
                label2.Text = "Uso de CPU: " + Math.Round(cpuUsage, 2) + "%";
                progressBarCPUUsage.Value = Convert.ToInt32(cpuUsage);
            }
        }


        // evento das teclas
        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            Keys k = (Keys)e.KeyChar;

            if(k==Keys.Escape)
            {
                ExitNow(); // cham o método
            }
            else if (e.KeyChar == 'h' || e.KeyChar == 'H')
            {
                JARVISHelp.Introduction(); // introdução
            }
            else if (e.KeyChar == 's' || e.KeyChar == 'S')
            {
                Speaker.StopSpeak(); // parar de falar
            }
            else if (k == Keys.Up)
            {
                Speaker.VolumeUp();
            }
            else if (k == Keys.Down)
            {
                Speaker.VolumeDown();
            }
            else if (e.KeyChar == 'p' || e.KeyChar == 'P')
            {
                Speaker.ResumeOrPause(); // pausa ou resume
            }
            else if (e.KeyChar == 'i' || e.KeyChar == 'I')
            {
                SystemInfo.GetUserName();
                SystemInfo.GetMachineName();
                SystemInfo.GetOSVersion();
                SystemInfo.OSArch();
            }
            else if (e.KeyChar == 't' || e.KeyChar == 'T')
            {
                if (processList == null)
                {
                    ProcessList processList = new ProcessList();
                    processList.Show();
                    processList.ShowProcesses();
                }
                else
                {
                    ProcessList processList = new ProcessList();
                    processList.Show();
                }
            }
            else if (e.KeyChar == 'a' || e.KeyChar == 'A')
            {
                if (appsDialog == null)
                {
                    AppsDialog appsDialog = new AppsDialog();
                    appsDialog.Show();
                }
                else
                {
                    AppsDialog appsDialog = new AppsDialog();
                    appsDialog.Show();
                }
            }
            else if (k==Keys.D5)
            {
                ChangeSkin();
            }
            else if (k == Keys.D6)
            {
                ChangeBackColor();
            }
            else if (k == Keys.D4) 
            {
                if (selectVoice == null) // e for null
                {
                    SelectVoice selectVoice = new SelectVoice();
                    selectVoice.ShowDialog(); // diálogo
                    selectVoice.Close(); // já foi mostrado, então vamos fechar
                    selectVoice = null; // deixando nulo 
                }
            }
            else if (k == Keys.D1)
            {
                MinimizeWindow();
            }
            else if (k == Keys.D2)
            {
                BackWindowToNormal();
            }
            else if (e.KeyChar == 'm' || e.KeyChar == 'M')
            {
                MotionDetection motionDetection = new MotionDetection();
                motionDetection.Visible = false;
                motionDetection.Show();
            }
            else if (e.KeyChar == 'f' || e.KeyChar == 'F')
            {
                PersonIdentifier personId = new PersonIdentifier();
                personId.Show();
                personId.StartRecognition();
            }else if (e.KeyChar == 'a' || e.KeyChar == 'A')
            {
                CommandList listDeComandos = new CommandList();
                listDeComandos.Show();
            }
        }

        // Sair do jarvis 
        public void ExitNow()
        {
            Speaker.SpeakSync("certo, até mais, senhor!"); // diz algo
            this.Close(); // fecha tudo o que é do jarvis
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            /*
            if (showMemoryUsage == false) // se mostrar memória usada for falsa, vasmos mostrar memória livre
            {
                int memUsage = (int)PCStats.GetTotalMemory() - (int)PCStats.GetFreeMemory();
                label3.Text = "Memória Usada: " + memUsage.ToString() + " MB";
                progressBarMemory.Maximum = (int)PCStats.GetTotalMemory();
                progressBarMemory.Value = memUsage;
                showMemoryUsage = true;

                double cpuUsage = PCStats.GetCPUUsage();
                progressBarCPUUsage.Maximum = 100;
                progressBarCPUUsage.Value = (int)cpuUsage; // uso de CPU
                label2.Text = "Uso de CPU: " + Math.Round(cpuUsage, 2) + "%";
            }
            else
            {
                label3.Text = "Memória Livre: " + PCStats.GetFreeMemory().ToString() + " MB";
                progressBarMemory.Maximum = (int)PCStats.GetTotalMemory();
                progressBarMemory.Value = (int)PCStats.GetFreeMemory();
                showMemoryUsage = false;

                double cpuUsage = PCStats.GetCPUUsage();
                progressBarCPUUsage.Maximum = 100;
                progressBarCPUUsage.Value = (int)cpuUsage; // uso de CPU
                label2.Text = "Uso de CPU: " + Math.Round(cpuUsage, 2) + "%";
            }
            */
            double cpuUsage = PCStats.GetCPUUsage();
            label2.Text = "Uso de CPU: " + Math.Round(cpuUsage, 2) + "%";
            progressBarCPUUsage.Value =  Convert.ToInt32(cpuUsage);
            if (showMemoryFree == false)
            {
                int memFree = (int)PCStats.GetFreeMemory();
                int memUsage = (int)PCStats.GetTotalMemory() - memFree;
                progressBarMemory.Maximum = (int)PCStats.GetTotalMemory();
                progressBarMemory.Value = memUsage;
                label3.Text = "Memória Usada: " + memUsage + " MB";
                showMemoryFree = true;
            }
            else
            {
                int memFree = (int)PCStats.GetFreeMemory();
                label3.Text = "Memória Livre: " + memFree + " MB";
                progressBarMemory.Maximum = (int)PCStats.GetTotalMemory();
                progressBarMemory.Value = memFree;
                showMemoryFree = false;
            }
        }

        public void CloseJarvis()
        {
            this.Close();
        }


        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            trackBar1.Maximum = 20;
            Speaker.SetVolume(trackBar1.Value * 5);
        }

        public void ChangeSkin() // vamos usar async para evitar travamentos
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                Speaker.Speak("selecione uma imagem para aplicar");
                ofd.InitialDirectory = Directory.GetCurrentDirectory() + "\\Themes";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    this.BackgroundImage = (Image)Image.FromFile(ofd.FileName);
                }
            }
            catch (Exception ex)
            {
                Speaker.Speak("erro " + ex.Message);
            }
        }

        public void ChangeBackColor()
        {
            try
            {
                ColorDialog cd = new ColorDialog();
                Speaker.Speak("selecione uma cor para o fundo");
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    this.BackColor = cd.Color;
                }
            }
            catch (Exception ex)
            {
                Speaker.Speak("erro " + ex.Message);
            }
        }

        public void MinimizeWindow() // minimizar janela
        {
            try
            {
                if (this.WindowState == FormWindowState.Normal || this.WindowState == FormWindowState.Maximized) // se a janela estiver normal ou maximizada
                {
                    this.WindowState = FormWindowState.Minimized;
                    Speaker.Speak("minimizando a janela principal");
                }
            }
            catch(Exception ex)
            {
                Speaker.Speak("erro " + ex.Message);
            }
        }

        public void BackWindowToNormal()
        {
            try
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                    Speaker.Speak("tudo bem, voltando ao tamanho normal");
                }
            }
            catch (Exception ex)
            {
                Speaker.Speak("erro " + ex.Message);
            }
        }

        

        private void label5_Click(object sender, EventArgs e)
        {
            Menu menu = new Menu();
            menu.Show();
        }
    }
}
