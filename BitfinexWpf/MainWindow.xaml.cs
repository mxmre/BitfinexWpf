using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using BitfinexWpf;
using BitfinexAPI;

namespace BitfinexWpf
{



    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private ObservableCollection<Money> _Moneys;
        public ObservableCollection<Money> Moneys
        {
            get => _Moneys;
            set
            {
                _Moneys = value;
                OnPropertyChanged(nameof(Moneys));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private BitfinexClientRestAPI _rest;
        private BitfinexClientWebsocketAPI _socket;

        private BitfinexConnector _connector;

        public void LogData(string msg)
        {
            textLogs.AppendText(msg + Environment.NewLine);
            textLogs.ScrollToEnd();

            var lines = textLogs.Document.Blocks.Count;
            if (lines > 100)
            {
                textLogs.Document.Blocks.Remove(textLogs.Document.Blocks.FirstBlock);
            }
        }
        void EventsInit()
        {
            _connector.OnWebsocketConnection += (BitfinexConnector sender) => { LogData("Подключение через Websocket..."); };
            _connector.OnWebsocketConnected += (BitfinexConnector sender) => { LogData("Подключение через Websocket успешно!"); };
            _connector.OnWebsocketConnectionError += (BitfinexConnector sender) => { LogData("Подключение через Websocket: Ошибка!"); };

            _connector.OnWebsocketSubscribeCandles += (BitfinexConnector sender) => { LogData("Подписка на свечи..."); };
            _connector.OnWebsocketSubscribeCandlesError += (BitfinexConnector sender) => { LogData("Подписка на свечи: Ошибка!"); };
            _connector.OnWebsocketSubscribeTrades += (BitfinexConnector sender) => { LogData("Подписка на трейды..."); };
            _connector.OnWebsocketSubscribeTradesError += (BitfinexConnector sender) => { LogData("Подписка на трейды: Ошибка!"); };

            _connector.OnWebsocketUnSubscribeCandles += (BitfinexConnector sender) => { LogData("Отписка от свечей..."); };
            _connector.OnWebsocketUnSubscribeCandlesError += (BitfinexConnector sender) => { LogData("Отписка от свечей: Ошибка!"); };
            _connector.OnWebsocketUnSubscribeTrades += (BitfinexConnector sender) => { LogData("Отписка от трейдов..."); };
            _connector.OnWebsocketUnSubscribeTradesError += (BitfinexConnector sender) => { LogData("Отписка от трейдов: Ошибка!"); };

            _connector.OnRestGetCandleSeries += (BitfinexConnector sender) => { LogData("Rest API: Получение данных о свечах. Завершено!"); };
            _connector.OnRestGettingCandleSeries += (BitfinexConnector sender) => { LogData("Rest API: Получение данных о свечах..."); };
            _connector.OnRestGettingCandleSeriesError += (BitfinexConnector sender) => { LogData("Rest API: Получение данных о свечах. Ошибка!"); };

            _connector.OnRestGetTrades += (BitfinexConnector sender) => { LogData("Rest API: Получение данных о трейдах. Завершено!"); };
            _connector.OnRestGettingTrades += (BitfinexConnector sender) => { LogData("Rest API: Получение данных о трейдах"); };
            _connector.OnRestGettingTradesError += (BitfinexConnector sender) => { LogData("Rest API: Получение данных о трейдах. Ошибка!"); };
        }
        public MainWindow()
        {
            InitializeComponent();
            _Moneys = new();
            Moneys = new ObservableCollection<Money> { new Money { TotalUSDT = 1m, TotalBTC = 1m, TotalDASH = 1m, TotalXMR = 1m, TotalXRP = 1m } };
            dataGrid.ItemsSource = Moneys;

            _rest = new();
            _socket = new();
            _connector = new(_rest, _socket);

            EventsInit();
            UpdateButtons();
        }
        bool _isConnected = false;
        void UpdateButtons()
        {
            if(!_isConnected)
            {
                btnDis.IsEnabled = false;
                btnUpdate.IsEnabled = true;
            }
            else 
            {
                btnUpdate.IsEnabled = false;
                btnDis.IsEnabled = true;
            }
        }

        private void btnDis_Click(object sender, RoutedEventArgs e)
        {
            _isConnected = false;
            UpdateButtons();
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateButtons();
        }

        private void btnCon_Click(object sender, RoutedEventArgs e)
        {
            _isConnected = true;
            UpdateButtons();
        }
    }
}
