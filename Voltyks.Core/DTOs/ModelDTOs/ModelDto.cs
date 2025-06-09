using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voltyks.Core.DTOs.BrandsDTOs;

namespace Voltyks.Core.DTOs.ModelDTOs
{
    public class ModelDto
    {
        public int ModelId { get; set; }
        public string ModelName { get; set; }
        public BrandDto Brand { get; set; }
    }



}
