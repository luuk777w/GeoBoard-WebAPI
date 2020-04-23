using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using GeoBoardWebAPI.Attributes;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.DAL.Repositories;
using GeoBoardWebAPI.Models.Account;
using GeoBoardWebAPI.Models.Country;

namespace GeoBoardWebAPI.Controllers
{
    [TypeFilter(typeof(ValidateModelAttribute))]
    public class CountryController : BaseController
    {
        private readonly CountryRepository CountryRepository;

        public CountryController(
            CountryRepository countryRepository,
            IServiceProvider scopeFactory)
            : base(scopeFactory)
        {
            CountryRepository = countryRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get()
        {
            return await Ok<Country, CountryViewModel>(CountryRepository.GetAll(), (x => x.CreationDateTime, System.ComponentModel.ListSortDirection.Ascending));
        }

        // GET: api/Interest/john.doe@email.com
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            var dbObject = await CountryRepository.GetAll().SingleOrDefaultAsync(m => m.Id == id);

            if (dbObject == null)
            {
                return NotFound();
            }

            var viewModel = _mapper.Map<CountryViewModel>(dbObject);

            return Ok(viewModel);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] Guid id, [FromBody] CountryMutateModel mutateModel)
        {
            var dbObject = await CountryRepository.FindAsync(id);
            dbObject = _mapper.Map(mutateModel, dbObject);

            if (id != dbObject.Id)
            {
                return BadRequest();
            }

            CountryRepository.Update(dbObject);

            try
            {
                await CountryRepository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ObjectExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CountryMutateModel mutateModel)
        {
            var dbObject = _mapper.Map<Country>(mutateModel);

            CountryRepository.Add(dbObject);
            await CountryRepository.SaveChangesAsync();

            return CreatedAtAction("Get", new { id = dbObject.Id }, dbObject);
        }

        // DELETE: api/Settings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var dbObject = await CountryRepository.FindAsync(id);
            if (dbObject == null)
            {
                return NotFound();
            }

            CountryRepository.Remove(dbObject);
            await CountryRepository.SaveChangesAsync();

            var viewModel = _mapper.Map<CountryViewModel>(dbObject);

            return Ok(viewModel);
        }

        private bool ObjectExists(Guid id)
        {
            return null != CountryRepository.Find(id);
        }
    }
}
