using AutoMapper;
using couch_backend.ModelDTOs.Requests;
using couch_backend.ModelDTOs.Responses;
using couch_backend.Models;
using couch_backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace couch_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SubscriptionsController : Controller
    {
        private readonly ILogger<SubscriptionsController> _logger;
        private IMapper _mapper { get; }
        private readonly ISubscriptionRepository _subscriptionRepository;

        public SubscriptionsController(
            ILogger<SubscriptionsController> logger,
            IMapper mapper,
            ISubscriptionRepository subscriptionRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _subscriptionRepository = subscriptionRepository;
        }

        /// <summary>Subscribe to Coming soon</summary>
        /// <param name="model"></param>
        [HttpPost]
        [Route("coming-soon")]
        [ProducesResponseType(typeof(DataResponseDTO<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PostApplicationUser([FromBody] ComingSoonDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ModelStateErrorResponseDTO(
                    HttpStatusCode.BadRequest, ModelState));

            var email = _subscriptionRepository.GetAsync(
                x => x.Email == model.Email
            ).Result.FirstOrDefault();

            if (email != null)
                return Ok(new DataResponseDTO<string>("Thanks, we will be in touch."));

            var subscription = _mapper.Map<Subscription>(model);

            try
            {
                await _subscriptionRepository.InsertAsync(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while recording email for coming soon");
            }

            return Ok(new DataResponseDTO<string>("Email received, we will be in touch."));
        }
    }
}
