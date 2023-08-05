using AutoMapper;
using Bulky.Models;
using Bulky.Models.ViewModels;

namespace Mapping
{
    public class OrderHeaderProfile : Profile
    {
        public OrderHeaderProfile()
        {
            CreateMap<OrderViewModel, OrderHeader>()
              .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.OrderHeader.Name))
              .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.OrderHeader.PhoneNumber))
              .ForMember(dest => dest.StreetAddress, opt => opt.MapFrom(src => src.OrderHeader.StreetAddress))
              .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.OrderHeader.City))
              .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.OrderHeader.State))
              .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.OrderHeader.PostalCode))
              .ForMember(dest => dest.Carrier, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.OrderHeader.Carrier) ? src.OrderHeader.Carrier : null))
              .ForMember(dest => dest.TrackingNumber, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.OrderHeader.TrackingNumber) ? src.OrderHeader.TrackingNumber : null));     
        }
    }
}