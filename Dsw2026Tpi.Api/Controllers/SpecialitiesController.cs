using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Dsw2026Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpecialtiesController : ControllerBase
    {

        private readonly ISpecialtyService _specialtyService;
        public SpecialtiesController(ISpecialtyService specialtyService)
        {
            _specialtyService = specialtyService;
        }

        //ENDPOINTS//

        [HttpGet]
        public async Task<ActionResult<Pagination<SpecialtyModel>>> Get([FromQuery] SpecialtyQueryFilter filter)
        {
            var result = await _specialtyService.GetSpecialties(filter);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SpecialtyCreateModel model)
        {
            var result = await _specialtyService.Createspecialty(model);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<SpecialtyModel>>Put(Guid id, [FromBody] SpecialtyCreateModel model)
        {
            var result = await _specialtyService.UpdateSpecialty(id, model);
            if(result==null) return NotFound();
            return Ok(result);
        }


        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _specialtyService.DeleteSpecialty(id);
            if (!result) return NotFound();
            return NoContent();
        }

    }
}
