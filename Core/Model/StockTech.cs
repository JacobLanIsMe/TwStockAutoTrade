using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Model
{
    public class StockTech
    {
        public string StockCode { get; set; }
        public string CompanyName { get; set; } 
        public long IssuedShare { get; set; }
        public string TechData { get; set; }
        // 建立一個私有欄位來存放解碼後的資料
        private List<StockTechData> _techDataList;

        public List<StockTechData> TechDataList
        {
            get
            {
                // 如果還沒解碼過，就解碼一次；否則直接回傳現有的列表
                if (_techDataList == null && !string.IsNullOrEmpty(TechData))
                {
                    _techDataList = JsonConvert.DeserializeObject<List<StockTechData>>(TechData);
                }
                return _techDataList;
            }
        }
    }
}
