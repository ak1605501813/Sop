﻿using Aspose.Words.Lists;
using MstSopService.Entity;
using SqlSugar;
using System.Collections.Generic;

namespace MstSopService.DTO
{
    [SugarTable("sop_base")]
    public class SopBaseTreeDTO: SopBase
    {
        [SugarColumn(IsIgnore = true)]
        public List<SopBaseTreeDTO> Subsets { set; get; }
    }
}
