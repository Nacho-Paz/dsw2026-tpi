using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface ISpecialityService
    {
        Pagination<SpecialityModel> GetSpecialities(SpecialityQueryFilter filter);
        SpecialityModel Createspeciality(SpecialityCreateModel model);
        SpecialityModel UpdateSpeciality(int id, SpecialityCreateModel model);
        bool DeleteSpeciality(int id);


    }
}


