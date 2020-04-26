using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using GeoBoardWebAPI.Attributes;
using GeoBoardWebAPI.DAL.Entities;
using GeoBoardWebAPI.DAL.Repositories;
using GeoBoardWebAPI.Extensions.Authorization;
using GeoBoardWebAPI.Models.Account;

namespace GeoBoardWebAPI.Controllers
{
    [Authorize]
    [TypeFilter(typeof(ValidateModelAttribute))]
    public class UserController : BaseController
    {
        //private readonly UserRepository UserRepository;
        private readonly AppUserManager UserManager;

        public UserController(
            //UserRepository userRepository,
            AppUserManager userManager,
            IServiceProvider scopeFactory)
            : base(scopeFactory)
        {
            //UserRepository = userRepository;
            UserManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Get()
        {
            var result = UserRepository.GetAll();
            return await Ok<User, UserViewModel>(result, (x => x.CreatedAt, System.ComponentModel.ListSortDirection.Ascending));
        }

        // GET: api/Interest/john.doe@email.com
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            var dbObject = await UserRepository.GetAll().SingleOrDefaultAsync(m => m.Id == id);

            if (dbObject == null)
            {
                return NotFound();
            }

            var viewModel = _mapper.Map<UserViewModel>(dbObject);

            return Ok(viewModel);
        }

        [HttpGet("Profile/Get")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await UserRepository.GetAll()
                .Include(x => x.Person)
                .ThenInclude(x => x.Country)
                .Include(x => x.Settings)
                .SingleOrDefaultAsync(m => m.Id == GetUserId());

            if (user == null)
            {
                return NotFound();
            }

            var userViewModel = _mapper.Map<UserViewModel>(user);
            userViewModel.Roles = await UserManager.GetRolesAsync(user);

            return Ok(userViewModel);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] Guid id, [FromBody] UserMutateModel mutateModel)
        {
            var dbObject = await UserRepository.FindAsync(id);
            dbObject = _mapper.Map(mutateModel, dbObject);

            if (id != dbObject.Id)
            {
                return BadRequest();
            }

            UserRepository.Update(dbObject);

            try
            {
                await UserRepository.SaveChangesAsync();
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
        public async Task<IActionResult> Post([FromBody] UserMutateModel mutateModel)
        {
            var dbObject = _mapper.Map<User>(mutateModel);

            UserRepository.Add(dbObject);
            await UserRepository.SaveChangesAsync();

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

            var dbObject = await UserRepository.FindAsync(id);
            if (dbObject == null)
            {
                return NotFound();
            }

            UserRepository.Remove(dbObject);
            await UserRepository.SaveChangesAsync();

            var viewModel = _mapper.Map<UserViewModel>(dbObject);

            return Ok(viewModel);
        }

        private bool ObjectExists(Guid id)
        {
            return null != UserRepository.Find(id);
        }
    }
}
