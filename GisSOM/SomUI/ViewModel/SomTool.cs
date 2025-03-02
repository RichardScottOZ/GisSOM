﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using SomUI.Model;
using SomUI.Service;
using NLog;
using System.Runtime.CompilerServices;
using System.Net;
using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;
using System.ComponentModel;
using System.Windows.Controls;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace SomUI.ViewModel
{
    /// Python process executables are launched from this VM. Most of the UI bound properties and interaction logic is handled by SomViewModel.
    public class SomTool //: ViewModelBase, INotifyPropertyChanged
    {
        //public int runningProcessCount = 0;
        //public string flyOutText = "";
        //public bool statusFlyOutOpen = false;
        public string pythonLogText = "";
        private readonly IDialogService dialogService;
        //public string browserToolTip = ""; 
        public ImageSource dataHistogram;
        public bool isBusy = false;
        private readonly ILogger logger = NLog.LogManager.GetCurrentClassLogger();
        public string pythonPath = "C:/Users/shautala/AppData/Local/Programs/Python/Python37/pythonw.exe"; // used for debugging.
        public bool usePyExes = true;//for switching running of scripts between packed python executables and full python installation. used for debugging.
                                     

        public event PropertyChangedEventHandler PropertyChanged;
        public SomTool()
        {
            this.logger = NLog.LogManager.GetCurrentClassLogger();
            if (File.Exists(Path.Combine(System.IO.Path.GetTempPath(), "GisSOM", "settingsFile.txt")))
            {
                pythonPath = File.ReadAllText(Path.Combine(System.IO.Path.GetTempPath(), "GisSOM", "settingsFile.txt"));
            }
        }
        public async Task SplitLrnFile(SomModel Model, Action<Process> ScriptOutput, Action<Process> ScriptError) 
        {
                
                PythonLogText = "";

                var scriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "SplitToColumns.py");
                var executablepath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "SplitToColumns.exe");

                ProcessStartInfo myProcessStartInfo;
                if (usePyExes)
                    myProcessStartInfo = new ProcessStartInfo(executablepath);
                else
                    myProcessStartInfo = new ProcessStartInfo(pythonPath);


                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.RedirectStandardError = true;                                                                          
                if (usePyExes)
                    myProcessStartInfo.Arguments = "\"" + Model.InputFile + "\"" + " " + "\"" + Model.Output_Folder + "\"";
                else
                    myProcessStartInfo.Arguments = "\"" + scriptPath + "\"" + " " + "\"" + Model.InputFile + "\"" + " " + "\"" + Model.Output_Folder + "\"";
                myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");
                using (var myProcess = new Process())
                {
                    myProcess.StartInfo = myProcessStartInfo;
                    ScriptOutput(myProcess);
                    ScriptError(myProcess);
                    myProcess.Start();
                //StreamReader myStreamReader = myProcess.StandardOutput;
                //StreamReader errorReader = myProcess.StandardError;
                //string errors = errorReader.ReadToEnd();
                //string returnValue = myStreamReader.ReadLine();
                //if (returnValue != null)
                //    Model.NoDataValue = returnValue;
                myProcess.BeginErrorReadLine();
                myProcess.BeginOutputReadLine();
                myProcess.WaitForExit();
                    myProcess.Close();
                //    if (errors != "" && !errors.Contains("Warning")) //Make a more robust solution later
                //    {                      
                //        dialogService.ShowNotification("Failed to read data. See the log file for details", "Error");    
                //    }
                //if (errors != "")
                //{
                //    PythonLogText += errors + "\r\n";
                //    logger.Error(errors);//throw ex

                //}

                };
                var SomViewModel = ServiceLocator.Current.GetInstance<SomViewModel>();
                SomViewModel.SelectedColumnIndex = -1;
                SomViewModel.SelectedColumnIndex = 2;
                
          
            
        }

        /// <summary>
        /// Draw histogram of the data column selected in the GUI data preparation stage.
        /// </summary>
        public async Task<ImageSource> DrawHistogram(SomModel Model, int SelectedColumnIndex, bool IsSelectedNorthing, bool IsSelectedEasting) 
        {

            if (SelectedColumnIndex > -1)
            {
                var BitMapPath = string.Empty;
                var scriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "DrawSomHistogram.py");
                string inputFile;
                var dataPrepFolder = Path.Combine(Model.Output_Folder, "DataPreparation");

                if (File.Exists(Path.Combine(dataPrepFolder, ("outfile" + SelectedColumnIndex + "_edited.npy")))) 
                {
                    inputFile = Path.Combine(dataPrepFolder, ("outfile" + SelectedColumnIndex + "_edited.npy"));
                }
                else
                    inputFile = Path.Combine(dataPrepFolder, ("outfile" + SelectedColumnIndex + ".npy"));

                //if (!File.Exists(scriptPath))
                //  dialogService.ShowNotification("Python script not found", "Error");

                if (!File.Exists(inputFile))
                    inputFile = Path.Combine(dataPrepFolder, "outfile2.npy"); //defaults to column index 2 (usually "first data column")                                                                        
                if (!File.Exists(inputFile))
                {
                    PythonLogText += "Input file not found";
                    logger.Trace("Input file not found");
                }
                    //dialogService.ShowNotification("Input file not found", "Error");
                var executablepath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "DrawSomHistogram.exe");
                ProcessStartInfo myProcessStartInfo;
                if (usePyExes)
                    myProcessStartInfo = new ProcessStartInfo(executablepath);
                else
                    myProcessStartInfo = new ProcessStartInfo(pythonPath);

                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.RedirectStandardError = true;
                if (usePyExes)
                    myProcessStartInfo.Arguments = "\"" + inputFile + "\"" + " " + "\"" + Model.Output_Folder + "\"" + " " + Model.NoDataValue; //inputFile or Model.InputFile?  keep it consistent.
                else
                    myProcessStartInfo.Arguments = "\"" + scriptPath + "\"" + " " + inputFile + " " + Model.Output_Folder + " " + Model.NoDataValue;

                myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");

                using (var myProcess = new Process())
                {
                    myProcess.StartInfo = myProcessStartInfo;
                    myProcess.Start();
                    StreamReader myStreamReader = myProcess.StandardOutput;
                    StreamReader errorReader = myProcess.StandardError;
                    string errors = errorReader.ReadToEnd();
                    string returnValue = myStreamReader.ReadLine();
                    myProcess.WaitForExit();
                    myProcess.Close();
                    if (errors != "" && !errors.Contains("Warning")) //Make a more robust solution later
                    {
                        PythonLogText += errors + "\r\n";
                        //dialogService.ShowNotification("Failed to draw histogram. See the log file for details", "Error");   
                        logger.Trace(errors);
                        return null;
                    }
                    returnValue = returnValue.Replace("(", "");
                    returnValue = returnValue.Replace(")", "");
                    returnValue = returnValue.Replace("'", "");
                    Console.WriteLine("Value received from script: " + returnValue);  //returnvalue gives the parameters (col type, winsorize, log transformed...) saved in the column that was drawn.
                    //Return value is saved to the model, and UI updates accordingly
                    string[] processed_value = (returnValue.Split(new string[] { ", " }, StringSplitOptions.None));
                    if (processed_value.Length < 2)
                        processed_value = (returnValue.Split(new string[] { " " }, StringSplitOptions.None));
                    Model.IsWinsorized = processed_value[0];
                    Model.WinsorMin = processed_value[1];
                    Model.WinsorMax = processed_value[2];
                    Model.IsLogTransformed = processed_value[3];
                    Model.IsExcluded = processed_value[4];
                };

                ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default); 
                var SomViewModel = ServiceLocator.Current.GetInstance<SomViewModel>();
                if (SelectedColumnIndex == Model.EastingColumnIndex)
                    SomViewModel.IsSelectedEasting = true;
                else
                    SomViewModel.IsSelectedEasting = false;

                if (SelectedColumnIndex == Model.NorthingColumnIndex)
                    SomViewModel.IsSelectedNorthing = true;
                else
                    SomViewModel.IsSelectedNorthing = false;

                    dataHistogram = null;
                    BitMapPath = string.Empty;
                    BitMapPath = Path.Combine(Model.Output_Folder, "SomHistogramTest.png");
                    ImageSource imageSrc = BitmapFromUri(new Uri(BitMapPath));
                    dataHistogram = imageSrc;
                    return imageSrc;
            }
            return dataHistogram;
            //});
        }

        /// <summary>
        /// Function to edit data columns in the preparation stage: applying log transform, winsoring, excluding a column, selecting x and y columns.
        /// Proper column is loaded according to SelectedColumnIndex parameter, and the respective data operations are passed on as parameters (User selects these in the UI and they are bound to Model).
        /// </summary>
        public void EditColumn(SomModel Model, int SelectedColumnIndex, bool IsSelectedNorthing, bool IsSelectedEasting, bool IsSelectedLabel, Action<Process> ScriptOutput, Action<Process> ScriptError) 
        {
            if (SelectedColumnIndex > -1)
            {
                if (SelectedColumnIndex == Model.NorthingColumnIndex)
                {
                    if (IsSelectedNorthing == true)
                        Model.NorthingColumnIndex = SelectedColumnIndex;
                    else //Northing checkbox was unchecked(and this was the case where this column was marked as northing col)
                        Model.NorthingColumnIndex = -1;//set as -1 ("not selected")
                }
                else
                {
                    if (IsSelectedNorthing == true)
                        Model.NorthingColumnIndex = SelectedColumnIndex;
                }
                if (SelectedColumnIndex == Model.EastingColumnIndex)
                {
                    if (IsSelectedEasting == true)
                        Model.EastingColumnIndex = SelectedColumnIndex; 
                    else 
                        Model.EastingColumnIndex = -1;
                }
                else if (IsSelectedEasting == true)
                {
                    Model.EastingColumnIndex = SelectedColumnIndex;
                }
                if (SelectedColumnIndex == Model.LabelColumnIndex)
                {
                    if (IsSelectedLabel == true)
                        Model.LabelColumnIndex = SelectedColumnIndex; 
                    else 
                        Model.LabelColumnIndex = -1;
                }
                else if (IsSelectedLabel == true)
                {
                    Model.LabelColumnIndex = SelectedColumnIndex;
                }
                if (IsSelectedNorthing || IsSelectedEasting)
                    Model.IsExcluded = "0";   //if selected was set to northing or easting, force it to be excluded. --should removal of northing/easting remove exclusion as well? currently it does not.
                var scriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "EditColumn.py");
                var executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "Executables", "EditColumn.exe");
                string outfile = "outfile" + SelectedColumnIndex + ".npy";
                var inputFile = Path.Combine(Model.Output_Folder, "DataPreparation", outfile);
                ProcessStartInfo myProcessStartInfo;
                if (usePyExes)
                    myProcessStartInfo = new ProcessStartInfo(executablePath);
                else
                    myProcessStartInfo = new ProcessStartInfo(pythonPath);

                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.RedirectStandardError = true;
                if (usePyExes)
                    myProcessStartInfo.Arguments = "\"" + inputFile + "\"" + " " + Model.IsWinsorized + " " + Model.WinsorMin + " " + Model.WinsorMax + " " + Model.IsLogTransformed + " " + Model.IsExcluded + " " + Model.NoDataValue;
                else
                    myProcessStartInfo.Arguments = "\"" + scriptPath + "\"" + " " + inputFile + " " + Model.IsWinsorized + " " + Model.WinsorMin + " " + Model.WinsorMax + " " + Model.IsLogTransformed + " " + Model.IsExcluded + " " + Model.NoDataValue;

                myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");
                using (var myProcess = new Process())
                {
                    myProcess.StartInfo = myProcessStartInfo;
                    ScriptOutput(myProcess);
                    ScriptError(myProcess);
                    myProcess.Start();
                    myProcess.BeginErrorReadLine();
                    myProcess.BeginOutputReadLine();
                    myProcess.WaitForExit();
                    myProcess.Close();
                };

            }
        }

        /// <summary>
        /// Function to save the changes made in data preparation stage to the data file. Saves the data as EditedData.lrn
        /// </summary>
        public void SaveChanges(SomModel Model, int SelectedColumnIndex, bool IsSelectedNorthing, bool IsSelectedEasting, bool IsSelectedLabel, Action<Process> ScriptOutput, Action<Process> ScriptError)
        {
            
            
            var scriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "CombineToLrnFile.py");
            var executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "CombineToLrnFile.exe");
            ProcessStartInfo myProcessStartInfo;
            if (usePyExes)
                myProcessStartInfo = new ProcessStartInfo(executablePath);
            else
                myProcessStartInfo = new ProcessStartInfo(pythonPath);

            myProcessStartInfo.UseShellExecute = false;
            myProcessStartInfo.CreateNoWindow = true;
            myProcessStartInfo.RedirectStandardOutput = true;
            myProcessStartInfo.RedirectStandardError = true;

            if (usePyExes)
                myProcessStartInfo.Arguments = "--input_file=" + "\"" + Model.InputFile + "\"" + " " + "--output_folder=" + "\"" + Model.Output_Folder + "\"";  //in the case of geoTiff input file, northing and easting can be ignored (they are locked in to the file structure).
            else
                myProcessStartInfo.Arguments = "\"" + scriptPath + "\"" + " " + "--input_file=" + "\"" + Model.InputFile + "\"" + " " + "--output_folder=" + "\"" + Model.Output_Folder + "\"";

            if (Model.IsSpatial)
            {
                myProcessStartInfo.Arguments += " " + "--eastingIndex=" + "\"" + Model.EastingColumnIndex + "\"";
                myProcessStartInfo.Arguments += " " + "--northingIndex=" + "\"" + Model.NorthingColumnIndex + "\"";
            }
            if (Model.NoDataValue.Length > 0)
            {
                myProcessStartInfo.Arguments += " " + "--na_value=" + "\"" + Model.NoDataValue + "\"";
            }
            if (Model.IsNormalized)
            {
                myProcessStartInfo.Arguments += " " + "--normalized=" + "\"true\""+" "+"--min_N="+ "\""+Model.NormalizationMin+ "\"" + " " +"--max_N="+ "\"" + Model.NormalizationMax+"\"" ;
            }

            myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");
            using (var myProcess = new Process())
            {
                myProcess.StartInfo = myProcessStartInfo;
                ScriptOutput(myProcess);
                ScriptError(myProcess);
                myProcess.Start();
                myProcess.BeginErrorReadLine();
                myProcess.BeginOutputReadLine();
                myProcess.WaitForExit();
                myProcess.Close();
            };
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            var main = ServiceLocator.Current.GetInstance<MainViewModel>();
            main.ChangeToSomParameterView();

        }

        /// <summary>
        /// Main somoclu run
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="SomImageList"></param>
        /// <param name="GeoSpaceImageList"></param>
        /// <param name="BoxPlotList"></param>
        /// <param name="ScatterPlotList"></param>
        /// <param name="ClusterPlotList"></param>
        public async Task RunTool(SomModel Model, ObservableCollection<ImageSource> SomImageList, ObservableCollection<ImageSource> GeoSpaceImageList, ObservableCollection<ImageSource> BoxPlotList, ObservableCollection<ImageSource> ScatterPlotList, ObservableCollection<ImageSource> ClusterPlotList, Action<Process> ScriptOutput, Action<Process> ScriptError)//kopiona model?
        {
            await Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    SomImageList.Clear();
                    GeoSpaceImageList.Clear();
                    BoxPlotList.Clear();
                    ScatterPlotList.Clear();
                    ClusterPlotList.Clear();
                });
                
                var scriptPath = System.AppDomain.CurrentDomain.BaseDirectory + "scripts/nextsom_wrap.py";
                var executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "nextsom_wrap.exe");
                var editedData = Path.Combine(Model.Output_Folder, "DataPreparation", "EditedData.lrn");
                Model.Output_file_somspace = Model.OutputFolderTimestamped + "/result_som.txt";
                if (Model.IsSpatial == true)
                    Model.Output_file_geospace = Model.OutputFolderTimestamped + "/result_geo.txt";
                else
                    Model.Output_file_geospace = "";
                string inputFile;
                if (File.Exists(editedData))
                {
                    inputFile = editedData;
                }
                else
                    inputFile = Model.InputFile;

                try
                {
                    File.Copy(inputFile, Path.Combine(Model.OutputFolderTimestamped, "InputData.lrn"));
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to copy input data.");
                }

                var logFilePath = Path.Combine(Model.OutputFolderTimestamped, "RunStats.txt");
                try
                {
                    if (File.Exists(logFilePath))
                        File.Delete(logFilePath);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to clear RunStats.txt.");
                }
                using (StreamWriter sw = File.CreateText(logFilePath))
                {
                    sw.WriteLine("Run Date:{0}\r\n\r\n", DateTime.UtcNow);
                    sw.Write(
                    "Som input Parameters: \r\n\r\n" + "PythonPath: '{0}'\r\n" + "ScriptPath: '{1}'\r\n" + "OutGeoFile: '{2}'\n" + "OutSomFile: '{3}'\r\n" + "som_x: '{4}'\r\n" + "som_y: '{5}'\r\n" +
                    "epochs: '{6}'\r\n" + "kmeans_min: '{7}'\r\n" + "kmeans_max: '{8}'\r\n" + "kmeans_init: '{9}'\r\n" + "kmeans: '{10}'\r\n" +
                    "neighborhood: {11}\r\n" + "radius0: {12}\r\n" + "radiusN: {13}\r\n" + "maptype: {14}\r\n" + "scalecooling: {15}\r\n" +
                    "scale0: {16}\r\n" + "scaleN: {17}\r\n" + "initialization: {18}\r\n" + "gridtype: {19}\r\n" + "dataShape: {20}\r\n" + "output_folder: {21}\r\n\r\n",
                    pythonPath, scriptPath, Model.Output_file_geospace, Model.Output_file_somspace, Model.Som_x, Model.Som_y,
                    Model.Epochs, Model.KMeans_min, Model.KMeans_max, Model.KMeans_initializations, Model.KMeans,
                    Model.Neighborhood, Model.InitialNeighborhood, Model.FinalNeighborhood, Model.MapType, Model.TrainingRateFunction,
                    Model.TrainingRateInitial, Model.TrainingRateFinal, Model.Initialization, Model.GridShape, Model.DataShape, Model.OutputFolderTimestamped
                );
                    WriteRunStatsXml(Model,Path.Combine(Model.OutputFolderTimestamped, "RunStats.xml"));
                }
                logger.Trace( 
                    "SomInputParams:\n" + "\tPythonPath: '{0}'\n" + "\tScriptPath: '{1}'\n" + "\tOutGeoFile: '{2}'\n" + "\tOutSomFile: '{3}'\n" + "\tsom_x: '{4}'\n" + "\tsom_y: '{5}'\n" +
                    "\tepochs: '{6}'\n" + "\tkmeans_min: '{7}'\n" + "\tkmeans_max: '{8}'\n" + "\tkmeans_init: '{9}'\n" + "\tkmeans: '{10}'\n" +
                    "\tneighborhood: {11}\n" + "\tradius0: {12}\n" + "\tradiusN: {13}\n" + "\tmaptype: {14}\n" + "\tscalecooling: {15}\n" +
                    "\tscale0: {16}\n" + "\tscaleN: {17}\n" + "\tinitialization: {18}\n" + "\tgridtype: {19}\n" + "\toutput_folder: {20}\n",
                    pythonPath, scriptPath, Model.Output_file_geospace, Model.Output_file_somspace, Model.Som_x, Model.Som_y,
                    Model.Epochs, Model.KMeans_min, Model.KMeans_max, Model.KMeans_initializations, Model.KMeans,
                    Model.Neighborhood, Model.InitialNeighborhood, Model.FinalNeighborhood, Model.MapType, Model.TrainingRateFunction,
                    Model.TrainingRateInitial, Model.TrainingRateFinal, Model.Initialization, Model.GridShape, Model.OutputFolderTimestamped
                );

                ProcessStartInfo myProcessStartInfo;
                if (usePyExes)
                    myProcessStartInfo = new ProcessStartInfo(executablePath);
                else
                    myProcessStartInfo = new ProcessStartInfo(pythonPath);

                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.RedirectStandardError = true;
                if (usePyExes)
                    myProcessStartInfo.Arguments = "";
                else
                    myProcessStartInfo.Arguments = "-u" + " " + "\"" + scriptPath + "\"" + " ";


                myProcessStartInfo.Arguments += "--input_file=" + "\"" + inputFile + "\"" + " " +
                        "--output_file_somspace=" + "\"" + Model.Output_file_somspace + "\"" + " " +
                        "--som_x=" + Model.Som_x + " " +
                        "--som_y=" + Model.Som_y + " " +
                        "--epochs=" + Model.Epochs + " " +
                        "--kmeans_min=" + Model.KMeans_min + " " +
                        "--kmeans_max=" + Model.KMeans_max + " " +
                        "--kmeans_init=" + Model.KMeans_initializations + " " +
                        "--kmeans=" + Model.KMeans + " " +
                        "--neighborhood=" + Model.Neighborhood + " " +
                        "--radius0=" + Model.InitialNeighborhood + " " +
                        "--radiusN=" + Model.FinalNeighborhood + " " +
                        "--maptype=" + Model.MapType + " " +
                        "--scalecooling=" + Model.TrainingRateFunction + " " +
                        "--scale0=" + Model.TrainingRateInitial + " " +
                        "--scaleN=" + Model.TrainingRateFinal + " " +
                        "--initialization=" + Model.Initialization + " " +
                        "--gridtype=" + Model.GridShape + " " +
                        "--output_folder=" + "\"" + Model.OutputFolderTimestamped + "\"";

                if (Model.InitialCodeBook != "")     //If initial codebook parameter was given, pass it on as a command line variable
                    myProcessStartInfo.Arguments += " " + "--initialcodebook=" + "\"" + Model.InitialCodeBook + "\"";
                if (Model.Output_file_geospace.Length > 0)
                    myProcessStartInfo.Arguments += " " + "--output_file_geospace=" + "\"" + Model.Output_file_geospace + "\"";
                if (Model.InputFile.Substring(Model.InputFile.Length - 3) == "tif")
                    myProcessStartInfo.Arguments += " " + "--geotiff_input=" + "\"" + Model.InputFile + "\"";
                if (Model.IsNormalized == true)
                {
                    myProcessStartInfo.Arguments += " " + "--normalized=" + "\"" + "True" + "\"" + " " +"--minN=" +Model.NormalizationMin+ " " +"--maxN="+ Model.NormalizationMax;
                }

                myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");
                using (var myProcess = new Process())
                {
                    myProcess.StartInfo = myProcessStartInfo;
                    PythonLogText = "";
                    ScriptOutput(myProcess);
                    ScriptError(myProcess);
                    myProcess.Start();
                    myProcess.BeginErrorReadLine();
                    myProcess.BeginOutputReadLine();
                    myProcess.WaitForExit();
                    myProcess.Close();
                };
            });

            Model.KMeans_min_last_calculation = Model.KMeans_min;
            Model.KMeans_max_last_calculation = Model.KMeans_max;
            Model.ClusterFilePath = Path.Combine(Model.OutputFolderTimestamped, "cluster.dictionary");
            //if (Model.KMeans != "False")
            //    DrawClusters(Model, ClusterPlotList);
            //App.Current.Dispatcher.Invoke((Action)delegate
            //{
            //    ClusterPlotList.Clear();
            //});
            
        }


        /// <summary>
        /// Draw result images
        /// </summary>
        /// <param name="redraw"></param>
        /// <param name="Model"></param>
        /// <param name="SomImageList"></param>
        /// <param name="GeoSpaceImageList"></param>
        /// <param name="BoxPlotList"></param>
        /// <param name="ScatterPlotList"></param>
        public async Task DrawResults(string redraw, SomModel Model, ObservableCollection<ImageSource> SomImageList, ObservableCollection<ImageSource> GeoSpaceImageList, ObservableCollection<ImageSource> BoxPlotList, ObservableCollection<ImageSource> ScatterPlotList, Action<Process> ScriptOutput, Action<Process> ScriptError)
        {
            //IsBusy = true;
            

            await Task.Run(() =>
            {
                string GeoPlotDirectory = Path.Combine(Model.OutputFolderTimestamped, "Geo");
                string SomPlotDirectory = Path.Combine(Model.OutputFolderTimestamped, "Som");
                string BoxPlotDirectory = Path.Combine(Model.OutputFolderTimestamped, "Boxplot");
                string ScatterPlotDirectory = Path.Combine(Model.OutputFolderTimestamped, "Scatterplot");
                ClearFolder(SomPlotDirectory);
                ClearFolder(GeoPlotDirectory);
                ClearFolder(BoxPlotDirectory);
                ClearFolder(ScatterPlotDirectory);

                HttpPost("http://localhost:8050/shutdown", "message=shuts down interactive plots"); //send shutdown message to close any open interactive plots. The actual message doesnt matter, only the address.
                var dataPrepFolder = Path.Combine(Model.Output_Folder, "DataPreparation"); //Folder to read edited input file from
                var outputDir = Model.OutputFolderTimestamped;
                string inputFile;
                if (File.Exists(Path.Combine(dataPrepFolder, "EditedData.lrn")))
                {
                    inputFile = Path.Combine(dataPrepFolder, "EditedData.lrn");
                }
                else
                    inputFile = Model.InputFile;

                var somPlotScriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "NextSomPlot.py");            //Path to python script file in case of script launch
                var executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "NextSomPlot.exe"); //path to executable file in case of executable launch (default).
                ProcessStartInfo myProcessStartInfo;
                if (usePyExes)
                    myProcessStartInfo = new ProcessStartInfo(executablePath);
                else
                    myProcessStartInfo = new ProcessStartInfo(pythonPath);

                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.RedirectStandardError = true;


                if (usePyExes)
                    myProcessStartInfo.Arguments =  " "+"--outsomfile=" + "\"" + Model.Output_file_somspace + "\"" + " " + "--som_x=" + Model.Som_x + " " + "--som_y=" + Model.Som_y + " " + "\"" + "--input_file=" + inputFile + "\"" + " " + "\"" + "--dir=" + Model.OutputFolderTimestamped + "\"" + " " + "--grid_type=" + Model.GridShape;
                else
                    myProcessStartInfo.Arguments = "-u" + " " + "\"" + somPlotScriptPath + "\"" + " " + "--outsomfile=" + "\"" + Model.Output_file_somspace + "\"" + " " + "--som_x=" + Model.Som_x + " " + "--som_y=" + Model.Som_y + " " + "--input_file=" + "\"" + inputFile + "\"" + " " + "--dir=" + "\"" + Model.OutputFolderTimestamped + "\"" + " " + "--grid_type=" + Model.GridShape;

                myProcessStartInfo.Arguments += " " + "--labelIndex=" + "\"" + Model.LabelColumnIndex + "\"";
                myProcessStartInfo.Arguments += " " + "--original_data_dir=" + "\"" + Model.Output_Folder + "\"";
                myProcessStartInfo.Arguments += " " + "--dataType=" + "\"" + Model.DataShape + "\"";
                if (Model.Output_file_geospace.Length > 0)
                {
                    myProcessStartInfo.Arguments += " " + "--outgeofile=" + "\"" + Model.Output_file_geospace + "\"";
                    myProcessStartInfo.Arguments += " " + "--eastingIndex=" + "\"" + Model.EastingColumnIndex + "\"";
                    myProcessStartInfo.Arguments += " " + "--northingIndex=" + "\"" + Model.NorthingColumnIndex + "\"";                   
                }
                 myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");
                using (var myProcess = new Process())
                {
                    myProcess.StartInfo = myProcessStartInfo;
                    ScriptOutput(myProcess);
                    ScriptError(myProcess);
                    myProcess.Start();
                    myProcess.BeginErrorReadLine();
                    myProcess.BeginOutputReadLine();
                    myProcess.WaitForExit();
                    myProcess.Close();
                };               
            });          
        }
        public async Task DrawScatterPlots(SomModel Model, Action<Process> ScriptOutput, Action<Process> ScriptError)
        {

            await Task.Run(() =>
            {

                var formattedDrawList = new List<int>();
                for(int i=0; i < Model.ScatterPlotList.Count; i++)
                {
                    if (Model.ScatterPlotList[i].IsSelected == true)
                        formattedDrawList.Add(1);
                    else 
                        formattedDrawList.Add(0);
                }
                string ScatterPlotDirectory = Path.Combine(Model.OutputFolderTimestamped, "Scatterplot");
                //ClearFolder(ScatterPlotDirectory);
                var dataPrepFolder = Path.Combine(Model.Output_Folder, "DataPreparation"); //Folder to read edited input file from
                var outputDir = Model.OutputFolderTimestamped;
                string inputFile;
                if (File.Exists(Path.Combine(dataPrepFolder, "EditedData.lrn")))
                {
                    inputFile = Path.Combine(dataPrepFolder, "EditedData.lrn");
                }
                else
                    inputFile = Model.InputFile;

                var somPlotScriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "Draw_Scatterplots.py");            //Path to python script file in case of script launch
                var executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "Draw_Scatterplots.exe"); //path to executable file in case of executable launch (default).
                ProcessStartInfo myProcessStartInfo;
                if (usePyExes)
                    myProcessStartInfo = new ProcessStartInfo(executablePath);
                else
                    myProcessStartInfo = new ProcessStartInfo(pythonPath);
                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.RedirectStandardError = true;


                if (usePyExes)
                    myProcessStartInfo.Arguments = " " + "--outsomfile=" + "\"" + Model.Output_file_somspace + "\"" + " " + "--som_x=" + Model.Som_x + " " + "--som_y=" + Model.Som_y + " " + "\"" + "--input_file=" + inputFile + "\"" + " " + "\"" + "--dir=" + Model.OutputFolderTimestamped + "\"" + " " + "--draw_list=" + "\""+ String.Join(", ", formattedDrawList.ToArray()).Replace(" ","")+ "\""; 
                else
                    myProcessStartInfo.Arguments = "-u" + " " + "\"" + somPlotScriptPath + "\"" + " " + "--outsomfile=" + "\"" + Model.Output_file_somspace + "\"" + " " + "--som_x=" + Model.Som_x + " " + "--som_y=" + Model.Som_y + " " + "--input_file=" + "\"" + inputFile + "\"" + " " + "--dir=" + "\"" + Model.OutputFolderTimestamped + "\"" + " " + "--draw_list=" + "\"" + String.Join(", ", formattedDrawList.ToArray()).Replace(" ", "") + "\"";

                myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");


                using (var myProcess = new Process())
                {
                    myProcess.StartInfo = myProcessStartInfo;
                    ScriptOutput(myProcess);
                    ScriptError(myProcess);
                    myProcess.Start();
                    myProcess.BeginOutputReadLine();
                    myProcess.BeginErrorReadLine();
                    myProcess.WaitForExit();
                    myProcess.Close();
                };
            });
        }


        /// <summary>
        /// Draw interactive plots
        /// </summary>
        /// <param name="Model">SomModel</param>
        public async void DrawResultsInteractive(SomModel Model, Action<Process> ScriptOutput, Action<Process> ScriptError)
        {
            //IsBusy = true;
            await Task.Run(() =>
            {
                string uuid = HttpPost("http://localhost:8050/shutdown", "message=shuts down interactive plots"); //send shutdown message to close any open interactive plots. The actual message doesnt matter, only the address.
                var dataPrepFolder = Path.Combine(Model.OutputFolderTimestamped, "DataPreparation"); //Folder to read edited input file from
                var outputDir = Path.Combine(Model.OutputFolderTimestamped, "somresults");
                string inputFile;
                if (File.Exists(Path.Combine(dataPrepFolder, "EditedData.lrn")))
                {
                    inputFile = Path.Combine(dataPrepFolder, "EditedData.lrn");
                }
                else
                    inputFile = Model.InputFile;

                var somPlotScriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "NextSomPlot.py");            //Path to python script file in case of script launch
                var executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "NextSomPlot.exe"); //path to executable file in case of executable launch (default).
                ProcessStartInfo myProcessStartInfo;
                if (usePyExes)
                    myProcessStartInfo = new ProcessStartInfo(executablePath);
                else
                    myProcessStartInfo = new ProcessStartInfo(pythonPath);

                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.RedirectStandardError = true;



                somPlotScriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "NextSomPlot_Dash.py");
                executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "NextSomPlot_Dash.exe");
                if (usePyExes)
                    myProcessStartInfo = new ProcessStartInfo(executablePath);
                else
                    myProcessStartInfo = new ProcessStartInfo(pythonPath);


                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.RedirectStandardError = true;

                var interactiveFolder = Path.Combine(Model.OutputFolderTimestamped, "Interactive");
                try
                {
                    System.IO.File.WriteAllText(Path.Combine(Model.OutputFolderTimestamped, "clickData2.txt"), "-1"); //create these files. this should be really handled in a better way, the whole system for checking if the user has clicked a new cluster
                    System.IO.File.WriteAllText(Path.Combine(interactiveFolder, "clickData2.txt"), "-1");
                }
                catch (Exception ex)
                {
                    logger.Error("Selection error in interactive plot, " + ex);
                }              
                if (usePyExes)
                    myProcessStartInfo.Arguments = "\"" + Model.Output_file_somspace + "\"" + " " + Model.Som_x + " " + Model.Som_y + " " + "\"" + Model.Output_file_geospace + "\"" + " " + "\"" + Model.InputFile + "\"" + " " + "\"" + interactiveFolder + "\"" + " " + "\"" + Model.OutputFolderTimestamped + "\"" + " " + Model.GridShape;
                else
                    myProcessStartInfo.Arguments = "-u" + " " + "\"" + somPlotScriptPath + "\"" + " "+ "\"" + Model.Output_file_somspace + "\""+ " " + Model.Som_x + " " + Model.Som_y + " " + "\"" + Model.Output_file_geospace + "\""+ " " + "\""+ Model.InputFile + "\""+ " " + "\"" + interactiveFolder + "\"" + " " + "\"" + Model.OutputFolderTimestamped + "\"" + " " + Model.GridShape;

                myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");
                using (var myProcess = new Process())
                {
                    myProcess.StartInfo = myProcessStartInfo;
                    ScriptOutput(myProcess);
                    ScriptError(myProcess);
                    myProcess.Start();
                    myProcess.BeginOutputReadLine();
                    myProcess.BeginErrorReadLine();
                    myProcess.Close();
                };


            });
            //kludge for refreshing browser. try to think of a better way.
        }

        /// <summary>
        /// Run new round of clustering on somoclu result data
        /// </summary>
        public async Task RunClustering(SomModel Model, ObservableCollection<ImageSource> ClusterPlotList, Action<Process> ScriptOutput, Action<Process> ScriptError)
        {
            await Task.Run(() =>
            {
                
                
                var scriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "cluster_wrap.py");
                var executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "cluster_wrap.exe");
                var som = Path.Combine(Model.OutputFolderTimestamped, "som.dictionary");
                ProcessStartInfo myProcessStartInfo;
                if (usePyExes)
                    myProcessStartInfo = new ProcessStartInfo(executablePath);
                else
                    myProcessStartInfo = new ProcessStartInfo(pythonPath);
                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.RedirectStandardError = true; 
                if (usePyExes)
                    myProcessStartInfo.Arguments = "\"" + som + "\"" + " " + Model.KMeans_min + " " + Model.KMeans_max + " " + Model.KMeans_initializations + " " + "\"" + Model.OutputFolderTimestamped + "\"";
                else
                    myProcessStartInfo.Arguments = "\"" + scriptPath + "\"" + " " + som + " " + Model.KMeans_min + " " + Model.KMeans_max + " " + Model.KMeans_initializations + " " + "\"" + Model.OutputFolderTimestamped + "\"";

                myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");

                using (var myProcess = new Process())
                {
                    myProcess.StartInfo = myProcessStartInfo;
                    ScriptOutput(myProcess);
                    ScriptError(myProcess);
                    myProcess.Start();
                    myProcess.BeginErrorReadLine();
                    myProcess.BeginOutputReadLine();
                    myProcess.WaitForExit();
                    myProcess.Close();
                };
            });
            
            Model.KMeans_min_last_calculation = Model.KMeans_min;
            Model.KMeans_max_last_calculation = Model.KMeans_max;
            EditRunStatsXml(Path.Combine(Model.OutputFolderTimestamped, "RunStats.xml"), "kmeans_min_last_calculation", Model.KMeans_min_last_calculation.ToString());
            Model.ClusterFilePath = Path.Combine(Model.OutputFolderTimestamped, "cluster.dictionary");
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Model"></param>
        /// <param name="ClusterPlotList"></param>
        public async void DrawClusters(SomModel Model, ObservableCollection<ImageSource> ClusterPlotList, Action<Process> ScriptOutput, Action<Process> ScriptError)
        {
            await Task.Run(() =>
            {
                
                var scriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "cluster_draw_2.py");
                var executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "cluster_draw_2.exe");
                var cluster_file = Path.Combine(Model.OutputFolderTimestamped, "cluster.dictionary");
                var som = Path.Combine(Model.OutputFolderTimestamped, "som.dictionary");
                ProcessStartInfo myProcessStartInfo;
                if (usePyExes)
                    myProcessStartInfo = new ProcessStartInfo(executablePath);
                else
                    myProcessStartInfo = new ProcessStartInfo(pythonPath);

                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.RedirectStandardError = true;

                if (usePyExes)
                    myProcessStartInfo.Arguments = "\"" + Model.OutputFolderTimestamped + "\"" + " " + "\"" + Model.ClusterFilePath + "\"";
                else
                    myProcessStartInfo.Arguments = "\"" + scriptPath + "\"" + " " + "\"" + Model.OutputFolderTimestamped + "\"" + " " + "\"" + Model.ClusterFilePath + "\"";// "\"" + Model.Output_file_geospace + "\"" + " "

                myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");
                using (var myProcess = new Process())
                {
                    myProcess.StartInfo = myProcessStartInfo;
                    ScriptOutput(myProcess);
                    ScriptError(myProcess);
                    myProcess.Start();
                    myProcess.BeginErrorReadLine();
                    myProcess.BeginOutputReadLine();
                    myProcess.WaitForExit();
                    myProcess.Close();
                };


                string ClusterPlotDirectory = Model.OutputFolderTimestamped;
                try
                {
                    //Draw cluster plots
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ClusterPlotList.Clear();
                    });
                    var d = new DirectoryInfo(ClusterPlotDirectory);
                    var Files = d.GetFiles("*.png");
                    string fullPath;
                    ImageSource imageSrc;
                    foreach (FileInfo file in Files) //Loop through result images: create bitmap, add bitmap to list, delete original file.
                    {
                        if (file.Name == "cluster_plot.png")
                        {
                            fullPath = Path.Combine(ClusterPlotDirectory, file.Name);
                            imageSrc = BitmapFromUri(new Uri(fullPath));
                            App.Current.Dispatcher.Invoke((Action)delegate   //add to list. same thing as previously with the delegate.
                            {
                                ClusterPlotList.Add(imageSrc);
                            });
                            file.Delete();//delete file to reduce clutter.
                        }
                    }

                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to show Cluster plot images");
                }
            });
            
        }

        /// <summary>
        /// Get som results from a spesific folder.
        /// </summary>
        /// <param name="ClusterPlotDirectory"></param>
        /// <param name="ClusterPlotList"></param>
        public void DrawClusterPlots(string ClusterPlotDirectory, ObservableCollection<ImageSource> ClusterPlotList)
        {
            try
            {
                //Draw cluster plots
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    ClusterPlotList.Clear();
                });
                var d = new DirectoryInfo(ClusterPlotDirectory);
                var Files = d.GetFiles("*.png");
                string fullPath;
                ImageSource imageSrc;
                foreach (FileInfo file in Files) //Loop through result images: create bitmap, add bitmap to list, delete original file.
                {
                    fullPath = Path.Combine(ClusterPlotDirectory, file.Name);
                    imageSrc = BitmapFromUri(new Uri(fullPath));
                    App.Current.Dispatcher.Invoke((Action)delegate   //add to list. same thing as previously with the delegate.
                    {
                        ClusterPlotList.Add(imageSrc);
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to show Cluster plot images");
            }
        }

        /// <summary>
        /// Save selected clustering result to som calculation result txt files.
        /// </summary>
        public async Task SaveCluster(SomModel Model, int selectedClusterNumber, ObservableCollection<ImageSource> SomImageList, ObservableCollection<ImageSource> GeoSpaceImageList, ObservableCollection<ImageSource> BoxPlotList, ObservableCollection<ImageSource> ScatterPlotList, Action<Process> ScriptOutput, Action<Process> ScriptError)
        {
            await Task.Run(() =>
            {
                //SelectedClusterIndex needs to be converted into selected number of clusters.
                var selectedClusterIndex = selectedClusterNumber - Model.KMeans_min_last_calculation;
                
                var scriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "cluster_save.py");
                var executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "cluster_save.exe");
                var cluster_file = Path.Combine(Model.OutputFolderTimestamped, "cluster.dictionary");
                var som = Path.Combine(Model.OutputFolderTimestamped, "som.dictionary");
                Model.InputFile = Path.Combine(Model.OutputFolderTimestamped, "InputData.lrn");
                ProcessStartInfo myProcessStartInfo;
                if (usePyExes)
                    myProcessStartInfo = new ProcessStartInfo(executablePath);
                else
                    myProcessStartInfo = new ProcessStartInfo(pythonPath);

                myProcessStartInfo.UseShellExecute = false;
                myProcessStartInfo.CreateNoWindow = true;
                myProcessStartInfo.RedirectStandardOutput = true;
                myProcessStartInfo.RedirectStandardError = true;

                if (usePyExes)
                    myProcessStartInfo.Arguments = "--n_clusters=" + selectedClusterIndex + " " + "--cluster_file=" + "\"" + cluster_file + "\"" + " " + "--som_dict=" + "\"" + som + "\"" + " " + "--input_file=" + "\"" + Model.InputFile + "\"" + " " + "--outsomfile=" + "\"" + Model.Output_file_somspace + "\"" + " " + "--workingdir=" + "\"" + Model.OutputFolderTimestamped + "\"";
                else
                    myProcessStartInfo.Arguments = "\"" + scriptPath + "\"" + " " + "--n_clusters=" + selectedClusterIndex + " " + "--cluster_file=" + "\"" + cluster_file + "\"" + " " + "--som_dict=" + "\"" + som + "\"" + " " + "--input_file=" + "\"" + Model.InputFile + "\"" + " " + "--outsomfile=" + "\"" + Model.Output_file_somspace + "\"" + " " + "--workingdir=" + "\"" + Model.OutputFolderTimestamped + "\"";

                if (Model.Output_file_geospace.Length > 0)
                    myProcessStartInfo.Arguments += " " + "--outgeofile=" + "\"" + Model.Output_file_geospace + "\"";
                myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");
                using (var myProcess = new Process())
                {
                    myProcess.StartInfo = myProcessStartInfo;
                    ScriptOutput(myProcess);
                    ScriptError(myProcess);
                    myProcess.Start();
                    myProcess.BeginErrorReadLine();
                    myProcess.BeginOutputReadLine();
                    myProcess.WaitForExit();
                    myProcess.Close();
                };

                //re-draw results with new clustering
            });
            
        }

        /// <summary>
        /// Function to draw selected cluster
        /// </summary>
        public void RunDashDraw(SomModel Model, Action<Process> ScriptOutput, Action<Process> ScriptError)
        {
            var scriptPath="";
            var executablePath="";
            if (Model.DataShape == "grid")
            {
                scriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "NextSomPlot_Dash_Draw.py");
                executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "NextSomPlot_Dash_Draw.exe");
            }
            else
            {
                scriptPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "NextSomPlot_Dash_Draw_Scatter.py");
                executablePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "scripts", "executables", "NextSomPlot_Dash_Draw_Scatter.exe");
            }
            ProcessStartInfo myProcessStartInfo;
            if (usePyExes)
                myProcessStartInfo = new ProcessStartInfo(executablePath);
            else
                myProcessStartInfo = new ProcessStartInfo(pythonPath);
            myProcessStartInfo.UseShellExecute = false;
            myProcessStartInfo.CreateNoWindow = true;
            myProcessStartInfo.RedirectStandardOutput = true;
            myProcessStartInfo.RedirectStandardError = true;

            if (usePyExes)
                myProcessStartInfo.Arguments = "\"" + Model.Output_file_somspace + "\"" + " " + Model.Som_x + " " + Model.Som_y + " " + "\"" + Model.Output_file_geospace + "\"" + " " + "\"" + Model.InputFile + "\"" + " " + "\"" + Model.OutputFolderTimestamped + "\"" + " " + "\"" + Model.InteractiveType + "\"";
            else
                myProcessStartInfo.Arguments = "\"" + scriptPath + "\"" + " " + "\"" + Model.Output_file_somspace + "\"" + " " + Model.Som_x + " " + Model.Som_y + " " + "\"" + Model.Output_file_geospace + "\"" + " " + "\"" + Model.InputFile + "\"" + " " + "\"" + Model.OutputFolderTimestamped + "\"" + " " + "\"" + Model.InteractiveType + "\"";

            myProcessStartInfo.Arguments = myProcessStartInfo.Arguments.Replace("\\", "/");

            using (var myProcess = new Process())
            {
                myProcess.StartInfo = myProcessStartInfo;
                ScriptOutput(myProcess);
                ScriptError(myProcess);
                myProcess.Start();
                myProcess.BeginErrorReadLine();
                myProcess.BeginOutputReadLine();
                myProcess.WaitForExit();
                myProcess.Close();
            };
        }

        //private void ShowErrorFlyout(string msg)
        //{
        //    if (msg.ToLower().Contains("error"))
        //    {
        //        if (StatusFlyOutOpen == false)
        //        {
        //            FlyOutText = msg;
        //            StatusFlyOutOpen = true;
        //        }
        //    }
        //}

        //private void ShowErrorFlyoutAlways(string msg)
        //{
        //    if (StatusFlyOutOpen == false)
        //    {
        //        FlyOutText = msg;
        //        StatusFlyOutOpen = true;
        //    }
        //}

        

        //For sending the shutdown message to the interactive plot.
        private string HttpPost(string URI, string Parameters)
        {
            try
            {
                System.Net.WebRequest req = System.Net.WebRequest.Create(URI);
                req.Proxy = WebRequest.DefaultWebProxy;
                req.ContentType = "application/x-www-form-urlencoded";
                req.Method = "POST";
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(Parameters);
                req.ContentLength = bytes.Length;
                System.IO.Stream os = req.GetRequestStream();
                os.Write(bytes, 0, bytes.Length); 
                os.Close();
                System.Net.WebResponse resp = req.GetResponse();
                if (resp == null) return null;
                System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                resp.Close();
                return null; 
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static ImageSource BitmapFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = source;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; //Image cache must be ignored, to be able to update the images
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); //Bitmap must be freezable, so it can be accessed from other threads.
            return bitmap;
        }

        public static ImageSource BitmapWithCacheFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = source;
            //bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; //Image cache must be ignored, to be able to update the images
            //bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); //Bitmap must be freezable, so it can be accessed from other threads.
            return bitmap;
        }
        /// <summary>
        /// Hacky solution for refreshing browser (for interactive plot)
        /// </summary>
        //public string BrowserToolTip
        //{
        //    get
        //    {
        //        return "";
        //    }
        //    set
        //    {
        //        OnPropertyChanged();
        //        RaisePropertyChanged("BrowserToolTip");
        //    }
        //}

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string PythonLogText
        {
            get { return pythonLogText; }
            set
            {
                if (pythonLogText == value) return;
                pythonLogText = value;
                OnPropertyChanged(); 
            }
        }

        //public string FlyOutText
        //{
        //    get { return flyOutText; }
        //    set
        //    {
        //        if (flyOutText == value) return;
        //        flyOutText = value;
        //        OnPropertyChanged();
        //    }
        //}

        //public bool StatusFlyOutOpen
        //{
        //    get { return statusFlyOutOpen; }
        //    set
        //    {
        //        if (statusFlyOutOpen == value) return;
        //        statusFlyOutOpen = value;
        //        OnPropertyChanged();
        //    }
        //}



        private void ClearFolder(string folderPath)
        {
            DirectoryInfo d;
            FileInfo[] Files;
            try
            {
                d = new DirectoryInfo(folderPath);
                Files = d.GetFiles(); 
                foreach (FileInfo file in Files)
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to clear folder");
            }
        }

        private void AddToImageCollection(ObservableCollection<ImageSource> ImageCollection, string PlotDirectory)
        { 
            ImageSource imageSrc;
            DirectoryInfo d;
            FileInfo[] Files;
            string fullPath;
            try
            {
                App.Current.Dispatcher.Invoke((Action)delegate         //delegate to access different thread
                {
                    ImageCollection.Clear();
                });
                d = new DirectoryInfo(PlotDirectory);
                Files = d.GetFiles("*.png").OrderBy(p => p.CreationTime).ToArray(); //Getting png files
                foreach (FileInfo file in Files)
                {
                    fullPath = Path.Combine(PlotDirectory, file.Name);
                    imageSrc = BitmapFromUri(new Uri(fullPath)); 
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ImageCollection.Add(imageSrc);
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to show output images");
            }
        }
        private void EditRunStatsXml( string xmlPath, string elementName, string elementText)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlPath);
            XmlNode root = doc.DocumentElement;
            XmlNode myNode = root.SelectSingleNode("descendant::"+ elementName);
            myNode.InnerText = elementText;//
            doc.Save(xmlPath);
        }
        private void WriteRunStatsXml(SomModel model, string xmlPath)
        {
            XmlWriter xmlWriter = XmlWriter.Create(xmlPath);

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("som");

            xmlWriter.WriteStartElement("som_x");
            xmlWriter.WriteString(model.Som_x.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("som_y");
            xmlWriter.WriteString(model.Som_y.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("epochs");
            xmlWriter.WriteString(model.Epochs.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("OutGeoFile");
            xmlWriter.WriteString(model.Output_file_geospace);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("OutSomFile");
            xmlWriter.WriteString(model.Output_file_somspace);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("kmeans_min");
            xmlWriter.WriteString(model.KMeans_min.ToString());
            xmlWriter.WriteEndElement();     

            xmlWriter.WriteStartElement("kmeans_max");
            xmlWriter.WriteString(model.KMeans_max.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("kmeans_init");
            xmlWriter.WriteString(model.KMeans_initializations.ToString()); ;
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("kmeans_min_last_calculation");
            xmlWriter.WriteString(model.KMeans_min_last_calculation.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("kmeans_max_last_calculation");
            xmlWriter.WriteString(model.KMeans_max_last_calculation.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("kmeans");
            xmlWriter.WriteString(model.KMeans);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("neighborhood");
            xmlWriter.WriteString(model.Neighborhood);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("radius0");
            xmlWriter.WriteString(model.InitialNeighborhood.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("radiusN");
            xmlWriter.WriteString(model.FinalNeighborhood.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("maptype");
            xmlWriter.WriteString(model.MapType);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("scalecooling");
            xmlWriter.WriteString(model.KMeans);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("scale0");
            xmlWriter.WriteString(model.TrainingRateFunction.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("scaleN");
            xmlWriter.WriteString(model.TrainingRateFinal.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("initialization");
            xmlWriter.WriteString(model.Initialization);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("gridtype");
            xmlWriter.WriteString(model.GridShape);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("dataShape");
            xmlWriter.WriteString(model.DataShape);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("output_folder");
            xmlWriter.WriteString(model.OutputFolderTimestamped);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }
    }
}
