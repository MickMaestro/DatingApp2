using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DatingApp.API.Data;
using AutoMapper;
using Microsoft.Extensions.Options;
using DatingApp.API.Helpers;
using CloudinaryDotNet;
using System.Threading.Tasks;
using System.Security.Claims;
using CloudinaryDotNet.Actions;
using DatingApp.API.Models;
using System.Linq;
using DatingApp.API.Dtos;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _reppy;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository reppy, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _mapper = mapper;
            _reppy = reppy;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName="mickymaestro",
                _cloudinaryConfig.Value.ApiKey="446895824347624",
                _cloudinaryConfig.Value.ApiSecret="ef5dXaFRh2Q-INs5gY4T_1shyq0"
            );

            _cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo = await _reppy.GetPhoto(id);
            // stores photo

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);
            return Ok(photo);
        }
        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser (int userId,
         [FromForm]Dtos.PhotoForCreationDto photoForCreationDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            var userFromRepo = await _reppy.GetUser(userId);
            var file =photoForCreationDto.File;
            var uploadResult = new ImageUploadResult();
            if(file.Length>0)
            {
                using (var stream =file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation()
                        .Width(500).Height(500).Crop("fill").Gravity("face")
                    };

                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }

            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            if(!userFromRepo.Photos.Any(u => u.IsMain))
            {
                photo.IsMain = true;
            }

            userFromRepo.Photos.Add(photo);

            if(await _reppy.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto", new { id = photo.Id}, photoToReturn);
                // later work on returning a CreatedAtRoute response; object that produces a http status repsonse (201)
                // has three overloads
                // alternative is return Ok()
            }

            return BadRequest("Couldn't add the picture.");
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            // checks if the user is authorized to make changes to the main pic
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return Unauthorized();
            }
            // makes sure the user is trying to update one of their own photos
            var user = await _reppy.GetUser(userId);
            if(!user.Photos.Any(p => p.Id == id))// checks if the pic exists in the user's photo collection
            {
                return Unauthorized();
            }

            var photoFromRepo = await _reppy.GetPhoto(id);// gets the pic from the repository
            if(photoFromRepo.IsMain)// checks if the pic is the Main pic
            {
                return BadRequest("This is already the main photo");
            }

            var currentMainPhoto = await _reppy.GetMainPhotoForUser(userId);// get current main photo from the repository and make the new pic the main one
            currentMainPhoto.IsMain = false;// set previous pic status to not main

            photoFromRepo.IsMain = true;// set new pic status to be main
            if(await _reppy.SaveAll())
            {
                return NoContent();
            }

            return BadRequest("Can't set photo to main");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var user = await _reppy.GetUser(userId);

            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized();

            var photoFromRepo = await _reppy.GetPhoto(id);

            if (photoFromRepo.IsMain)
                return BadRequest("You cannot delete your main photo");

            if (photoFromRepo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);

                var result = _cloudinary.Destroy(deleteParams);

                if (result.Result == "ok")
                {
                    _reppy.Delete(photoFromRepo);
                }
            }

            if (photoFromRepo.PublicId == null)
            {
                _reppy.Delete(photoFromRepo);
            }

            if (await _reppy.SaveAll())
                return Ok();

            return BadRequest("Failed to delete the photo");
        }
    }
}