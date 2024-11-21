using static EH_KTS.MessageStructs;
using Timer = System.Windows.Forms.Timer;

namespace EH_KTS
{
    public partial class Form1 : Form
    {
        private readonly List<MDF> mdfList = new();
        private MDF selectedMDF;
        private bool missileLaunch;
        private Rectangle missileCoordinate;
        private int centerX;
        private int centerY;
        private Task? flareTask;
        private Task? chaffTask;
        Random random;
        Server server;
        Timer timer;

        private int aircraftAltitude = 0;
        private ThreatInfo threatInfo = new ThreatInfo();
        private static readonly string logFilePath = Path.Combine(Application.StartupPath, "LOG Files/logFile.txt");
        private Queue<int> autoQueue = new Queue<int>();
        private Queue<int> manualQueue = new Queue<int>();
        private bool isProgramCompletedSuccesfully = false;
        private bool isProgramRunning = false;
        private bool isManuel;
        public Form1()
        {
            InitializeComponent();
            Server.Log_Record_Txt(LogTypes.ProgramStarted);
            random = new();
            server = new Server(this);
            timer = new();
            timer.Interval = 500;
            timer.Tick += Timer_Tick;

        }

        public void SendLog()
        {
            if (server.IsConnected())
            {

                server.SendLog(File.ReadAllBytes(logFilePath));
                File.WriteAllText(logFilePath, String.Empty);
            }
            else
            {
                MessageBox.Show($"Baðlantý hatasý yüzünden gönderilemedi!");
            }
        }

        private void ButtonConnectionAct_Click(object sender, EventArgs e)
        {
            if (server.IsOpen())
            {
                server.StopServer();
                ButtonConnectionAct.Text = "Baðlantýyý Aktifleþtir";
                ButtonStartFly.Enabled = true;
            }
            else
            {
                if (server.StartServer())
                {
                    ButtonConnectionAct.Text = "Baðlantýyý Kapat";
                    ButtonStartFly.Enabled = false;
                }
            }

        }

        private void CBoxDispensePrograms_SelectedIndexChanged(object sender, EventArgs e)
        {
            TBoxDespensorID.Text = String.Empty;
            TableGrid.Rows.Clear();
            int selectedProgram = ((ComboBox)sender).SelectedIndex;
            if (selectedProgram == -1)
            {
                return;
            }

            MDF mDf = mdfList[CBoxMdfID.SelectedIndex];
            DispenseProgram dispenseProgram = mDf.Dispense_Programs[selectedProgram];
            TBoxDespensorID.Text = "Atýcý - " + dispenseProgram.Dispenser;

            TableGrid.Rows.Add("Chaff", dispenseProgram.Chaff.Salvo_Count, dispenseProgram.Chaff.Salvo_Interval + " ms");
            TableGrid.Rows.Add("Flare", dispenseProgram.Flare.Salvo_Count, dispenseProgram.Flare.Salvo_Interval + " ms");
        }



        private async void TBoxHeigh_Plane()
        {
            ButtonStartFly.Enabled = false;
            ButtonStopFly.Enabled = true;
            ButtonDispenserFill.Enabled = false;
            ButtonConnectionAct.Enabled = false;
            ButtonShoot.Enabled = true;
            ButtonStartDispense.Enabled = true;

            int currentValue = 0;

            while (currentValue < 40000)
            {
                int randomNumber;

                if ((currentValue > 8000 && currentValue < 9000) || (currentValue > 10000 && currentValue < 15000))
                {
                    randomNumber = random.Next(-100, 200);
                    currentValue += randomNumber;

                    if (currentValue < 8000)
                    {
                        currentValue = 8000;
                    }
                    TBoxHeigh.Text = currentValue.ToString();
                    aircraftAltitude = currentValue;
                    await Task.Delay(1500);
                }

                else
                {
                    randomNumber = random.Next(500, 1000);
                    currentValue += randomNumber;

                    if (currentValue > 40000)
                    {
                        currentValue = 40000;
                        TBoxHeigh.Text = currentValue.ToString();
                        await Task.Delay(1000);
                    }

                    TBoxHeigh.Text = currentValue.ToString();
                    aircraftAltitude = currentValue;
                    await Task.Delay(1000);
                }

                if (!ButtonStopFly.Enabled)
                {
                    TBoxHeigh.Text = "0";
                    aircraftAltitude = 0;
                    break;
                }

                Application.DoEvents();

            }
        }

