# Backend Guide

> Documento interno de seguimiento, pruebas y documentación técnica del backend del TPI DSW 2026.
>
> Este archivo debe mantenerse actualizado durante el desarrollo y la etapa de refactorización.

---

## 1. Objetivo del documento

Este documento tiene como objetivo centralizar información técnica del backend que puede resultar difícil de obtener únicamente a partir de los commits, Pull Requests o del código fuente.

Aquí se registran:

* Cambios y refactors importantes.
* Decisiones de implementación.
* Explicaciones de métodos complejos.
* Problemas encontrados y su solución.
* Estado actual de los endpoints.
* Requests utilizados para probarlos.
* Respuestas obtenidas.
* Casos que funcionan y casos que todavía presentan problemas.
* Casos pendientes de probar.

### Importante

Este documento es una **bitácora técnica viva**.

Cada integrante puede agregar información cuando:

1. Corrige un error importante.
2. Cambia el comportamiento de un endpoint.
3. Refactoriza una parte compleja.
4. Descubre una condición necesaria para que un endpoint funcione.
5. Encuentra un caso que produce un error.
6. Realiza una prueba que conviene conservar.
7. Detecta un problema que todavía queda pendiente.

---

# 2. Estado actual

## Rama de trabajo

```text
backend-fixes
```

## Estado general

| Área                  | Estado         | Observaciones                                |
| --------------------- | -------------- | -------------------------------------------- |
| Autenticación         | 🟡 En revisión | Se está revisando JWT, Identity y login      |
| Administradores       | 🟡 En revisión | Login y registro                             |
| Pacientes             | 🟡 En revisión | Primer acceso y asociación con Identity      |
| Especialidades        | 🟡 En revisión | CRUD                                         |
| Médicos               | 🟡 En revisión | CRUD y relación con especialidad             |
| Disponibilidades      | 🟡 En revisión | Reglas mensuales y generación de slots       |
| Turnos                | 🟡 En revisión | Reserva, cancelación y estados               |
| EF Core               | 🟡 En revisión | Relaciones, índices y configuraciones        |
| Soft Delete           | 🟡 En revisión | Verificar todos los DELETE                   |
| JWT                   | 🟡 En revisión | Claims, roles y validación                   |
| Manejo de excepciones | 🟡 En revisión | Validar detalles y códigos                   |
| Pruebas de endpoints  | 🟡 En progreso | Actualizar esta tabla a medida que se pruebe |

> Los estados deben actualizarse durante el desarrollo.

### Referencia de estados

* 🟢 Funciona
* 🟡 En revisión / parcialmente probado
* 🔴 No funciona
* ⚪ No probado
* 🔵 Funciona, pero requiere mejoras

---

# 3. Arquitectura

El flujo general esperado es:

```text
Controller
    ↓
Service
    ↓
Repository / Persistence
    ↓
EF Core
    ↓
SQL Server
```

Los DTOs se utilizan para separar los modelos expuestos por la API de las entidades de dominio.

La autenticación utiliza:

```text
Controller
    ↓
AuthenticationService
    ↓
ASP.NET Core Identity
    ↓
JWT
```

---

# 4. Bases de datos

El proyecto utiliza dos contextos:

### Base de datos de aplicación

```text
Dsw2026TpiDbContext
```

Contiene las entidades propias del sistema:

* Specialty
* Doctor
* AvailabilityRule
* AvailabilitySlot
* Patient
* Appointment

### Base de datos de autenticación

```text
AuthenticationDbContext
```

Contiene ASP.NET Core Identity:

* ApplicationUser
* ApplicationRole
* Users
* Roles
* UsersRoles
* UsersClaims
* UsersLogins
* RolesClaims
* UsersTokens

### Importante

`Patient` y `ApplicationUser` pertenecen a contextos diferentes.

Por lo tanto:

```text
Patient.UserId
```

se utiliza para relacionar lógicamente al paciente con su usuario de Identity.

No existe una navegación EF directa entre ambos contextos.

---

