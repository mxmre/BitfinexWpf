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
using BitfinexAPI.TestHQ;
using System.Collections.Concurrent;
using System.Windows.Threading;

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
        private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
        private readonly DispatcherTimer _timer;
        void EventsInit()
        {
            _connector.OnWebsocketConnection += (BitfinexConnector sender) => { AddMessage("Подключение через Websocket..."); };
            _connector.OnWebsocketDisConnected += (BitfinexConnector sender) => { AddMessage("Отключение..."); };
            _connector.OnWebsocketConnected += (BitfinexConnector sender) => { AddMessage("Подключение через Websocket успешно!"); };
            _connector.OnWebsocketConnectionError += (BitfinexConnector sender) => { AddMessage("Подключение через Websocket: Ошибка!"); };

            _connector.OnWebsocketSubscribeCandles += (BitfinexConnector sender) => { AddMessage("Подписка на свечи..."); };
            _connector.OnWebsocketSubscribeCandlesError += (BitfinexConnector sender) => { AddMessage("Подписка на свечи: Ошибка!"); };
            _connector.OnWebsocketSubscribeTrades += (BitfinexConnector sender) => { AddMessage("Подписка на трейды..."); };
            _connector.OnWebsocketSubscribeTradesError += (BitfinexConnector sender) => { AddMessage("Подписка на трейды: Ошибка!"); };

            _connector.OnWebsocketUnSubscribeCandles += (BitfinexConnector sender) => { AddMessage("Отписка от свечей..."); };
            _connector.OnWebsocketUnSubscribeCandlesError += (BitfinexConnector sender) => { AddMessage("Отписка от свечей: Ошибка!"); };
            _connector.OnWebsocketUnSubscribeTrades += (BitfinexConnector sender) => { AddMessage("Отписка от трейдов..."); };
            _connector.OnWebsocketUnSubscribeTradesError += (BitfinexConnector sender) => { AddMessage("Отписка от трейдов: Ошибка!"); };

            _connector.OnRestGetCandleSeries += (BitfinexConnector sender) => { AddMessage("Rest API: Получение данных о свечах. Завершено!"); };
            _connector.OnRestGettingCandleSeries += (BitfinexConnector sender) => { AddMessage("Rest API: Получение данных о свечах..."); };
            _connector.OnRestGettingCandleSeriesError += (BitfinexConnector sender) => { AddMessage("Rest API: Получение данных о свечах. Ошибка!"); };

            _connector.OnRestGetTrades += (BitfinexConnector sender) => { AddMessage("Rest API: Получение данных о трейдах. Завершено!"); };
            _connector.OnRestGettingTrades += (BitfinexConnector sender) => { AddMessage("Rest API: Получение данных о трейдах"); };
            _connector.OnRestGettingTradesError += (BitfinexConnector sender) => { AddMessage("Rest API: Получение данных о трейдах. Ошибка!"); };
        }

        private void ProcessMessageQueue(object sender, EventArgs e)
        {
            while (_messageQueue.TryDequeue(out string message))
            {
                textLogs.AppendText(message + Environment.NewLine);
            }
            textLogs.ScrollToEnd();
        }

        public void AddMessage(string text)
        {
            _messageQueue.Enqueue(text);
        }

        public MainWindow()
        {
            InitializeComponent();
            _Moneys = new();
            Moneys = new ObservableCollection<Money> { new Money { TotalUSDT = 1m, TotalBTC = 1m, TotalDASH = 1m,
                TotalXMR = 1m, TotalXRP = 1m} };
            dataGrid.ItemsSource = Moneys;

            _rest = new();
            _socket = new();
            _connector = new(_rest, _socket);
            _connector.CandleSeriesProcessing += (Candle candle) =>
            {
                
                switch(candle.Pair)
                {
                    case "tBTCUSD":
                        usdToBTC = candle.TotalPrice;
                        AddMessage($"Получена свеча. [BTC: {usdToBTC}$]");
                        break;
                    case "tXMRUSD":
                        usdToXMR = candle.TotalPrice;
                        AddMessage($"Получена свеча. [XMR: {usdToXMR}$]");
                        break;
                    case "tXRPUSD":
                        usdToXRP = candle.TotalPrice;
                        AddMessage($"Получена свеча. [XRP: {usdToXRP}$]");
                        break;
                    case "tDSHUSD":
                        usdToDSH = candle.TotalPrice;
                        AddMessage($"Получена свеча. [DASH: {usdToDSH}$]");
                        break;
                    case "tUSTUSD":
                        usdToUSDt = candle.TotalPrice;
                        AddMessage($"Получена свеча. [USDT: {usdToUSDt}$]");
                        break;
                    default:
                        AddMessage($"Получена свеча. [Unknown]");
                        break;
                }
                var USD = usdToBTC + usdToXMR * 50 + usdToXRP * 15000 + usdToDSH * 30;
                var newMoney = new Money
                {
                    TotalUSDT = USD / usdToUSDt,
                    TotalBTC = USD / usdToBTC,
                    TotalDASH = USD / usdToDSH,
                    TotalXMR = USD / usdToXMR,
                    TotalXRP = USD / usdToXRP,
                };
                dataGrid.ItemsSource = new ObservableCollection<Money>
            {
               newMoney
            };
            };
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += ProcessMessageQueue;
            _timer.Start();

            EventsInit();
            UpdateButtons();

            GetFromRest();

        }
          
        void GetFromRest()
        {
            var USD = GetTotalUSDRest();
            AddMessage("Сумма в USD: " + USD.ToString() + "$");
            var newMoney = new Money
            {
                TotalUSDT = USD / GetFromUSDRest("UST"),
                TotalBTC = USD / usdToBTC,
                TotalDASH = USD / usdToDSH,
                TotalXMR = USD / usdToXMR,
                TotalXRP = USD / usdToXRP,
            };
            dataGrid.ItemsSource = new ObservableCollection<Money>
            {
               newMoney
            };
        }

        decimal GetFromUSDRest(string currency)
        {
            return GetPairRestCandle(currency + "USD");
        }

        bool _isConnected = false;
        bool _isEnabledConnectButton = true;
        void UpdateButtons()
        {
            if(!_isEnabledConnectButton)
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
            
            if (_isSubscribed)
            {
                _isSubscribed = true;
                _connector.UnsubscribeCandles("tBTCUSD");
                _connector.UnsubscribeCandles("tXMRUSD");
                _connector.UnsubscribeCandles("tXRPUSD");
                _connector.UnsubscribeCandles("tDSHUSD");
                _connector.UnsubscribeCandles("tUSTUSD");
            }
            _isEnabledConnectButton = false;
            UpdateButtons();

        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateButtons();

            GetFromRest();
        }
        private bool _isSubscribed = false;
        private void btnCon_Click(object sender, RoutedEventArgs e)
        {

            _isEnabledConnectButton = true;
            if (!_isConnected)
            {
                _isConnected = true;
                _connector.Connect();
            }
            if(!_isSubscribed)
            {
                _isSubscribed = true;
                _connector.SubscribeCandles("tBTCUSD", 0, null, null, 1);
                _connector.SubscribeCandles("tXMRUSD", 0, null, null, 1);
                _connector.SubscribeCandles("tXRPUSD", 0, null, null, 1);
                _connector.SubscribeCandles("tDSHUSD", 0, null, null, 1);
                _connector.SubscribeCandles("tUSTUSD", 0, null, null, 1);
            }
            UpdateButtons();
        }
        decimal GetUSDRest(string currancy, decimal value)
        {
            return GetPairRestCandle(currancy + "USD") * value;
        }
        decimal GetTotalUSDRest()
        {
            usdToBTC = GetUSDRest("BTC", 1);
            usdToXRP = GetUSDRest("XRP", 15000);
            usdToXMR = GetUSDRest("XMR", 50);
            usdToDSH = GetUSDRest("DSH", 30);
            return usdToBTC + usdToXRP + usdToXMR + usdToDSH;
        }

        private decimal usdToBTC = 1, usdToXRP = 1, usdToXMR = 1, usdToDSH = 1, usdToUSDt = 1;
        decimal GetPairRestCandle(string pair)
        {
            decimal result = 0;
            List<Candle> candles = (List<Candle>)_connector
                .GetCandleSeriesAsync("t" + pair, 0, null, null, 1).Result;
            if (candles.Count != 1)
                return 0m;
            return result += candles.First().TotalPrice;
        }
    }
}
