#!/bin/bash

# ============================================================
#  ICare247 Core Platform — Project Setup Script
#  Phiên bản: 1.0
#  Mô tả: Tự động tạo toàn bộ cấu trúc solution .NET 9
# ============================================================

set -e  # Dừng nếu có lỗi

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

print_step() { echo -e "\n${CYAN}===> $1${NC}"; }
print_ok()   { echo -e "${GREEN}[OK]${NC} $1"; }
print_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }

# ============================================================
# BƯỚC 0: Kiểm tra điều kiện tiên quyết
# ============================================================
print_step "Kiểm tra môi trường..."

if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}[ERROR] .NET SDK chưa được cài. Vui lòng cài .NET 9 SDK trước.${NC}"
    echo "  → https://dotnet.microsoft.com/download/dotnet/9.0"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
print_ok ".NET SDK: $DOTNET_VERSION"

# ============================================================
# BƯỚC 1: Xác định thư mục gốc
# ============================================================
print_step "Xác định thư mục gốc..."

# Nếu thư mục hiện tại tên là ICare247_Core thì dùng luôn
# Nếu không thì tạo thư mục ICare247_Core
CURRENT_DIR=$(basename "$PWD")
if [ "$CURRENT_DIR" = "ICare247_Core" ]; then
    ROOT="$PWD"
    print_ok "Đang ở thư mục ICare247_Core: $ROOT"
else
    ROOT="$PWD/ICare247_Core"
    mkdir -p "$ROOT"
    print_ok "Tạo thư mục: $ROOT"
fi

cd "$ROOT"

# ============================================================
# BƯỚC 2: Tạo cấu trúc thư mục
# ============================================================
print_step "Tạo cấu trúc thư mục..."

mkdir -p src/backend/src
mkdir -p src/frontend
mkdir -p db
mkdir -p docs

print_ok "Cấu trúc thư mục gốc đã tạo"

# ============================================================
# BƯỚC 3: Tạo Solution & Projects
# ============================================================
print_step "Tạo Solution và Projects..."

cd "$ROOT/src/backend"

# Tạo solution
dotnet new sln -n ICare247 --force
print_ok "Solution: ICare247.sln"

# Tạo 4 projects theo Clean Architecture
dotnet new classlib -n ICare247.Domain      -f net9.0 -o src/ICare247.Domain      --force
dotnet new classlib -n ICare247.Application -f net9.0 -o src/ICare247.Application  --force
dotnet new classlib -n ICare247.Infrastructure -f net9.0 -o src/ICare247.Infrastructure --force
dotnet new webapi   -n ICare247.Api         -f net9.0 -o src/ICare247.Api          --force

print_ok "4 projects đã tạo"

# Xóa file Class1.cs mặc định
find src -name "Class1.cs" -delete
print_ok "Xóa Class1.cs mặc định"

# ============================================================
# BƯỚC 4: Add projects vào Solution
# ============================================================
print_step "Add projects vào Solution..."

dotnet sln add src/ICare247.Domain/ICare247.Domain.csproj
dotnet sln add src/ICare247.Application/ICare247.Application.csproj
dotnet sln add src/ICare247.Infrastructure/ICare247.Infrastructure.csproj
dotnet sln add src/ICare247.Api/ICare247.Api.csproj
print_ok "Tất cả projects đã được add vào solution"

# ============================================================
# BƯỚC 5: Thiết lập Project References
# ============================================================
print_step "Thiết lập Project References (Clean Architecture)..."

# Application → Domain
dotnet add src/ICare247.Application/ICare247.Application.csproj \
    reference src/ICare247.Domain/ICare247.Domain.csproj
print_ok "Application → Domain"

# Infrastructure → Application
dotnet add src/ICare247.Infrastructure/ICare247.Infrastructure.csproj \
    reference src/ICare247.Application/ICare247.Application.csproj
print_ok "Infrastructure → Application"

# Api → Application + Infrastructure (để DI registration)
dotnet add src/ICare247.Api/ICare247.Api.csproj \
    reference src/ICare247.Application/ICare247.Application.csproj
dotnet add src/ICare247.Api/ICare247.Api.csproj \
    reference src/ICare247.Infrastructure/ICare247.Infrastructure.csproj
print_ok "Api → Application + Infrastructure"

# ============================================================
# BƯỚC 6: Cài NuGet Packages
# ============================================================
print_step "Cài NuGet Packages..."

# --- Domain (không cần package external) ---
print_ok "Domain: không cần package external"

# --- Application ---
dotnet add src/ICare247.Application/ICare247.Application.csproj package MediatR --version 12.*
dotnet add src/ICare247.Application/ICare247.Application.csproj package FluentValidation --version 11.*
dotnet add src/ICare247.Application/ICare247.Application.csproj package Microsoft.Extensions.DependencyInjection.Abstractions
print_ok "Application packages: MediatR, FluentValidation"

