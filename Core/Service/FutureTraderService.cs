using Core.Model;
using Core.Service.Interface;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
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
        public FutureTraderService(IConfiguration config, ILogger logger, IYuantaService yuantaService)
        {
            objYuantaOneAPI.OnResponse += new OnResponseEventHandler(objApi_OnResponse);
            string environment = config.GetValue<string>("Environment").ToUpper();
            _enumEnvironmentMode = environment == "PROD" ? enumEnvironmentMode.PROD : enumEnvironmentMode.UAT;
            _logger = logger;
            _yuantaService = yuantaService;
        }
        public async Task Trade()
        {
            objYuantaOneAPI.Open(_enumEnvironmentMode);
        }
        private 
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
                    default:
                        strResult = Convert.ToString(objValue);
                        break;
                }
                _logger.Information(strResult);
            }
            catch (Exception ex)
            {
                _logger.Error("Error: " + ex);
            }
        }
    }
}