        private void ButtonStartFly_Click(object sender, EventArgs e)
        {
            TBoxHeigh_Plane();
            Server.Log_Record_Txt(LogTypes.FlightStarted);

        }

        private void ButtonStopFly_Click(object sender, EventArgs e)
        {
            Server.Log_Record_Txt(LogTypes.FlightEnd);
            ButtonStopFly.Enabled = false;
            ButtonStartFly.Enabled = true;
            ButtonDispenserFill.Enabled = true;
            ButtonConnectionAct.Enabled = true;
            ButtonShoot.Enabled = false;
            ButtonStartDispense.Enabled = false;
            TBoxHeigh.Text = "0";
            aircraftAltitude = 0;
            TBoxAngleShow.Text = String.Empty;
            TBoxHeighShow.Text = String.Empty;
            TBoxProgramRunning.Text = String.Empty;
            NumericAngle.Value = 0;
            NumericHeigh.Value = 0;
            missileLaunch = false;
            timer.Stop();
            PictureF16.Invalidate();
            LBoxProgramRunnig.Items.Clear();
            LBoxThreatMod.Items.Clear();
            LBoxProgramOrder.Items.Clear();
        }


        private void Picture_F16_Paint(object sender, PaintEventArgs e)
        {
            Graphics circle = e.Graphics;

            centerX = PictureF16.Width / 2;
            centerY = PictureF16.Height / 2;

            int initialRadius = 70;
            int spacing = 25;

            for (int i = 0; i < 5; i++)
            {
                int radius = initialRadius + (i * spacing);

                int x = centerX - radius;
                int y = centerY - radius;

                circle.DrawEllipse(Pens.MediumBlue, x, y, radius * 2, radius * 2);

            }

            string circleText = "20 mi";
            Font font = new Font("Verdana", 8);
            Brush brush = new SolidBrush(Color.MediumBlue);

            SizeF textSize = circle.MeasureString(circleText, Font);
            float textX = centerX + (float)(170 * Math.Cos(0 * Math.PI / 180) - textSize.Width / 2);
            float textY = centerY - (float)(170 * Math.Sin(0 * Math.PI / 180) - textSize.Height / 2);
            circle.DrawString(circleText, font, brush, textX, textY);

            if (missileLaunch)
            {
                circle.FillEllipse(brush, missileCoordinate);
            }

        }


        private void ButtonShoot_Click(object sender, EventArgs e)
        {
            if (CBoxMdfID.SelectedItem != null)
            {
                if (int.TryParse(TBoxHeigh.Text, out int numericValue))
                {
                    if (numericValue >= 8000)
                    {
                        ButtonShoot.Enabled = false;
                        missileLaunch = true;
                        timer.Start();

                        int startAngle;
                        int startHeight;

                        if (CBoxManuel.Checked)
                        {
                            startAngle = (int)NumericAngle.Value;
                            startHeight = (int)NumericHeigh.Value;
                        }
                        else
                        {
                            startAngle = random.Next(0, 360);
                            startHeight = random.Next(0, 17000);
                        }

                        int radius = 170;

                        TBoxAngleShow.Text = startAngle.ToString();
                        TBoxHeighShow.Text = startHeight.ToString();
                        threatInfo.Altitude = startHeight;
                        threatInfo.Angle = startAngle;
                        Server.Log_Record_Txt(LogTypes.AngleAndAltitude, startAngle, startHeight);
                        startAngle = ((90 - startAngle) % 360);

                        missileCoordinate = new Rectangle((int)(centerX + (radius * Math.Cos(startAngle * Math.PI / 180)) - 5),
                           (int)(centerY - (radius * Math.Sin(startAngle * Math.PI / 180)) - 5), 10, 10);

                        LBoxThreatMod.Items.Clear();
                        FindThreatModes();
                    }

                    else
                    {
                        MessageBox.Show("Füze Gönderilemedi!");
                    }
                }
            }
            else
            {
                MessageBox.Show("GVD ID Seçiniz!");
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!missileLaunch)
            {
                timer.Stop();
                return;
            }

            double angle = Math.Atan2(centerY - missileCoordinate.Y, centerX - missileCoordinate.X);
            int speed = 5;

            missileCoordinate.X += (int)(speed * Math.Cos(angle));
            missileCoordinate.Y += (int)(speed * Math.Sin(angle));

            int targetRadius = 120;

            if (Math.Sqrt(Math.Pow(missileCoordinate.X - centerX, 2) + Math.Pow(missileCoordinate.Y - centerY, 2)) <= targetRadius)
            {
                timer.Stop();
                ButtonShoot.Enabled = true;

            }

            PictureF16.Invalidate();
        }

