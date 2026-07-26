using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.Domain.Entities;

public interface ISpecialityRepository
{
    Task<Speciality?> GetByIdAsync(Guid id);
    Task<IEnumerable<Speciality>> GetAllAsync();
    Task AddAsync(Speciality speciality);
    Task UpdateAsync(Speciality speciality);

   
}

