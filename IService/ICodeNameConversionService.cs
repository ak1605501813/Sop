using MstSopService.DTOs;
using System.Collections.Generic;

namespace MstSopService.IService
{
    public interface ICodeNameConversionService
    {
        List<CodeNamesDTO> GetCodeNamesDatas();
    }
}