# 5. Identidad y autenticación

## Roles

Los roles utilizados por el sistema son:

```text
Administrador
Paciente
```

## Policies

```text
AdminPolicy
PatientPolicy
```

## JWT

El JWT identifica al usuario mediante:

```text
ApplicationUser.Id
```

El identificador del usuario se incluye en:

```text
sub
ClaimTypes.NameIdentifier
```

El rol se incluye mediante:

```text
ClaimTypes.Role
```

### Importante

El DNI del paciente **no debe utilizarse como identificador principal del JWT**.

El DNI pertenece al dominio de pacientes, mientras que `ApplicationUser.Id` pertenece al sistema de autenticación.

---

# 6. Autenticación de administrador

## Login

Endpoint:

```http
POST /...
```

### Request

```json
{
  "email": "admin@example.com",
  "password": "Password123!"
}
```

### Validaciones

* Email obligatorio.
* Email válido.
* Password obligatoria.
* Password mínima de 8 caracteres.
* Usuario existente.
* Usuario no eliminado.
* Password correcta.
* Usuario perteneciente al rol `Administrador`.

### Resultado esperado

```json
{
  "token": "...",
  "role": "Administrador"
}
```

### Estado

🟡 En revisión

---

# 7. Registro de administrador

## Endpoint

```http
POST /...
```

### Request

```json
{
  "email": "admin@example.com",
  "password": "Password123!"
}
```

### Validaciones

* Email obligatorio.
* Email válido.
* Password obligatoria.
* Password mínima de 8 caracteres.
* Identity valida además las reglas de complejidad configuradas.

### Flujo

```text
Crear ApplicationUser
        ↓
Asignar rol Administrador
        ↓
Retornar resultado
```

Si la creación del usuario funciona pero la asignación del rol falla, se intenta realizar rollback eliminando el usuario creado.

### Estado

🟡 En revisión

---

# 8. Login de paciente

## Endpoint

```http
POST /...
```

### Request

```json
{
  "email": "paciente@example.com",
  "dni": 12345678
}
```

### Validaciones

* Email obligatorio.
* Email válido.
* DNI obligatorio.
* DNI de 7 u 8 dígitos para el login definido por el TPI.

### Primer acceso

Si no existe un `Patient` con ese DNI:

```text
Buscar Patient por DNI
        ↓
No existe
        ↓
Buscar ApplicationUser por email
        ↓
¿Existe?
   ┌────┴────┐
   │         │
  NO        SÍ
   │         │
Crear       Validar
User        User
   │         │
   └────┬────┘
        ↓
Asignar rol Paciente
        ↓
Crear Patient
        ↓
Generar JWT
```

### Importante

El primer acceso del paciente no requiere password.

El sistema crea automáticamente el usuario y lo asigna al rol:

```text
Paciente
```

### Resultado esperado

```json
{
  "token": "...",
  "role": "Paciente"
}
```

### Estado

🟡 En revisión

---

# 9. Manejo de errores en autenticación

Los errores de autenticación no deberían revelar información innecesaria sobre la existencia de usuarios o roles.

Por ejemplo, se utiliza:

```text
Authentication/Invalid Credentials
```

para situaciones en las que no corresponde revelar si:

* El usuario existe.
* El usuario está eliminado.
* El usuario tiene determinado rol.
* La contraseña es incorrecta.

---

# 10. Creación automática del paciente

La creación del paciente y del usuario de Identity pertenecen a dos contextos diferentes.

Flujo actual:

```text
Crear ApplicationUser
        ↓
Asignar rol Paciente
        ↓
Crear Patient
```

Si el usuario de Identity fue creado durante el proceso pero falla la creación del paciente, se intenta realizar compensación:

```text
Crear User
    ↓
Crear Patient ❌
    ↓
Eliminar User
```

Esto evita dejar un usuario de paciente creado sin su correspondiente entidad `Patient`.

---

# 11. Especialidades

## Endpoints

