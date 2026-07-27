using Dsw2026Tpi.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface ISpecialityService
    {
        PaginatedResponse<SpecialityModel> GetSpecialities(SpecialityQueryFilter filter);
        SpecialityModel Createspeciality(SpecialityCreateModel model);
        SpecialityModel UpdateSpeciality(int id, SpecialityCreateModel model);
        bool DeleteSpeciality(int id);

    }
}
