using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebWallet.API.v1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion(ApiConstants.V1)]
    public class TransferController : ControllerBase
    {
    }
}