# --- Infrastructure ---
dotnet add src/ICare247.Infrastructure/ICare247.Infrastructure.csproj package Dapper
dotnet add src/ICare247.Infrastructure/ICare247.Infrastructure.csproj package Microsoft.Data.SqlClient
dotnet add src/ICare247.Infrastructure/ICare247.Infrastructure.csproj package StackExchange.Redis
dotnet add src/ICare247.Infrastructure/ICare247.Infrastructure.csproj package Serilog.AspNetCore
dotnet add src/ICare247.Infrastructure/ICare247.Infrastructure.csproj package Serilog.Sinks.Console
dotnet add src/ICare247.Infrastructure/ICare247.Infrastructure.csproj package Serilog.Sinks.File
dotnet add src/ICare247.Infrastructure/ICare247.Infrastructure.csproj package OpenTelemetry.Extensions.Hosting
dotnet add src/ICare247.Infrastructure/ICare247.Infrastructure.csproj package Microsoft.Extensions.Caching.Memory
dotnet add src/ICare247.Infrastructure/ICare247.Infrastructure.csproj package Microsoft.Extensions.Caching.StackExchangeRedis
print_ok "Infrastructure packages: Dapper, SqlClient, Redis, Serilog, OpenTelemetry"

# --- Api ---
dotnet add src/ICare247.Api/ICare247.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/ICare247.Api/ICare247.Api.csproj package Swashbuckle.AspNetCore
dotnet add src/ICare247.Api/ICare247.Api.csproj package Serilog.AspNetCore
print_ok "Api packages: JWT, Swagger, Serilog"

# ============================================================
# BƯỚC 7: Tạo cấu trúc thư mục bên trong từng project
# ============================================================
print_step "Tạo cấu trúc thư mục chi tiết..."

DOMAIN="src/ICare247.Domain"
APP="src/ICare247.Application"
INFRA="src/ICare247.Infrastructure"
API="src/ICare247.Api"

# --- Domain ---
mkdir -p $DOMAIN/Entities/Form
mkdir -p $DOMAIN/Entities/Rule
mkdir -p $DOMAIN/Entities/Event
mkdir -p $DOMAIN/Entities/Grammar
mkdir -p $DOMAIN/Ast
mkdir -p $DOMAIN/Engines
mkdir -p $DOMAIN/ValueObjects
mkdir -p $DOMAIN/Exceptions
print_ok "Domain folders"

# --- Application ---
mkdir -p $APP/Common/Interfaces
mkdir -p $APP/Common/Behaviors
mkdir -p $APP/Common/Models
mkdir -p $APP/Metadata/Queries
mkdir -p $APP/Metadata/Handlers
mkdir -p $APP/Metadata/Models
mkdir -p $APP/Grammar/Queries
mkdir -p $APP/Grammar/Handlers
mkdir -p $APP/Grammar/Models
mkdir -p $APP/Grammar/Services
mkdir -p $APP/Validation/Commands
mkdir -p $APP/Validation/Handlers
mkdir -p $APP/Validation/Models
mkdir -p $APP/Event/Commands
mkdir -p $APP/Event/Handlers
mkdir -p $APP/Event/Models
mkdir -p $APP/Form/Queries
mkdir -p $APP/Form/Handlers
mkdir -p $APP/Form/Models
print_ok "Application folders"

# --- Infrastructure ---
mkdir -p $INFRA/Persistence/Metadata
mkdir -p $INFRA/Persistence/Rule
mkdir -p $INFRA/Persistence/Event
mkdir -p $INFRA/Persistence/Grammar
mkdir -p $INFRA/Cache
mkdir -p $INFRA/Grammar/Functions/String
mkdir -p $INFRA/Grammar/Functions/Numeric
mkdir -p $INFRA/Grammar/Functions/DateTime
mkdir -p $INFRA/Grammar/Functions/Logic
mkdir -p $INFRA/Validation/Handlers
mkdir -p $INFRA/Logging
mkdir -p $INFRA/Http
print_ok "Infrastructure folders"

# --- Api ---
mkdir -p $API/Controllers
mkdir -p $API/Middleware
mkdir -p $API/Filters
mkdir -p $API/Extensions
print_ok "Api folders"

# ============================================================
# BƯỚC 8: Tạo các file stub quan trọng
# ============================================================
print_step "Tạo file stub (interfaces & exceptions)..."

# --- Domain: Interface gốc AST ---
cat > $DOMAIN/Ast/IExpressionNode.cs << 'EOF'
namespace ICare247.Domain.Ast;

