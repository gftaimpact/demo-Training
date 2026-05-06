# TemporaryCredentials.cs: Generación Segura de Credenciales Temporales

## Overview
Este controlador, **TemporaryCredentialsController**, gestiona la creación de credenciales temporales para usuarios mediante validaciones de seguridad (MFA e IP corporativa), generación de tokens JWT cifrados con AES y registro de auditoría.  
Integra mecanismos de seguridad y persistencia utilizando **Entity Framework**, **JWT** y **cifrado RSA/AES**.

## Process Flow
```mermaid
graph TD
    A["Inicio de solicitud (POST /api/v1/auth/temporarycredentials)"] --> B["Extracción de Header: X-AdminID y X-MFAValid"]
    B --> C{"MFA válido y IP corporativa?"}
    C -- No --> D["Registrar auditoría: MFA inválido o IP no autorizada"] --> E["403 Forbidden"]
    C -- Sí --> F{"Usuario existe en sistema?"}
    F -- No --> G["Registrar auditoría: Usuario inexistente"] --> H["400 BadRequest"]
    F -- Sí --> I["Verificar duplicados: credencial activa existente"]
    I -- Sí --> J["Registrar auditoría: Credencial ya activa"] --> K["409 Conflict"]
    I -- No --> L["Generar Token JWT seguro (RSA + SHA256)"]
    L --> M["Cifrar Token con AES"]
    M --> N["Persistir credencial temporal en BD (EF Core)"]
    N --> O["Registrar auditoría exitosa"]
    O --> P["Devolver TempCredentialResponse (200 OK)"]
    P --> Q("Fin")
    E --> Q
    H --> Q
    K --> Q
```

## Insights
- El proceso está fuertemente asegurado: **RSA** se usa para firmar el JWT y **AES** para cifrarlo antes de persistirlo.
- Las validaciones de seguridad adicionales (MFA e IP corporativa) previenen accesos no autorizados.
- Se implementa trazabilidad completa mediante registros de auditoría (`LogAudit`) en todas las ramas críticas.
- La verificación de duplicados evita conflictos con credenciales activas para el mismo usuario.
- El flujo de errores es granular, devolviendo códigos HTTP adecuados (`403`, `400`, `409`, `500`).
- El modelo `TemporaryCredential` persiste la información esencial del token cifrado y su estado.

## Dependencies
```mermaid
graph LR
    TemporaryCredentialsController --- |"Uses"| authdbcontext["AuthDbContext"]
    TemporaryCredentialsController --- |"Uses"| ilogger_tempcred["ILogger<TemporaryCredentialsController>"]
    TemporaryCredentialsController --- |"Uses"| JwtSecurityTokenHandler
    TemporaryCredentialsController --- |"Uses"| RSA
    TemporaryCredentialsController --- |"Uses"| Aes
```

- `AuthDbContext`: Acceso y manipulación de la tabla `TemporaryCredentials`.
- `ILogger<TemporaryCredentialsController>`: Registro de acciones y auditoría.
- `JwtSecurityTokenHandler`: Creación y codificación del token JWT firmado con RSA.
- `RSA`: Firma digital del token.
- `Aes`: Cifrado simétrico del token antes de su persistencia.

## Data Manipulation (SQL)
### Tabla `TemporaryCredential`
| Campo           | Tipo          | Descripción                                      |
|-----------------|----------------|--------------------------------------------------|
| Id              | `string`       | Identificador único (GUID).                     |
| UserId          | `string`       | ID del usuario propietario de la credencial.    |
| AdminId         | `string`       | ID del admin que generó la credencial.          |
| TokenEncrypted  | `string`       | Token JWT cifrado mediante AES.                 |
| ExpiresAt       | `DateTime`     | Fecha/hora de expiración del token.             |
| Status          | `CredentialStatus` | Estado actual: `Active`, `Revoked`, `Expired`. |
| CreatedAt       | `DateTime`     | Fecha/hora de creación.                         |

### Entidad `CredentialStatus`
| Valor     | Descripción                       |
|------------|-----------------------------------|
| Active     | Credencial activa y vigente.      |
| Revoked    | Credencial anulada.               |
| Expired    | Credencial caducada.              |
