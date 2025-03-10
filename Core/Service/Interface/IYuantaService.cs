using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service.Interface
{
    public interface IYuantaService
    {
        string FunAPILogin_Out(byte[] abyData);
        string FunRealStocktick_Out(byte[] abyData);
    }
}