| Operación      | Método | Endpoint    | Estado |
| -------------- | ------ | ----------- | ------ |
| Listar         | GET    | `/...`      | ⚪      |
| Obtener por ID | GET    | `/.../{id}` | ⚪      |
| Crear          | POST   | `/...`      | ⚪      |
| Modificar      | PUT    | `/.../{id}` | ⚪      |
| Eliminar       | DELETE | `/.../{id}` | ⚪      |

### Crear especialidad

#### Request

```json
{
  "name": "Cardiología"
}
```

#### Estado

⚪ No probado

---

# 12. Médicos

## Endpoints

| Operación      | Método | Endpoint    | Estado |
| -------------- | ------ | ----------- | ------ |
| Listar         | GET    | `/...`      | ⚪      |
| Obtener por ID | GET    | `/.../{id}` | ⚪      |
| Crear          | POST   | `/...`      | ⚪      |
| Modificar      | PUT    | `/.../{id}` | ⚪      |
| Eliminar       | DELETE | `/.../{id}` | ⚪      |

### Crear médico

#### Request

```json
{
  "fullName": "Dr. Juan Pérez",
  "specialtyId": "GUID"
}
```

#### Estado

⚪ No probado

---

# 13. Reglas de disponibilidad

Las disponibilidades se definen mediante reglas mensuales.

Una regla contiene conceptualmente:

```text
Doctor
Year
Month
DayOfWeek
StartTime
EndTime
```

---

# 14. Generación de slots

Los slots representan intervalos concretos de atención.

La generación debe utilizar intervalos de:

```text
30 minutos
```

Ejemplo:

```text
08:00 - 08:30
08:30 - 09:00
09:00 - 09:30
09:30 - 10:00
```

No deben generarse slots:

* En días no laborables.
* En feriados.
* Fuera del horario configurado.
* Para fechas que no correspondan al período solicitado.

---

# 15. Feriados

Los días no laborables se obtienen desde el archivo JSON correspondiente.

Durante la generación de slots se debe verificar si la fecha:

```text
es feriado
```

o

```text
es día no laborable
```

En esos casos no se generan slots.

---

# 16. Actualización de disponibilidad mensual

El PUT de disponibilidad mensual tiene comportamiento de reemplazo.

Conceptualmente:

```text
PUT disponibilidad
        ↓
Eliminar/reemplazar reglas del mes
        ↓
Crear nuevas reglas
        ↓
Regenerar slots
```

Debe verificarse especialmente que una actualización no deje slots antiguos que ya no correspondan a la nueva configuración.

### Estado

⚪ No probado

---

# 17. Turnos / Appointments

## Relación

```text
Patient
   │
   └── 1:N Appointment

Doctor
   │
   └── 1:N Appointment

AvailabilitySlot
   │
   └── 0..1 Appointment
```

Un slot puede tener como máximo un turno reservado.

---

# 18. Crear turno

## Endpoint

```http
POST /...
```

### Request

```json
{
  "doctorId": "GUID",
  "availabilitySlotId": "GUID",
  "patient": {
    "dni": "12345678"
  },
  "reason": "Consulta médica"
}
```

> Ajustar este ejemplo al DTO definitivo del proyecto si cambia durante la implementación.

### Validaciones

* Doctor existente.
* Doctor no eliminado.
* Slot existente.
* Slot disponible.
* Slot correspondiente al doctor.
* Slot no perteneciente al pasado.
* Paciente existente.
* DNI válido.
* DNI entre 7 y 10 dígitos según la validación del dominio.
* Motivo con mínimo 5 caracteres.
* No debe existir otro turno para el mismo slot.

### Estado

⚪ No probado

---

# 19. Estados de Appointment

Los estados definidos por el TPI son:

```text
BOOKED
CANCELLED
ATTENDED
NO_SHOW
```

### Flujo esperado

```text
BOOKED
  │
  ├── CANCELLED
  │
  ├── ATTENDED
  │
  └── NO_SHOW
```

Las transiciones permitidas deben verificarse en el servicio correspondiente.

---