        private void CBoxMdfID_SelectedIndexChanged(object sender, EventArgs e)
        {
            lock (autoQueue)
            {
                autoQueue.Clear();
            }
            lock (manualQueue)
            {
                manualQueue.Clear();
            }
            CBoxDispensePrograms.SelectedIndex = -1;
            CBoxDispensePrograms.Items.Clear();
            if (CBoxMdfID.SelectedIndex < 0)
                return;

            selectedMDF = mdfList[CBoxMdfID.SelectedIndex];
            foreach (DispenseProgram item in selectedMDF.Dispense_Programs)
            {
                if (item.Chaff.Salvo_Count == 0 && item.Flare.Salvo_Count == 0)
                    break;
                CBoxDispensePrograms.Items.Add("Prog - " + item.Program_Number);
            }
            if (CBoxDispensePrograms.Items.Count > 0)
            {
                CBoxDispensePrograms.SelectedIndex = 0;
            }
        }

        public void FindMDFs()
        {
            if (CBoxMdfID.InvokeRequired)
            {
                this.Invoke(new Action(FindMDFs));
                return;
            }
            string? lastSelected = CBoxMdfID.SelectedIndex != -1 ? CBoxMdfID.SelectedItem.ToString() : null;

            CBoxMdfID.Items.Clear();
            string path = Path.Combine(Application.StartupPath, "MDF Files");
            if (Directory.Exists(path))
            {
                string[] paths = Directory.GetFiles(path);

                foreach (var item in paths)
                {
                    byte[] bytes = File.ReadAllBytes(item);
                    if (bytes.Length == 192)
                    {
                        mdfList.Add(ByteArrayToMDF(bytes));
                        CBoxMdfID.Items.Add(Path.GetFileNameWithoutExtension(item));
                    }
                }
            }

            if (lastSelected != null)
            {
                CBoxMdfID.SelectedIndex = CBoxMdfID.FindStringExact(lastSelected);
            }

            if (CBoxMdfID.SelectedIndex == -1)
            {
                CBoxMdfID.SelectedIndex = 0;
            }
        }

        private void ButtonSendLog_Click(object sender, EventArgs e)
        {
            SendLog();
        }

        private void DispenserFill()
        {
            for (int i = 1; i <= 40; i++)
            {
                Panel? chaffPanel = this.Controls.Find("chaff" + i, true).FirstOrDefault() as Panel;
                if (chaffPanel != null)
                {
                    chaffPanel.BackColor = Color.DarkBlue;
                }
            }

            for (int i = 1; i <= 40; i++)
            {
                Panel? flarePanel = this.Controls.Find("flare" + i, true).FirstOrDefault() as Panel;
                if (flarePanel != null)
                {
                    flarePanel.BackColor = Color.LightSteelBlue;
                }
            }
        }

        private void ButtonDispenserFill_Click(object sender, EventArgs e)
        {
            DispenserFill();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            FindMDFs();
            DispenserFill();
            backgroundWorker2.RunWorkerAsync();
        }


        private async Task ChaffStart(DispenseProgram dispenseProgram)
        {
            //isProgramCompletedSuccesfully = true;

            int chaffCount = dispenseProgram.Chaff.Salvo_Count;
            int chaffInterval = dispenseProgram.Chaff.Salvo_Interval;
            int dispenserID = dispenseProgram.Dispenser;

            for (int j = 0; j < 4; j++)
            {
                for (int i = 1; i <= 10 && chaffCount > 0; i++)
                {
                    Panel? chaffPanel = this.Controls.Find("chaff" + (i + dispenserID * 10), true).FirstOrDefault() as Panel;
                    if (chaffPanel != null)
                    {
                        if (chaffPanel.BackColor == Color.DarkBlue)
                        {
                            chaffPanel.BackColor = Color.White;
                            chaffCount--;
                            await Task.Delay(chaffInterval);
                        }

                    }

                }
                dispenserID = (dispenserID + 1) % 4;
            }
            if (chaffCount > 0)
            {
                isProgramCompletedSuccesfully = false;
            }
        }

