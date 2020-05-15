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
            CreateMap<UserWallet, WalletInfo>()
                .ForMember(x => x.Balances, x => x.MapFrom(src => src.Balances.Where(z => z.Balance > 0)));

            CreateMap<MoneyTransfer, TransferInfo>()
                .ForMember(x => x.From, x=> x.MapFrom(src => src.FromCurrencyId))
                .ForMember(x => x.To, x => x.MapFrom(src => src.ToCurrencyId))
                .ForMember(x=> x.Rate, x=> x.MapFrom(src => src.ActualCurrencyRate > 0 ? src.ActualCurrencyRate : (double?)null));
        }
    }
}