# 20. Cancelación de turno

## Endpoint

```http
DELETE /...
```

### Importante

El TPI requiere **soft delete**.

No debería eliminarse físicamente el registro de la base de datos.

Conceptualmente:

```text
Deleted = true
```

o, cuando corresponda al estado de negocio:

```text
Status = CANCELLED
```

Debe verificarse cuál de las dos operaciones corresponde según el endpoint y el requisito específico.

### Estado

🟡 En revisión

---

# 21. Soft Delete

Todas las operaciones DELETE que correspondan según el TPI deben utilizar eliminación lógica.

La entidad debe conservarse en la base de datos.

Ejemplo:

```text
Deleted = false
```

↓

```text
DELETE
```

↓

```text
Deleted = true
```

Las consultas normales deben excluir registros eliminados.

Ejemplo:

```csharp
!entity.Deleted
```

### Pendiente de verificar

Revisar todos los DELETE del proyecto y confirmar que ninguno realice:

```csharp
_context.Remove(entity);
```

cuando el requisito indique soft delete.

---

# 22. Persistencia

La capa de persistencia actualmente centraliza operaciones como:

```text
Add
Update
Delete
First
GetById
GetAll
GetFiltered
Paginate
```

### Punto importante

Debe revisarse especialmente el método:

```text
Delete<T>
```

porque la implementación genérica actual puede realizar eliminación física.

Antes de modificarlo globalmente se debe verificar cómo están modeladas todas las entidades y cómo se utiliza DELETE en cada servicio.

---

# 23. Endpoints - Tabla general

Esta tabla debe ser la referencia rápida para saber qué parte del backend está funcionando.

| #  | Recurso      | Método | Endpoint    | Auth    | Estado | Última prueba |
| -- | ------------ | ------ | ----------- | ------- | ------ | ------------- |
| 1  | Admin        | POST   | `/...`      | Público | 🟡     | -             |
| 2  | Admin        | POST   | `/...`      | Admin   | 🟡     | -             |
| 3  | Patient      | POST   | `/...`      | Público | 🟡     | -             |
| 4  | Specialty    | GET    | `/...`      | Admin   | ⚪      | -             |
| 5  | Specialty    | GET    | `/.../{id}` | Admin   | ⚪      | -             |
| 6  | Specialty    | POST   | `/...`      | Admin   | ⚪      | -             |
| 7  | Specialty    | PUT    | `/.../{id}` | Admin   | ⚪      | -             |
| 8  | Specialty    | DELETE | `/.../{id}` | Admin   | ⚪      | -             |
| 9  | Doctor       | GET    | `/...`      | Admin   | ⚪      | -             |
| 10 | Doctor       | GET    | `/.../{id}` | Admin   | ⚪      | -             |
| 11 | Doctor       | POST   | `/...`      | Admin   | ⚪      | -             |
| 12 | Doctor       | PUT    | `/.../{id}` | Admin   | ⚪      | -             |
| 13 | Doctor       | DELETE | `/.../{id}` | Admin   | ⚪      | -             |
| 14 | Availability | GET    | `/...`      | Admin   | ⚪      | -             |
| 15 | Availability | POST   | `/...`      | Admin   | ⚪      | -             |
| 16 | Availability | PUT    | `/...`      | Admin   | ⚪      | -             |
| 17 | Appointment  | GET    | `/...`      | Auth    | ⚪      | -             |
| 18 | Appointment  | POST   | `/...`      | Patient | ⚪      | -             |
| 19 | Appointment  | PUT    | `/...`      | Auth    | ⚪      | -             |
| 20 | Appointment  | DELETE | `/...`      | Patient | ⚪      | -             |

> Reemplazar `/...` por las rutas reales de los controllers a medida que se consoliden.

---

# 24. Registro de pruebas de endpoints

Para cada endpoint que se pruebe, registrar:

### Endpoint

```http
POST /ruta
```

### Usuario utilizado

```text
Administrador / Paciente
```

### Request

```json
{
}
```

