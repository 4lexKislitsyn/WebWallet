using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebWallet.API.v1.DTO;
using WebWallet.DB.Entities;

namespace WebWallet.API.AutomapperProfiles
{
    /// <summary>
    /// Profile to map database entity to data transfer objects.
    /// </summary>
    public class EntityToModelProfile: Profile
    {
        /// <summary>
        /// Create an instance of <see cref="EntityToModelProfile"/>.
        /// </summary>
        public EntityToModelProfile()
        {
            CreateMap<CurrencyBalance, BalanceInfo>();
            CreateMap<UserWallet, WalletInfo>();
        }
    }
}
