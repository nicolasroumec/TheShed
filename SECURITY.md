# Guía de Seguridad - SwimAnalytics

## ⚠️ NUNCA COMMITEAR INFORMACIÓN SENSIBLE

Este proyecto está configurado para **NUNCA** exponer información sensible en el repositorio público.

## Archivos Protegidos por .gitignore

Los siguientes archivos están automáticamente ignorados y **NO** se subirán a GitHub:

- `appsettings.*.json` (excepto appsettings.json base y appsettings.Example.json)
- `.env` y variantes
- `*.db`, `*.sqlite` (bases de datos)
- `secrets.json`
- Certificados (`.pfx`, `.pem`, `.key`, `.crt`)
- Carpetas `bin/` y `obj/`

## Configuración Segura para Desarrollo Local

### Opción 1: User Secrets (RECOMENDADO para .NET)

User Secrets es la forma más segura de manejar secretos en desarrollo local:

```bash
# Navega al proyecto Server
cd SwimAnalytics.Server

# Inicializar User Secrets
dotnet user-secrets init

# Agregar secretos (ejemplos)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=SwimAnalytics;User Id=sa;Password=TuPassword123;"
dotnet user-secrets set "JwtSettings:SecretKey" "tu-clave-secreta-super-segura-minimo-32-caracteres"
dotnet user-secrets set "EmailSettings:Password" "tu-password-email"

# Listar todos los secretos configurados
dotnet user-secrets list

# Remover un secreto
dotnet user-secrets remove "ConnectionStrings:DefaultConnection"

# Limpiar todos los secretos
dotnet user-secrets clear
```

**Ventajas de User Secrets:**
- ✅ Los secretos se almacenan FUERA del proyecto (en tu perfil de usuario)
- ✅ Nunca se commitean accidentalmente
- ✅ Fácil de usar y gestionar
- ✅ Integración nativa con .NET

**Ubicación de User Secrets en Windows:**
```
%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json
```

### Opción 2: Archivo appsettings.Local.json

Crea un archivo `appsettings.Local.json` en `SwimAnalytics.Server/`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "TU_CONEXION_REAL_AQUI"
  },
  "JwtSettings": {
    "SecretKey": "TU_CLAVE_SECRETA_REAL"
  }
}
```

Este archivo ya está en `.gitignore` y no se subirá al repositorio.

### Opción 3: Variables de Entorno

Copia `.env.example` a `.env` y completa con tus valores reales:

```bash
cp .env.example .env
```

Luego edita `.env` con tus valores reales. Este archivo está en `.gitignore`.

## Configuración para Producción

### Azure App Service
Usa **Application Settings** en Azure Portal para configurar variables de entorno.

### AWS
Usa **AWS Systems Manager Parameter Store** o **AWS Secrets Manager**.

### Docker
Usa **Docker Secrets** o variables de entorno en el `docker-compose.yml`:

```yaml
environment:
  - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
  - JwtSettings__SecretKey=${JWT_SECRET}
```

### Kubernetes
Usa **Kubernetes Secrets**:

```bash
kubectl create secret generic swimanalytics-secrets \
  --from-literal=db-connection='tu-conexion' \
  --from-literal=jwt-secret='tu-secreto'
```

## Checklist Antes de Hacer Commit

- [ ] ¿Verificaste que no hay contraseñas en el código?
- [ ] ¿Verificaste que no hay API keys hardcodeadas?
- [ ] ¿Las cadenas de conexión usan placeholders o User Secrets?
- [ ] ¿Revisaste los archivos .json antes de commitear?
- [ ] ¿Ejecutaste `git status` para verificar qué archivos se van a subir?

## Comando de Verificación

Antes de hacer commit, ejecuta:

```bash
# Buscar posibles secretos en el código
git grep -i "password\s*=\s*['\"][^'\"]*['\"]"
git grep -i "apikey\s*=\s*['\"][^'\"]*['\"]"
git grep -i "secret\s*=\s*['\"][^'\"]*['\"]"
```

## Rotación de Secretos Comprometidos

Si accidentalmente commiteaste un secreto:

1. **INMEDIATAMENTE** rota/cambia el secreto comprometido
2. Usa `git-filter-branch` o [BFG Repo-Cleaner](https://rtyley.github.io/bfg-repo-cleaner/) para remover el secreto del historial
3. Fuerza un push: `git push --force`
4. Notifica al equipo
5. Revisa logs de acceso para detectar uso no autorizado

## Recursos Adicionales

- [ASP.NET Core User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [GitHub Secret Scanning](https://docs.github.com/en/code-security/secret-scanning)
- [Git-Secrets](https://github.com/awslabs/git-secrets)
- [OWASP Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)

## Contacto

Si encuentras una vulnerabilidad de seguridad, por favor repórtala de forma privada.