### Resultado esperado

```text
HTTP 200
```

### Resultado obtenido

```text
HTTP ...
```

### Response

```json
{
}
```

### Estado

```text
🟢 Funciona
```

### Observaciones

```text
...
```

---

# 25. Ejemplo de prueba fallida

### Endpoint

```http
POST /...
```

### Request

```json
{
  "email": "test@example.com"
}
```

### Resultado

```text
HTTP 400
```

### Problema

```text
Falta el campo requerido ...
```

### Estado

```text
🔴 No funciona
```

### Solución

```text
Pendiente.
```

---

# 26. Problemas encontrados

Registrar aquí errores importantes descubiertos durante la refactorización.

| Fecha      | Área        | Problema                                          | Solución                              | Estado |
| ---------- | ----------- | ------------------------------------------------- | ------------------------------------- | ------ |
| YYYY-MM-DD | Identity    | Roles con tipos incompatibles                     | Usar ApplicationRole                  | 🟢     |
| YYYY-MM-DD | JWT         | El token identificaba incorrectamente al paciente | Utilizar ApplicationUser.Id           | 🟢     |
| YYYY-MM-DD | Patient     | Relación entre contextos                          | Usar Patient.UserId                   | 🟢     |
| YYYY-MM-DD | Persistence | DELETE físico                                     | Revisar implementación de soft delete | 🟡     |

---

# 27. Refactors importantes

## AuthenticationService

Se eliminaron métodos repetidos relacionados con:

* Validación de requests.
* Validación de usuarios.
* Validación de roles.
* Creación de usuarios de pacientes.
* Recuperación de usuarios asociados a pacientes.
* Compensación ante errores.

La creación/obtención del usuario de paciente devuelve:

```csharp
(ApplicationUser User, bool Created)
```

Esto permite saber si el usuario fue creado durante la operación sin utilizar estado mutable dentro del servicio.

---

## Validación de roles

En lugar de obtener todos los roles del usuario y buscar manualmente uno:

```csharp
GetRolesAsync(...)
```

se utiliza:

```csharp
IsInRoleAsync(...)
```

cuando únicamente necesitamos verificar la pertenencia a un rol determinado.

---

# 28. Métodos complejos

Esta sección debe utilizarse para explicar métodos cuya lógica pueda resultar difícil de entender para otro integrante del grupo.

Formato recomendado:

## Nombre del método

```text
NombreClase.NombreMetodo
```

### ¿Qué hace?

Descripción breve.

### ¿Por qué existe?

Explicación del problema que resuelve.

### Flujo

```text
Paso 1
  ↓
Paso 2
  ↓
Paso 3
```

### Consideraciones

* ...
* ...
* ...

---

# 29. Casos especiales a tener en cuenta

## Paciente existente pero usuario inexistente

Situación inconsistente:

```text
Patient existe
ApplicationUser no existe
```

Debe producir un error de autenticación y registrarse mediante logging.

---

## Usuario existente pero Patient inexistente

Puede ocurrir durante el primer acceso.

Debe verificarse:

```text
User activo
    ↓
Rol Paciente
    ↓
No existe Patient
    ↓
Crear Patient
```

---

## Usuario eliminado

Un usuario con:

```text
Deleted = true
```

no debe poder autenticarse.

---

## Usuario existente con rol incorrecto

No debe autenticarse como paciente si no posee:

```text
Paciente
```

ni como administrador si no posee:

```text
Administrador
```

---

# 30. Checklist de seguridad

* [ ] Login de administrador público.
* [ ] Login de paciente público.
* [ ] Resto de endpoints protegidos.
* [ ] Operaciones administrativas requieren `Administrador`.
* [ ] Operaciones de paciente requieren `Paciente` cuando corresponda.
* [ ] JWT correctamente validado.
* [ ] Issuer validado.
* [ ] Audience validada.
* [ ] Lifetime validado.
* [ ] Signing key validada.
* [ ] Password mínima de 8 caracteres.
* [ ] Password almacenada mediante Identity.
* [ ] Usuarios eliminados no pueden autenticarse.
* [ ] JWT utiliza `ApplicationUser.Id`.
* [ ] JWT contiene el rol correspondiente.
* [ ] No se filtra información sensible mediante errores de autenticación.

