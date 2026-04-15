// File    : ValidationEngineTests.cs
// Module  : Engines
// Layer   : Tests
// Purpose : Unit tests cho ValidationEngine — validate field/form, condition, dependency sort.

using ICare247.Application.Engines;
using ICare247.Application.Interfaces;
using ICare247.Domain.Engine.Models;
using ICare247.Domain.Entities.Form;
using ICare247.Domain.Entities.Rule;
using ICare247.Domain.ValueObjects;

namespace ICare247.Application.Tests.Engines;

public sealed class ValidationEngineTests
{
    private readonly ValidationEngine _engine;
    private readonly StubRuleRepository _ruleRepo;
    private readonly StubDependencyRepository _depRepo;
    private readonly StubFieldRepository _fieldRepo;

    public ValidationEngineTests()
    {
        var registry = new FunctionRegistry();
        BuiltinFunctions.RegisterAll(registry);
        var parser = new AstParser();
        var compiler = new AstCompiler(registry);
        var astEngine = new AstEngine(parser, compiler);

        _ruleRepo  = new StubRuleRepository();
        _depRepo   = new StubDependencyRepository();
        _fieldRepo = new StubFieldRepository();
        _engine = new ValidationEngine(astEngine, _ruleRepo, _depRepo, _fieldRepo);
    }

    // ── ValidateFieldAsync ──────────────────────────────────────

