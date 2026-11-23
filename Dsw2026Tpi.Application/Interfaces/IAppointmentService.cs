using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentModel.Response> CreateAppointmentAsync(
        AppointmentModel.Request request,
        Guid patientUserId);
    Task CancelAppointmentAsync(Guid appointmentId, Guid? patientUserId = null);
    Task<object> GetActiveAppointmentsByPatientAsync(long dni, Guid? patientUserId = null);
    Task<object> GetAppointmentsByDateAsync(string date);
    Task<Pagination<AppointmentModel.SearchItem>> SearchAppointmentsAsync(int pageSize, int pageIndex,
        Guid? specialtyId, Guid? doctorId, long? dni, string date);
}
