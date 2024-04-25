using Microsoft.AspNetCore.Mvc;

namespace Task7.Controller;

public class WarhouseController
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public WarehouseController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
    }
}