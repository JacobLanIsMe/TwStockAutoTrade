using Core.Model;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Service
{
    public class FutureTraderService : IFutureTraderService
    {
        YuantaOneAPITrader objYuantaOneAPI = new YuantaOneAPITrader();
        private readonly enumEnvironmentMode _enumEnvironmentMode;

        private readonly ILogger _logger;
        private readonly IYuantaService _yuantaService;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _responseTasks = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
        private readonly string _futureAccount;
        private readonly string _futurePassword;
        private readonly string _targetFutureCode;
        private readonly int _maxOrderQuantity;
        public FutureTraderService(IConfiguration config, ILogger logger, IYuantaService yuantaService)
        {
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
            string environment = config.GetValue<string>("Environment").ToUpper();
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _logger = logger;
            _yuantaService = yuantaService;
            _futureAccount = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("FutureAccount") : "S98875005091";
            _futurePassword = _enumEnvironmentMode == enumEnvironmentMode.PROD ? Environment.GetEnvironmentVariable("FuturePassword") : "1234";
            _targetFutureCode = config.GetValue<string>("TargetFutureCode");
            _maxOrderQuantity = config.GetValue<int>("MaxOrderQuantity");
        }
        public async Task Trade()
        {
            try
            {
                bool isConnected = await TryConnectToYuanta();
                if (!isConnected) throw new Exception("無法連線到元大");
                bool isLogin = await Login();
                if (!isLogin) throw new Exception("登入失敗");
                SubscribeWatchList();
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                objYuantaOneAPI.LogOut();
                objYuantaOneAPI.Close();
                objYuantaOneAPI.Dispose();
            }
        }
        private async Task<bool> TryConnectToYuanta()
        {
            string result = await CallYuantaApiAsync("Open", () => objYuantaOneAPI.Open(_enumEnvironmentMode));
            if (result.Contains("Connected")) return true;
            return false;
        }
        private async Task<bool> Login()
        {
            string result = await CallYuantaApiAsync("Login", () => objYuantaOneAPI.Login(_futureAccount, _futurePassword));
            if (result.Contains(_futureAccount)) return true;
            return false;
        }
        private void SubscribeWatchList()
        {
            List<Watchlist> lstWatchlist = new List<Watchlist>();
            enumMarketType enumMarketNo = enumMarketType.TAIFEX;
            Watchlist watch = new Watchlist();
            watch.IndexFlag = Convert.ToByte(7);                //填入訂閱索引值, 7: 成交價
            watch.MarketNo = Convert.ToByte(enumMarketNo);      //填入查詢市場代碼
            watch.StockCode = _targetFutureCode;                //填入查詢股票代碼
            lstWatchlist.Add(watch);
            objYuantaOneAPI.SubscribeWatchlist(lstWatchlist);
        }
        private async Task<string> CallYuantaApiAsync(string apiName, Action action = null, Func<bool> func = null)
        {
            var tcs = new TaskCompletionSource<string>();

            // 註冊 TaskCompletionSource 等待回應
            if (!_responseTasks.TryAdd(apiName, tcs))
            {
                return "Call API failed, duplicated key";
            }
            try
            {
                if (action != null)
                {
                    action();
                }
                if (func != null)
                {
                    func();
                }
            }
            catch (Exception ex)
            {
                _responseTasks.TryRemove(apiName, out _);
                return $"Call API failed: {ex.Message}";
            }

            // 設定超時機制（例如 10 秒）
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            // 無論成功或失敗都移除註冊
            _responseTasks.TryRemove(apiName, out _);

            if (completedTask == tcs.Task)
            {
                return tcs.Task.Result; // 成功取得回應
            }
            else
            {
                return "API timeout"; // 超時未回應
            }
        }
        void objApi_OnResponse(int intMark, uint dwIndex, string strIndex, object objHandle, object objValue)
        {
            string strResult = "";
            try
            {
                switch (intMark)
                {
                    case 0: //系統回應
                        strResult = Convert.ToString(objValue);
                        break;
                    case 1: //代表為RQ/RP 所回應的
                        switch (strIndex)
                        {
                            case "Login":       //一般/子帳登入
                                strResult = _yuantaService.FunAPILogin_Out((byte[])objValue);
                                break;
                            default:           //不在表列中的直接呈現訊息
                                {
                                    if (strIndex == "")
                                        strResult = Convert.ToString(objValue);
                                    else
                                        strResult = String.Format("{0},{1}", strIndex, objValue);
                                }
                                break;
                        }
                        break;
                    case 2: //訂閱所回應
                        switch (strIndex)
                        {
                            case "210.10.70.11":    //Watchlist報價表(指定欄位)
                                strResult = _yuantaService.FunRealWatchlist_Out((byte[])objValue);
                                break;
                            default:
                                {
                                    if (strIndex == "")
                                        strResult = Convert.ToString(objValue);
                                    else
                                        strResult = String.Format("{0},{1}", strIndex, objValue);
                                }
                                break;
                        }
                        break;
                    default:
                        strResult = Convert.ToString(objValue);
                        break;
                }
                _logger.Information(strResult);
            }
            catch (Exception ex)
            {
                strResult = "Error: " + ex;
                _logger.Error(strResult);
            }
            string key = intMark == 0 ? "Open" : strIndex;
            if (_responseTasks.TryGetValue(key, out var tcs))
            {
                tcs.TrySetResult(strResult);
            }
        }
    }
}
