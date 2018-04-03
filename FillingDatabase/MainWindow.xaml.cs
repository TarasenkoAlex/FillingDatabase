using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows;
using MapGen.Model.Database;
using MapGen.Model.Database.DbWorker;
using MapGen.Model.Database.EDM;
using Point = MapGen.Model.Database.EDM.Point;

namespace FillingDatabase
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _nameMap;
        private int _scale;
        private Point[] _cloudPoints;
        private int _length;
        private int _width;
        private string _message;

        private DatabaseWorker _databaseWorker;

        public MainWindow()
        {
            InitializeComponent();
            InitFields();
        }

        private void InitFields()
        {
            _cloudPoints = new Point[] { };
            _databaseWorker = new DatabaseWorker();
            _databaseWorker.CreateDatabase(out _message);
            _databaseWorker.Connect(out _message);
        }

        private void ButtonInsertToBD_OnClick(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                Dispatcher.Invoke(() => ProgressBar.Maximum = _cloudPoints.Length);
                //Dispatcher.Invoke(() => ProgressBar.IsIndeterminate = true);

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                //UnitOfWork uow = new UnitOfWork();
                Map map = new Map
                { 
                    Name = _nameMap,
                    Width = _width,
                    Length = _length,
                    Scale = _scale,
                    Latitude = "59.915188",
                    Longitude = "30.4379001"
                };

                _databaseWorker.InsertMap(map, out _message);
                _databaseWorker.Save(out _message);

                for (int i = 0; i < _cloudPoints.Length; ++i)
                {

                    _databaseWorker.InsertPoint(new Point
                    {
                        X = _cloudPoints[i].X,
                        Y = _cloudPoints[i].Y,
                        Depth = _cloudPoints[i].Depth,
                        Map = map
                    }, out _message);
                }

                _databaseWorker.Save(out _message);

                int elem = 0;
                while (true)
                {
                    elem = _databaseWorker.CountQueueInserts;
                    if (elem == 0)
                    {
                        break;
                    }

                    Dispatcher.Invoke(() => ProgressBar.Value = ProgressBar.Maximum - elem);

                    Thread.Sleep(1000);
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;

                Dispatcher.Invoke(() => ProgressBar.Value = 0);

                MessageBox.Show($"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}");
                
            }) {IsBackground = true}.Start();
        }

        private void ButtonSelectNrm_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Multiselect = false,
                Filter = "nrm files (*.nrm)|*.nrm"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ReadCloudPointFromFile(openFileDialog.FileName);
                TextBoxFileName.Text = Path.GetFileName(openFileDialog.FileName);
            }
        }

        private void ReadCloudPointFromFile(string fileName)
        {
            try
            {
                BinaryReader outBin = new BinaryReader(File.Open(fileName, FileMode.Open));

                // Считываем количество точек.
                int countPoints = outBin.ReadInt32();
                _cloudPoints = new Point[countPoints];

                // Считываем точки из файла.
                for (int i = 0; i < countPoints; i++)
                {
                    _cloudPoints[i] = new Point()
                    {
                        X = (long) outBin.ReadDouble(),
                        Y = (long) outBin.ReadDouble(),
                        Depth = Math.Round(outBin.ReadDouble(), 3)
                    };
                    if (_cloudPoints[i].X + 1 > _width)
                        _width = (int)_cloudPoints[i].X + 1;
                    if (_cloudPoints[i].Y + 1 > _length)
                        _length = (int)_cloudPoints[i].Y + 1;
                }

                outBin.Close();

                MessageBox.Show(
                    $"Облако точек успешно загружено. Количество точек: {countPoints}.",
                    "Загрузка файла с облаком точек");
            }
            catch
            {
                MessageBox.Show($"Не удалось считать файл {fileName}.", "Ошибка подключения");
            }
        }

        private void TextBoxRegion_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _nameMap = TextBoxRegion.Text;
        }

        private void TextBoxScale_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TextBoxRegion.Text))
                _scale = int.Parse(TextBoxScale.Text);
        }
    }
}
