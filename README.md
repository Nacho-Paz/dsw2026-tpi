# Trabajo Práctico Integrador
## Desarrollo de Software 2026

### 👤 Integrantes
| Legajo | Nombre |
| ------------- | ------ |  
| 58096   | Coronel Arrieta, Ana Milena |
| 57892   | Luna Acosta, Lourdes Valentina |
| 57828   | Paz, Ignacio Javier | 
| 58350   | Zambrano, Agustina Nahir |



### Requisitos previos
* Visual Studio 2026 (con carga de trabajo ASP.NET y desarrollo web)
* SQL Server LocalDB
* .NET SDK 8
* Swagger / OpenAPI y Serilog.
* 
### 1. Clonar el repositorio y crear la rama development
git clone https://github.com/Nacho-Paz/dsw2026-tpi
git checkout -b development

## 2. Configurar Base de datos
Verifica la cadena de conexión en  
`Dsw2026Tpi.Api/appsettings.Development.json` 
Por defecto utiliza esta configuración: 

"ConnectionStrings": {
  "DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Database=Dsw2026Tpi;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True" 
} 
En caso de utilizar otra instancia de SQL Server, modificar `DefaultConnection` según corresponda.


## 3. Antes de ejecutar el proyecto, se deben aplicar las migraciones de los dos contextos utilizados: 

# Migración para el contexto del dominio
dotnet ef database update --context Dsw2026TpiDbContext --project Dsw2026Tpi.Data --startup-project Dsw2026Tpi.Api

# Migración para el contexto de autenticación e identidad
dotnet ef database update --context AuthenticationDbContext --project Dsw2026Tpi.Data --startup-project Dsw2026Tpi.Api

Ambos contextos utilizan la misma cadena de conexión. 

## 4. Ejecución de la API 
dotnet restore
dotnet build
dotnet run --project Dsw2026Tpi.Api

## Endpoints implementados:
   ## Autenticación (/api/auth)
   POST /api/auth/admin/login: Autentica al administrador y devuelve el token JWT. 
   POST /api/auth/patient/login: Autentica o registra automáticamente a un paciente, devolviendo su JWT.  
   
   ## Especialidades (/api/specialties)
   GET /api/specialties?pageSize=&pageIndex=&name=: Lista especialidades activas con paginación y filtro opcional.
   POST /api/specialties: Da de alta una nueva especialidad.  
   PUT /api/specialties/{id}: Actualiza una especialidad existente.  
   DELETE /api/specialties/{id}: Realiza la eliminación lógica (soft delete).  
   
   ## Médicos (/api/doctors)
   GET /api/doctors?pageSize=&pageIndex=&name=: Lista el directorio paginado de médicos activos.  
   GET /api/doctors/{id}/availabilities: Retorna la grilla de disponibilidad semanal del profesional.  
   POST /api/doctors: Registra un nuevo médico asociado a una especialidad
   PUT /api/doctors/{id}: Modifica los datos de un médico
   DELETE /api/doctors/{id}: Baja lógica del profesional 

   ## Disponibilidades (/api/availabilities) 
   POST /api/availabilities: Registra reglas semanales y genera turnos de 30 minutos omitiendo feriados.
   PUT /api/availabilities: Actualiza las reglas del mes conservando los turnos que ya fueron reservados por pacientes.

   ## Citas Médicas (/api/appointments) 
   POST /api/appointments: Reserva un turno enviando DoctorId, AvailabilitySlotId, Dni del paciente y motivo (Reason). 
   GET /api/appointments: Consulta la grilla de turnos para una fecha (?date=YYYY-MM-DD). 
   GET /api/appointments/search: Búsqueda avanzada paginada combinando filtros (?pageSize=10&pageIndex=1&specialtyId={id}&doctorId={id}&dni={dni}&date=YYYY-MM-DD). 
   GET /api/appointments/patient: Obtiene los turnos activos reservados para un DNI de paciente (?dni=12345678). 
   DELETE /api/appointments/{id}: Cancela una cita reservada y libera la franja horaria correspondiente.
   

   
