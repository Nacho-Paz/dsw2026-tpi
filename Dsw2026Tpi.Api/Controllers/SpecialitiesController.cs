using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Dsw2026Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpecialitiesController : ControllerBase
    {

        private readonly ISpecialityService _specialityService;
        public SpecialitiesController(ISpecialityService specialityService)
        {
            _specialityService = specialityService;
        }

        //ENDPOINTS//

        [HttpGet]
        public async Task<ActionResult<Pagination<SpecialityModel>>> Get([FromQuery] SpecialityQueryFilter filter)
        {
            var result = await _specialityService.GetSpecialities(filter);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SpecialityCreateModel model)
        {
            var result = await _specialityService.Createspeciality(model);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SpecialityModel>>Put(Guid id, [FromBody] SpecialityCreateModel model)
        {
            var result = await _specialityService.UpdateSpeciality(id, model);
            if(result==null) return NotFound();
            return Ok(result);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _specialityService.DeleteSpeciality(id);
            if (!result) return NotFound();
            return NoContent();
        }

    }
}
