using AutoMapper;
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
    [Produces("application/json", "application/xml")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var userEntity = userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();
        
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
}