---

# 31. Checklist de base de datos

* [ ] Todas las entidades tienen GUID.
* [ ] Relaciones EF Core correctamente configuradas.
* [ ] Foreign Keys verificadas.
* [ ] Índices únicos verificados.
* [ ] `Patient.Dni` único.
* [ ] `Patient.UserId` único.
* [ ] `Appointment.AvailabilitySlotId` único.
* [ ] `AvailabilityRule` no permite duplicados para la misma combinación.
* [ ] RowVersion configurado donde corresponda.
* [ ] Soft delete revisado.
* [ ] Migraciones actualizadas.
* [ ] Base de datos actualizada mediante migraciones.
* [ ] No existen relaciones innecesarias entre contextos.

---

# 32. Checklist de endpoints

## Authentication

* [ ] Login Admin
* [ ] Register Admin
* [ ] Login Patient

## Specialty

* [ ] GET all
* [ ] GET by ID
* [ ] POST
* [ ] PUT
* [ ] DELETE

## Doctor

* [ ] GET all
* [ ] GET by ID
* [ ] POST
* [ ] PUT
* [ ] DELETE

## Availability

* [ ] GET
* [ ] POST
* [ ] PUT
* [ ] Generación de slots
* [ ] Feriados
* [ ] Días no laborables
* [ ] Reemplazo mensual

## Appointment

* [ ] GET
* [ ] GET by ID
* [ ] POST
* [ ] PUT / cambio de estado
* [ ] DELETE / cancelación
* [ ] Validación de slot
* [ ] Validación de doctor
* [ ] Validación de paciente
* [ ] Validación de fecha
* [ ] Validación de motivo
* [ ] Prevención de doble reserva

---

# 33. Pendientes

Registrar aquí tareas que todavía no fueron resueltas.

* [ ] Completar pruebas de todos los endpoints.
* [ ] Completar rutas reales de cada endpoint.
* [ ] Verificar comportamiento de DELETE y soft delete.
* [ ] Verificar todas las migraciones.
* [ ] Probar generación de slots.
* [ ] Probar feriados.
* [ ] Probar actualización mensual de disponibilidades.
* [ ] Probar doble reserva de un mismo slot.
* [ ] Probar todos los estados de Appointment.
* [ ] Verificar autorización por roles.
* [ ] Revisar logs.
* [ ] Revisar mensajes `WithDetail`.
* [ ] Ejecutar pruebas completas antes del merge.

---

# 34. Historial de cambios técnicos

Utilizar este formato para registrar cambios importantes.

## YYYY-MM-DD — [Título del cambio]

### Problema

Descripción del problema encontrado.

### Solución

Descripción de la solución implementada.

### Archivos afectados

```text
Archivo1.cs
Archivo2.cs
Archivo3.cs
```

### Impacto

Descripción de qué partes del sistema cambia.

### Pruebas realizadas

```text
- Caso 1
- Caso 2
- Caso 3
```

### Resultado

```text
🟢 Funciona
```

### Commit relacionado

```text
<hash o nombre del commit>
```

---

# 35. Convención para actualizar este archivo

Cada integrante debería intentar actualizar este documento cuando haga un cambio que:

* Modifique el comportamiento de un endpoint.
* Corrija un error importante.
* Cambie una relación de EF Core.
* Modifique autenticación/autorización.
* Cambie una validación.
* Cambie una regla de negocio.
* Resuelva un problema que otro integrante podría volver a encontrar.
* Agregue o modifique una prueba importante.

No es necesario registrar cada pequeño cambio de formato o refactor trivial.

---

# 36. Regla general

> Si otro integrante del grupo podría perder tiempo intentando descubrir por qué algo funciona de determinada manera, probablemente conviene documentarlo aquí.
