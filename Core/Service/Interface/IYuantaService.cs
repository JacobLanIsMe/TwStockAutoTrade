using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service.Interface
{
    public interface IYuantaService
    {
        string FunRealWatchlist_Out(byte[] abyData);
        string FunAPILogin_Out(byte[] abyData);
    }
}
