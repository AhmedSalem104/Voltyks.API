using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.Common;

namespace Voltyks.Core.DTOs.ChargerRequest
{

    public class TransRequest : ILocalizedRequest
    {
        public int RequestId { get; set; }

        /// <summary>Optional language for the notification ("en"/"ar"). Defaults to "en".</summary>
        public string? Lang { get; set; }
    }
}
