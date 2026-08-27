using AutoMapper;
using EventHub.Application.Common.Dtos.Registrations;
using EventHub.Domin.Models;

namespace EventHub.Application.Common.Mapping.Registrations
{
    public class RegistrationProfile : Profile
    {
        public RegistrationProfile()
        {
            CreateMap<Registration, UserRegistrationDto>()
                .ForMember(des => des.EventTitle, opt => opt.MapFrom(src => src.Event.Title))
                .ForMember(des => des.EventLocation, opt => opt.MapFrom(src => src.Event.Location))
                .ForMember(des => des.EventStartDate, opt => opt.MapFrom(src => src.Event.EventDate));

            CreateMap<Registration, EventRegistrationDto>()
                .ForMember(dest => dest.RegistrationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.AttendeeId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.AttendeeName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.AttendeeEmail, opt => opt.MapFrom(src => src.User.Email));

            CreateMap<Registration, AdminRegistrationDto>()
                .ForMember(dest => dest.RegistrationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.AttendeeId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.AttendeeName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.AttendeeEmail, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.Event.Title))
                .ForMember(dest => dest.EventDate, opt => opt.MapFrom(src => src.Event.EventDate))
                .ForMember(dest => dest.RegistrationStatus, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentTransactions
                    .OrderByDescending(transaction => transaction.CreatedAt)
                    .Select(transaction => (EventHub.Domin.Enums.PaymentTransactionStatus?)transaction.Status)
                    .FirstOrDefault()))
                .ForMember(dest => dest.PaymentAmount, opt => opt.MapFrom(src => src.PaymentTransactions
                    .OrderByDescending(transaction => transaction.CreatedAt)
                    .Select(transaction => (decimal?)transaction.Amount)
                    .FirstOrDefault()))
                .ForMember(dest => dest.PaymentCurrency, opt => opt.MapFrom(src => src.PaymentTransactions
                    .OrderByDescending(transaction => transaction.CreatedAt)
                    .Select(transaction => transaction.Currency)
                    .FirstOrDefault()));
        }
    }
}
