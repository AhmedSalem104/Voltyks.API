using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Charger
{
    public class NearChargerSearchDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double SearchRangeInKm { get; set; }
        public int ProtocolId { get; set; }


        // إضافات للpagination
        public int PageNumber { get; set; } = 1;   // الصفحة الافتراضية 1
        public int PageSize { get; set; } = 10;    // عدد النتائج في كل صفحة
    }

}
