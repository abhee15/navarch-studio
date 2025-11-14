using System.Collections.Generic;
using Shared.DTOs;

namespace HullSizingService.Services.Geometry;

public interface IShipDToHydroMapper
{
    (List<StationDto> stations, List<WaterlineDto> waterlines, List<OffsetDto> offsets) ConvertSections(HullSectionsDto sections, decimal lpp);
}
