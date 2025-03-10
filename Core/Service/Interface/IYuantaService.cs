using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YuantaOneAPI;

namespace Core.Service.Interface
{
    public interface IYuantaService
    {
        void SystemResponseHandler(string strResult, YuantaOneAPITrader objYuantaOneAPI, string account, string password, CancellationTokenSource cts, Action subscribeStockTick);
        string FunAPILogin_Out(byte[] abyData);
        string FunRealStocktick_Out(byte[] abyData);
        string FunRealWatchlist_Out(byte[] abyData);
    }
}