        private async Task FlareStart(DispenseProgram dispenseProgram)
        {
            isProgramCompletedSuccesfully = true;

            int flareCount = dispenseProgram.Flare.Salvo_Count;
            int flareInterval = dispenseProgram.Flare.Salvo_Interval;
            int dispenserID = dispenseProgram.Dispenser;

            for (int j = 0; j < 4; j++)
            {
                for (int i = 1; i <= 10 && flareCount > 0; i++)
                {
                    Panel? flarePanel = this.Controls.Find("flare" + (i + dispenserID * 10), true).FirstOrDefault() as Panel;
                    if (flarePanel != null)
                    {
                        if (flarePanel.BackColor == Color.LightSteelBlue)
                        {
                            flarePanel.BackColor = Color.White;
                            flareCount--;
                            await Task.Delay(flareInterval);
                        }

                    }

                }
                dispenserID = (dispenserID + 1) % 4;
            }
            if (flareCount > 0)
            {
                isProgramCompletedSuccesfully = false;
            }
        }

        private async void StartDispense(DispenseProgram dispenseProgram)
        {
            if (int.TryParse(TBoxHeigh.Text, out int numericValue))
            {
                if (numericValue >= 8000)
                {
                    if ((flareTask != null && !flareTask.IsCompleted) || (chaffTask != null && !chaffTask.IsCompleted))
                    {

                        MessageBox.Show("Ýþlem hala devam ediyor.");
                        return;
                    }

                    try
                    {
                        flareTask = FlareStart(dispenseProgram);
                        chaffTask = ChaffStart(dispenseProgram);
                        isProgramRunning = true;

                        LBoxProgramRunnig_Update(dispenseProgram);
                        await Task.WhenAll(flareTask, chaffTask);

                        if (isProgramCompletedSuccesfully)
                        {
                            missileLaunch = false;
                            timer.Stop();
                            PictureF16.Invalidate();

                            MessageBox.Show("Program Baþarýlý");
                        }

                        else
                        {
                            MessageBox.Show("Program Baþarýsýz");
                        }


                        if (LBoxProgramRunnig.InvokeRequired)
                        {
                            LBoxProgramRunnig.Invoke(new Action(() => LBoxProgramRunnig.Items.Clear()));
                            TBoxProgramRunning.Invoke(new Action(() => TBoxProgramRunning.Text = ""));
                            LBoxThreatMod.Invoke(new Action(() => LBoxThreatMod.Items.Clear()));
                        }


                        isProgramRunning = false;

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Bir hata oluþtu: GVD ID Seçiniz! " + ex.Message);
                    }
                }

                else
                {
                    MessageBox.Show("Mühimmat Gönderilemedi!");
                }
            }
        }

        private void ButtonStartDispense_Click(object sender, EventArgs e)
        {
            if (CBoxMdfID.InvokeRequired)
            {
                this.Invoke(new Action(FindMDFs));
                return;
            }

            if (CBoxMdfID.SelectedIndex < 0)
            {
                MessageBox.Show("GVD Seçiniz!");
                return;
            }

            MDF mDF = mdfList[CBoxMdfID.SelectedIndex];
            DispenseProgram selectedDispense = mDF.Dispense_Programs[CBoxDispensePrograms.SelectedIndex];
            Server.Log_Record_Txt(LogTypes.DispenceProgram, ID: (int)selectedDispense.Program_Number);
            lock (manualQueue)
                manualQueue.Enqueue(selectedDispense.Program_Number);

            LBoxProgramOrder.Items.Add("Prog ID-" + selectedDispense.Program_Number + ", MANUEL");


        }

        private void LBoxProgramRunnig_Update(DispenseProgram dispenseProgram)
        {

            if (TBoxProgramRunning.InvokeRequired)
            {
                if (!isManuel)
                {
                    TBoxProgramRunning.Invoke(new Action(() => TBoxProgramRunning.Text = "Prog - " + dispenseProgram.Program_Number + ", AUTO"));
                }
                else
                {
                    TBoxProgramRunning.Invoke(new Action(() => TBoxProgramRunning.Text = "Prog - " + dispenseProgram.Program_Number + ", MANUEL"));
                }

            }



            if (LBoxProgramRunnig.InvokeRequired)
            {
                LBoxProgramRunnig.Invoke(new Action(() => LBoxProgramRunnig.Items.AddRange(new string[] {
                    "Prog ID - " + dispenseProgram.Program_Number,
                    "Atýcý - " + dispenseProgram.Dispenser,"",
                    "-Chaff Tekniði-", "Salvo Sayýsý: " + dispenseProgram.Chaff.Salvo_Count +"", "Salvo Aralýðý: " , dispenseProgram.Chaff.Salvo_Interval + " ms","",
                    "-Flare Tekniði-", "Salvo Sayýsý: " + dispenseProgram.Flare.Salvo_Count +"", "Salvo Aralýðý: " , dispenseProgram.Flare.Salvo_Interval + " ms"
                })
             ));

            }

        }

