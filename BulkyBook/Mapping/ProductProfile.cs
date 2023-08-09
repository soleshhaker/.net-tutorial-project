using AutoMapper;
using Bulky.Models.DTO;
using Bulky.Models.ViewModels;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.ProductImage, opt => opt.MapFrom(src => src.ProductImages.FirstOrDefault()));
        }
    }
}
