// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.AspNetCore.Mvc;
using SharingService.Data;
using System.Threading.Tasks;

namespace SharingService.Controllers
{
    [Route("api/apptoken")]
    [ApiController]
    public class AppTokenController : ControllerBase
    {
        private SpatialAnchorsTokenService tokenService;

        public AppTokenController(SpatialAnchorsTokenService tokenService)
        {
            this.tokenService = tokenService;
        }

        // GET api/apptoken
        [HttpGet]
        public Task<string> GetAsync()
        {
            // TODO: Put your application-specific authorization and authentication logic here

            return this.tokenService.RequestToken();
        }
    }
}
