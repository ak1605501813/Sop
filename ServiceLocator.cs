﻿using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MstSopService
{
    public static class ServiceLocator
    {
        public static IApplicationBuilder ApplicationBuilder { get; set; }
    }
}
