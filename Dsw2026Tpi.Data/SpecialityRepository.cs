using Dsw2026Tpi.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Data
{
    public class SpecialityRepository : ISpecialityRepository
    {

        private readonly Dsw2026TpiDbContext _context;
        public SpecialityRepository(Dsw2026TpiDbContext context)
        {
            _context = context;
        }
        public async Task<Speciality?> GetByIdAsync(Guid id)
        {
            return await _context.Set<Speciality>().FindAsync(id);


        }

        public async Task<IEnumerable<Speciality>> GetAllAsync()
        {
            return await _context.Set<Speciality>().ToListAsync();

        }

        public async Task AddAsync(Speciality speciality)
        {
            await _context.Set<Speciality>().AddAsync(speciality);

        }

        public Task UpdateAsync(Speciality speciality)
        {
            _context.Set<Speciality>().Update(speciality);
            return Task.CompletedTask;
        }

        




    }
}
