using AutoMapper;
using Bulky.Models;
using Bulky.Models.DTO;
using Bulky.Models.ViewModels;

namespace Mapping
{
    public class OrderHeaderProfile : Profile
    {
        public OrderHeaderProfile()
        {
            CreateMap<OrderViewModel, OrderHeader>();
            CreateMap<OrderViewModel, OrderHeaderDTO>();
            CreateMap<OrderHeaderDTO, OrderHeader>()
                .ForMember(dest => dest.ApplicationUser, opt => opt.Ignore()) // Ignore ApplicationUser for mapping                                                             
                .ReverseMap();
        }
    }
}