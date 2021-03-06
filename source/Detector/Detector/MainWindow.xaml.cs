﻿//Omid55
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Ink;
using System.IO;
using System.ComponentModel;

namespace Detector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private const int MAXSIZE = 200;
        private int _counter = 0;
        private MLApp.MLApp matlab;
        private bool testMode = false;
        private List<MyNumber> data;
        private String inputsX, inputsY, inputsP, targets, testInputX, testInputY, testInputP;
        private int nowResult;
        private int runsCount;
        private int errorCount;

        private BackgroundWorker worker;

        #endregion

        #region Properties

        public int Counter
        {
            get { return _counter; }
            set { CounterLabel.Content = _counter = value; }
        }

        #endregion

        #region Methods

        #region Public Methods

        public MainWindow()
        {
            InitializeComponent();

            init();
        }

        public void myTrainFunction()
        {
            if (String.IsNullOrEmpty(inputsX))
            {
                // load from files
                StreamReader sr = new StreamReader("X.txt");
                inputsX = sr.ReadToEnd();

                sr = new StreamReader("Y.txt");
                inputsY = sr.ReadToEnd();

                sr = new StreamReader("P.txt");
                inputsP = sr.ReadToEnd();

                sr = new StreamReader("T.txt");
                targets = sr.ReadToEnd();

                sr.Dispose();
                sr.Close();
            }

            matlab.Execute(inputsX);
            matlab.Execute(inputsY);
            matlab.Execute(inputsP);
            matlab.Execute(targets);

            matlab.Execute("nowInputs=inputsX;");
            loadMatlabFile("MyOCRProject.m");
            matlab.Execute("netX=net;");

            matlab.Execute("nowInputs=inputsY;");
            loadMatlabFile("MyOCRProject.m");
            matlab.Execute("netY=net;");

            matlab.Execute("nowInputs=inputsP;");
            loadMatlabFile("MyOCRProject.m");
            matlab.Execute("netP=net;");

            loadMatlabFile("CreateCompoundInputsFromResults.m");
            matlab.Execute("nowInputs=resultInputs;");
            loadMatlabFile("MyOCRProject.m");
            matlab.Execute("netResult=net;");
        }

        public void mySimulateFunction()
        {
            matlab.Execute(testInputX);
            matlab.Execute(testInputY);
            matlab.Execute(testInputP);

            loadMatlabFile("MySimulations.m");
            double xRes = (double) matlab.GetVariable("xRes", "base");
            double yRes = (double) matlab.GetVariable("yRes", "base");
            double pRes = (double) matlab.GetVariable("pRes", "base");
            double myResult = (double) matlab.GetVariable("myResult", "base");
            int res = 0;

            int r = (int) myResult;
            if (Math.Abs(r - myResult) < Math.Abs(r + 1 - myResult))
            {
                res = r;
            }
            else
            {
                res = r + 1;
            }

            double errorPerc = (double)100*errorCount/(double)runsCount;

            XResultLabel.Content = (string) FindResource("XResults") + xRes;
            YResultLabel.Content = (string) FindResource("YResults") + yRes;
            PResultLabel.Content = (string) FindResource("PResults") + pRes;
            CResultLabel.Content = (string) FindResource("CResults") + myResult;
            ErrorPercentLabel.Content = (string) FindResource("ErrorPercent") + errorPerc + " %";
            MyResultLabel.Content = (string) FindResource("Result") + res;

            nowResult = res;
        }

        #endregion

        #region Private Methods

        private void init()
        {
            matlab = new MLApp.MLApp();
            matlab.Visible = 0;

            runsCount = errorCount = 0;

            data = new List<MyNumber>();

            ResourceDictionary rd = new ResourceDictionary();
            rd.Source = new Uri("MyResourceDictionary.xaml", UriKind.Relative);
            this.Resources.MergedDictionaries.Add(rd);

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            myTrainFunction();
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show(this.FindResource("TrainMessage") as string, FindResource("TrainTitle") as string,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
            if (
                MessageBox.Show(FindResource("TestModeMessage") as string, FindResource("TestMode") as string,
                                MessageBoxButton.YesNo) ==
                MessageBoxResult.Yes)
            {
                TestButton_Click(null, null);
            }
        }

        private void InkCanvas_Gesture(object sender, InkCanvasGestureEventArgs e)
        {
            try
            {
                StrokeCollection collection = e.Strokes;
                StylusPointCollection points = collection[0].StylusPoints;

                while (points.Count < MAXSIZE)
                {
                    var prev = points[points.Count - 2];
                    var last = points[points.Count - 1];
                    double diffX = last.X - prev.X;
                    double diffY = last.Y - prev.Y;
                    double newX = diffX + last.X;
                    double newY = diffY + last.Y;
                    points.Add(new StylusPoint(newX, newY));
                }

                if (!testMode)
                {
                    int t = Int32.Parse(TargetTextBox.Text);
                    data.Add(new MyNumber(points, t));
                    updateTargetsLabel();

                    Counter++;
                }
                else
                {
                    testInputX = "testInputX=[";
                    testInputY = "testInputY=[";
                    testInputP = "testInputP=[";

                    for (int i = 0; i < points.Count; i++)
                    {
                        testInputX += points[i].X + " ;";
                        testInputY += points[i].Y + " ;";
                        testInputP += points[i].PressureFactor + " ;";
                    }
                    testInputX = testInputX.Remove(testInputX.Length - 1, 1) + "];";
                    testInputY = testInputY.Remove(testInputY.Length - 1, 1) + "];";
                    testInputP = testInputP.Remove(testInputP.Length - 1, 1) + "];";

                    StreamWriter swx = new StreamWriter("XTest.txt");
                    StreamWriter swy = new StreamWriter("YTest.txt");
                    StreamWriter swp = new StreamWriter("PTest.txt");

                    swx.WriteLine(testInputX);
                    swy.WriteLine(testInputY);
                    swp.WriteLine(testInputP);

                    swp.Close();
                    swx.Close();
                    swy.Close();

                    SimulateButton_Click(null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //MessageBox.Show("Ok Data Wrote ...");
        }

        private void updateTargetsLabel()
        {
            var q = (from targ in data
                     select targ.Target.ToString()).Distinct();

            enteredTargets.Content = string.Join(",", q);
        }

        private bool loadMatlabFile(String filename)  // return that commands executes successfully without any errors or not 
        {
            using (
                Stream stream =
                    Assembly.GetExecutingAssembly().GetManifestResourceStream("Detector.MatlabCodes." + filename))
            using (StreamReader sr = new StreamReader(stream))
            {
                string content = sr.ReadToEnd();
                sr.Close();
                string result = matlab.Execute(content);
                if (!String.IsNullOrEmpty(result))
                {
                    MessageBox.Show(result);
                    return false;
                }
            }
            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            inputsX = "inputsX=[";
            inputsY = "inputsY=[";
            inputsP = "inputsP=[";
            targets = "targets=[";

            string[] lastInputsX = null;
            string[] lastInputsY = null;
            string[] lastInputsP = null;
            string lastTargets = "";

            if (mergeCheckBox.IsChecked.Value)
            {
                // we must load last data from files
                StreamReader srx = new StreamReader("X.txt");
                lastInputsX = getStrArray(srx);
                srx.Close();

                StreamReader sry = new StreamReader("Y.txt");
                lastInputsY = getStrArray(sry);
                sry.Close();

                StreamReader srp = new StreamReader("P.txt");
                lastInputsP = getStrArray(srp);
                srp.Close();

                StreamReader srt = new StreamReader("T.txt");
                lastTargets = getMyStr(srt);
                srt.Close();
            }

            for (int j = 0; j < data.Count; j++)
            {
                targets += data[j].Target + " ";
            }
            targets += lastTargets + "];";


            for (int i = 0; i < MAXSIZE; i++)
            {
                for (int j = 0; j < data.Count; j++)
                {
                    inputsX += data[j].Points[i].X + " ";
                    inputsY += data[j].Points[i].Y + " ";
                    inputsP += data[j].Points[i].PressureFactor + " ";
                }

                if (mergeCheckBox.IsChecked.Value)
                {
                    // we must merge now data with before data in files
                    inputsX += lastInputsX[i];
                    inputsY += lastInputsY[i];
                    inputsP += lastInputsP[i];
                }

                inputsX += ";";
                inputsY += ";";
                inputsP += ";";
            }
            inputsX = inputsX.Remove(inputsX.Length - 1, 1) + "];";
            inputsY = inputsY.Remove(inputsY.Length - 1, 1) + "];";
            inputsP = inputsP.Remove(inputsP.Length - 1, 1) + "];";

            StreamWriter swx = new StreamWriter("X.txt");
            StreamWriter swy = new StreamWriter("Y.txt");
            StreamWriter swp = new StreamWriter("P.txt");
            StreamWriter swt = new StreamWriter("T.txt");

            swx.WriteLine(inputsX);
            swy.WriteLine(inputsY);
            swp.WriteLine(inputsP);
            swt.WriteLine(targets);

            swp.Close();
            swx.Close();
            swy.Close();
            swt.Close();

            updateTargetsLabelFromTargets();
        }

        private void updateTargetsLabelFromTargets()
        {
            string str = targets;
            str = str.Substring(str.IndexOf('[') + 1);
            str = str.Remove(str.IndexOf(']'));
            string[] ts = str.Split(' ');
            var q = (from targ in ts
                     select targ).Distinct();

            enteredTargets.Content = string.Join(",", q);
        }

        private string getMyStr(StreamReader sr)
        {
            string str = sr.ReadToEnd();
            str = str.Substring(str.IndexOf('[') + 1);
            str = str.Remove(str.IndexOf(']'));
            return str;
        }

        private string[] getStrArray(StreamReader sr)
        {
            string str = getMyStr(sr);
            return str.Split(';');
        }

        //private void saveAndCloseFile(StreamWriter f)
        //{
        //    f.Flush();
        //    f.Dispose();
        //    f.Close();
        //}

        private void SaveClearButton_Click(object sender, RoutedEventArgs e)
        {
            SaveButton_Click(null, null);
            data.Clear();
        }

        private void TrainButton_Click(object sender, RoutedEventArgs e)
        {
            worker.RunWorkerAsync();
        }

        private void SimulateButton_Click(object sender, RoutedEventArgs e)
        {
            mySimulateFunction();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            ModeLabel.Content = FindResource("TestMode");
            testMode = true;
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            nowAdaptIt(nowResult.ToString());

            runsCount++;

            double errorPerc = (double)100 * errorCount / (double)runsCount;
            ErrorPercentLabel.Content = (string)FindResource("ErrorPercent") + errorPerc + " %";
        }

        private void nowAdaptIt(string res)
        {
            matlab.Execute("nowTarget=" + res + ";");
            if (loadMatlabFile("MyAdaptations.m"))
            {
                MessageBox.Show((string)FindResource("AdaptMessage"), (string)FindResource("AdaptTitle"),
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
        }

        private void InkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            MyColorOffset.Offset = e.GetPosition(MyInkCanvas).X/MyInkCanvas.ActualWidth;
            MyLinearGradientBrush.StartPoint = new Point(e.GetPosition(MyInkCanvas).X/MyInkCanvas.ActualWidth,
                                                         e.GetPosition(MyInkCanvas).Y/MyInkCanvas.ActualHeight);
        }

        private void MyInkCanvas_TouchMove(object sender, TouchEventArgs e)
        {
            MyColorOffset.Offset = e.GetTouchPoint(MyInkCanvas).Position.Y/MyInkCanvas.ActualHeight;
        }

        private void AdaptItButton_Click(object sender, RoutedEventArgs e)
        {
            nowAdaptIt(AdaptWithTextBox.Text);

            runsCount++;
            errorCount++;

            double errorPerc = (double)100 * errorCount / (double)runsCount;
            ErrorPercentLabel.Content = (string)FindResource("ErrorPercent") + errorPerc + " %";
        }

        private void NoButton_MouseEnter(object sender, MouseEventArgs e)
        {
            popLink.IsOpen = true;
            popLink.StaysOpen = true;
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            popLink.IsOpen = true;
        }

        private void popLink_MouseLeave(object sender, MouseEventArgs e)
        {
            popLink.StaysOpen = false;
        }

        private void AdaptWithTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AdaptItButton_Click(null, null);
            }
        }

        #endregion

        #endregion

    }
}