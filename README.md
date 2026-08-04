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


## 3. Antes de ejecutar el proyecto, se deben aplicar las migraciones de los dos contextos utilizados:  


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

   