    [Fact]
    public async Task ValidateField_NoRules_ReturnsValid()
    {
        // Không có rules → field hợp lệ
        var result = await _engine.ValidateFieldAsync(
            1, "Email", "test@email.com", EvaluationContext.Empty, 1);

        Assert.True(result.IsValid);
        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task ValidateField_RequiredRule_NullValue_ReturnsFail()
    {
        _ruleRepo.SetFieldRules(1, "Name", 1, new List<RuleMetadata>
        {
            Rule(1, "Name", "Required", severity: "error",
                errorMessage: "Tên không được để trống")
        });

        var result = await _engine.ValidateFieldAsync(
            1, "Name", null, EvaluationContext.Empty, 1);

        Assert.False(result.IsValid);
        Assert.Single(result.Results);
        Assert.Equal("Tên không được để trống", result.Results[0].Message);
    }

    [Fact]
    public async Task ValidateField_RequiredRule_EmptyString_ReturnsFail()
    {
        _ruleRepo.SetFieldRules(1, "Name", 1, new List<RuleMetadata>
        {
            Rule(1, "Name", "Required", severity: "error",
                errorMessage: "Tên không được để trống")
        });

        var result = await _engine.ValidateFieldAsync(
            1, "Name", "   ", EvaluationContext.Empty, 1);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateField_RequiredRule_HasValue_ReturnsValid()
    {
        _ruleRepo.SetFieldRules(1, "Name", 1, new List<RuleMetadata>
        {
            Rule(1, "Name", "Required", severity: "error",
                errorMessage: "Tên không được để trống")
        });

        var result = await _engine.ValidateFieldAsync(
            1, "Name", "Alice", EvaluationContext.Empty, 1);

        Assert.True(result.IsValid);
        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task ValidateField_CustomRule_ExpressionFalse_ReturnsFail()
    {
        // Age >= 18 → false khi Age = 15
        var expr = """
        {
          "type":"binary","op":">=",
          "left":{"type":"identifier","name":"Age"},
          "right":{"type":"literal","value":18}
        }
        """;

        _ruleRepo.SetFieldRules(1, "Age", 1, new List<RuleMetadata>
        {
            Rule(1, "Age", "Custom", expressionJson: expr,
                errorMessage: "Phải >= 18 tuổi")
        });

        var result = await _engine.ValidateFieldAsync(
            1, "Age", 15, EvaluationContext.Empty, 1);

        Assert.False(result.IsValid);
        Assert.Equal("Phải >= 18 tuổi", result.Results[0].Message);
    }

    [Fact]
    public async Task ValidateField_CustomRule_ExpressionTrue_ReturnsValid()
    {
        var expr = """
        {
          "type":"binary","op":">=",
          "left":{"type":"identifier","name":"Age"},
          "right":{"type":"literal","value":18}
        }
        """;

        _ruleRepo.SetFieldRules(1, "Age", 1, new List<RuleMetadata>
        {
            Rule(1, "Age", "Custom", expressionJson: expr,
                errorMessage: "Phải >= 18 tuổi")
        });

        var result = await _engine.ValidateFieldAsync(
            1, "Age", 25, EvaluationContext.Empty, 1);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateField_ConditionFalse_SkipsRule()
    {
        // Condition: IsAdult == true → skip khi IsAdult = false
        var condition = """{"type":"identifier","name":"IsAdult"}""";
        var expr = """
        {
          "type":"binary","op":">=",
          "left":{"type":"identifier","name":"Age"},
          "right":{"type":"literal","value":18}
        }
        """;

        _ruleRepo.SetFieldRules(1, "Age", 1, new List<RuleMetadata>
        {
            Rule(1, "Age", "Custom", expressionJson: expr,
                conditionExpr: condition, errorMessage: "Phải >= 18 tuổi")
        });

        // IsAdult = false → condition skip → rule không apply → valid
        var ctx = new EvaluationContext(new Dictionary<string, object?>
        {
            ["IsAdult"] = false
        });

        var result = await _engine.ValidateFieldAsync(1, "Age", 15, ctx, 1);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateField_ConditionTrue_EvaluatesRule()
    {
        var condition = """{"type":"identifier","name":"IsAdult"}""";
        var expr = """
        {
          "type":"binary","op":">=",
          "left":{"type":"identifier","name":"Age"},
          "right":{"type":"literal","value":18}
        }
        """;

        _ruleRepo.SetFieldRules(1, "Age", 1, new List<RuleMetadata>
        {
            Rule(1, "Age", "Custom", expressionJson: expr,
                conditionExpr: condition, errorMessage: "Phải >= 18 tuổi")
        });

        var ctx = new EvaluationContext(new Dictionary<string, object?>
        {
            ["IsAdult"] = true
        });

        // IsAdult = true → condition pass → Age = 15 < 18 → fail
        var result = await _engine.ValidateFieldAsync(1, "Age", 15, ctx, 1);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateField_MultipleRules_CollectsAllFailures()
    {
        _ruleRepo.SetFieldRules(1, "Email", 1, new List<RuleMetadata>
        {
            Rule(1, "Email", "Required", errorMessage: "Email bắt buộc"),
            Rule(2, "Email", "Custom",
                expressionJson: """
                {
                  "type":"function_call","name":"contains",
                  "args":[
                    {"type":"identifier","name":"Email"},
                    {"type":"literal","value":"@"}
                  ]
                }
                """,
                errorMessage: "Email phải chứa @")
        });

        // null → cả Required lẫn Custom đều fail
        var result = await _engine.ValidateFieldAsync(
            1, "Email", null, EvaluationContext.Empty, 1);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public async Task ValidateField_WarningSeverity_StillValid()
    {
        _ruleRepo.SetFieldRules(1, "Age", 1, new List<RuleMetadata>
        {
            Rule(1, "Age", "Custom", severity: "warning",
                expressionJson: """
                {
                  "type":"binary","op":"<",
                  "left":{"type":"identifier","name":"Age"},
                  "right":{"type":"literal","value":65}
                }
                """,
                errorMessage: "Tuổi khá cao")
        });

        // Age = 70 → rule fail nhưng severity = warning → IsValid vẫn true
        var result = await _engine.ValidateFieldAsync(
            1, "Age", 70, EvaluationContext.Empty, 1);

        Assert.True(result.IsValid); // Warning không ảnh hưởng IsValid
        Assert.Single(result.Results);
        Assert.Equal("warning", result.Results[0].Severity);
    }

    // ── Length rule ──────────────────────────────────────────────

    [Fact]
    public async Task ValidateField_LengthRule_TooShort_ReturnsFail()
    {
        // len(Phone) >= 10 && len(Phone) <= 15 → fail khi Phone = "123" (3 ký tự)
        var expr = """
        {
          "type":"binary","op":"&&",
          "left":{
            "type":"binary","op":">=",
            "left":{"type":"function_call","name":"len","args":[{"type":"identifier","name":"Phone"}]},
            "right":{"type":"literal","value":10}
          },
          "right":{
            "type":"binary","op":"<=",
            "left":{"type":"function_call","name":"len","args":[{"type":"identifier","name":"Phone"}]},
            "right":{"type":"literal","value":15}
          }
        }
        """;

        _ruleRepo.SetFieldRules(1, "Phone", 1, new List<RuleMetadata>
        {
            Rule(1, "Phone", "Length", expressionJson: expr,
                errorMessage: "Số điện thoại phải từ 10 đến 15 ký tự")
        });

        var result = await _engine.ValidateFieldAsync(1, "Phone", "123", EvaluationContext.Empty, 1);

        Assert.False(result.IsValid);
        Assert.Equal("Số điện thoại phải từ 10 đến 15 ký tự", result.Results[0].Message);
    }

    [Fact]
    public async Task ValidateField_LengthRule_ValidLength_ReturnsValid()
    {
        var expr = """
        {
          "type":"binary","op":"&&",
          "left":{
            "type":"binary","op":">=",
            "left":{"type":"function_call","name":"len","args":[{"type":"identifier","name":"Phone"}]},
            "right":{"type":"literal","value":10}
          },
          "right":{
            "type":"binary","op":"<=",
            "left":{"type":"function_call","name":"len","args":[{"type":"identifier","name":"Phone"}]},
            "right":{"type":"literal","value":15}
          }
        }
        """;

        _ruleRepo.SetFieldRules(1, "Phone", 1, new List<RuleMetadata>
        {
            Rule(1, "Phone", "Length", expressionJson: expr,
                errorMessage: "Số điện thoại phải từ 10 đến 15 ký tự")
        });

        // "0123456789" = 10 ký tự → pass
        var result = await _engine.ValidateFieldAsync(1, "Phone", "0123456789", EvaluationContext.Empty, 1);

        Assert.True(result.IsValid);
    }

    // ── Compare rule ─────────────────────────────────────────────

    [Fact]
    public async Task ValidateField_CompareRule_EndDateBeforeStart_ReturnsFail()
    {
        // EndDate >= StartDate → fail khi EndDate < StartDate
        var expr = """
        {
          "type":"binary","op":">=",
          "left":{"type":"identifier","name":"EndDate"},
          "right":{"type":"identifier","name":"StartDate"}
        }
        """;

        _ruleRepo.SetFieldRules(1, "EndDate", 1, new List<RuleMetadata>
        {
            Rule(1, "EndDate", "Compare", expressionJson: expr,
                errorMessage: "Ngày kết thúc phải sau ngày bắt đầu")
        });

        var ctx = new EvaluationContext(new Dictionary<string, object?>
        {
            ["StartDate"] = "2025-06-01",
            ["EndDate"]   = "2025-01-01"   // trước StartDate → fail
        });

        var result = await _engine.ValidateFieldAsync(1, "EndDate", "2025-01-01", ctx, 1);

        Assert.False(result.IsValid);
        Assert.Equal("Ngày kết thúc phải sau ngày bắt đầu", result.Results[0].Message);
    }

    [Fact]
    public async Task ValidateField_CompareRule_EndDateAfterStart_ReturnsValid()
    {
        var expr = """
        {
          "type":"binary","op":">=",
          "left":{"type":"identifier","name":"EndDate"},
          "right":{"type":"identifier","name":"StartDate"}
        }
        """;

        _ruleRepo.SetFieldRules(1, "EndDate", 1, new List<RuleMetadata>
        {
            Rule(1, "EndDate", "Compare", expressionJson: expr,
                errorMessage: "Ngày kết thúc phải sau ngày bắt đầu")
        });

        var ctx = new EvaluationContext(new Dictionary<string, object?>
        {
            ["StartDate"] = "2025-01-01",
            ["EndDate"]   = "2025-06-01"   // sau StartDate → pass
        });

        var result = await _engine.ValidateFieldAsync(1, "EndDate", "2025-06-01", ctx, 1);

        Assert.True(result.IsValid);
    }

    // ── ValidateFormAsync ───────────────────────────────────────

    [Fact]
    public async Task ValidateForm_NoRules_ReturnsEmpty()
    {
        var result = await _engine.ValidateFormAsync(
            1, EvaluationContext.Empty, 1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateForm_MultipleFields_ValidatesAll()
    {
        _ruleRepo.SetFormRules(1, 1, new Dictionary<string, IReadOnlyList<RuleMetadata>>
        {
            ["Name"] = new List<RuleMetadata>
            {
                Rule(1, "Name", "Required", errorMessage: "Tên bắt buộc")
            },
            ["Age"] = new List<RuleMetadata>
            {
                Rule(2, "Age", "Custom",
                    expressionJson: """
                    {
                      "type":"binary","op":">=",
                      "left":{"type":"identifier","name":"Age"},
                      "right":{"type":"literal","value":0}
                    }
                    """,
                    errorMessage: "Tuổi phải >= 0")
            }
        });

        var ctx = new EvaluationContext(new Dictionary<string, object?>
        {
            ["Name"] = null,   // fail Required
            ["Age"] = 25       // pass >= 0
        });

        var result = await _engine.ValidateFormAsync(1, ctx, 1);

        // Chỉ field có lỗi được đưa vào result — Age pass nên không có trong dictionary
        Assert.Single(result);
        Assert.False(result["Name"].IsValid);
        Assert.False(result.ContainsKey("Age")); // Age pass >= 0, không fail
    }

    // ── Topological Sort ────────────────────────────────────────

    [Fact]
    public void TopologicalSort_NoDependencies_KeepsOriginalOrder()
    {
        var fields = new[] { "A", "B", "C" };
        var sorted = ValidationEngine.TopologicalSort(fields, Array.Empty<FieldDependency>());

        Assert.Equal(3, sorted.Count);
    }

    [Fact]
    public void TopologicalSort_WithDependencies_SortsCorrectly()
    {
        // B phụ thuộc A → A phải đi trước B
        var fields = new[] { "B", "A", "C" };
        var deps = new List<FieldDependency>
        {
            new("A", "B") // A → B (B depends on A)
        };

        var sorted = ValidationEngine.TopologicalSort(fields, deps);

        var idxA = sorted.ToList().FindIndex(f => f.Equals("A", StringComparison.OrdinalIgnoreCase));
        var idxB = sorted.ToList().FindIndex(f => f.Equals("B", StringComparison.OrdinalIgnoreCase));
        Assert.True(idxA < idxB, "A phải đi trước B");
    }

    [Fact]
    public void TopologicalSort_Chain_SortsCorrectly()
    {
        // C → B → A (C phụ thuộc B, B phụ thuộc A)
        var fields = new[] { "C", "B", "A" };
        var deps = new List<FieldDependency>
        {
            new("A", "B"),
            new("B", "C")
        };

        var sorted = ValidationEngine.TopologicalSort(fields, deps);
        var list = sorted.ToList();

        Assert.True(list.IndexOf("A") < list.IndexOf("B"));
        Assert.True(list.IndexOf("B") < list.IndexOf("C"));
    }

    [Fact]
    public void TopologicalSort_Cycle_ReturnsAllFields()
    {
        // A → B → A (cycle) — should return all fields (best-effort)
        var fields = new[] { "A", "B" };
        var deps = new List<FieldDependency>
        {
            new("A", "B"),
            new("B", "A")
        };

        var sorted = ValidationEngine.TopologicalSort(fields, deps);

        // Best-effort: vẫn trả đủ fields
        Assert.Equal(2, sorted.Count);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static RuleMetadata Rule(
        int ruleId, string fieldCode, string ruleType,
        string severity = "error",
        string? expressionJson = null,
        string? conditionExpr = null,
        string errorMessage = "")
    {
        return new RuleMetadata
        {
            RuleId = ruleId,
            FormId = 1,
            FieldCode = fieldCode,
            TenantId = 1,
            RuleType = ruleType,
            Severity = severity,
            ExpressionJson = expressionJson ?? string.Empty,
            ConditionExpr = conditionExpr,
            ErrorMessage = errorMessage,
            SortOrder = ruleId // Sử dụng ruleId làm thứ tự mặc định
        };
    }
}

// ── Stub repositories cho unit test ─────────────────────────────

internal sealed class StubRuleRepository : IRuleRepository
{
    private readonly Dictionary<string, IReadOnlyList<RuleMetadata>> _fieldRules = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, IReadOnlyDictionary<string, IReadOnlyList<RuleMetadata>>> _formRules = new();

    public void SetFieldRules(int formId, string fieldCode, int tenantId, IReadOnlyList<RuleMetadata> rules)
    {
        _fieldRules[$"{formId}:{fieldCode}:{tenantId}"] = rules;
    }

    public void SetFormRules(int formId, int tenantId, IReadOnlyDictionary<string, IReadOnlyList<RuleMetadata>> rules)
    {
        _formRules[formId * 10000 + tenantId] = rules;
    }

    public Task<IReadOnlyList<RuleMetadata>> GetByFieldAsync(
        int formId, string fieldCode, int tenantId, CancellationToken ct = default)
    {
        var key = $"{formId}:{fieldCode}:{tenantId}";
        var result = _fieldRules.TryGetValue(key, out var rules)
            ? rules
            : (IReadOnlyList<RuleMetadata>)Array.Empty<RuleMetadata>();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyDictionary<string, IReadOnlyList<RuleMetadata>>> GetByFormAsync(
        int formId, int tenantId, CancellationToken ct = default)
    {
        var key = formId * 10000 + tenantId;
        var result = _formRules.TryGetValue(key, out var rules)
            ? rules
            : (IReadOnlyDictionary<string, IReadOnlyList<RuleMetadata>>)
              new Dictionary<string, IReadOnlyList<RuleMetadata>>();
        return Task.FromResult(result);
    }
}

internal sealed class StubFieldRepository : IFieldRepository
{
    private readonly Dictionary<int, List<FieldMetadata>> _fields = new();

    public void SetFields(int formId, params FieldMetadata[] fields)
    {
        _fields[formId] = [.. fields];
    }

    public Task<IReadOnlyList<FieldMetadata>> GetByFormIdAsync(
        int formId, int tenantId, string langCode = "vi", CancellationToken ct = default)
    {
        var result = _fields.TryGetValue(formId, out var list)
            ? (IReadOnlyList<FieldMetadata>)list
            : Array.Empty<FieldMetadata>();
        return Task.FromResult(result);
    }

    public Task<FieldMetadata?> GetByIdAsync(
        int fieldId, int tenantId, CancellationToken ct = default)
        => Task.FromResult<FieldMetadata?>(null);

    public Task<IReadOnlyList<FieldMetadata>> GetBySectionIdAsync(
        int sectionId, int tenantId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<FieldMetadata>>(Array.Empty<FieldMetadata>());
}

internal sealed class StubDependencyRepository : IDependencyRepository
{
    private readonly List<FieldDependency> _deps = new();

    public void SetDependencies(params FieldDependency[] deps)
    {
        _deps.Clear();
        _deps.AddRange(deps);
    }

    public Task<IReadOnlyList<FieldDependency>> GetByFormAsync(
        int formId, int tenantId, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<FieldDependency>>(_deps);
    }
}
