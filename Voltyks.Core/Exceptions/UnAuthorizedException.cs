using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.Exceptions
{
    public class UnAuthorizedException(string messsage = "Invalid Email Or Password") :Exception( messsage)
    {
    }
}
