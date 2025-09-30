using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return NotFound();
        }

        var userEntity = userRepository.FindById(userGuid);
        if (userEntity == null)
            return NotFound();
        
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.ContentType = Request.Headers.Accept.ToString().Contains("application/xml") 
                ? "application/xml; charset=utf-8" 
                : "application/json; charset=utf-8";
            return Ok();
        }
        
        var userDto = mapper.Map<UserDto>(userEntity);
        return Ok(userDto);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto user)
    {
        // Проверка на null (когда пришел пустой или некорректный контент)
        if (user == null)
        {
            return BadRequest();
        }

        // Проверка, что логин состоит только из букв и цифр
        if (user.Login != null && !user.Login.All(char.IsLetterOrDigit))
        {
            ModelState.AddModelError("Login", "Login should contain only letters or digits");
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        // Создаем UserEntity из DTO с помощью AutoMapper
        var userEntity = mapper.Map<UserEntity>(user);
        
        // Вставляем в репозиторий (ID будет назначен автоматически)
        var createdUserEntity = userRepository.Insert(userEntity);
        
        // Возвращаем ID созданного пользователя
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUserEntity.Id },
            createdUserEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdateUser([FromRoute] string userId, [FromBody] UpdateUserDto user)
    {
        if (user == null)
        {
            return BadRequest();
        }

        if (!Guid.TryParse(userId, out var userGuid))
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var existingUser = userRepository.FindById(userGuid);
        bool isInserted = false;

        if (existingUser != null)
        {
            mapper.Map(user, existingUser);
            var updatedEntity = mapper.Map(user, new UserEntity(userGuid));
            userRepository.UpdateOrInsert(updatedEntity, out isInserted);
            
            return NoContent();
        }
        else
        {
            var userEntity = mapper.Map(user, new UserEntity(userGuid));
            userRepository.UpdateOrInsert(userEntity, out isInserted);
            
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userGuid },
                userGuid);
        }
    }
    
    [HttpPatch("{userId}")]
    [Consumes("application/json-patch+json")]
    [Produces("application/json")]
    public IActionResult PartiallyUpdateUser([FromRoute] string userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {   
        if (patchDoc == null)
        {
            return BadRequest();
        }

        if (!Guid.TryParse(userId, out var userGuid))
        {
            return NotFound();
        }

        var existingUser = userRepository.FindById(userGuid);
        if (existingUser == null)
        {
            return NotFound();
        }

        var userToPatch = mapper.Map<UpdateUserDto>(existingUser);

        patchDoc.ApplyTo(userToPatch, ModelState);

        TryValidateModel(userToPatch);

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        mapper.Map(userToPatch, existingUser);
        userRepository.Update(existingUser);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return NotFound();
        }

        var existingUser = userRepository.FindById(userGuid);
        if (existingUser == null)
        {
            return NotFound();
        }

        userRepository.Delete(userGuid);

        return NoContent();
    }
}
