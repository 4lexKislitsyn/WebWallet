using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.AutomapperProfiles
{
    /// <summary>
    /// Profile to map input models to database entity.
    /// </summary>
    public class ModelToEntityProfile : Profile
    {
        /// <summary>
        /// Create an instance of <see cref="ModelToEntityProfile"/>.
        /// </summary>
        public ModelToEntityProfile()
        {
            CreateMap<v1.Models.CreateTransfer, DB.Entities.MoneyTransfer>()
                .ForMember(x => x.FromCurrencyId, x => x.MapFrom(z => z.From))
                .ForMember(x => x.ToCurrencyId, x => x.MapFrom(z => z.To))
                .ForMember(x=> x.WalletId, x=> x.MapFrom(z=> z.WalletId.ToString()));
        }
    }
}