/// <summary>
/// Interface gốc cho tất cả AST Node trong Grammar V1.
/// </summary>
public interface IExpressionNode
{
    string NodeType { get; }
}
EOF

# --- Domain: EvaluationContext ---
cat > $DOMAIN/ValueObjects/EvaluationContext.cs << 'EOF'
namespace ICare247.Domain.ValueObjects;

/// <summary>
/// Context chứa giá trị các field trong form khi thực thi expression.
/// Key: Field_Code, Value: giá trị hiện tại của field.
/// </summary>
public sealed class EvaluationContext
{
    private readonly Dictionary<string, object?> _values;

    public int TenantId { get; }
    public string FormCode { get; }
    public string CorrelationId { get; }

    public EvaluationContext(int tenantId, string formCode, string correlationId)
    {
        TenantId = tenantId;
        FormCode = formCode;
        CorrelationId = correlationId;
        _values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    public void Set(string fieldCode, object? value) => _values[fieldCode] = value;
    public object? Get(string fieldCode) => _values.GetValueOrDefault(fieldCode);
    public bool Contains(string fieldCode) => _values.ContainsKey(fieldCode);
    public IReadOnlyDictionary<string, object?> All => _values;
}
EOF

# --- Domain: Exceptions ---
cat > $DOMAIN/Exceptions/ExpressionDepthException.cs << 'EOF'
namespace ICare247.Domain.Exceptions;

public sealed class ExpressionDepthException : Exception
{
    public ExpressionDepthException(int depth, int maxDepth)
        : base($"AST depth {depth} vượt quá giới hạn cho phép {maxDepth}.") { }
}
EOF

cat > $DOMAIN/Exceptions/FunctionNotFoundException.cs << 'EOF'
namespace ICare247.Domain.Exceptions;

public sealed class FunctionNotFoundException : Exception
{
    public FunctionNotFoundException(string functionCode)
        : base($"Function '{functionCode}' không tìm thấy trong registry.") { }
}
EOF

cat > $DOMAIN/Exceptions/TypeMismatchException.cs << 'EOF'
namespace ICare247.Domain.Exceptions;

public sealed class TypeMismatchException : Exception
{
    public TypeMismatchException(string expected, string actual)
        : base($"Kiểu dữ liệu không khớp. Mong đợi: {expected}, Thực tế: {actual}.") { }
}
EOF

cat > $DOMAIN/Exceptions/ConfigurationException.cs << 'EOF'
namespace ICare247.Domain.Exceptions;

public sealed class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
}
EOF

print_ok "Domain stubs: IExpressionNode, EvaluationContext, Exceptions"

# --- Domain: Engine Interfaces ---
cat > $DOMAIN/Engines/IAstEngine.cs << 'EOF'
using ICare247.Domain.Ast;
using ICare247.Domain.ValueObjects;

namespace ICare247.Domain.Engines;

public interface IAstEngine
{
    object? Evaluate(IExpressionNode node, EvaluationContext context);
}
EOF

cat > $DOMAIN/Engines/IFunctionRegistry.cs << 'EOF'
using ICare247.Domain.ValueObjects;

namespace ICare247.Domain.Engines;

public interface IFunctionHandler
{
    string FunctionCode { get; }
    object? Execute(object?[] args, EvaluationContext context);
}

public interface IFunctionRegistry
{
    void Register(IFunctionHandler handler);
    IFunctionHandler Resolve(string functionCode);
    bool TryResolve(string functionCode, out IFunctionHandler? handler);
}
EOF

cat > $DOMAIN/Engines/IValidationEngine.cs << 'EOF'
using ICare247.Domain.ValueObjects;

namespace ICare247.Domain.Engines;

public interface IValidationEngine
{
    Task<ValidationResult> ValidateFieldAsync(
        string fieldCode,
        EvaluationContext context,
        CancellationToken ct = default);
}
EOF

cat > $DOMAIN/Engines/IEventEngine.cs << 'EOF'
using ICare247.Domain.ValueObjects;

namespace ICare247.Domain.Engines;

public interface IEventEngine
{
    Task<UiDeltaCollection> ExecuteAsync(
        string fieldCode,
        string triggerCode,
        EvaluationContext context,
        CancellationToken ct = default);
}
EOF

cat > $DOMAIN/ValueObjects/ValidationResult.cs << 'EOF'
namespace ICare247.Domain.ValueObjects;

public sealed class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<FieldError> Errors { get; } = [];

    public void AddError(string fieldCode, string message, string? ruleCode = null)
        => Errors.Add(new FieldError(fieldCode, message, ruleCode));
}

public sealed record FieldError(string FieldCode, string Message, string? RuleCode);
EOF

cat > $DOMAIN/ValueObjects/UiDeltaCollection.cs << 'EOF'
namespace ICare247.Domain.ValueObjects;