        private void FindThreatModes()
        {
            if (CBoxMdfID.SelectedItem != null)
            {
                MDF mdf = mdfList[CBoxMdfID.SelectedIndex];
                bool isHigh = aircraftAltitude < threatInfo.Altitude;
                int sectorId = 3;

                if (threatInfo.Angle >= mdf.Sectors[0] && threatInfo.Angle < mdf.Sectors[1])
                {
                    sectorId = 0;
                }
                else if (threatInfo.Angle >= mdf.Sectors[1] && threatInfo.Angle < mdf.Sectors[2])
                {
                    sectorId = 1;
                }
                else if (threatInfo.Angle >= mdf.Sectors[2] && threatInfo.Angle < mdf.Sectors[3])
                {
                    sectorId = 2;
                }

                List<ThreatMode> suitableModes = mdf.ThreatModes.Where(x => x.GetAltitude() == isHigh && x.GetSelectedSectors()[sectorId]).ToList();
                List<DispenseProgram> dispensePrograms = suitableModes.Select(x => mdf.Dispense_Programs.First(d => d.Program_Number == x.Dispense_Program_Number)).ToList();

                if (suitableModes.Count == 0)
                {
                    MessageBox.Show("Tehdit Modu Bulunamadý.Karþý Tedbir Baþarýsýz!");
                    Server.Log_Record_Txt(LogTypes.Success, success: 0);
                }
                else
                {
                    Server.Log_Record_Txt(LogTypes.Success, success: 1);
                }

                foreach (ThreatMode item in suitableModes)
                {
                    autoQueue.Enqueue(item.Dispense_Program_Number);


                    LBoxProgramOrder.Items.Add("Prog ID-" + item.Dispense_Program_Number + ", AUTO");


                    Server.Log_Record_Txt(LogTypes.ThreatMode, ID: (int)item.Id);
                    LBoxThreatMod.Items.AddRange(new string[] {
                        "Tehdit Mod ID: " + item.Id,
                        "Prog - " + item.Dispense_Program_Number,""
                        });

                    break;
                }


                Console.WriteLine("");
            }
            else
            {
                MessageBox.Show("GVD ID Seçiniz!");
            }
        }

        private void backgroundWorker2_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            while (true)
            {
                if (backgroundWorker2.CancellationPending)
                {
                    break;
                }

                bool autoProgExist = false;
                lock (autoQueue)
                {
                    if (autoQueue.Count > 0)
                    {
                        isManuel = false;
                        var programNumber = autoQueue.Dequeue();
                        if (LBoxProgramOrder.InvokeRequired)
                        {
                            LBoxProgramOrder.Invoke(new Action(() => LBoxProgramOrder.Items.Remove("Prog ID-" + programNumber + ", AUTO")));
                        }

                        StartDispense(selectedMDF.Dispense_Programs.First(dp => dp.Program_Number == programNumber));

                        autoProgExist = true;

                    }

                }
                while (isProgramRunning)
                {
                    Thread.Sleep(5);
                }

                if (!autoProgExist)
                {
                    isManuel = true;
                    lock (manualQueue)
                    {
                        if (manualQueue.Count > 0)
                        {
                            var programNumber = manualQueue.Dequeue();
                            if (LBoxProgramOrder.InvokeRequired)
                            {
                                LBoxProgramOrder.Invoke(new Action(() => LBoxProgramOrder.Items.Remove("Prog ID-" + programNumber + ", MANUEL")));
                            }

                            StartDispense(selectedMDF.Dispense_Programs.First(dp => dp.Program_Number == programNumber));

                            autoProgExist = true;

                        }
                    }

                    while (isProgramRunning)
                    {
                        Thread.Sleep(5);
                    }
                }
            }
        }

       
    }
}