public sealed class UiDeltaCollection
{
    public List<UiDelta> Deltas { get; } = [];

    public void Add(string fieldCode, string property, object? value)
        => Deltas.Add(new UiDelta(fieldCode, property, value));
}

public sealed record UiDelta(string FieldCode, string Property, object? Value);
EOF

print_ok "Domain engine interfaces"

# --- Application: DependencyInjection.cs ---
cat > $APP/DependencyInjection.cs << 'EOF'
using Microsoft.Extensions.DependencyInjection;

namespace ICare247.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
EOF

# --- Infrastructure: DependencyInjection.cs ---
cat > $INFRA/DependencyInjection.cs << 'EOF'
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ICare247.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO Phase 1: Register repositories, cache, logging
        // services.AddScoped<IFormRepository, FormRepository>();
        // services.AddMemoryCache();
        // services.AddStackExchangeRedisCache(...);

        return services;
    }
}
EOF

print_ok "DependencyInjection stubs"

# --- Api: Program.cs (chuẩn) ---
cat > $API/Program.cs << 'EOF'
using ICare247.Application;
using ICare247.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Clean Architecture DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT (sẽ cấu hình đầy đủ sau)
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
EOF

print_ok "Program.cs"

# --- Api: appsettings.json ---
cat > $API/appsettings.json << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ICare247_Config;User Id=sa;Password=YourPassword;TrustServerCertificate=true;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Jwt": {
    "Key": "CHANGE_ME_USE_SECRET_MANAGER_IN_PRODUCTION",
    "Issuer": "ICare247",
    "Audience": "ICare247Client",
    "ExpiryMinutes": 60
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  },
  "AllowedHosts": "*"
}
EOF

print_ok "appsettings.json"

# ============================================================
# BƯỚC 9: Tạo .gitignore
# ============================================================
print_step "Tạo .gitignore..."

cd "$ROOT"
cat > .gitignore << 'EOF'
# Build output
bin/
obj/
out/

# VS Code
.vscode/
!.vscode/extensions.json
!.vscode/settings.json

# Rider / VS
.vs/
*.user
*.suo
.idea/

# .NET
*.nupkg
*.snupkg
project.lock.json
project.fragment.lock.json
artifacts/

# Logs
logs/
*.log

# Secrets
appsettings.Production.json
appsettings.Secrets.json
secrets.json

# OS
.DS_Store
Thumbs.db
EOF

print_ok ".gitignore"

# ============================================================
# BƯỚC 10: Copy docs
# ============================================================
print_step "Tạo README.md..."

cat > README.md << 'EOF'
# ICare247 Core Platform

Enterprise metadata-driven low-code form engine.

## Tech Stack
- Backend: .NET 9 / ASP.NET Core 9
- Database: MS SQL Server + Dapper
- Cache: MemoryCache + Redis
- Logging: Serilog + OpenTelemetry
- Auth: JWT

## Cấu trúc Solution

```
ICare247.Domain         ← Entities, AST, Engine Interfaces
ICare247.Application    ← CQRS (MediatR), Use Cases
ICare247.Infrastructure ← Dapper, Redis, Serilog
ICare247.Api            ← Controllers, Middleware, Swagger
```

## Chạy dự án

```bash
cd src/backend/src/ICare247.Api
dotnet run
```

API: https://localhost:5001/swagger
EOF

print_ok "README.md"

# ============================================================
# BƯỚC 11: Build kiểm tra
# ============================================================
print_step "Build kiểm tra toàn bộ solution..."

cd "$ROOT/src/backend"
dotnet build --nologo -v minimal

# ============================================================
# KẾT QUẢ
# ============================================================
echo ""
echo -e "${GREEN}============================================================${NC}"
echo -e "${GREEN}  ICare247 Core Platform — Setup hoàn tất!${NC}"
echo -e "${GREEN}============================================================${NC}"
echo ""
echo -e "  Thư mục gốc : ${CYAN}$ROOT${NC}"
echo -e "  Solution    : ${CYAN}src/backend/ICare247.sln${NC}"
echo ""
echo -e "  Chạy dự án:"
echo -e "  ${YELLOW}cd $ROOT/src/backend/src/ICare247.Api${NC}"
echo -e "  ${YELLOW}dotnet run${NC}"
echo ""
echo -e "  Swagger UI  : ${CYAN}https://localhost:5001/swagger${NC}"
echo ""
echo -e "  Bước tiếp theo:"
echo -e "  1. Cập nhật connection string trong ${YELLOW}appsettings.json${NC}"
echo -e "  2. Chạy ${YELLOW}db/ICare247_SeedData.sql${NC} lên SQL Server"
echo -e "  3. Mở VS Code: ${YELLOW}code $ROOT${NC}"
echo ""